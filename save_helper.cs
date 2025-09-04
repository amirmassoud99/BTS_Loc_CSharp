using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BTS_Location_Estimation
{
    public static class SaveHelper
    {
        public static void save_estimation_results(List<Dictionary<string, string>> estimationResults, string outputFilename)
        {
            if (!estimationResults.Any())
            {
                Console.WriteLine($"No estimation results to save to {outputFilename}");
                return;
            }

            // Delete the file if it exists to ensure a clean run
            if (File.Exists(outputFilename))
            {
                File.Delete(outputFilename);
            }

            // Define the core headers in the desired order.
            var headers = new List<string>
            {
                "Channel", "CellId", "cellIdentity", "xhat1", "yhat1", "xhat2", "yhat2",
                "est_Lat1", "est_Lon1", "est_Lat2", "est_Lon2", "Max_cinr", "Num_points", "Confidence"
            };

            // Check if any result contains a BeamIndex to determine if the column should be added.
            bool hasBeamIndex = estimationResults.Any(r => r.ContainsKey("BeamIndex"));

            if (hasBeamIndex)
            {
                // Insert "BeamIndex" at the correct position (after "CellId").
                int cellIdIndex = headers.IndexOf("CellId");
                headers.Insert(cellIdIndex + 1, "BeamIndex");
            }

            using (var writer = new StreamWriter(outputFilename))
            {
                writer.WriteLine(string.Join(",", headers));

                foreach (var result in estimationResults)
                {
                    var values = headers.Select(header => result.GetValueOrDefault(header, ""));
                    writer.WriteLine(string.Join(",", values));
                }
            }
            Console.WriteLine($"\nFinal estimation results saved to {outputFilename}");

            // Generate map files per channel
            var resultsByChannel = estimationResults.GroupBy(r => r.GetValueOrDefault("Channel", "NoChannel"));

            foreach (var channelGroup in resultsByChannel)
            {
                string channel = channelGroup.Key;
                var channelResults = channelGroup.ToList();
                if (!channelResults.Any()) continue;

                string baseOutputFilename = Path.GetFileNameWithoutExtension(outputFilename).Replace("Estimate_", "");
                string mapBaseFilename = $"map_ch{channel}_{baseOutputFilename}";
                
                string directory = Path.GetDirectoryName(outputFilename) ?? string.Empty;
                string mapCsvFilename = Path.Combine(directory, mapBaseFilename + ".csv");
                string mapKmlFilename = Path.Combine(directory, mapBaseFilename + ".kml");

                generate_map_csv(channelResults, mapCsvFilename);
                generate_map_kml(channelResults, mapKmlFilename);
            }
        }

        private static void generate_map_csv(List<Dictionary<string, string>> estimationResults, string mapCsvFilename)
        {
            try
            {
                using (var writer = new StreamWriter(mapCsvFilename))
                {
                    writer.WriteLine("Latitude,Longitude,CellID,CellIdentity,BeamIndex");
                    foreach (var result in estimationResults)
                    {
                        string lat = result.GetValueOrDefault("est_Lat2", "");
                        string lon = result.GetValueOrDefault("est_Lon2", "");
                        string cellId = result.GetValueOrDefault("CellId", "");
                        string cellIdentity = result.GetValueOrDefault("cellIdentity", "");
                        string beamIndex = result.GetValueOrDefault("BeamIndex", "");
                        writer.WriteLine($"{lat},{lon},{cellId},{cellIdentity},{beamIndex}");
                    }
                }
                Console.WriteLine($"Map CSV saved to {mapCsvFilename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating map CSV: {ex.Message}");
            }
        }

        private class Placemark
        {
            public string Lat { get; set; } = "";
            public string Lon { get; set; } = "";
            public string CellId { get; set; } = "";
            public string CellIdentity { get; set; } = "";
            public string BeamInfo { get; set; } = "";
            public string Confidence { get; set; } = "";
            public string StyleId { get; set; } = "red_circle_style"; // Default to red circle
        }

        private static void generate_map_kml(List<Dictionary<string, string>> estimationResults, string mapKmlFilename)
        {
            try
            {
                var cellPlacemarks = new Dictionary<string, Placemark>();
                var towerPlacemarks = new List<Placemark>();

                // First pass: Create all placemark objects
                foreach (var result in estimationResults)
                {
                    string cellId = result.GetValueOrDefault("CellId", "");
                    if (string.IsNullOrEmpty(cellId) || !result.ContainsKey("est_Lat2") || !result.ContainsKey("est_Lon2"))
                    {
                        continue;
                    }

                    var placemark = new Placemark
                    {
                        Lat = result["est_Lat2"],
                        Lon = result["est_Lon2"],
                        CellId = cellId,
                        CellIdentity = result.GetValueOrDefault("cellIdentity", ""),
                        Confidence = result.GetValueOrDefault("Confidence", "N/A"),
                        BeamInfo = result.ContainsKey("BeamIndex") ? $", Beam: {result["BeamIndex"]}" : ""
                    };

                    // Note: Assuming tower CellIds are joined by '_'.
                    if (cellId.Contains("_"))
                    {
                        placemark.StyleId = "blue_pin_style";
                        towerPlacemarks.Add(placemark);
                    }
                    else
                    {
                        placemark.StyleId = "red_balloon_style"; // Default style
                        if (!cellPlacemarks.ContainsKey(cellId))
                        {
                            cellPlacemarks.Add(cellId, placemark);
                        }
                    }
                }

                // Second pass: Update styles for cells that are part of a tower
                foreach (var tower in towerPlacemarks)
                {
                    var individualCellIds = tower.CellId.Split('_');
                    foreach (var id in individualCellIds)
                    {
                        if (cellPlacemarks.TryGetValue(id, out var placemarkToUpdate))
                        {
                            placemarkToUpdate.StyleId = "blue_balloon_style";
                        }
                    }
                }

                // Third pass: Override styles for low confidence points
                foreach (var placemark in cellPlacemarks.Values)
                {
                    if (placemark.Confidence == "Low")
                    {
                        placemark.StyleId = "yellow_balloon_style";
                    }
                }

                string mapName = Path.GetFileNameWithoutExtension(mapKmlFilename);
                using (var writer = new StreamWriter(mapKmlFilename))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                    writer.WriteLine("  <Document>");
                    writer.WriteLine($"    <name>{mapName}</name>");

                    // Style for single points (red circle)
                    writer.WriteLine("    <Style id=\"red_balloon_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/red-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Style for tower member points (blue circle)
                    writer.WriteLine("    <Style id=\"blue_balloon_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/blu-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Style for low confidence points (yellow circle)
                    writer.WriteLine("    <Style id=\"yellow_balloon_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/ylw-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Style for tower points (blue pushpin)
                    writer.WriteLine("    <Style id=\"blue_pin_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/pushpin/blue-pushpin.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"20\" y=\"2\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Write Placemarks
                    var allPlacemarks = cellPlacemarks.Values.Concat(towerPlacemarks);
                    foreach (var p in allPlacemarks)
                    {
                        writer.WriteLine("    <Placemark>");
                        writer.WriteLine($"      <name>{p.CellId}</name>");
                        writer.WriteLine("      <description>");
                        if (p.StyleId == "blue_pin_style")
                        {
                            writer.WriteLine($"        <![CDATA[Tower containing Cell IDs: {p.CellId.Replace("_", ", ")}<br/>Cell Identities: {p.CellIdentity.Replace("_", ", ")}]]>");
                        }
                        else
                        {
                            writer.WriteLine($"        <![CDATA[Cell ID: {p.CellId}{p.BeamInfo}<br/>Cell Identity: {p.CellIdentity}<br/>Confidence: {p.Confidence}]]>");
                        }
                        writer.WriteLine("      </description>");
                        writer.WriteLine($"      <styleUrl>#{p.StyleId}</styleUrl>");
                        writer.WriteLine("      <Point>");
                        writer.WriteLine($"        <coordinates>{p.Lon},{p.Lat},0</coordinates>");
                        writer.WriteLine("      </Point>");
                        writer.WriteLine("    </Placemark>");
                    }

                    writer.WriteLine("  </Document>");
                    writer.WriteLine("</kml>");
                }
                Console.WriteLine($"Map KML saved to {mapKmlFilename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating map KML: {ex.Message}");
            }
        }

        public static void save_extract_step3(List<Dictionary<string, string>> finalPoints, string outputFilename, double maxCinr)
        {
            // This function saves the final, distance-filtered points for a single cell
            // to a CSV file. It ensures all columns are correctly aligned, even if some
            // rows are missing certain keys like 'cellIdentity'.
            if (!finalPoints.Any())
            {
                Console.WriteLine($"No data to save for {outputFilename}");
                return;
            }

            using (var writer = new StreamWriter(outputFilename))
            {
                // Get a comprehensive list of all possible headers from all points
                var allHeaders = finalPoints.SelectMany(p => p.Keys).Distinct().ToList();

                // Ensure a consistent order, e.g., by sorting
                allHeaders.Sort();

                // Write the header row
                writer.WriteLine(string.Join(",", allHeaders));

                // Write data rows, ensuring values align with the headers
                foreach (var point in finalPoints)
                {
                    var values = allHeaders.Select(header => point.GetValueOrDefault(header, ""));
                    writer.WriteLine(string.Join(",", values));
                }
            }
            Console.WriteLine($"Step 3 data for cell saved to {outputFilename} (Max CINR: {maxCinr:F2})");
        }

        public static void save_extract_step2(List<Dictionary<string, string>> filteredData, string outputFilename)
        {
            // Group data by channel and cell ID, count occurrences, and sort
            var step2Data = filteredData
                .GroupBy(row => new
                {
                    Channel = row.GetValueOrDefault("channel", "N/A"),
                    CellId = row.GetValueOrDefault("cellId", "N/A")
                })
                .Select(group => new
                {
                    group.Key.Channel,
                    group.Key.CellId,
                    Count = group.Count()
                })
                .OrderBy(item => item.Channel)
                .ThenBy(item => item.CellId)
                .ToList();

            using (var writer = new StreamWriter(outputFilename))
            {
                writer.WriteLine("Channel,CellID,Count");
                foreach (var item in step2Data)
                {
                    writer.WriteLine($"{item.Channel},{item.CellId},{item.Count}");
                }
            }
            Console.WriteLine($"Step 2 data saved to {outputFilename}");
        }

        public static void save_extrac_step1(List<Dictionary<string, string>> allData, string outputFilename)
        {
            // Group data by channel, cell ID, and beam index and count occurrences
            var processedData = allData
                .GroupBy(row => new
                {
                    Channel = row.ContainsKey("channel") ? row["channel"] : "N/A",
                    CellId = row.ContainsKey("cellId") ? row["cellId"] : "N/A",
                    BeamIndex = row.ContainsKey("beamIndex") ? row["beamIndex"] : "N/A"
                })
                .Select(group => new
                {
                    group.Key.Channel,
                    group.Key.CellId,
                    group.Key.BeamIndex,
                    Count = group.Count()
                })
                .OrderBy(item => item.Channel)
                .ThenBy(item => item.CellId)
                .ThenBy(item => item.BeamIndex)
                .ToList();

            using (var writer = new StreamWriter(outputFilename))
            {
                // Write header
                writer.WriteLine("Channel,CellID,BeamIndex,Count");

                // Write data
                foreach (var item in processedData)
                {
                    writer.WriteLine($"{item.Channel},{item.CellId},{item.BeamIndex},{item.Count}");
                }
            }
            Console.WriteLine($"Step 1 data saved to {outputFilename}");
        }

        public static void save_debug_map(List<Dictionary<string, string>> results, string outputFilename)
        {
            if (results == null || !results.Any())
            {
                return;
            }

            // Get the first channel and cell ID from the dataset
            var firstEntry = results.First();
            string channelToFind = firstEntry.GetValueOrDefault("channel", "N/A");
            string cellIdToFind = firstEntry.GetValueOrDefault("cellId", "N/A");

            // Find all row numbers for that specific channel and cell ID
            var rowNumbers = results
                .Where(row => row.GetValueOrDefault("channel", "N/A") == channelToFind &&
                              row.GetValueOrDefault("cellId", "N/A") == cellIdToFind)
                .Select(row => row.GetValueOrDefault("rowNumber", "N/A"))
                .ToList();

            // Get the total count
            int count = rowNumbers.Count;

            // Join the row numbers into a single string, e.g., "1;230;550"
            string rowNumbersString = string.Join(";", rowNumbers);

            using (var writer = new StreamWriter(outputFilename))
            {
                writer.WriteLine("Channel,CellID,Count,RowNumbers");
                writer.WriteLine($"{channelToFind},{cellIdToFind},{count},\"{rowNumbersString}\"");
            }
            Console.WriteLine($"Debug map for Channel {channelToFind}, CellID {cellIdToFind} saved to {outputFilename}");
        }
    }
}
