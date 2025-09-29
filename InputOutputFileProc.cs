using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static BTS_Location_Estimation.DataBaseProc;

namespace BTS_Location_Estimation
{
    public static class InputOutputFileProc
    
        /// <summary>
        /// For each channel/cellId pair, if any entry has a non-blank/non-zero mcc/mnc, copy that pair to all entries for that channel/cellId.
        /// </summary>
        /// <param name="allData">List of data rows</param>
        /// <returns>Updated allData</returns>
    {

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
            //The file and the estimation results are saved in the Refbackup folder.
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 032201005_TD-LTE_EB 41  TDD 2.5 GHz_Enhanced Top N Signal Auto Bandwidth Channel 39750 - 2506.000000 MHz.csv"

            };*/

            /*****GSM file*/
            /*string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\GSM\";
            List<string> inputFilenames = new List<string>
            {
                "BCCH IBflex_0000000019999039_ColorCodeData_Snum90_134014723539980889.csv"

            };*/
            /**********WCDMA Batch */

            /*string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\Drive test WCDMA MBS_20230111_144045\";
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 019999095_UMTS WCDMA_UB I  2100 (IMT-2000) DL_Blind Scan.dtr",
                "Gflex Device 019999095_UMTS WCDMA_UB III  1800 (DCS) DL_Blind Scan.dtr",
                "Gflex Device 019999095_UMTS WCDMA_UB VII  2600 (IMT Extension) DL_Blind Scan.dtr",
                "Gflex Device 019999095_UMTS WCDMA_UB VIII  900 DL_Blind Scan.dtr"
            };*/


            //Batch processing One time SIB1 for debugging.

            /*string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\7.0.2.4\20250813_Drive2-SIB1-onetime\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {

                "Gflex Device 019999090_LTE_EB 14  Upper 700-D Block DL_Blind Scan.dtr"
                "Gflex Device 019999090_LTE_EB 66  AWS-3 DL_Blind Scan.dtr",
                "Gflex Device 019999090_LTE_EB 30  2.3 GHz (WCS A B) DL_Blind Scan.dtr",
                "Gflex Device 019999090_NR_FR1 TDD n77_Blind Scan SCS Autodetect.dtr",
                "Gflex Device 019999090_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.dtr"
                
            };*/


            //dRIVE bts_5 att n77

            string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\n77 drive\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {

                "Gflex Device 032201005_LTE_EB 66  AWS-3 DL_Blind Scan.DTR",
                "Gflex Device 032201005_NR_FR1 TDD n77_Blind Scan SCS Autodetect.DTR",
                "Gflex Device 032201005_LTE_EB 30  2.3 GHz (WCS A B) DL_Blind Scan.DTR",
                "Gflex Device 032201005_NR_FR1 TDD n77_nr Top N Signal ARFCN  658080 - 3871.20 MHz SCS 30 kHz.dtr"
            };


            //Batch processing Drive 3_Washington DC

            /*string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\20250827_DC-Detailed-Drive\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 032201005_LTE_EB 02  1900 (PCS) DL_Blind Scan.dtr",
                "Gflex Device 032201005_LTE_EB 12  US Lower 700-A B C Blocks DL_Blind Scan.dtr",
                "Gflex Device 032201005_LTE_EB 66  AWS-3 DL_Blind Scan.dtr",
                "Gflex Device 032201005_LTE_EB 71 DL_Blind Scan.dtr",

                "Gflex Device 032201005_NR_FR1 FDD n71 DL_Blind Scan SCS Autodetect.dtr",
                "Gflex Device 032201005_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.dtr",
                "Gflex Device 032201005_TD-LTE_EB 41  TDD 2.5 GHz_Blind Scan.dtr",
                "Gflex Device 032201005_NR_FR1 FDD n25 DL_Blind Scan SCS Autodetect.dtr"
            };*/



            //Batch processing Drive 5_Gaithersuburg
            /*string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\20250828_Gaitherburg-Drive\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 032201005_LTE_EB 02  1900 (PCS) DL_Blind Scan.dtr",
                "Gflex Device 032201005_LTE_EB 12  US Lower 700-A B C Blocks DL_Blind Scan.dtr",
                "Gflex Device 032201005_LTE_EB 66  AWS-3 DL_Blind Scan.dtr",
                "Gflex Device 032201005_NR_FR1 FDD n71 DL_Blind Scan SCS Autodetect.dtr",
                "Gflex Device 032201005_NR_FR1 TDD n41   n90_Blind Scan SCS Autodetect.dtr"
            };*/
            
