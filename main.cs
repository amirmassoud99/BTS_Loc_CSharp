using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static BTS_Location_Estimation.InputOutputFileProc;

namespace BTS_Location_Estimation
{
    // Data structure to hold row data (similar to C++ Row)
    public class Row
    {
        public Dictionary<string, string> Data { get; set; }
        public int RowNumber { get; set; }
        public Row(Dictionary<string, string> data, int rowNumber)
        {
            Data = data;
            RowNumber = rowNumber;
        }
    }

    public static class MainModule
    {
        // --- Software Version ---
        public const string SW_VERSION = "1.0.0.3";

        // --- Constants ---
        public const double METERS_PER_DEGREE = 111139.0;
        public const double LTE_SAMPLING_RATE_HZ = 30.72e6;
        public const double TIME_OFFSET_WRAP_VALUE = 307200.0;
        public const double NR_SAMPLING_RATE_MULTIPLIER = 4.0;
        public const double WCDMA_SAMPLING_RATE_DIVISOR = 4.0;
        public const double WCDMA_TIME_OFFSET_WRAP_VALUE = 38400.0;
        public const int MINIMUM_POINTS_FOR_TSWLS = 4;
        public const double SPEED_OF_LIGHT = 3e8; // Speed of light in m/s
        public const double CINR_THRESH = 0.0;
        public const double EC_IO_THRESHOLD = -18.0;
        public const double DISTANCE_THRESH = 100.0;
        public const double SEARCH_DIRECTION = 600.0;
        public const int MAX_POINTS = 60;
        public const int MINIMUM_CELL_ID_COUNT = 20;

        // ************************************************************************************
        //
        //                            BTS Location Estimation
        //
        // This program processes cellular drive test data to estimate the location of
        // Base Transceiver Stations (BTS). It reads CSV files containing signal measurements,
        // filters the data based on signal quality (CINR) and other parameters, and
        // prepares the data for location estimation algorithms like TSWLS.
        // The main workflow consists of identifying channels, cells, and beams,
        // extracting relevant data points, and saving intermediate results.
        //
        // ************************************************************************************
        public static void Main(string[] args)
        {
            Console.WriteLine($"BTS Location Estimation version {SW_VERSION}");
            // --- Target and File Configuration ---
            // Update the path and filenames as needed
            //string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\7.0.2.4\20250813_Drive2-SIB1-onetime\\";
           string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\MatlabRef\\";
           
            List<string> inputFilenames = new List<string>
            {
                
                "Gflex Device 032201005_TD-LTE_EB 41  TDD 2.5 GHz_Enhanced Top N Signal Auto Bandwidth Channel 39750 - 2506.000000 MHz.csv"
                // ...other files...
            };

            foreach (var inputFilename in inputFilenames)
            {
                Console.WriteLine($"\n=== Processing file: {inputFilename} ===");
                string filename = Path.Combine(fileDirectory, inputFilename);
                int fileType = InputOutputFileProc.GetFileType(filename);

                // 1. Call ExtractChannelCellMap to get all standardized data rows
                var allData = InputOutputFileProc.ExtractChannelCellMap(filename, fileType);

                // The 'allData' variable now holds a list of all the relevant rows
                // from the CSV, with standardized headers. You can now perform
                // grouping and processing directly on this list in memory.
                Console.WriteLine($"Extracted {allData.Count} rows from {inputFilename}");

                string filenameOnly = Path.GetFileNameWithoutExtension(inputFilename);
                string step1Filename = $"step1_{filenameOnly}.csv";
                save_extrac_step1(allData, step1Filename);

                var filteredData = InputOutputFileProc.filter_cinr_minimum_PCI(allData, CINR_THRESH, MINIMUM_CELL_ID_COUNT);
                string step2Filename = $"step2_{filenameOnly}.csv";
                save_extract_step2(filteredData, step2Filename);

                // Group data by channel and cell to process each one individually
                var groupedData = filteredData.GroupBy(row => new {
                    Channel = row.GetValueOrDefault("channel", "N/A"),
                    CellId = row.GetValueOrDefault("cellId", "N/A")
                });

                var estimationResults = new List<Dictionary<string, string>>();

                foreach (var group in groupedData)
                {
                    var pointsForCell = group.ToList();
                    var (finalPoints, maxCinr) = InputOutputFileProc.ExtractPointsWithDistance(pointsForCell, DISTANCE_THRESH, MAX_POINTS, METERS_PER_DEGREE);

                    // Adjust time offset values for the filtered points
                    var timeAdjustedPoints = InputOutputFileProc.ProcessTimeOffset(finalPoints, fileType, TIME_OFFSET_WRAP_VALUE, WCDMA_TIME_OFFSET_WRAP_VALUE, LTE_SAMPLING_RATE_HZ, NR_SAMPLING_RATE_MULTIPLIER, WCDMA_SAMPLING_RATE_DIVISOR);

                    // You can now save or process the 'finalPoints' and 'maxCinr' for each cell
                    // For example, save to a new CSV file for step 3
                    string step3Filename = $"step3_{filenameOnly}_ch{group.Key.Channel}_cell{group.Key.CellId}.csv";
                    save_extract_step3(timeAdjustedPoints, step3Filename, maxCinr);

                    // Run the TSWLS algorithm
                    var tswlsResult = TSWLS.run_tswls(timeAdjustedPoints, MINIMUM_POINTS_FOR_TSWLS, SPEED_OF_LIGHT, METERS_PER_DEGREE, SEARCH_DIRECTION, DISTANCE_THRESH);

                    if (tswlsResult != null)
                    {
                        double xhat1 = tswlsResult[0];
                        double yhat1 = tswlsResult[1];
                        double xhat2 = tswlsResult[2];
                        double yhat2 = tswlsResult[3];

                        double latRef = double.Parse(timeAdjustedPoints[0]["latitude"], CultureInfo.InvariantCulture);
                        double lonRef = double.Parse(timeAdjustedPoints[0]["longitude"], CultureInfo.InvariantCulture);

                        var (est_Lat1, est_Lon1) = TSWLS.xy2LatLon(xhat1, yhat1, latRef, lonRef, METERS_PER_DEGREE);
                        var (est_Lat2, est_Lon2) = TSWLS.xy2LatLon(xhat2, yhat2, latRef, lonRef, METERS_PER_DEGREE);

                        Console.WriteLine($"Estimated Final Location for Cell {group.Key.CellId} (Lat, Lon): ({est_Lat2:F6}, {est_Lon2:F6})");

                        var resultDict = new Dictionary<string, string>
                        {
                            { "Channel", group.Key.Channel },
                            { "CellId", group.Key.CellId },
                            { "est_Lat1", est_Lat1.ToString("F6", CultureInfo.InvariantCulture) },
                            { "est_Lon1", est_Lon1.ToString("F6", CultureInfo.InvariantCulture) },
                            { "est_Lat2", est_Lat2.ToString("F6", CultureInfo.InvariantCulture) },
                            { "est_Lon2", est_Lon2.ToString("F6", CultureInfo.InvariantCulture) }
                        };
                        estimationResults.Add(resultDict);
                    }
                }

                // Save the final estimation results
                string estimateFilename = $"Estimate_{filenameOnly}.csv";
                save_estimation_results(estimationResults, estimateFilename);
            }
            Console.WriteLine("Batch processing complete.");
        }

