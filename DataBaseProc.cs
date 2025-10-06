    /***************************************************************************************************
    *
    *   Function:       Expand_mcc_mnc_cellIdentity
    *
    *   Description:    For each channel/cellId pair, expands mcc/mnc and cellIdentity fields.
    *                   - mcc/mnc expansion: For each group, finds the first non-blank/non-zero mcc/mnc
    *                     and copies those values to all rows in the group (static propagation).
    *                   - cellIdentity expansion: Sequentially propagates the latest non-blank cellIdentity
    *                     value forward through the group. If a new non-blank cellIdentity is encountered,
    *                     it is used for all subsequent rows until another new value is found (dynamic, sequential propagation).
    *                   This ensures that mcc/mnc are unified for each group, while cellIdentity tracks changes
    *                   and propagates them as they appear in the data.
    *
    *   Input:          allData (List<Dictionary<string, string>>) - The full list of extracted data.
    *
    *   Output:         The updated list with expanded mcc, mnc, and cellIdentity fields.
    *
    *   Author:         Amir Soltanian
    *
    *   Date:           September 11, 2025
    *
    ***************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BTS_Location_Estimation
{
    public static class DataBaseProc
{
    public const int LTE_TOPN_FILE_TYPE = 1;
    public const int LTE_BLIND_FILE_TYPE = 2;
    public const int NR_TOPN_FILE_TYPE = 3;
    public const int NR_FILE_TYPE = 4;
    public const int WCDMA_FILE_TYPE_CSV = 5;
    public const int GSM_FILE_TYPE = 6;
    public const int WCDMA_FILE_TYPE_DTR = 50;
        /***************************************************************************************************
    *
    *   Function:       generate_unique_cellID
    *
    *   Description:    For NR file types, updates the cellId value for each row by combining cellId and beamIndex:
    *                   cellId = cellId * 100 + beamIndex.
    *                   For non-NR file types, cellId is recalculated as:
    *                   cellId = cellIdentity * 1000 + cellId, where cellIdentity can be a large integer.
    *                   If cellIdentity is blank or cannot be parsed, the row is left unchanged.
    *                   All parsing uses double integer logic for safety.
    *
    *   Input:          allData (List<Dictionary<string, string>>) - The full list of extracted data.
    *                   fileType (int) - The file type code.
    *
    *   Output:         The updated list with unique cellId values for NR and non-NR files.
    *
    *   Author:         Amir Soltanian
    *
    *   Date:           September 11, 2025
    *
    ***************************************************************************************************/
        public static List<Dictionary<string, string>> generate_unique_cellID(List<Dictionary<string, string>> allData, int fileType)
        {
            if (allData == null || allData.Count == 0) return allData!;
            bool isNrFile = fileType == NR_TOPN_FILE_TYPE || fileType == NR_FILE_TYPE || fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;
            foreach (var row in allData)
            {
                if (isNrFile)
                {
                    if (row.TryGetValue("cellId", out var cellIdStr) &&
                        row.TryGetValue("beamIndex", out var beamIndexStr) &&
                        int.TryParse(cellIdStr, out int cellId) &&
                        int.TryParse(beamIndexStr, out int beamIndex))
                    {
                        row["cellId"] = (cellId * 100 + beamIndex).ToString();
                    }
                }
                else
                {
                    if (row.TryGetValue("cellId", out var cellIdStr) &&
                        row.TryGetValue("cellIdentity", out var cellIdentityStr) &&
                        long.TryParse(cellIdentityStr, out long cellIdentity) &&
                        int.TryParse(cellIdStr, out int cellId))
                    {
                        // cellIdentity can be a large 10 digit decimal integer
                        row["cellId"] = (cellIdentity * 1000 + cellId).ToString();
                    }
                }
            }
            return allData!;
        }
    

        /***************************************************************************************************
        *
        *   Function:       Confidence_and_Filtering
        *
        *   Description:    Filters out entries where Confidence == "Low" from the input list.
        *
        *   Input:          data (List<Dictionary<string, string>>) - List of entries to filter.
        *
        *   Output:         List<Dictionary<string, string>> with entries of Confidence != "Low".
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 10, 2025
        *
        ***************************************************************************************************/
        public static List<Dictionary<string, string>> Confidence_and_Filtering(List<Dictionary<string, string>> data, string? filterType = null, string? filterValue = null)
        {
            if (data == null) return new List<Dictionary<string, string>>();

            // Step 1: Filter out entries where Confidence is "Low"
            var confidenceFilteredData = data
                .Where(row => !row.GetValueOrDefault("Confidence", "Low")!.Equals("Low", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (string.IsNullOrEmpty(filterType) || !confidenceFilteredData.Any())
            {
                return confidenceFilteredData;
            }

            // Step 2: Determine the key to filter on
            string filterKey;
            if (filterType.Equals("channel", StringComparison.OrdinalIgnoreCase))
            {
                filterKey = "Channel";
            }
            else if (filterType.Equals("mnc", StringComparison.OrdinalIgnoreCase))
            {
                filterKey = "mnc";
            }
            else
            {
                return confidenceFilteredData; // Unrecognized filterType
            }

            // Step 3: Apply filtering
            if (!string.IsNullOrEmpty(filterValue))
            {
                // For mnc, match only the first three digits before any semicolon
                if (filterKey == "mnc")
                {
                    string filterValuePrefix = filterValue.Split(';')[0];
                    return confidenceFilteredData
                        .Where(row => row.TryGetValue("mnc", out var mncVal) && mncVal.Split(';')[0] == filterValuePrefix)
                        .ToList();
                }
                // For other keys, match directly
                return confidenceFilteredData
                    .Where(row => row.GetValueOrDefault(filterKey) == filterValue)
                    .ToList();
            }
            else
            {
                // Original logic: filter by the most common value
                var mostCommonValue = confidenceFilteredData
                    .Select(row => row.GetValueOrDefault(filterKey))
                    .Where(val => !string.IsNullOrWhiteSpace(val))
                    .GroupBy(val => val)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (mostCommonValue == null)
                {
                    return confidenceFilteredData;
                }

                return confidenceFilteredData
                    .Where(row => row.GetValueOrDefault(filterKey) == mostCommonValue)
                    .ToList();
            }
        }





        /***************************************************************************************************
        *
        *   Function:       filter_cinr_minimum_PCI
        *
        *   Description:    Filters the extracted data based on three criteria:
        *                   1. Invalid Coordinates: Removes rows where latitude or longitude is zero.
        *                   2. Signal Strength: Removes rows where the CINR is below a specified threshold.
        *                   3. Minimum Count: Removes entire groups of (Channel, CellId) if they do not
        *                      have at least a minimum number of data points after the other filters.
        *
        *   Input:          allData (List<...>) - The full list of extracted data.
        *                   cinrThresh (double) - The minimum required CINR value.
        *                   minimumCellIdCount (int) - The minimum number of rows for a cell to be kept.
        *
        *   Output:         A new list containing only the data that passed all filtering stages.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 8, 2025
        *
        ***************************************************************************************************/
        public static List<Dictionary<string, string>> filter_cinr_minimum_PCI(List<Dictionary<string, string>> allData, int fileType, int minimumCellIdCount)
        {
            // Determine CINR/ECIO threshold based on fileType
            double cinrThresh = (fileType == WCDMA_FILE_TYPE_CSV || fileType == WCDMA_FILE_TYPE_DTR) ? MainModule.EC_IO_THRESHOLD : MainModule.CINR_THRESH;

            // 1. Filter out rows with invalid coordinates (lat/lon = 0)
            var coordinateFiltered = allData.Where(row =>
            {
                bool latValid = row.TryGetValue("latitude", out var latStr) &&
                                double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                                lat != 0;
                bool lonValid = row.TryGetValue("longitude", out var lonStr) &&
                                double.TryParse(lonStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon) &&
                                lon != 0;
                return latValid && lonValid;
            }).ToList();


            Console.WriteLine($"Filtered data down to {coordinateFiltered.Count} rows after coordinate check.");

            // 2. Filter out rows where CINR is below the threshold, or
            // for  GSM, filter by BSIC that is non-blank. since blank BSCIs are filtered before
            //no action is neccessary.
            List<Dictionary<string, string>> cinrFiltered;
            if ((fileType != GSM_FILE_TYPE*10) && (fileType != GSM_FILE_TYPE) )
            {
                cinrFiltered = coordinateFiltered.Where(row =>
                {
                    if (row.TryGetValue("cinr", out var cinrString) && double.TryParse(cinrString, NumberStyles.Any, CultureInfo.InvariantCulture, out var cinrValue))
                    {
                        return cinrValue >= cinrThresh;
                    }
                    return false; // Discard rows without a valid CINR
                }).ToList();
            }
            else
            {
                cinrFiltered = coordinateFiltered;
            }

            // 3. Group by channel and cell ID, then filter by count
            var finalFilteredData = cinrFiltered
                .GroupBy(row => new
                {
                    Channel = row.GetValueOrDefault("channel", "N/A"),
                    CellId = row.GetValueOrDefault("cellId", "N/A")
                })
                .Where(group => group.Count() >= minimumCellIdCount)
                .SelectMany(group => group) // Flatten the groups back into a list of rows
                .ToList();


            Console.WriteLine($"Filtered data down to {finalFilteredData.Count} rows after coordinate, CINR, and minimum count checks.");

            return finalFilteredData;
        }

        /***************************************************************************************************
        *
        *   Function:       GSMHrtoaAverage
        *
        *   Description:    Averages the 'TimeOffset' (HrToA) values for each point in the input list
        *                   based on nearby points within a specified distance threshold.
        *                   Initializes 'cinr' to the CountAvgHrToA value to ensure a better performance.
        *
        *   Input:          extractedData (List<...>) - Data points for a single cell.
        *                   distanceThreshold (double) - The maximumdistance between selected points.
        *                   metersPerDegree (double) - Conversion factor for distance calculation.
        *
        *   Output:         A tuple containing the filtered list of points and the maximum CINR found.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 25, 2025
        *
        ***************************************************************************************************/
        public static Tuple<List<Dictionary<string, string>>, double> GSMHrtoaAverage(
            List<Dictionary<string, string>> extractedData)
        {
            if (extractedData == null || !extractedData.Any())
            {
                return Tuple.Create(new List<Dictionary<string, string>>(), -999.0);
            }

            // Helper to calculate Euclidean distance between two points
            double CalculateSqrDistance(Dictionary<string, string> p1, Dictionary<string, string> p2)
            {
                if (!p1.TryGetValue("latitude", out var latStr1) || !p1.TryGetValue("longitude", out var lonStr1) ||
                    !p2.TryGetValue("latitude", out var latStr2) || !p2.TryGetValue("longitude", out var lonStr2) ||
                    !double.TryParse(latStr1, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat1) ||
                    !double.TryParse(lonStr1, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon1) ||
                    !double.TryParse(latStr2, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat2) ||
                    !double.TryParse(lonStr2, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon2))
                {
                    return double.MaxValue;
                }
                // Convert degree distance to meters
                return (Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2)) * MainModule.METERS_PER_DEGREE * MainModule.METERS_PER_DEGREE;
            }

            var updatedPoints = new List<Dictionary<string, string>>();
            double maxAvgHrToA = -999.0;
            double distanceThresholdSquared = MainModule.DISTANCE_THRESH * MainModule.DISTANCE_THRESH;

            for (int i = 0; i < extractedData.Count; i++)
            {
                var currentPoint = extractedData[i];
                var adjacentHrToA = new List<double>();

                // Always include the current point itself
                if (currentPoint.TryGetValue("TimeOffset", out var hrtoaStr) && double.TryParse(hrtoaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double hrtoaVal))
                {
                    adjacentHrToA.Add(hrtoaVal);
                }

                // Search up to 5 points before
                for (int j = i - 1, found = 0; j >= 0 && found < 5; j--)
                {
                    var candidate = extractedData[j];
                    double dist = CalculateSqrDistance(currentPoint, candidate);
                    if (dist <= distanceThresholdSquared)
                    {
                        if (candidate.TryGetValue("TimeOffset", out var hrtoaStr2) && double.TryParse(hrtoaStr2, NumberStyles.Any, CultureInfo.InvariantCulture, out double hrtoaVal2))
                        {
                            adjacentHrToA.Add(hrtoaVal2);
                        }
                        found++;
                    }
                    else
                    {
                        break; // Stop searching if next point is too far
                    }
                }

                // Search up to 5 points after
                for (int j = i + 1, found = 0; j < extractedData.Count && found < 5; j++)
                {
                    var candidate = extractedData[j];
                    double dist = CalculateSqrDistance(currentPoint, candidate);
                    if (dist <= distanceThresholdSquared)
                    {
                        if (candidate.TryGetValue("TimeOffset", out var hrtoaStr2) && double.TryParse(hrtoaStr2, NumberStyles.Any, CultureInfo.InvariantCulture, out double hrtoaVal2))
                        {
                            adjacentHrToA.Add(hrtoaVal2);
                        }
                        found++;
                    }
                    else
                    {
                        break; // Stop searching if next point is too far
                    }
                }

                // Compute average HrToA for this point
                double avgHrToA = adjacentHrToA.Any() ? adjacentHrToA.Average() : -999.0;
                int countAvgHrToA = adjacentHrToA.Count;
                currentPoint["AvgHrToA"] = avgHrToA.ToString("G17", CultureInfo.InvariantCulture);
                currentPoint["CountAvgHrToA"] = countAvgHrToA.ToString();
                // Initialize cinr to the CountAvgHrToA value
                currentPoint["cinr"] = countAvgHrToA.ToString();
                updatedPoints.Add(new Dictionary<string, string>(currentPoint));
                if (avgHrToA > maxAvgHrToA) maxAvgHrToA = avgHrToA;
            }

            // Remove any rows with cinr below GSM_AvgHrToA_COUNT
            var gsmCountThreshold = MainModule.GSM_AvgHrToA_COUNT;
            var filteredPoints = updatedPoints
                .Where(row => row.TryGetValue("cinr", out var cinrStr) && double.TryParse(cinrStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double cinrVal) && cinrVal >= gsmCountThreshold)
                .ToList();
            return Tuple.Create(filteredPoints, maxAvgHrToA);
        }


        /***************************************************************************************************
        *
        *   Function:       ExtractPointsWithDistance
        *
        *   Description:    Processes a list of data points for a single cell, filtering them based on
        *                   geographic distance and signal quality (CINR). It ensures that the selected
        *                   points are not too close to each other, picking the one with the best CINR
        *                   if they are. This helps to select geographically distinct points with strong
        *                   signals, which is crucial for accurate location estimation algorithms.
        *
        *   Input:          extractedData (List<...>) - Data points for a single cell.
        *                   distanceThreshold (double) - The minimum distance between selected points.
        *                   maxPoints (int) - The maximum number of points to return.
        *                   metersPerDegree (double) - Conversion factor for distance calculation.
        *
        *   Output:         A tuple containing the filtered list of points and the average CINR found.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static Tuple<List<Dictionary<string, string>>, double> ExtractPointsWithDistance(
            List<Dictionary<string, string>> extractedData)
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
                return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2)) * MainModule.METERS_PER_DEGREE;
            }

                for (int i = 1; i < extractedData.Count; ++i)
                {
                    if (selectedPoints.Count >= MainModule.MAX_POINTS)
                    {
                        break;
                    }
                    var currentPoint = extractedData[i];
                    var lastSelectedPoint = selectedPoints.Last();

                    double distance = CalculateDistance(currentPoint, lastSelectedPoint);
                    if (distance < 0) continue;

                    if (distance < MainModule.DISTANCE_THRESH)
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

            double avgCinr = selectedPoints
                .Select(row => row.TryGetValue("cinr", out var cinrStr) && double.TryParse(cinrStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double cinr) ? cinr : -999.0)
                .DefaultIfEmpty(-999.0)
                .Average();

            // Check if all remaining points have the same TimeOffset
            if (selectedPoints.Count > 1)
            {
                var distinctTimeOffsets = selectedPoints.Select(p => p.GetValueOrDefault("TimeOffset")).Distinct().Count();
                if (distinctTimeOffsets == 1)
                {
                    string channel = selectedPoints.First().GetValueOrDefault("channel", "N/A");
                    string cellId = selectedPoints.First().GetValueOrDefault("cellId", "N/A");
#if DEBUG
                    Console.WriteLine($"Removing cell {cellId} on channel {channel} because all points have an identical TimeOffset.");
#endif
                    return Tuple.Create(new List<Dictionary<string, string>>(), -999.0); // Return empty list
                }
            }

            return Tuple.Create(selectedPoints, avgCinr);
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
            int fileType
            )
        {
            if (data == null || !data.Any())
            {
                return new List<Dictionary<string, string>>();
            }

            bool isNr = fileType == NR_TOPN_FILE_TYPE || fileType == NR_FILE_TYPE || fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;
            bool isWcdma = fileType == WCDMA_FILE_TYPE_CSV || fileType == WCDMA_FILE_TYPE_DTR;
            bool isGsm = fileType == GSM_FILE_TYPE || fileType == GSM_FILE_TYPE * 10;

            double wrapValue;
            double samplingRateHz;

            if (isNr)
            {
                const double ssbPeriod = 20e-3; // 20 ms
                wrapValue = MainModule.LTE_SAMPLING_RATE_HZ * MainModule.NR_SAMPLING_RATE_MULTIPLIER * ssbPeriod;
                samplingRateHz = MainModule.LTE_SAMPLING_RATE_HZ * MainModule.NR_SAMPLING_RATE_MULTIPLIER;
            }
            else if (isWcdma)
            {
                wrapValue = MainModule.WCDMA_TIME_OFFSET_WRAP_VALUE;
                samplingRateHz = MainModule.LTE_SAMPLING_RATE_HZ / MainModule.WCDMA_SAMPLING_RATE_DIVISOR;
            }
            else if (isGsm)
            {
                wrapValue = MainModule.TIME_OFFSET_WRAP_VALUE;
                samplingRateHz = MainModule.GSM_SAMPLING_RATE_HZ;
            }
            else // LTE cases (1, 2, 10, 20) and any other defaults
            {
                wrapValue = MainModule.TIME_OFFSET_WRAP_VALUE;
                samplingRateHz = MainModule.LTE_SAMPLING_RATE_HZ;
            }

            var timeOffsets = data.Select(row =>
                row.TryGetValue("TimeOffset", out var tsStr) &&
                double.TryParse(tsStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double ts) ? ts : 0.0)
                .ToList();

            if (!timeOffsets.Any()) return data;

            double tsMin = timeOffsets.Min();
            double tsMax = timeOffsets.Max();

            // Adjust for wrapping
            if (tsMin < wrapValue / 4.0 && tsMax > wrapValue * 3.0 / 4.0 && !isGsm)
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
            if (isGsm)
            {
                foreach (var row in data)
                {
                    if (row.TryGetValue("AvgHrToA", out var tsStr) &&
                        double.TryParse(tsStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double ts))
                    {
                        // Use "G17" format specifier for full double precision
                        row["TimeOffset"] = (ts / samplingRateHz).ToString("G17", CultureInfo.InvariantCulture);
                    }
                }
            }
            else
            {
                foreach (var row in data)
                {
                    if (row.TryGetValue("TimeOffset", out var tsStr) &&
                        double.TryParse(tsStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double ts))
                    {
                        // Use "G17" format specifier for full double precision
                        row["TimeOffset"] = (ts / samplingRateHz).ToString("G17", CultureInfo.InvariantCulture);
                    }
                }
            }

            return data;
        }

        /***************************************************************************************************
        *
        *   Function:       splitCellid
        *
        *   Description:    For NR Blind Scan files (fileType = 4), this function takes the composite
        *                   Cell ID (which includes the Beam Index) and splits it back into separate
        *                   'CellId' and 'BeamIndex' fields in the final results.
        *                   For non-NR files, it attempts to restore the original CellId by applying a modulo
        *                   operation to handle any encoding or scaling applied previously.
        *                   This ensures consistent CellId formatting across different technologies.
        *                   The function returns a new list with updated dictionaries accordingly.
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
        public static List<Dictionary<string, string>> splitCellid(int fileType, List<Dictionary<string, string>> estimationResults)
        {
            bool isNrFile = fileType == NR_TOPN_FILE_TYPE || fileType == NR_FILE_TYPE || fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;
            var newEstimationResults = new List<Dictionary<string, string>>();
            foreach (var result in estimationResults)
            {
                if (isNrFile)
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
                else
                {
                    // For non-NR files, restore cellId by cellId % 100 (use double integer if needed)
                    if (long.TryParse(result["CellId"], out long cellIdLong))
                    {
                        long restoredCellId = cellIdLong % 1000;
                        var newResult = new Dictionary<string, string>(result);
                        newResult["CellId"] = restoredCellId.ToString();
                        newEstimationResults.Add(newResult);
                    }
                    else
                    {
                        newEstimationResults.Add(result);
                    }
                }
            }
            return newEstimationResults;
        }

        /***************************************************************************************************
        *
        *   Function:       AddTowerEstimate
        *
        *   Description:    Identifies groups of cells (towers) based on their channel and consecutive
        *                   cell identities. It creates new summary entries for these groups by averaging
        *                   their locations and combining their IDs.
        *
        *   Input:          resultsWithBeamIndex (List<...>) - The list of individual cell estimation results.
        *                   fileType (int) - The type of the processed file.
        *
        *   Output:         A new list containing both the original results and the new summary "tower"
        *                   entries, sorted by channel and cell identity.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static List<Dictionary<string, string>> AddTowerEstimate(List<Dictionary<string, string>> resultsWithBeamIndex, int fileType, string mode)
        {
            // Add "Type" column and initialize to "Sector" for all modes except Cluster (TBD)

            foreach (var row in resultsWithBeamIndex)
            {
                row["Type"] = "Sector";
            }


            var initialSorted = resultsWithBeamIndex
                .OrderBy(d => int.TryParse(d["Channel"], out int ch) ? ch : int.MaxValue)
                .ThenBy(d => d.GetValueOrDefault("cellIdentity", string.Empty))
                .ToList();

            switch (mode)
            {
                case "Sector":
                    // In "Sector" mode, we just add the Type and sort.
                    return initialSorted;

                case "Tower":
                    // In "Tower" mode, we proceed with the full tower estimation logic.
                    var newTowerEstimates = new List<Dictionary<string, string>>();
                    var processedIndices = new HashSet<int>();

                    var groupedByChannel = initialSorted
                        .Select((value, index) => new { value, index })
                        .GroupBy(x => x.value.GetValueOrDefault("Channel"));

                    bool isNrFile = fileType == NR_TOPN_FILE_TYPE || fileType == NR_FILE_TYPE || fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;

                    foreach (var channelGroup in groupedByChannel)
                    {
                        var items = channelGroup.ToList();
                        if (items.Count < 2) continue;

                        bool NRMultiBeam = false;

                        // --- Pass 0: Find NR groups with same CellId and CellIdentity ---
                        if (isNrFile)
                        {
                            var remainingItems = items.Where(item => !processedIndices.Contains(item.index)).ToList();

                            var sameIdGroups = remainingItems
                                .Where(item => !string.IsNullOrWhiteSpace(item.value.GetValueOrDefault("cellIdentity")))
                                .GroupBy(item => new
                                {
                                    CellId = item.value.GetValueOrDefault("CellId"),
                                    CellIdentity = item.value.GetValueOrDefault("cellIdentity")
                                })
                                .Where(g => g.Count() > 1)
                                .ToList();

                            if (sameIdGroups.Any())
                            {
                                NRMultiBeam = true;
                            }

                            foreach (var idGroup in sameIdGroups)
                            {
                                var groupItems = idGroup.Select(g => g.value).ToList();

                                newTowerEstimates.Add(CreateNrBeamTowerEstimate(groupItems));

                                // Mark these items as processed
                                foreach (var item in idGroup)
                                {
                                    processedIndices.Add(item.index);
                                }
                            }
                        }

                        // Only run the next two passes if NRMultiBeam is false
                        if (!NRMultiBeam)
                        {
                            // --- Pass 1: Find groups of three ---
                            for (int i = 0; i < items.Count - 2; i++)
                            {
                                if (processedIndices.Contains(items[i].index)) continue;

                                for (int j = i + 1; j < items.Count - 1; j++)
                                {
                                    if (processedIndices.Contains(items[j].index)) continue;

                                    for (int k = j + 1; k < items.Count; k++)
                                    {
                                        if (processedIndices.Contains(items[k].index)) continue;

                                        var p1 = items[i].value;
                                        var p2 = items[j].value;
                                        var p3 = items[k].value;

                                        if (p1.TryGetValue("cellIdentity", out var idStr1) && long.TryParse(idStr1, out long id1) &&
                                            p2.TryGetValue("cellIdentity", out var idStr2) && long.TryParse(idStr2, out long id2) &&
                                            p3.TryGetValue("cellIdentity", out var idStr3) && long.TryParse(idStr3, out long id3) )
                                        {
                                            // Accept increments of 1 or 10 for triplets
                                            bool isGroupOfThree =
                                                // (a, b, c) consecutive by 1
                                                (id2 == id1 + 1 && id3 == id2 + 1) || // (1,2,3)
                                                (id2 == id1 + 1 && id3 == id2 + 2) || // (1,2,4)
                                                (id2 == id1 + 2 && id3 == id2 + 1) || // (1,3,4)
                                                // increments of 10
                                                (id2 == id1 + 10 && id3 == id2 + 10) || // (10,20,30)
                                                (id2 == id1 + 10 && id3 == id2 + 20) || // (10,20,40)
                                                (id2 == id1 + 20 && id3 == id2 + 10);   // (10,30,40)

                                            if (isGroupOfThree)
                                            {
                                                var group = new List<Dictionary<string, string>> { p1, p2, p3 };
                                                newTowerEstimates.Add(CreateTowerEstimate(group, isNrFile));
                                                processedIndices.Add(items[i].index);
                                                processedIndices.Add(items[j].index);
                                                processedIndices.Add(items[k].index);
                                                goto next_i_loop_3; // Continue outer loop
                                            }
                                        }
                                    }
                                }
                            next_i_loop_3:;
                            }

                            // --- Pass 2: Find groups of two ---
                            for (int i = 0; i < items.Count - 1; i++)
                            {
                                if (processedIndices.Contains(items[i].index)) continue;

                                for (int j = i + 1; j < items.Count; j++)
                                {
                                    if (processedIndices.Contains(items[j].index)) continue;

                                    var p1 = items[i].value;
                                    var p2 = items[j].value;

                                    if (p1.TryGetValue("cellIdentity", out var idStr1) && long.TryParse(idStr1, out long id1) &&
                                        p2.TryGetValue("cellIdentity", out var idStr2) && long.TryParse(idStr2, out long id2))
                                    {
                                        // Accept increments of 1, 2, 10, or 20 for pairs
                                        bool isGroupOfTwo = (id2 == id1 + 1) || // (1,2)
                                                           (id2 == id1 + 2) || // (1,3)
                                                           (id2 == id1 + 10) || // (10,20)
                                                           (id2 == id1 + 20);  // (10,30)

                                        if (isGroupOfTwo)
                                        {
                                            var group = new List<Dictionary<string, string>> { p1, p2 };
                                            newTowerEstimates.Add(CreateTowerEstimate(group, isNrFile));
                                            processedIndices.Add(items[i].index);
                                            processedIndices.Add(items[j].index);
                                            goto next_i_loop_2; // Continue outer loop
                                        }
                                    }
                                }
                            next_i_loop_2:;
                            }
                        }
                    }

                    var combinedResults = initialSorted.Concat(newTowerEstimates).ToList();

                    // Final sort for Tower mode
                    return combinedResults
                        .OrderBy(d => int.TryParse(d["Channel"], out int ch) ? ch : int.MaxValue)
                        .ThenBy(d => d.GetValueOrDefault("cellIdentity", string.Empty))
                        .ToList();

                case "Cluster":
                    #if DEBUG
                    Console.WriteLine("Warning: 'Cluster' mode is not yet implemented. Returning original data.");
                    #endif
                    return resultsWithBeamIndex;

                default:
                    #if DEBUG
                    Console.WriteLine($"Warning: Unknown mode '{mode}'. Returning original data.");
                    #endif
                    return resultsWithBeamIndex;
            }
        }

        /***************************************************************************************************
        *
        *   Function:       CreateNrBeamTowerEstimate
        *
        *   Description:    Aggregates a group of NR sectors (with the same CellId and CellIdentity) into a single
        *                   "Tower" entry by averaging their estimated locations and combining their beam indices.
        *                   This is used for NR (5G) blind scan results, where multiple beams (sectors) share the same
        *                   physical cell but have different beam indices. The function calculates the centroid of all
        *                   sectors in the group and creates a summary dictionary entry for the tower.
        *
        *   Input:          group (List<Dictionary<string, string>>) - List of sector entries with the same CellId and CellIdentity.
        *
        *   Output:         Dictionary<string, string> representing the aggregated tower entry, with averaged location fields
        *                   and combined beam indices.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 10, 2025
        *
        ***************************************************************************************************/
        private static Dictionary<string, string> CreateNrBeamTowerEstimate(List<Dictionary<string, string>> group)
        {
            double avgLat1 = group.Average(d => double.Parse(d["est_Lat1"], CultureInfo.InvariantCulture));
            double avgLon1 = group.Average(d => double.Parse(d["est_Lon1"], CultureInfo.InvariantCulture));
            double avgLat2 = group.Average(d => double.Parse(d["est_Lat2"], CultureInfo.InvariantCulture));
            double avgLon2 = group.Average(d => double.Parse(d["est_Lon2"], CultureInfo.InvariantCulture));

            // CellId and CellIdentity are the same for all in the group
            string cellId = group[0]["CellId"];
            string cellIdentity = group[0]["cellIdentity"];

            // Concatenate beam indices, mnc, and mcc
            string combinedBeamIndex = string.Join("/", group.Select(d => d.GetValueOrDefault("BeamIndex", "")));
            string combinedMnc = string.Join("/", group.Select(d => d.GetValueOrDefault("mnc", "")));
            string combinedMcc = string.Join("/", group.Select(d => d.GetValueOrDefault("mcc", "")));

            var towerConfidence = group.Any(d => d.GetValueOrDefault("Confidence") == "Low") ? "Low" : "High";
            var newEstimate = new Dictionary<string, string>
            {
                { "Channel", group[0]["Channel"] },
                { "CellId", cellId },
                { "BeamIndex", combinedBeamIndex },
                { "mnc", combinedMnc },
                { "mcc", combinedMcc },
                { "Type", "Tower" },
                { "cellIdentity", cellIdentity },
                { "xhat1", "0" },
                { "yhat1", "0" },
                { "xhat2", "0" },
                { "yhat2", "0" },
                { "est_Lat1", avgLat1.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lon1", avgLon1.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lat2", avgLat2.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lon2", avgLon2.ToString("F6", CultureInfo.InvariantCulture) },
                { "Max_cinr", group.Max(d => double.Parse(d["Max_cinr"], CultureInfo.InvariantCulture)).ToString("F2", CultureInfo.InvariantCulture) },
                { "Num_points", group.Sum(d => int.Parse(d["Num_points"])).ToString() },
                { "Confidence", towerConfidence }
            };
            return newEstimate;
        }

        private static Dictionary<string, string> CreateTowerEstimate(List<Dictionary<string, string>> group, bool isNrFile)
        {
            double avgLat1 = group.Average(d => double.Parse(d["est_Lat1"], CultureInfo.InvariantCulture));
            double avgLon1 = group.Average(d => double.Parse(d["est_Lon1"], CultureInfo.InvariantCulture));
            double avgLat2 = group.Average(d => double.Parse(d["est_Lat2"], CultureInfo.InvariantCulture));
            double avgLon2 = group.Average(d => double.Parse(d["est_Lon2"], CultureInfo.InvariantCulture));

            string combinedCellId = string.Join("/", group.Select(d => d["CellId"]));

            // Format cellIdentity as "firstId_last3_last3"
            string combinedCellIdentity = string.Empty;
            if (group.Any())
            {
                var identities = group.Select(d => d["cellIdentity"]).ToList();
                string firstId = identities.First();
                var subsequentLastThree = identities.Skip(1).Select(id => id.Length >= 3 ? id.Substring(id.Length - 3) : id);
                if (subsequentLastThree.Any())
                {
                    combinedCellIdentity = $"{firstId}_{string.Join("_", subsequentLastThree)}";
                }
                else
                {
                    combinedCellIdentity = firstId;
                }
            }

            string beamIndexValue = ""; // Default to empty
            string combinedMnc = string.Join("/", group.Select(d => d.GetValueOrDefault("mnc", "")));
            string combinedMcc = string.Join("/", group.Select(d => d.GetValueOrDefault("mcc", "")));
            if (isNrFile)
            {
                beamIndexValue = string.Join("/", group.Select(d => d.GetValueOrDefault("BeamIndex", "")));
            }

            var towerConfidence = group.Any(d => d.GetValueOrDefault("Confidence") == "Low") ? "Low" : "High";
            var newEstimate = new Dictionary<string, string>
            {
                { "Channel", group[0]["Channel"] },
                { "CellId", combinedCellId },
                { "BeamIndex", beamIndexValue },
                { "mnc", combinedMnc },
                { "mcc", combinedMcc },
                { "Type", "Tower" },
                { "cellIdentity", combinedCellIdentity },
                { "xhat1", "0" },
                { "yhat1", "0" },
                { "xhat2", "0" },
                { "yhat2", "0" },
                { "est_Lat1", avgLat1.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lon1", avgLon1.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lat2", avgLat2.ToString("F6", CultureInfo.InvariantCulture) },
                { "est_Lon2", avgLon2.ToString("F6", CultureInfo.InvariantCulture) },
                // Add other fields with default/aggregated values if needed
                { "Max_cinr", group.Max(d => double.Parse(d["Max_cinr"], CultureInfo.InvariantCulture)).ToString("F2", CultureInfo.InvariantCulture) },
                { "Num_points", group.Sum(d => int.Parse(d["Num_points"])).ToString() },
                { "Confidence", towerConfidence }
            };
            return newEstimate;
        }

        /***************************************************************************************************
        *
        *   Function:       DBSCAN_Cluster
        *
        *   Description:    Performs clustering on location estimation results using the DBSCAN algorithm.
        *                   DBSCAN (Density-Based Spatial Clustering of Applications with Noise) groups points
        *                   that are closely packed together (points with many nearby neighbors) and marks points
        *                   that lie alone in low-density regions as outliers (noise). This implementation uses
        *                   latitude/longitude (est_Lat2/est_Lon2) and clusters points within a specified distance
        *                   threshold (eps_miles). Each cluster's centroid is calculated and returned as a new entry.
        *
        *   Algorithm Details:
        *                   - Each point is classified as a core point, border point, or noise.
        *                   - Core points have at least minPts neighbors within eps_miles.
        *                   - Clusters are formed by connecting core points and their neighbors.
        *                   - Outliers (noise) are points not belonging to any cluster.
        *                   - The function returns a list of cluster entries, each representing the centroid of a cluster.
        *
        *   Pros:
        *                   - Can find arbitrarily shaped clusters.
        *                   - Does not require specifying the number of clusters in advance.
        *                   - Robust to noise and outliers.
        *                   - Suitable for spatial/geographic data.
        *
        *   Cons:
        *                   - Sensitive to the choice of eps_miles and minPts parameters.
        *                   - Struggles with clusters of varying density.
        *                   - Distance calculation assumes Euclidean geometry (approximate for lat/lon).
        *                   - Performance may degrade for very large datasets.
        *
        *   Input:          data (List<Dictionary<string, string>>) - List of location estimation results.
        *                   eps_miles (double) - Maximum distance (in miles) for points to be considered neighbors.
        *                   minPts (int) - Minimum number of points required to form a cluster.
        *
        *   Output:         List of cluster entries (each as Dictionary<string, string>) representing cluster centroids.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 10, 2025
        *
        ***************************************************************************************************/
        public static List<Dictionary<string, string>> DBSCAN_Cluster(List<Dictionary<string, string>> data, double eps_miles = .50, int minPts = 4)
        {
            // Convert miles to degrees (approximate)
            double eps_degrees = eps_miles / 69.0; // 1 degree ~ 69 miles
            var clusters = new List<List<Dictionary<string, string>>>();
            var visited = new HashSet<int>();
            var noise = new HashSet<int>();
            var clusterLabels = new int[data.Count];
            Array.Fill(clusterLabels, -1);
            int clusterId = 0;

            double Distance(Dictionary<string, string> a, Dictionary<string, string> b)
            {
                double lat1 = double.Parse(a["est_Lat2"]);
                double lon1 = double.Parse(a["est_Lon2"]);
                double lat2 = double.Parse(b["est_Lat2"]);
                double lon2 = double.Parse(b["est_Lon2"]);
                double dLat = lat2 - lat1;
                double dLon = lon2 - lon1;
                return Math.Sqrt(dLat * dLat + dLon * dLon);
            }

            for (int i = 0; i < data.Count; i++)
            {
                if (visited.Contains(i)) continue;
                visited.Add(i);
                var neighbors = new List<int>();
                for (int j = 0; j < data.Count; j++)
                {
                    if (Distance(data[i], data[j]) <= eps_degrees)
                        neighbors.Add(j);
                }
                if (neighbors.Count < minPts)
                {
                    noise.Add(i);
                    continue;
                }
                clusters.Add(new List<Dictionary<string, string>>());
                clusterLabels[i] = clusterId;
                clusters[clusterId].Add(data[i]);
                var seeds = new Queue<int>(neighbors);
                while (seeds.Count > 0)
                {
                    int curr = seeds.Dequeue();
                    if (!visited.Contains(curr))
                    {
                        visited.Add(curr);
                        var currNeighbors = new List<int>();
                        for (int k = 0; k < data.Count; k++)
                        {
                            if (Distance(data[curr], data[k]) <= eps_degrees)
                                currNeighbors.Add(k);
                        }
                        if (currNeighbors.Count >= minPts)
                        {
                            foreach (var n in currNeighbors)
                                if (clusterLabels[n] == -1) seeds.Enqueue(n);
                        }
                    }
                    if (clusterLabels[curr] == -1)
                    {
                        clusterLabels[curr] = clusterId;
                        clusters[clusterId].Add(data[curr]);
                    }
                }
                clusterId++;
            }

            // Create cluster entries
            var clusterEntries = new List<Dictionary<string, string>>();
            foreach (var cluster in clusters)
            {
                if (cluster.Count == 0) continue;
                var entry = new Dictionary<string, string>();
                entry["Technology"] = string.Join("/", cluster.Select(p => p.GetValueOrDefault("Technology", "")));
                entry["Channel"] = string.Join("/", cluster.Select(p => p.GetValueOrDefault("Channel", "")));
                entry["CellId"] = string.Join("/", cluster.Select(p => p.GetValueOrDefault("CellId", "")));
                entry["BeamIndex"] = string.Join("/", cluster.Select(p => p.GetValueOrDefault("BeamIndex", "")));
                entry["Type"] = "cluster entry";
                entry["cellIdentity"] = string.Join("/", cluster.Select(p => p.GetValueOrDefault("cellIdentity", "")));
                entry["mnc"] = string.Join("/", cluster.Select(p => p.GetValueOrDefault("mnc", "")));
                entry["mcc"] = string.Join("/", cluster.Select(p => p.GetValueOrDefault("mcc", "")));
                entry["xhat1"] = "0";
                entry["yhat1"] = "0";
                entry["xhat2"] = "0";
                entry["yhat2"] = "0";
                entry["est_Lat1"] = "0";
                entry["est_Lon1"] = "0";
                entry["est_Lat2"] = cluster.Average(p => double.Parse(p["est_Lat2"])).ToString("F6", CultureInfo.InvariantCulture);
                entry["est_Lon2"] = cluster.Average(p => double.Parse(p["est_Lon2"])).ToString("F6", CultureInfo.InvariantCulture);
                entry["Max_cinr"] = "0";
                entry["Num_points"] = cluster.Count.ToString();
                entry["Confidence"] = "0";
                clusterEntries.Add(entry);
            }
            return clusterEntries;
        }
    /***************************************************************************************************
    *
    *   Function:       Expand_mcc_mnc_cellIdentity
    *
    *   Description:    For each channel/cellId pair, expands mcc/mnc and cellIdentity fields.
    *                   - mcc/mnc expansion: Finds the first non-blank/non-zero mcc/mnc in the group and propagates
    *                     those values to all rows in the group, both forward and backward.
    *                   - cellIdentity expansion: Finds the first non-blank cellIdentity and propagates it backward,
    *                     then sequentially propagates the latest non-blank cellIdentity forward through the group.
    *                   This ensures that mcc/mnc are unified for each group, while cellIdentity tracks changes
    *                   and propagates them as they appear in the data.
    *
    *   Input:          allData (List<Dictionary<string, string>>) - The full list of extracted data.
    *
    *   Output:         The updated list with expanded mcc, mnc, and cellIdentity fields.
    *
    *   Author:         Amir Soltanian
    *
    *   Date:           September 12, 2025
    *
    ***************************************************************************************************/
    public static List<Dictionary<string, string>> Expand_mcc_mnc_cellIdentity(List<Dictionary<string, string>> allData)
        {
            if (allData == null || allData.Count == 0) return allData!;
            // Group by channel and cellId
            var groups = allData.GroupBy(row => new {
                Channel = row.GetValueOrDefault("channel", ""),
                CellId = row.GetValueOrDefault("cellId", "")
            });
            foreach (var group in groups)
            {
                // --- Expand mcc/mnc both forward and backward ---
                int firstMccMncIdx = -1;
                string mccValue = "";
                string mncValue = "";
                var groupList = group.ToList();
                for (int i = 0; i < groupList.Count; i++)
                {
                    var row = groupList[i];
                    var mcc = row.GetValueOrDefault("mcc", "");
                    var mnc = row.GetValueOrDefault("mnc", "");
                    if (!string.IsNullOrWhiteSpace(mcc) && mcc != "0" && !string.IsNullOrWhiteSpace(mnc) && mnc != "0")
                    {
                        firstMccMncIdx = i;
                        mccValue = mcc;
                        mncValue = mnc;
                        break;
                    }
                }
                if (firstMccMncIdx != -1)
                {
                    // Fill backward
                    for (int i = 0; i <= firstMccMncIdx; i++)
                    {
                        groupList[i]["mcc"] = mccValue;
                        groupList[i]["mnc"] = mncValue;
                    }
                    // Fill forward
                    for (int i = firstMccMncIdx + 1; i < groupList.Count; i++)
                    {
                        groupList[i]["mcc"] = mccValue;
                        groupList[i]["mnc"] = mncValue;
                    }
                }

                // --- Expand cellIdentity both forward and backward ---
                int firstCellIdentityIdx = -1;
                string cellIdentityValue = "";
                for (int i = 0; i < groupList.Count; i++)
                {
                    var cellIdentity = groupList[i].GetValueOrDefault("cellIdentity", "");
                    if (!string.IsNullOrWhiteSpace(cellIdentity))
                    {
                        firstCellIdentityIdx = i;
                        cellIdentityValue = cellIdentity;
                        break;
                    }
                }
                if (firstCellIdentityIdx != -1)
                {
                    // Fill backward
                    for (int i = 0; i <= firstCellIdentityIdx; i++)
                    {
                        groupList[i]["cellIdentity"] = cellIdentityValue;
                    }
                    // Fill forward
                    string lastCellIdentity = cellIdentityValue;
                    for (int i = firstCellIdentityIdx + 1; i < groupList.Count; i++)
                    {
                        var cellIdentity = groupList[i].GetValueOrDefault("cellIdentity", "");
                        if (!string.IsNullOrWhiteSpace(cellIdentity))
                        {
                            lastCellIdentity = cellIdentity;
                        }
                        groupList[i]["cellIdentity"] = lastCellIdentity;
                    }
                }
            }
            return allData!;
        }
    }
 



}
