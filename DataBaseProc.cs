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
        public const int WCDMA_FILE_TYPE_DTR = 50;

        /***************************************************************************************************
        *
        *   Function:       filter_cinr_minimum_PCI
        *
        *   Description:    Filters the extracted data based on two criteria:
        *                   1. Signal Strength: Removes rows where the CINR is below a specified threshold.
        *                   2. Minimum Count: Removes entire groups of (Channel, CellId) if they do not
        *                      have at least a minimum number of data points after the CINR filtering.
        *
        *   Input:          allData (List<...>) - The full list of extracted data.
        *                   cinrThresh (double) - The minimum required CINR value.
        *                   minimumCellIdCount (int) - The minimum number of rows for a cell to be kept.
        *
        *   Output:         A new list containing only the data that passed both filtering stages.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
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
        *   Output:         A tuple containing the filtered list of points and the maximum CINR found.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
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

            bool isNr = fileType == NR_TOPN_FILE_TYPE || fileType == NR_FILE_TYPE || fileType == NR_TOPN_FILE_TYPE * 10 || fileType == NR_FILE_TYPE * 10;
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
        public static List<Dictionary<string, string>> splitCellidBeamforNR(int fileType, List<Dictionary<string, string>> estimationResults)
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
        public static List<Dictionary<string, string>> AddTowerEstimate(List<Dictionary<string, string>> resultsWithBeamIndex, int fileType)
        {
            var initialSorted = resultsWithBeamIndex
                .OrderBy(d => int.TryParse(d["Channel"], out int ch) ? ch : int.MaxValue)
                .ThenBy(d => d.GetValueOrDefault("cellIdentity", string.Empty))
                .ToList();

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

                            if (long.TryParse(p1["cellIdentity"], out long id1) &&
                                long.TryParse(p2["cellIdentity"], out long id2) &&
                                long.TryParse(p3["cellIdentity"], out long id3))
                            {
                                bool isGroupOfThree = (id2 == id1 + 1 && id3 == id2 + 1) || // (1,2,3)
                                                      (id2 == id1 + 1 && id3 == id2 + 2) || // (1,2,4)
                                                      (id2 == id1 + 2 && id3 == id2 + 1);   // (1,3,4)

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

                        if (long.TryParse(p1["cellIdentity"], out long id1) &&
                            long.TryParse(p2["cellIdentity"], out long id2))
                        {
                            bool isGroupOfTwo = (id2 == id1 + 1) || // (1,2)
                                                  (id2 == id1 + 2);   // (1,3)

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

            var combinedResults = initialSorted.Concat(newTowerEstimates).ToList();

            // Final sort
            var finalSortedResults = combinedResults
                .OrderBy(d => int.TryParse(d["Channel"], out int ch) ? ch : int.MaxValue)
                .ThenBy(d => d.GetValueOrDefault("cellIdentity", string.Empty))
                .ToList();

            return finalSortedResults;
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

            string beamIndexValue = "Tower";
            if (isNrFile)
            {
                beamIndexValue = string.Join("/", group.Select(d => d.GetValueOrDefault("BeamIndex", "")));
            }

            var newEstimate = new Dictionary<string, string>
            {
                { "Channel", group[0]["Channel"] },
                { "CellId", combinedCellId },
                { "BeamIndex", beamIndexValue }, // To identify these as tower estimates
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
                { "Confidence", "High" } // Or determine based on logic
            };
            return newEstimate;
        }
    }
}