            //Batch processing Drive 6 Gordon Hong Kong
            /*string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\Gordon_20250923_033301\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 019999102_NR_FR1 TDD n79_Blind Scan SCS Autodetect.dtr",
                "Gflex Device 019999102_GSM_900 DL_Blind Scan (0 - 125).dtr",
                "Gflex Device 019999102_GSM_1800 (DCS) DL_Blind Scan.dtr",
                "Gflex Device 019999102_LTE_EB 01  2100 (IMT-2000) DL_Blind Scan.dtr",
                "Gflex Device 019999102_LTE_EB 03  1800 (DCS) DL_Blind Scan.dtr",
                "Gflex Device 019999102_LTE_EB 07  2600 (IMT Extension) DL_Blind Scan.dtr",
                "Gflex Device 019999102_NR_FR1 FDD n1 DL_Blind Scan SCS Autodetect.dtr",
                "Gflex Device 019999102_NR_FR1 TDD n78_Blind Scan SCS Autodetect.dtr",
                "Gflex Device 019999102_UMTS WCDMA_UB I  2100 (IMT-2000) DL_Blind Scan.dtr",
                "Gflex Device 019999102_UMTS WCDMA_UB V  850 (Cellular) DL_Blind Scan (4355 - 4460).dtr",
                "Gflex Device 019999102_UMTS WCDMA_UB VIII  900 DL_Blind Scan.dtr"
            };*/
            //Batch processing Drive 7 Josue drive test in Washington DC, GSM only
            /*string fileDirectory = @"C:\Users\amirsoltanian\OneDrive - PCTEL, Inc\LocalDrive Tests\BTS Location_DriveTests\20250922_GSM_HRTOA\";
            // List of input filenames to process in batch
            List<string> inputFilenames = new List<string>
            {
                "Gflex Device 019999090_GSM_1900 (PCS) DL_Blind Scan.dtr"
                
            };*/
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
            if (filename.Contains("ColorCode") || filename.Contains("GSM")) baseFileType = GSM_FILE_TYPE;
            else if (filename.Contains("LTE") && filename.Contains("Top N")) baseFileType = LTE_TOPN_FILE_TYPE;
            else if (filename.Contains("LTE") && filename.Contains("Blind")) baseFileType = LTE_BLIND_FILE_TYPE;
            else if (filename.Contains("NR") && (filename.Contains("Topn") || filename.Contains("Top N"))) baseFileType = NR_TOPN_FILE_TYPE;
            else if (filename.Contains("NR") && filename.Contains("Blind")) baseFileType = NR_FILE_TYPE;
            else if (filename.Contains("WCDMA")) baseFileType = WCDMA_FILE_TYPE_CSV;
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
            string mncKeyword = "", mccKeyword = "";
            switch (fileType)
            {
                case LTE_TOPN_FILE_TYPE: // LTE eTOPN Scan (.csv)
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "cellIdentity";
                    cinrKeyword = "Ref Signal - CINR";
                    rssiKeyword = "Carrier RSSI Antenna Port 0";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
                    mncKeyword = "mnc";
                    mccKeyword = "mcc";
                    break;
                case LTE_TOPN_FILE_TYPE*10: // LTE eTOPN Scan (.dtr)
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "cellIdentity";
                    cinrKeyword = "Ref Signal - CINR";
                    rssiKeyword = "Carrier RSSI Antenna Port 0";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
                    mncKeyword = "mnc";
                    mccKeyword = "mcc";
                    break;

                case LTE_BLIND_FILE_TYPE: // LTE Blind Scan (.csv)
                    cellIdKeyword = "Cell Id";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Ref Signal - CINR";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
                    mncKeyword = "mnc";
                    mccKeyword = "mcc";
                    break;
                case LTE_BLIND_FILE_TYPE*10: // LTE Blind Scan (.dtr)
                    cellIdKeyword = "Cell Id";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "RS_CINR";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "RS_TimeOffset";
                    mncKeyword = "mnc";
                    mccKeyword = "mcc";
                    break;

                case NR_TOPN_FILE_TYPE*10: // NR TopN Scan (.dtr)
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "Cell Identity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "SSS_CINR";
                    beamIndexKeyword = "Beam Index";
                    rssiKeyword = "SSB RSSI";
                    timeOffsetKeyword = "Time Offset";
                    mncKeyword = "mcc";
                    mccKeyword = "mnc";
                    break;
                case NR_FILE_TYPE * 10: // NR Blind Scan (.dtr)
                    cellIdKeyword = "Cell Id";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "SSS_CINR";
                    beamIndexKeyword = "Beam Index";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "Time Offset";
                    mncKeyword = "mcc";
                    mccKeyword = "mnc";
                    break;

                case WCDMA_FILE_TYPE_CSV: // WCDMA (.csv)
                    cellIdKeyword = "Pilot";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Ec/Io";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "TimeOffset";//DTR code
                    mncKeyword = "mnc";
                    mccKeyword = "mcc";
                    break;
                case WCDMA_FILE_TYPE_DTR: // WCDMA (.dtr)
                    cellIdKeyword = "Pilot";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Ec/Io";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "TimeOffset";//DTR code
                    mncKeyword = "mnc";
                    mccKeyword = "mcc";
                    break;


                case GSM_FILE_TYPE*10://.dtr
                    cellIdKeyword = "BSIC";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "C/I";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "HrToA";
                    mncKeyword = "mnc";
                    mccKeyword = "mcc";
                    break;
                default: // Fallback for other types if needed
                    Console.WriteLine($"File type {fileType} not fully configured for extraction. Using defaults.");
                    cellIdKeyword = "Cell ID";
                    cinrKeyword = "Ref Signal - CINR";
                    mncKeyword = "mnc";
                    mccKeyword = "mcc";
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
                int mncIndex = !string.IsNullOrEmpty(mncKeyword) ? headers.FindIndex(h => h.Equals(mncKeyword, StringComparison.OrdinalIgnoreCase)) : -1;
                int mccIndex = !string.IsNullOrEmpty(mccKeyword) ? headers.FindIndex(h => h.Equals(mccKeyword, StringComparison.OrdinalIgnoreCase)) : -1;

                int timeOffsetIndex = -1;
                if (!string.IsNullOrEmpty(timeOffsetKeyword))
                {
                    bool isNrDtrFile = fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;
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

                    // Always read beamIndex for NR file types
                    bool isNrFile = fileType == NR_TOPN_FILE_TYPE || fileType == NR_FILE_TYPE || fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;
                    genericRow["cellId"] = values[cellIdIndex];
                    if (isNrFile && beamIndexCol != -1 && beamIndexCol < values.Count)
                    {
                        genericRow["beamIndex"] = values[beamIndexCol];
                    }

                    // Extract cellIdentity if present          
                    if (cellIdentityIndex != -1 && cellIdentityIndex < values.Count && !string.IsNullOrWhiteSpace(values[cellIdentityIndex]))
                    {
                        genericRow["cellIdentity"] = values[cellIdentityIndex];
                    }

                    // Extract mnc and mcc if present
                    if (mncIndex != -1 && mncIndex < values.Count && !string.IsNullOrWhiteSpace(values[mncIndex]))
                    {
                        genericRow["mnc"] = values[mncIndex];
                    }
                    if (mccIndex != -1 && mccIndex < values.Count && !string.IsNullOrWhiteSpace(values[mccIndex]))
                    {
                        genericRow["mcc"] = values[mccIndex];
                    }

                    // Handle Cell ID and Beam Index combination for all NR file types
                    /*
                    bool isNrFile = fileType == NR_TOPN_FILE_TYPE || fileType == NR_FILE_TYPE || fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;
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
                    }*/

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
        *   Function:       Save_Drive_Route
        *
        *   Description:    Saves the drive route data to a KML file for visualization in mapping software.
        *                   It generates a KML document with the drive route represented as a LineString
        *                   placemark, allowing the route to be displayed on geographic maps.
        *
        *   Input:          allData (List<Dictionary<string, string>>) - The drive route data, where each dictionary
        *                   represents a point on the route with keys for latitude, longitude, and other metrics.
        *                   inputFilename (string) - The name of the input file, used to derive the KML file name.
        *                   downsampleSize (int) - The factor by which to downsample the data for KML output.
        *                   Default is 10, meaning every 10th data point is used.
        *
        *   Output:         None. A KML file is written to the file system.
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

        /// <summary>
        /// For each channel/cellId pair, if any entry has a non-blank/non-zero mcc/mnc, copy that pair to all entries for that channel/cellId.
        /// </summary>
        /// <param name="allData">List of data rows</param>
        /// <returns>Updated allData</returns>

    }
}

