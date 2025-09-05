using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

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
        public const string SW_VERSION = "1.0.18.0";

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
                //SaveHelper.save_extrac_step1(allData, step1Filename);

                bool isWcdma = fileType == DataBaseProc.WCDMA_FILE_TYPE_CSV || fileType == DataBaseProc.WCDMA_FILE_TYPE_DTR;
                double cinrThreshold = isWcdma ? EC_IO_THRESHOLD : CINR_THRESH;
                var filteredData = DataBaseProc.filter_cinr_minimum_PCI(allData, cinrThreshold, MINIMUM_CELL_ID_COUNT);
                string step2Filename = $"step2_{filenameOnly}.csv";
                //SaveHelper.save_extract_step2(filteredData, step2Filename);

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
                    var (finalPoints, maxCinr) = DataBaseProc.ExtractPointsWithDistance(pointsForCell, DISTANCE_THRESH, MAX_POINTS, METERS_PER_DEGREE);
                    SaveHelper.map_cellid(finalPoints, "658080", "17104", "blue");


                    // Adjust time offset values for the filtered points
                    var timeAdjustedPoints = DataBaseProc.ProcessTimeOffset(finalPoints, fileType, TIME_OFFSET_WRAP_VALUE, WCDMA_TIME_OFFSET_WRAP_VALUE, LTE_SAMPLING_RATE_HZ, NR_SAMPLING_RATE_MULTIPLIER, WCDMA_SAMPLING_RATE_DIVISOR);



                    // You can now save or process the 'finalPoints' and 'maxCinr' for each cell
                    // For example, save to a new CSV file for step 3
                    string step3Filename = $"step3_{filenameOnly}_ch{group.Key.Channel}_cell{group.Key.CellId}.csv";
                    //SaveHelper.save_extract_step3(timeAdjustedPoints, step3Filename, maxCinr);


                    // Run the TSWLS algorithm
                    var tswlsResult = TSWLS.run_tswls(timeAdjustedPoints, MINIMUM_POINTS_FOR_TSWLS, SPEED_OF_LIGHT, METERS_PER_DEGREE, SEARCH_DIRECTION, DISTANCE_THRESH);

                    if (tswlsResult != null)
                    {
                        TSWLS.ProcessTswlsResult(tswlsResult, timeAdjustedPoints, group, maxCinr, estimationResults, fileType, METERS_PER_DEGREE, DataBaseProc.WCDMA_FILE_TYPE_CSV, DataBaseProc.WCDMA_FILE_TYPE_DTR, CONFIDENCE_MIN_POINTS_WCDMA, CONFIDENCE_MIN_ECIO_WCDMA, CONFIDENCE_MIN_POINTS_LTE_NR, CONFIDENCE_MIN_CINR_LTE_NR);
                    }
                }

                // Save the final estimation results
                string estimateFilename = $"Estimate_{filenameOnly}.csv";

                var resultsWithBeamIndex = DataBaseProc.splitCellidBeamforNR(fileType, estimationResults);

                var sortedResults = DataBaseProc.AddTowerEstimate(resultsWithBeamIndex, fileType);
                SaveHelper.save_estimation_results(sortedResults, estimateFilename);

                // Call map_cellid for debugging
                //SaveHelper.map_cellid(allData, "658080", "17104");
                //SaveHelper.map_cellid(filteredData, "658080", "17104");
                
            }
            Console.WriteLine("Batch processing complete.");
        }
    }
}
