using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using static BTS_Location_Estimation.DataBaseProc;
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
        public const string SW_VERSION = "1.0.10.0";

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
        public const double EC_IO_THRESHOLD = -12.0;
        public const double DISTANCE_THRESH = 100.0;
        public const double SEARCH_DIRECTION = 600.0;
        public const int MAX_POINTS = 60;
        public const int MINIMUM_CELL_ID_COUNT = 20;

        // Confidence Thresholds
        public const int CONFIDENCE_MIN_POINTS_LTE_NR = 8;
        public const double CONFIDENCE_MIN_CINR_LTE_NR = 12.0;
        public const int CONFIDENCE_MIN_POINTS_WCDMA = 8;
        public const double CONFIDENCE_MIN_ECIO_WCDMA = -10.0;

        /***************************************************************************************************
        *
        *   Function:       Main
        *
        *   Description:    The main entry point for the BTS Location Estimation program. It orchestrates
        *                   the entire workflow, from reading input files to processing data for each
        *                   cellular technology, running the TSWLS estimation, and saving the final results.
        *
        *   Input:          args (string[]) - Command-line arguments (currently unused).
        *
        *   Output:         None (void). The function writes progress to the console and saves
        *                   results to CSV files in the same directory as the input files.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
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

                InputOutputFileProc.Save_Drive_Route(allData, inputFilename);

                string filenameOnly = Path.GetFileNameWithoutExtension(inputFilename);
                string step1Filename = $"step1_{filenameOnly}.csv";
                //save_extrac_step1(allData, step1Filename);

                bool isWcdma = fileType == WCDMA_FILE_TYPE_CSV || fileType == WCDMA_FILE_TYPE_DTR;
                double cinrThreshold = isWcdma ? EC_IO_THRESHOLD : CINR_THRESH;
                var filteredData = filter_cinr_minimum_PCI(allData, cinrThreshold, MINIMUM_CELL_ID_COUNT);
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
                    var (finalPoints, maxCinr) = ExtractPointsWithDistance(pointsForCell, DISTANCE_THRESH, MAX_POINTS, METERS_PER_DEGREE);



                    // Adjust time offset values for the filtered points
                    var timeAdjustedPoints = ProcessTimeOffset(finalPoints, fileType, TIME_OFFSET_WRAP_VALUE, WCDMA_TIME_OFFSET_WRAP_VALUE, LTE_SAMPLING_RATE_HZ, NR_SAMPLING_RATE_MULTIPLIER, WCDMA_SAMPLING_RATE_DIVISOR);



                    // You can now save or process the 'finalPoints' and 'maxCinr' for each cell
                    // For example, save to a new CSV file for step 3
                    string step3Filename = $"step3_{filenameOnly}_ch{group.Key.Channel}_cell{group.Key.CellId}.csv";
                    //save_extract_step3(timeAdjustedPoints, step3Filename, maxCinr);

                    // Run the TSWLS algorithm
                    var tswlsResult = TSWLS.run_tswls(timeAdjustedPoints, MINIMUM_POINTS_FOR_TSWLS, SPEED_OF_LIGHT, METERS_PER_DEGREE, SEARCH_DIRECTION, DISTANCE_THRESH);

                    if (tswlsResult != null)
                    {
                        ProcessTswlsResult(tswlsResult, timeAdjustedPoints, group, maxCinr, estimationResults, fileType);
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

        /***************************************************************************************************
        *
        *   Function:       ProcessTswlsResult
        *
        *   Description:    Processes the successful result from the TSWLS algorithm for a single cell.
        *                   It converts the estimated x,y coordinates to latitude/longitude,
        *                   calculates a confidence level, and formats the output for saving.
        *
        *   Input:          tswlsResult (Vector<double>) - Vector with estimated x,y coordinates.
        *                   timeAdjustedPoints (List<...>) - Data points used for the estimation.
        *                   group (IGrouping<...>) - Grouping metadata (Channel, CellId).
        *                   maxCinr (double) - Maximum CINR for the cell.
        *                   estimationResults (List<...>) - The list to which the final result is added.
        *
        *   Output:         None (void). Modifies the 'estimationResults' list by adding a new
        *                   dictionary containing the full estimation result for the cell.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        private static void ProcessTswlsResult(Vector<double> tswlsResult, List<Dictionary<string, string>> timeAdjustedPoints, IGrouping<dynamic, Dictionary<string, string>> group, double maxCinr, List<Dictionary<string, string>> estimationResults, int fileType)
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
            bool isWcdma = fileType == WCDMA_FILE_TYPE_CSV || fileType == WCDMA_FILE_TYPE_DTR;
            if (isWcdma)
            {
                if (timeAdjustedPoints.Count < CONFIDENCE_MIN_POINTS_WCDMA && maxCinr < CONFIDENCE_MIN_ECIO_WCDMA)
                {
                    confidence = "Low";
                }
            }
            else // LTE and NR
            {
                if (timeAdjustedPoints.Count < CONFIDENCE_MIN_POINTS_LTE_NR && maxCinr < CONFIDENCE_MIN_CINR_LTE_NR)
                {
                    confidence = "Low";
                }
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

        /***************************************************************************************************
        *
        *   Function:       splitCellidBeamforNR
        *
        *   Description:    For NR Blind Scan files (fileType = 4), this function takes the composite
        *                   Cell ID (which includes the Beam Index) and splits it back into separate
        *                   'CellId' and 'BeamIndex' fields in the final results.
        *
        *   Input:          fileType (int) - The integer code for the file type.
        *                   estimationResults (List<...>) - The list of estimation results.
        *
        *   Output:         A new list of dictionaries with 'CellId' and 'BeamIndex' separated.
        *                   If the fileType is not 4, it returns the original list unmodified.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        private static List<Dictionary<string, string>> splitCellidBeamforNR(int fileType, List<Dictionary<string, string>> estimationResults)
        {
            bool isNrFile = fileType == NR_TOPN_FILE_TYPE || fileType == NR_FILE_TYPE || fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;
            if (!isNrFile)
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
