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
        public const int WCDMA_FILE_TYPE = 5;

        public static (string fileDirectory, List<string> inputFilenames) GetFileConfigurations()
        {
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

            return (fileDirectory, inputFilenames);
        }

        public static int GetFileType(string filename)
        {
            if (filename.Contains("LTE") && filename.Contains("Top N")) return 1;
            if (filename.Contains("LTE") && filename.Contains("Blind")) return 2;
            if (filename.Contains("NR") && (filename.Contains("Topn") || filename.Contains("Top N"))) return 3;
            if (filename.Contains("NR") && filename.Contains("Blind")) return 4;
            if (filename.Contains("WCDMA")) return 5;
            return 1; // Default to LTE Top N
        }



        public static string Trim(string s) => s?.Trim() ?? "";

        public static List<Dictionary<string, string>> ExtractChannelCellMap(string filePath, int fileType)
        {
            // Extracts relevant data from cellular drive test CSV files.
            // This function reads a CSV file, identifies the correct headers based on the file type,
            // and extracts key columns like Latitude, Longitude, Cell ID, and signal metrics (CINR, RSSI).
            // It returns a list of dictionaries, where each dictionary represents a row of data
            // with standardized, generic keys for easier processing downstream.
            //
            // A special handling is implemented for NR Blind Scan files (fileType = 4).
            // For these files, the 'Cell ID' and 'Beam Index' are combined into a single,
            // unique identifier using the formula: new_cell_id = (original_cell_id * 100) + beam_index.
            // This composite ID is then stored under the "cellId" key, and the "beamIndex" key is omitted.
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
                case 1: // LTE eTOPN Scan
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "cellIdentity";
                    cinrKeyword = "Ref Signal - CINR";
                    rssiKeyword = "Carrier RSSI Antenna Port 0";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
                    break;

                case 2: // LTE Blind Scan
                    cellIdKeyword = "Cell Id";
                    cellIdentityKeyword = "cellIdentity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Ref Signal - CINR";
                    rssiKeyword = "Channel RSSI";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
                    break;
                case 4: // NR Blind Scan
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "Cell Identity";
                    channelKeyword = "Channel Number";
                    cinrKeyword = "Secondary Sync Signal - CINR";
                    beamIndexKeyword = "Beam Index";
                    rssiKeyword = "SSB RSSI";
                    timeOffsetKeyword = "Time Offset";
                    break;
                case 5: // WCDMA
                    cellIdKeyword = "Cell ID";
                    cellIdentityKeyword = "cellIdentity";
                    cinrKeyword = "Ref Signal - Ec/Io";
                    timeOffsetKeyword = "Ref Signal - Timeoffset";
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
                int timeOffsetIndex = !string.IsNullOrEmpty(timeOffsetKeyword) ? headers.FindIndex(h => h.Equals(timeOffsetKeyword, StringComparison.OrdinalIgnoreCase)) : -1;


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

                    // Handle Cell ID and Beam Index combination for fileType 4
                    if (fileType == 4 && beamIndexCol != -1 &&
                        int.TryParse(values[cellIdIndex], out int cellId) &&
                        int.TryParse(values[beamIndexCol], out int beamIndex))
                    {
                        genericRow["cellId"] = (cellId * 100 + beamIndex).ToString();
                    }
                    else
                    {
                        genericRow["cellId"] = values[cellIdIndex];
                        // Only add beamIndex if it's not fileType 4
                        if (beamIndexCol != -1)
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
            return results;
        }

        public static List<Dictionary<string, string>> filter_cinr_minimum_PCI(List<Dictionary<string, string>> allData, double cinrThresh, int minimumCellIdCount)
        {
            // 1. Filter out rows where CINR is below the threshold
            var cinrFiltered = allData.Where(row =>
            {
                if (row.TryGetValue("cinr", out var cinrString) && double.TryParse(cinrString, NumberStyles.Any, CultureInfo.InvariantCulture, out var cinrValue))
                {
                    return cinrValue >= cinrThresh;
                }
                return false; // Discard rows without a valid CINR
            }).ToList();

            // 2. Group by channel and cell ID, then filter by count
            var finalFilteredData = cinrFiltered
                .GroupBy(row => new
                {
                    Channel = row.GetValueOrDefault("channel", "N/A"),
                    CellId = row.GetValueOrDefault("cellId", "N/A")
                })
                .Where(group => group.Count() >= minimumCellIdCount)
                .SelectMany(group => group) // Flatten the groups back into a list of rows
                .ToList();

            Console.WriteLine($"Filtered data down to {finalFilteredData.Count} rows after CINR and minimum count check.");
            return finalFilteredData;
        }

        public static Tuple<List<Dictionary<string, string>>, double> ExtractPointsWithDistance(
            List<Dictionary<string, string>> extractedData,
            double distanceThreshold,
            int maxPoints,
            double metersPerDegree)
        {
            // This function processes a list of data points for a single cell,
            // filtering them based on geographic distance and signal quality (CINR).
            // It ensures that the selected points are not too close to each other,
            // picking the one with the best CINR if they are. This helps to
            // select geographically distinct points with strong signals, which is
            // crucial for accurate location estimation algorithms. The function
            // returns the filtered list of points and the maximum CINR found.
            if (extractedData == null || !extractedData.Any())
            {
                return Tuple.Create(new List<Dictionary<string, string>>(), -999.0);
            }

            var selectedPoints = new List<Dictionary<string, string>> { extractedData.First() };

            double CalculateDistance(Dictionary<string, string> p1, Dictionary<string, string> p2)
            {
                if (p1 == null || p2 == null ||
                    !p1.TryGetValue("latitude", out var latStr1) || !p1.TryGetValue("longitude", out var lonStr1) ||
                    !p2.TryGetValue("latitude", out var latStr2) || !p2.TryGetValue("longitude", out var lonStr2) ||
                    !double.TryParse(latStr1, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat1) ||
                    !double.TryParse(lonStr1, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon1) ||
                    !double.TryParse(latStr2, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat2) ||
                    !double.TryParse(lonStr2, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon2))
                {
                    return -1.0; // Invalid data
                }
                return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2)) * metersPerDegree;
            }

            for (int i = 1; i < extractedData.Count; ++i)
            {
                if (selectedPoints.Count >= maxPoints)
                {
                    break;
                }
                var currentPoint = extractedData[i];
                var lastSelectedPoint = selectedPoints.Last();

                double distance = CalculateDistance(currentPoint, lastSelectedPoint);
                if (distance < 0) continue;

                if (distance < distanceThreshold)
                {
                    if (currentPoint.TryGetValue("cinr", out var currentCinrStr) &&
                        lastSelectedPoint.TryGetValue("cinr", out var lastCinrStr) &&
                        double.TryParse(currentCinrStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentCinr) &&
                        double.TryParse(lastCinrStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lastCinr) &&
                        currentCinr > lastCinr)
                    {
                        selectedPoints[selectedPoints.Count - 1] = currentPoint; // Replace last point
                    }
                }
                else
                {
                    selectedPoints.Add(currentPoint);
                }
            }

            double maxCinr = selectedPoints
                .Select(row => row.TryGetValue("cinr", out var cinrStr) && double.TryParse(cinrStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double cinr) ? cinr : -999.0)
                .DefaultIfEmpty(-999.0)
                .Max();

            return Tuple.Create(selectedPoints, maxCinr);
        }

        public static List<Dictionary<string, string>> ProcessTimeOffset(
            List<Dictionary<string, string>> data,
            int fileType,
            double timeOffsetWrapValue,
            double wcdmaTimeOffsetWrapValue,
            double lteSamplingRateHz,
            double nrSamplingRateMultiplier,
            double wcdmaSamplingRateDivisor)
        {
            // This function adjusts the 'TimeOffset' values for a given set of data points.
            // Cellular timing measurements can "wrap around" a zero point, leading to
            // large jumps in raw data (e.g., from a large positive to a large negative value).
            // This logic detects such wrapping by checking if values fall on opposite ends
            // of the possible range. If wrapping is detected, it adjusts the values to make
            // them continuous. Finally, it normalizes the adjusted time offset by the
            // technology-specific sampling rate.
            if (data == null || !data.Any())
            {
                return new List<Dictionary<string, string>>();
            }

            double wrapValue = timeOffsetWrapValue; // Default for LTE
            if (fileType == 4) // NR
            {
                const double ssbPeriod = 20e-3; // 20 ms
                wrapValue = lteSamplingRateHz * nrSamplingRateMultiplier * ssbPeriod;
            }
            else if (fileType == 5) // WCDMA
            {
                wrapValue = wcdmaTimeOffsetWrapValue;
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
            double samplingRateHz = lteSamplingRateHz; // Default for LTE
            if (fileType == 4) // NR
            {
                samplingRateHz = lteSamplingRateHz * nrSamplingRateMultiplier;
            }
            else if (fileType == 5) // WCDMA
            {
                samplingRateHz = lteSamplingRateHz / wcdmaSamplingRateDivisor;
            }

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
    }
}

