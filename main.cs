using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using static BTS_Location_Estimation.InputOutputFileProc;
using static BTS_Location_Estimation.SaveHelper;

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
        public const string SW_VERSION = "1.0.2.0";

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
            // --- Get Target and File Configuration ---
            var (fileDirectory, inputFilenames) = InputOutputFileProc.GetFileConfigurations();

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

                double cinrThreshold = (fileType == WCDMA_FILE_TYPE) ? EC_IO_THRESHOLD : CINR_THRESH;
                var filteredData = InputOutputFileProc.filter_cinr_minimum_PCI(allData, cinrThreshold, MINIMUM_CELL_ID_COUNT);
                string step2Filename = $"step2_{filenameOnly}.csv";
                //save_extract_step2(filteredData, step2Filename);

                // Group data by channel and cell to process each one individually
                var groupedData = filteredData.GroupBy(row => new
                {
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
                    //save_extract_step3(timeAdjustedPoints, step3Filename, maxCinr);

                    // Run the TSWLS algorithm
                    var tswlsResult = TSWLS.run_tswls(timeAdjustedPoints, MINIMUM_POINTS_FOR_TSWLS, SPEED_OF_LIGHT, METERS_PER_DEGREE, SEARCH_DIRECTION, DISTANCE_THRESH);

                    if (tswlsResult != null)
                    {
                        ProcessTswlsResult(tswlsResult, timeAdjustedPoints, group, maxCinr, estimationResults);
                    }
                }

                // Save the final estimation results
                string estimateFilename = $"Estimate_{filenameOnly}.csv";

                var resultsWithBeamIndex = splitCellidBeamforNR(fileType, estimationResults);

                var sortedResults = resultsWithBeamIndex
                    .OrderBy(d => int.TryParse(d["Channel"], out int ch) ? ch : int.MaxValue)
                    .ThenBy(d => int.TryParse(d["CellId"], out int id) ? id : int.MaxValue)
                    .ToList();
                save_estimation_results(sortedResults, estimateFilename);
            }
            Console.WriteLine("Batch processing complete.");
        }

        // Processes the successful result from the TSWLS algorithm for a single cell.
        // Inputs:
        //  - tswlsResult: Vector with estimated x,y coordinates.
        //  - timeAdjustedPoints: Data points used for the estimation.
        //  - group: Grouping metadata (Channel, CellId).
        //  - maxCinr: Maximum CINR for the cell.
        //  - estimationResults: The list to which the final result is added.
        // Process: Converts x,y coordinates to latitude/longitude and formats the output.
        // Output: Adds a new dictionary containing the full estimation result for one
        //         cell to the 'estimationResults' list.
        private static void ProcessTswlsResult(Vector<double> tswlsResult, List<Dictionary<string, string>> timeAdjustedPoints, IGrouping<dynamic, Dictionary<string, string>> group, double maxCinr, List<Dictionary<string, string>> estimationResults)
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



            // Extract and combine unique cellIdentity values for the group
            var cellIdentities = group
                .Where(p => p.ContainsKey("cellIdentity") && !string.IsNullOrWhiteSpace(p["cellIdentity"]))
                .Select(p => p["cellIdentity"])
                .Distinct()
                .ToList();
            string combinedCellIdentity = string.Join("-", cellIdentities);

            string confidence = "High";
            if (timeAdjustedPoints.Count < 8 && maxCinr < 12)
            {
                confidence = "Low";
            }

            var resultDict = new Dictionary<string, string>
            {
                { "Channel", group.Key.Channel },
                { "CellId", group.Key.CellId },
                { "cellIdentity", combinedCellIdentity },
                { "xhat1", xhat1.ToString("F4", CultureInfo.InvariantCulture) },
                { "yhat1", yhat1.ToString("F4", CultureInfo.InvariantCulture) },
                { "xhat2", xhat2.ToString("F4", CultureInfo.InvariantCulture) },
                { "yhat2", yhat2.ToString("F4", CultureInfo.InvariantCulture) },
                { "est_Lat1", est_Lat1.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lon1", est_Lon1.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lat2", est_Lat2.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lon2", est_Lon2.ToString("F6", CultureInfo.InvariantCulture) },
                { "Max_cinr", maxCinr.ToString("F2", CultureInfo.InvariantCulture) },
                { "Num_points", timeAdjustedPoints.Count.ToString() },
                { "Confidence", confidence }
            };
            estimationResults.Add(resultDict);
        }

        private static List<Dictionary<string, string>> splitCellidBeamforNR(int fileType, List<Dictionary<string, string>> estimationResults)
        {
            if (fileType != 4)
            {
                return estimationResults;
            }

            var newEstimationResults = new List<Dictionary<string, string>>();
            foreach (var result in estimationResults)
            {
                if (int.TryParse(result["CellId"], out int compositeCellId))
                {
                    int newCellId = compositeCellId / 100;
                    int beamIndex = compositeCellId % 100;

                    var newResult = new Dictionary<string, string>();
                    foreach (var kvp in result)
                    {
                        if (kvp.Key == "CellId")
                        {
                            newResult.Add("CellId", newCellId.ToString());
                            newResult.Add("BeamIndex", beamIndex.ToString());
                        }
                        else
                        {
                            newResult.Add(kvp.Key, kvp.Value);
                        }
                    }
                    newEstimationResults.Add(newResult);
                }
                else
                {
                    // If parsing fails, add the original result back
                    newEstimationResults.Add(result);
                }
            }
            return newEstimationResults;
        }
    }
}
