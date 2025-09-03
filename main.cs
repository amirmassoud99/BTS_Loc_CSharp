using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        public const string SW_VERSION = "1.0.0.9";

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

            /**********WCDMA Batch */
            /*
            string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\Drive test WCDMA MBS_20230111_144045\";
            List<string> inputFilenames = new List<string>
            {
                //"Gflex Device 019999095_UMTS WCDMA_UB I  2100 (IMT-2000) DL_Blind Scan.csv",
                "Gflex Device 019999095_UMTS WCDMA_UB III  1800 (DCS) DL_Blind Scan.csv",
                "Gflex Device 019999095_UMTS WCDMA_UB VII  2600 (IMT Extension) DL_Blind Scan.csv",
                "Gflex Device 019999095_UMTS WCDMA_UB VIII  900 DL_Blind Scan.csv"
            };
            */

            //Batch processing Drive 1 LTE NR
            /*
            string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\7.0.2.4\20250813_Drive2-SIB1-onetime\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 019999090_NR_FR1 FDD n5 DL_Blind Scan SCS Autodetect.csv",
                "Gflex Device 019999090_LTE_EB 25  1990 (Ext US PCS) DL_Blind Scan.csv",
                "Gflex Device 019999090_LTE_EB 14  Upper 700-D Block DL_Blind Scan.csv",
                "Gflex Device 019999090_LTE_EB 26  Upper Ext 850 DL_Blind Scan.csv",
                "Gflex Device 019999090_LTE_EB 29  US 700 DL_Blind Scan.csv",
                "Gflex Device 019999090_LTE_EB 30  2.3 GHz (WCS A B) DL_Blind Scan.csv",
                "Gflex Device 019999090_LTE_EB 66  AWS-3 DL_Blind Scan.csv",
                "Gflex Device 019999090_NR_FR1 FDD n25 DL_Blind Scan SCS Autodetect.csv",
                "Gflex Device 019999090_NR_FR1 TDD n77_Blind Scan SCS Autodetect.csv",
                "Gflex Device 019999090_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.csv",
                "Gflex Device 019999090_NR_FR1 FDD n71 DL_Blind Scan SCS Autodetect.csv"
            };
            */

            //Batch processing LTE NR Drive 2
            /*
            string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\20250825_Sib1-cont\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 032201005_LTE_EB 12  US Lower 700-A B C Blocks DL_Blind Scan.csv",
                "Gflex Device 032201005_LTE_EB 66  AWS-3 DL_Blind Scan.csv",
                "Gflex Device 032201005_LTE_EB 71 DL_Blind Scan.csv",
                "Gflex Device 032201005_TD-LTE_EB 41  TDD 2.5 GHz Lower_Blind Scan.csv",
                "Gflex Device 032201005_NR_FR1 FDD n71 DL_Blind Scan SCS Autodetect.csv",
                "Gflex Device 032201005_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.csv"
            };
            */

            //Batch processing Drive 3_Washington DC
            /*
            string fileDirectory = @"C:\Users\amirsoltanian.PCTELUS\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 032201005_LTE_EB 02  1900 (PCS) DL_Blind Scan.csv",
                "Gflex Device 032201005_LTE_EB 12  US Lower 700-A B C Blocks DL_Blind Scan.csv",
                "Gflex Device 032201005_LTE_EB 66  AWS-3 DL_Blind Scan.csv",
                "Gflex Device 032201005_LTE_EB 71 DL_Blind Scan.csv",
                "Gflex Device 032201005_NR_FR1 FDD n25 DL_Blind Scan SCS Autodetect.csv",
                "Gflex Device 032201005_NR_FR1 FDD n71 DL_Blind Scan SCS Autodetect.csv",
                "Gflex Device 032201005_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.csv",
                "Gflex Device 032201005_TD-LTE_EB 41  TDD 2.5 GHz_Blind Scan.csv"
            };
            */

            //Batch processing Drive 4_Rockville Pike
            /*
            string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\Drive_Rockville_Pike\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 032201020_LTE_EB 02  1900 (PCS) DL_Blind Scan.csv",
                "Gflex Device 032201020_LTE_EB 12  US Lower 700-A B C Blocks DL_Blind Scan.csv",
                "Gflex Device 032201020_LTE_EB 14  Upper 700-D Block DL_Blind Scan.csv",
                "Gflex Device 032201020_LTE_EB 30  2.3 GHz (WCS A B) DL_Blind Scan.csv",
                "Gflex Device 032201020_LTE_EB 66  AWS-3 DL_Blind Scan.csv",
                "Gflex Device 032201020_NR_FR1 FDD n71 DL_Blind Scan SCS Autodetect.csv",
                "Gflex Device 032201020_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.csv"
            };
            */

            //Batch processing Drive 5_Gaithersuburg
            string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\20250828_Gaitherburg-Drive\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 032201005_LTE_EB 02  1900 (PCS) DL_Blind Scan.csv",
                "Gflex Device 032201005_LTE_EB 12  US Lower 700-A B C Blocks DL_Blind Scan.csv",
                "Gflex Device 032201005_LTE_EB 66  AWS-3 DL_Blind Scan.csv",
                "Gflex Device 032201005_NR_FR1 FDD n71 DL_Blind Scan SCS Autodetect.csv",
                "Gflex Device 032201005_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.csv"
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
                //save_extrac_step1(allData, step1Filename);

                var filteredData = InputOutputFileProc.filter_cinr_minimum_PCI(allData, CINR_THRESH, MINIMUM_CELL_ID_COUNT);
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
                            { "Num_points", timeAdjustedPoints.Count.ToString() }
                        };
                        estimationResults.Add(resultDict);
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