        private static void save_estimation_results(List<Dictionary<string, string>> estimationResults, string outputFilename)
        {
            using (var writer = new StreamWriter(outputFilename))
            {
                if (estimationResults.Any())
                {
                    var headers = estimationResults.First().Keys;
                    writer.WriteLine(string.Join(",", headers));

                    foreach (var result in estimationResults)
                    {
                        writer.WriteLine(string.Join(",", result.Values));
                    }
                }
            }
            Console.WriteLine($"\nFinal estimation results saved to {outputFilename}");
        }

        private static void save_extract_step3(List<Dictionary<string, string>> finalPoints, string outputFilename, double maxCinr)
        {
            // This function saves the final, distance-filtered points for a single cell
            // to a CSV file. It includes all the original data for the selected points
            // and can be used as input for the final location estimation algorithms.
            // The maximum CINR value is also available if needed for reporting.
            using (var writer = new StreamWriter(outputFilename))
            {
                if (finalPoints.Any())
                {
                    // Write header from the keys of the first point
                    var headers = finalPoints.First().Keys;
                    writer.WriteLine(string.Join(",", headers));

                    // Write data rows
                    foreach (var point in finalPoints)
                    {
                        writer.WriteLine(string.Join(",", point.Values));
                    }
                }
            }
            Console.WriteLine($"Step 3 data for cell saved to {outputFilename} (Max CINR: {maxCinr:F2})");
        }

        private static void save_extract_step2(List<Dictionary<string, string>> filteredData, string outputFilename)
        {
            // Group data by channel and cell ID, count occurrences, and sort
            var step2Data = filteredData
                .GroupBy(row => new
                {
                    Channel = row.GetValueOrDefault("channel", "N/A"),
                    CellId = row.GetValueOrDefault("cellId", "N/A")
                })
                .Select(group => new
                {
                    group.Key.Channel,
                    group.Key.CellId,
                    Count = group.Count()
                })
                .OrderBy(item => item.Channel)
                .ThenBy(item => item.CellId)
                .ToList();

            using (var writer = new StreamWriter(outputFilename))
            {
                writer.WriteLine("Channel,CellID,Count");
                foreach (var item in step2Data)
                {
                    writer.WriteLine($"{item.Channel},{item.CellId},{item.Count}");
                }
            }
            Console.WriteLine($"Step 2 data saved to {outputFilename}");
        }

        private static void save_extrac_step1(List<Dictionary<string, string>> allData, string outputFilename)
        {
            // Group data by channel, cell ID, and beam index and count occurrences
            var processedData = allData
                .GroupBy(row => new
                {
                    Channel = row.ContainsKey("channel") ? row["channel"] : "N/A",
                    CellId = row.ContainsKey("cellId") ? row["cellId"] : "N/A",
                    BeamIndex = row.ContainsKey("beamIndex") ? row["beamIndex"] : "N/A"
                })
                .Select(group => new
                {
                    group.Key.Channel,
                    group.Key.CellId,
                    group.Key.BeamIndex,
                    Count = group.Count()
                })
                .OrderBy(item => item.Channel)
                .ThenBy(item => item.CellId)
                .ThenBy(item => item.BeamIndex)
                .ToList();

            using (var writer = new StreamWriter(outputFilename))
            {
                // Write header
                writer.WriteLine("Channel,CellID,BeamIndex,Count");

                // Write data
                foreach (var item in processedData)
                {
                    writer.WriteLine($"{item.Channel},{item.CellId},{item.BeamIndex},{item.Count}");
                }
            }
            Console.WriteLine($"Step 1 data saved to {outputFilename}");
        }
    }
}
