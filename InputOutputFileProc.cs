using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BTS_Location_Estimation
{


    public static class InputOutputFileProc
    {
        public const int LTE_TOPN_FILE_TYPE = 1;
        public const int LTE_BLIND_FILE_TYPE = 2;
        public const int NR_TOPN_FILE_TYPE = 3;
        public const int NR_FILE_TYPE = 4;
        public const int WCDMA_FILE_TYPE_CSV = 5;
        public const int WCDMA_FILE_TYPE_DTR = 50;

        /***************************************************************************************************
        *
        *   Function:       GetFileConfigurations
        *
        *   Description:    Provides a centralized place to define the set of input files for processing.
        *                   This function is used to easily switch between different batches of drive test
        *                   data by commenting and uncommenting blocks of code.
        *
        *   Input:          None.
        *
        *   Output:         A tuple containing the directory path (string) and a list of filenames (List<string>).
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static (string fileDirectory, List<string> inputFilenames) GetFileConfigurations()
        {
            /*****Reference Matlab file*/    
            /*string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\MatlabRef\";
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 032201005_TD-LTE_EB 41  TDD 2.5 GHz_Enhanced Top N Signal Auto Bandwidth Channel 39750 - 2506.000000 MHz.csv"

            };*/
            
            /**********WCDMA Batch */
            
           /* string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\Drive test WCDMA MBS_20230111_144045\";
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 019999095_UMTS WCDMA_UB I  2100 (IMT-2000) DL_Blind Scan.dtr",
                "Gflex Device 019999095_UMTS WCDMA_UB III  1800 (DCS) DL_Blind Scan.dtrv",
                "Gflex Device 019999095_UMTS WCDMA_UB VII  2600 (IMT Extension) DL_Blind Scan.dtr",
                "Gflex Device 019999095_UMTS WCDMA_UB VIII  900 DL_Blind Scan.dtr"
            };*/
            

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
                //"Gflex Device 032201005_LTE_EB 02  1900 (PCS) DL_Blind Scan.csv",
                //"Gflex Device 032201005_LTE_EB 12  US Lower 700-A B C Blocks DL_Blind Scan.csv",
                "Gflex Device 032201005_LTE_EB 66  AWS-3 DL_Blind Scan.csv",
                //"Gflex Device 032201005_NR_FR1 FDD n71 DL_Blind Scan SCS Autodetect.csv",
                "Gflex Device 032201005_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.dtr"
            };

            return (fileDirectory, inputFilenames);
        }

        /***************************************************************************************************
        *
        *   Function:       GetFileType
        *
        *   Description:    Determines the file type based on keywords in the filename and the file extension.
        *                   It first identifies the base technology (LTE, NR, WCDMA) and scan type (TopN, Blind).
        *                   If the file has a '.dtr' extension, it multiplies the base file type by 10.
        *
        *   Input:          filename (string) - The name of the file to analyze.
        *
        *   Output:         An integer code representing the file type.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static int GetFileType(string filename)
        {
            int baseFileType;
            if (filename.Contains("LTE") && filename.Contains("Top N")) baseFileType = 1;
            else if (filename.Contains("LTE") && filename.Contains("Blind")) baseFileType = 2;
            else if (filename.Contains("NR") && (filename.Contains("Topn") || filename.Contains("Top N"))) baseFileType = 3;
            else if (filename.Contains("NR") && filename.Contains("Blind")) baseFileType = 4;
            else if (filename.Contains("WCDMA")) baseFileType = 5;
            else baseFileType = 1; // Default to LTE Top N

            if (Path.GetExtension(filename).Equals(".dtr", StringComparison.OrdinalIgnoreCase))
            {
                return baseFileType * 10;
            }

            return baseFileType;
        }



        /***************************************************************************************************
        *
        *   Function:       Trim
        *
        *   Description:    A simple helper function to safely trim whitespace from a string,
        *                   handling null inputs gracefully.
        *
        *   Input:          s (string) - The string to trim.
        *
        *   Output:         The trimmed string, or an empty string if the input was null.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static string Trim(string s) => s?.Trim() ?? "";

        /***************************************************************************************************
        *
        *   Function:       ExtractChannelCellMap
        *
        *   Description:    Extracts relevant data from cellular drive test files (.csv and .dtr).
        *                   This function reads a file, identifies the correct headers based on the file type
        *                   (which accounts for technology and file extension), and extracts key columns like
        *                   Latitude, Longitude, Cell ID, and signal metrics (CINR, RSSI).
        *                   It returns a list of dictionaries, where each dictionary represents a row of data
        *                   with standardized, generic keys for easier processing downstream.
        *
        *                   A special handling is implemented for NR Blind Scan files (fileType = 4).
        *                   For these files, the 'Cell ID' and 'Beam Index' are combined into a single,
        *                   unique identifier using the formula: new_cell_id = (original_cell_id * 100) + beam_index.
        *                   This composite ID is then stored under the "cellId" key, and the "beamIndex" key is omitted.
        *
        *   Input:          filePath (string) - The full path to the input data file.
        *                   fileType (int) - An integer code representing the technology and file format.
        *
        *   Output:         A list of dictionaries, where each dictionary is a row from the file
        *                   with standardized keys. Returns an empty list if the file is not found or
        *                   if essential headers are missing.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static List<Dictionary<string, string>> ExtractChannelCellMap(string filePath, int fileType)
        {
            var results = new List<Dictionary<string, string>>();
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found at {filePath} in ExtractChannelCellMap.");
                return results;
            }

            // Define keywords based on file type
            string cellIdKeyword = "", cellIdentityKeyword = "", channelKeyword = "", cinrKeyword = "", beamIndexKeyword = "", latKeyword = "Latitude", lonKeyword = "Longitude", rssiKeyword = "", timeOffsetKeyword = "";
            switch (fileType)
            {
                case 1: // LTE eTOPN Scan (.csv)
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "cellIdentity";
                    cinrKeyword = "Ref Signal - CINR";
                    rssiKeyword = "Carrier RSSI Antenna Port 0";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
                    break;
                case 10: // LTE eTOPN Scan (.dtr)
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "cellIdentity";
                    cinrKeyword = "Ref Signal - CINR";
                    rssiKeyword = "Carrier RSSI Antenna Port 0";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
                    break;

                case 2: // LTE Blind Scan (.csv)
                    cellIdKeyword = "Cell Id";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Ref Signal - CINR";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
                    break;
                case 20: // LTE Blind Scan (.dtr)
                    cellIdKeyword = "Cell Id";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "RS_CINR";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "RS_TimeOffset";
                    break;

                case 3: // NR TopN Scan (.csv)
                case 4: // NR Blind Scan (.csv)
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "Cell Identity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Secondary Sync Signal - CINR";
                    beamIndexKeyword = "Beam Index";
                    rssiKeyword = "SSB RSSI";
                    timeOffsetKeyword = "Time Offset";
                    break;
                case 30: // NR TopN Scan (.dtr)
                case 40: // NR Blind Scan (.dtr)
                    cellIdKeyword = "Cell Id";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "SSS_CINR";
                    beamIndexKeyword = "Beam Index";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "Time Offset";
                    break;

                case 5: // WCDMA (.csv)
                    cellIdKeyword = "Pilot";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Ec/Io";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "TimeOffset";//DTR code
                    break;
                case 50: // WCDMA (.dtr)
                    cellIdKeyword = "Pilot";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Ec/Io";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "TimeOffset";//DTR code
                    break;

                default: // Fallback for other types if needed
                    Console.WriteLine($"File type {fileType} not fully configured for extraction. Using defaults.");
                    cellIdKeyword = "Cell ID";
                    cinrKeyword = "Ref Signal - CINR";
                    break;
            }

            try
            {
                using var file = new StreamReader(filePath);
                string? line;
                List<string> headers = new List<string>();
                bool foundHeader = false;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains(cellIdKeyword) && line.Contains(cinrKeyword))
                    {
                        headers = line.Split(',').Select(Trim).ToList();
                        foundHeader = true;
                        break;
                    }
                }

                if (!foundHeader)
                {
                    Console.WriteLine("Error: Could not find the data header row in the file.");
                    return results;
                }

                // Find column indices
                int latIndex = headers.FindIndex(h => h.Equals(latKeyword, StringComparison.OrdinalIgnoreCase));
                int lonIndex = headers.FindIndex(h => h.Equals(lonKeyword, StringComparison.OrdinalIgnoreCase));
                int cellIdIndex = headers.FindIndex(h => h.Equals(cellIdKeyword, StringComparison.OrdinalIgnoreCase));
                int cellIdentityIndex = !string.IsNullOrEmpty(cellIdentityKeyword) ? headers.FindIndex(h => h.Equals(cellIdentityKeyword, StringComparison.OrdinalIgnoreCase)) : -1;
                int channelNumIndex = !string.IsNullOrEmpty(channelKeyword) ? headers.FindIndex(h => h.Equals(channelKeyword, StringComparison.OrdinalIgnoreCase)) : -1;
                int cinrIndex = headers.FindIndex(h => h.Equals(cinrKeyword, StringComparison.OrdinalIgnoreCase));
                int beamIndexCol = !string.IsNullOrEmpty(beamIndexKeyword) ? headers.FindIndex(h => h.Equals(beamIndexKeyword, StringComparison.OrdinalIgnoreCase)) : -1;
                int rssiIndex = !string.IsNullOrEmpty(rssiKeyword) ? headers.FindIndex(h => h.Equals(rssiKeyword, StringComparison.OrdinalIgnoreCase)) : -1;
                
                int timeOffsetIndex = -1;
                if (!string.IsNullOrEmpty(timeOffsetKeyword))
                {
                    bool isNrDtrFile = fileType == 30 || fileType == 40;
                    bool isWcdmaDtrFile = fileType == WCDMA_FILE_TYPE_DTR;

                    if (isNrDtrFile)
                    {
                        // For NR .dtr files, only match the exact "Time Offset" header.
                        timeOffsetIndex = headers.FindIndex(h => h.Equals("Time Offset", StringComparison.Ordinal));
                    }
                    else if (isWcdmaDtrFile)
                    {
                        // For WCDMA .dtr files, only match the exact "TimeOffset" header.
                        timeOffsetIndex = headers.FindIndex(h => h.Equals("TimeOffset", StringComparison.Ordinal));
                    }
                    else
                    {
                        // For all other file types, use the configured keyword with a case-insensitive search.
                        timeOffsetIndex = headers.FindIndex(h => h.Equals(timeOffsetKeyword, StringComparison.OrdinalIgnoreCase));
                    }
                }


                if (cellIdIndex == -1 || cinrIndex == -1 || latIndex == -1 || lonIndex == -1)
                {
                    Console.WriteLine("Error: One or more required columns (Lat, Lon, CellID, CINR) not found.");
                    return results;
                }

                // Determine the highest index we'll need to access
                int maxIndex = new[] { latIndex, lonIndex, cellIdIndex, channelNumIndex, cinrIndex, beamIndexCol, rssiIndex, timeOffsetIndex, cellIdentityIndex }.Max();

                // Process data rows
                int rowCounter = 0; // Start counting data rows after the header
                while ((line = file.ReadLine()) != null)
                {
                    rowCounter++;
                    var values = line.Split(',').Select(Trim).ToList();
                    // Ensure the row has the same number of columns as the header.


                    // Ensure the row has enough columns to access all required fields
                    if (values.Count <= maxIndex) continue;

                    var genericRow = new Dictionary<string, string>();
                    genericRow["rowNumber"] = rowCounter.ToString();
                    genericRow["latitude"] = values[latIndex];
                    genericRow["longitude"] = values[lonIndex];
                    genericRow["cinr"] = values[cinrIndex];

                    if (cellIdentityIndex != -1 && !string.IsNullOrWhiteSpace(values[cellIdentityIndex]))
                    {
                        genericRow["cellIdentity"] = values[cellIdentityIndex];
                    }

                    // Handle Cell ID and Beam Index combination for all NR file types
                    bool isNrFile = fileType == 3 || fileType == 4 || fileType == 30 || fileType == 40;
                    if (isNrFile && beamIndexCol != -1 &&
                        int.TryParse(values[cellIdIndex], out int cellId) &&
                        int.TryParse(values[beamIndexCol], out int beamIndex))
                    {
                        genericRow["cellId"] = (cellId * 100 + beamIndex).ToString();
                    }
                    else
                    {
                        genericRow["cellId"] = values[cellIdIndex];
                        // Only add beamIndex if it's not an NR file where we are combining them
                        if (beamIndexCol != -1 && !isNrFile)
                        {
                            genericRow["beamIndex"] = values[beamIndexCol];
                        }
                    }

                    if (channelNumIndex != -1)
                    {
                        genericRow["channel"] = values[channelNumIndex];
                    }
                    else
                    {
                        genericRow["channel"] = "0";
                    }

                    if (rssiIndex != -1)
                    {
                        genericRow["RSSI"] = values[rssiIndex];
                    }
                    if (timeOffsetIndex != -1)
                    {
                        genericRow["TimeOffset"] = values[timeOffsetIndex];
                    }
                    results.Add(genericRow);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error accessing file: {Path.GetFileName(filePath)}. It is likely open in another program.");
                Console.WriteLine($"Full path: {filePath}");
                Console.WriteLine($"Details: {ex.Message}");
            }

            //SaveHelper.save_debug_map(results, "debug.csv");

            return results;
        }

        /***************************************************************************************************
        *
        *   Function:       ProcessTimeOffset
        *
        *   Description:    Adjusts the 'TimeOffset' values for a given set of data points.
        *                   Cellular timing measurements can "wrap around" a zero point, leading to
        *                   large jumps in raw data. This logic detects such wrapping and adjusts the
        *                   values to make them continuous. Finally, it normalizes the adjusted time
        *                   offset by the technology-specific sampling rate.
        *
        *   Input:          data (List<...>) - The list of data points to process.
        *                   fileType (int) - The file type code to determine the correct wrap value and sampling rate.
        *                   timeOffsetWrapValue, wcdmaTimeOffsetWrapValue (double) - Wrap-around thresholds.
        *                   lteSamplingRateHz, nrSamplingRateMultiplier, wcdmaSamplingRateDivisor (double) - Sampling rate parameters.
        *
        *   Output:         The input list with 'TimeOffset' values adjusted and normalized.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static List<Dictionary<string, string>> ProcessTimeOffset(
            List<Dictionary<string, string>> data,
            int fileType,
            double timeOffsetWrapValue,
            double wcdmaTimeOffsetWrapValue,
            double lteSamplingRateHz,
            double nrSamplingRateMultiplier,
            double wcdmaSamplingRateDivisor)
        {
            if (data == null || !data.Any())
            {
                return new List<Dictionary<string, string>>();
            }

            bool isNr = fileType == 3 || fileType == 4 || fileType == 30 || fileType == 40;
            bool isWcdma = fileType == WCDMA_FILE_TYPE_CSV || fileType == WCDMA_FILE_TYPE_DTR;

            double wrapValue;
            double samplingRateHz;

            if (isNr)
            {
                const double ssbPeriod = 20e-3; // 20 ms
                wrapValue = lteSamplingRateHz * nrSamplingRateMultiplier * ssbPeriod;
                samplingRateHz = lteSamplingRateHz * nrSamplingRateMultiplier;
            }
            else if (isWcdma)
            {
                wrapValue = wcdmaTimeOffsetWrapValue;
                samplingRateHz = lteSamplingRateHz / wcdmaSamplingRateDivisor;
            }
            else // LTE cases (1, 2, 10, 20) and any other defaults
            {
                wrapValue = timeOffsetWrapValue;
                samplingRateHz = lteSamplingRateHz;
            }

            var timeOffsets = data.Select(row =>
                row.TryGetValue("TimeOffset", out var tsStr) &&
                double.TryParse(tsStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double ts) ? ts : 0.0)
                .ToList();

            if (!timeOffsets.Any()) return data;

            double tsMin = timeOffsets.Min();
            double tsMax = timeOffsets.Max();

            // Adjust for wrapping
            if (tsMin < wrapValue / 4.0 && tsMax > wrapValue * 3.0 / 4.0)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (timeOffsets[i] > wrapValue * 3.0 / 4.0)
                    {
                        data[i]["TimeOffset"] = (timeOffsets[i] - wrapValue).ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            // Normalize the time offset by the sampling rate
            foreach (var row in data)
            {
                if (row.TryGetValue("TimeOffset", out var tsStr) &&
                    double.TryParse(tsStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double ts))
                {
                    // Use "G17" format specifier for full double precision
                    row["TimeOffset"] = (ts / samplingRateHz).ToString("G17", CultureInfo.InvariantCulture);
                }
            }

            return data;
        }

        /***************************************************************************************************
        *
        *   Function:       Save_Drive_Route
        *
        *   Description:    Creates a KML file to visualize the drive route from the collected data points.
        *                   The data is downsampled to reduce the number of points in the KML file,
        *                   and each point is represented as a small red dot on the map.
        *
        *   Input:          allData (List<...>) - The full list of data points from the input file.
        *                   inputFilename (string) - The name of the original input file, used to generate the output filename.
        *                   downsampleSize (int) - The factor by which to downsample the data. Default is 10.
        *
        *   Output:         None (void). A .kml file is written to the same directory as the executable.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static void Save_Drive_Route(List<Dictionary<string, string>> allData, string inputFilename, int downsampleSize = 10)
        {
            if (allData == null || !allData.Any())
            {
                Console.WriteLine("No data available to generate a route KML.");
                return;
            }

            var kmlContent = new System.Text.StringBuilder();
            kmlContent.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            kmlContent.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
            kmlContent.AppendLine("  <Document>");
            kmlContent.AppendLine($"    <name>Route for {Path.GetFileNameWithoutExtension(inputFilename)}</name>");
            
            // Add a style for the route line
            kmlContent.AppendLine("    <Style id=\"driveRouteStyle\">");
            kmlContent.AppendLine("      <LineStyle>");
            kmlContent.AppendLine("        <color>ff0000ff</color>"); // Red
            kmlContent.AppendLine("        <width>3</width>");
            kmlContent.AppendLine("      </LineStyle>");
            kmlContent.AppendLine("    </Style>");

            // Add route as a single LineString Placemark
            kmlContent.AppendLine("    <Placemark>");
            kmlContent.AppendLine("      <name>Drive Route</name>");
            kmlContent.AppendLine("      <styleUrl>#driveRouteStyle</styleUrl>");
            kmlContent.AppendLine("      <LineString>");
            kmlContent.AppendLine("        <tessellate>1</tessellate>");
            kmlContent.AppendLine("        <coordinates>");

            var coordinates = new System.Text.StringBuilder();
            for (int i = 0; i < allData.Count; i += downsampleSize)
            {
                var row = allData[i];
                if (row.TryGetValue("latitude", out var latStr) &&
                    row.TryGetValue("longitude", out var lonStr) &&
                    double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(lonStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                {
                    coordinates.Append($"{lon.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},0 ");
                }
            }
            kmlContent.AppendLine(coordinates.ToString().Trim());

            kmlContent.AppendLine("        </coordinates>");
            kmlContent.AppendLine("      </LineString>");
            kmlContent.AppendLine("    </Placemark>");

            kmlContent.AppendLine("  </Document>");
            kmlContent.AppendLine("</kml>");

            string outputFilename = $"Route_{Path.GetFileNameWithoutExtension(inputFilename)}.kml";
            try
            {
                File.WriteAllText(outputFilename, kmlContent.ToString());
                Console.WriteLine($"Successfully created route map: {outputFilename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing KML file: {ex.Message}");
            }
        }
    }
}

