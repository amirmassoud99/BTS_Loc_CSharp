using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BTS_Location_Estimation
{
    public static class DataBaseProc
    {
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
    }
}
