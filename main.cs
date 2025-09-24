#define Python_included
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
#if Python_included
using Python.Runtime;
#endif

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
        public const string SW_VERSION = "1.2.8.0";

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

        // DBSCAN clustering parameter
        //This parameter defines the maximum distance between two samples for one to be considered as in 
        // the neighborhood of the other.
        //This parameter is used in the function SaveHelper.ClusterProcessing. It is a c# function.
        //A more advanded clustering algorithm is implemented in Python using HDBSCAN.
        public const double EPS_MILES = 0.5;

        /***************************************************************************************************
        *
        *   Function:       Main
        *
        *   Description:    The main entry point for the BTS Location Estimation program. It orchestrates
        *                   the entire workflow, from reading input files to processing data for each
        *                   cellular technology, running the TSWLS estimation, and saving the final results.
        *                   It relies on helper functions from InputOutputFileProc, DataBaseProc, and SaveHelper.
        *
        *   Input:          args (string[]) - Command-line arguments (currently unused).
        *
        *   Output:         None (void). The function writes progress to the console and saves
        *                   results to CSV files in the same directory as the input files.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 5, 2025
        *
        ***************************************************************************************************/
        public static void Main(string[] args)
        {
            Console.WriteLine($"BTS Location Estimation version {SW_VERSION}");

            SaveHelper.DeleteOutputFiles(Directory.GetCurrentDirectory());

            // --- Get Target and File Configuration ---
            var (fileDirectory, inputFilenames) = InputOutputFileProc.GetFileConfigurations();

            foreach (var inputFilename in inputFilenames)
            {
                Console.WriteLine($"\n=== Processing file: {inputFilename} ===");

                string filename = Path.Combine(fileDirectory, inputFilename);
                string filenameOnly = Path.GetFileNameWithoutExtension(inputFilename);

                int fileType = InputOutputFileProc.GetFileType(filename);

                // 1. Call ExtractChannelCellMap to get all standardized data rows
                var allData = InputOutputFileProc.ExtractChannelCellMap(filename, fileType);
                //string step0Filename = $"step0_{filenameOnly}.csv";
                //SaveHelper.save_extrac_step1(allData, step0Filename);
                //SaveHelper.debug_csv(allData);
                //2. Expand mcc, mnc, cellIdentity and generate unique cellID
                allData = DataBaseProc.Expand_mcc_mnc_cellIdentity(allData);
                //SaveHelper.debug_csv(allData);
                allData = DataBaseProc.generate_unique_cellID(allData, fileType);
                //SaveHelper.debug_csv(allData);
                Console.WriteLine($"Extracted {allData.Count} rows from {inputFilename}");
                InputOutputFileProc.Save_Drive_Route(allData, inputFilename);

                //string step1Filename = $"step1_{filenameOnly}.csv";
                //SaveHelper.save_extrac_step1(allData, step1Filename);

                bool isWcdma = fileType == DataBaseProc.WCDMA_FILE_TYPE_CSV || fileType == DataBaseProc.WCDMA_FILE_TYPE_DTR;
                double cinrThreshold = isWcdma ? EC_IO_THRESHOLD : CINR_THRESH;
                // 3. Filter data based on CINR/ECIO and minimum cell ID count
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
                    //extract points with minimum distance
                    var pointsForCell = group.ToList();
                    var (finalPoints, maxCinr) = DataBaseProc.ExtractPointsWithDistance(pointsForCell, DISTANCE_THRESH, MAX_POINTS, METERS_PER_DEGREE);
                    //SaveHelper.map_cellid(finalPoints, "658080", "17104", "blue");


                    // Adjust time offset values for the filtered points
                    var timeAdjustedPoints = DataBaseProc.ProcessTimeOffset(finalPoints, fileType, TIME_OFFSET_WRAP_VALUE, WCDMA_TIME_OFFSET_WRAP_VALUE, LTE_SAMPLING_RATE_HZ, NR_SAMPLING_RATE_MULTIPLIER, WCDMA_SAMPLING_RATE_DIVISOR);



                    // You can now save or process the 'finalPoints' and 'maxCinr' for each cell
                    // For example, save to a new CSV file for step 3
                    //string step3Filename = $"step3_{filenameOnly}_ch{group.Key.Channel}_cell{group.Key.CellId}.csv";
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

                var resultsWithBeamIndex = DataBaseProc.splitCellid(fileType, estimationResults);

                //The Tower Estimation uses the Cellidentity information to find sectors
                //Associated with the same tower. It then averages the location of the sectors
                //to get the tower location.
                var sortedResults = DataBaseProc.AddTowerEstimate(resultsWithBeamIndex, fileType, "Tower");
                SaveHelper.save_estimation_results(sortedResults, estimateFilename);

                // Call map_cellid for debugging
                //SaveHelper.map_cellid(allData, "658080", "17104");
                //SaveHelper.map_cellid(filteredData, "658080", "17104");

            }

            /** Python Integration for Advanced Clustering **/
            /*
            The below code extract all the sectors belong to the same carrier. It then uses
            an advanced clustering algorithm to group them by their geographical location. This
            could be a choice by the user to enable this feature. This feature requires Python installation*/

            Console.WriteLine("Batch processing complete.");
            // Example: Filter by mnc and save cluster results with filter in filename
            string filterType = "mnc";
            string filterValue = "12";
            //string filterType = null;
            //string filterValue = null;
            var outputFile = SaveHelper.ClusterProcessing(filterType, filterValue, EPS_MILES);
            if (outputFile != null)
            {
                SaveHelper.map_cluster(outputFile);
#if Python_included
                string pythonScriptDir = Directory.GetCurrentDirectory();
                string pythonScriptName = "cluster_hdbscan"; // without .py
                string mapCsvFile = Path.ChangeExtension(Path.GetFileName(outputFile).Replace("Estimate", "map"), ".csv");
                string inputCsv = Path.Combine(pythonScriptDir, mapCsvFile);
                string outputCsv = Path.Combine(pythonScriptDir, $"Python_cluster_{mapCsvFile}");

                string kmlFile = Path.Combine(pythonScriptDir, "Python_kml_map.kml");
                Python.Runtime.Runtime.PythonDLL = @"C:\Users\amirsoltanian\AppData\Local\Programs\Python\Python310\python310.dll";

                PythonEngine.Initialize();

                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.append(pythonScriptDir);
                    dynamic cluster_hdbscan = Py.Import(pythonScriptName);
                    cluster_hdbscan.run_hdbscan_clustering(inputCsv, outputCsv, kmlFile);
                }
                try
                {
                    Python.Runtime.PythonEngine.Shutdown();
                }
                catch (System.NotSupportedException ex)
                {
                    Console.WriteLine("Python.NET shutdown exception suppressed: " + ex.Message);
                }
#endif  
            }
        }
    }
}
