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
    }
}
