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

            // Dynamically add optional columns like "BeamIndex" and "Type" if they exist in the data
            var allKeys = estimationResults.SelectMany(r => r.Keys).Distinct().ToList();
            
            if (allKeys.Contains("BeamIndex"))
            {
                headers.Insert(headers.IndexOf("CellId") + 1, "BeamIndex");
            }
            if (allKeys.Contains("Type"))
            {
                int beamIndexPos = headers.IndexOf("BeamIndex");
                if (beamIndexPos != -1)
                {
                    headers.Insert(beamIndexPos + 1, "Type");
                }
                else
                {
                    headers.Insert(headers.IndexOf("CellId") + 1, "Type");
                }
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
                    writer.WriteLine("Latitude,Longitude,CellID,CellIdentity,BeamIndex,Type");
                    foreach (var result in estimationResults)
                    {
                        string lat = result.GetValueOrDefault("est_Lat2", "");
                        string lon = result.GetValueOrDefault("est_Lon2", "");
                        string cellId = result.GetValueOrDefault("CellId", "");
                        string cellIdentity = result.GetValueOrDefault("cellIdentity", "");
                        string beamIndex = result.GetValueOrDefault("BeamIndex", "");
                        string type = result.GetValueOrDefault("Type", "");
                        writer.WriteLine($"{lat},{lon},{cellId},{cellIdentity},{beamIndex},{type}");
                    }
                }
                Console.WriteLine($"Map CSV saved to {mapCsvFilename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating map CSV: {ex.Message}");
            }
        }

        private static void generate_map_kml(List<Dictionary<string, string>> estimationResults, string mapKmlFilename)
        {
            try
            {
                string mapName = Path.GetFileNameWithoutExtension(mapKmlFilename);

                // First pass: identify all cell IDs that are part of a tower
                var towerMemberCellIds = new HashSet<string>();
                var towers = estimationResults.Where(r => r.GetValueOrDefault("Type") == "Tower").ToList();
                foreach (var tower in towers)
                {
                    string cellId = tower.GetValueOrDefault("CellId", "");
                    if (cellId.Contains("/"))
                    {
                        var individualCellIds = cellId.Split('/');
                        foreach (var id in individualCellIds)
                        {
                            towerMemberCellIds.Add(id);
                        }
                    }
                }

                using (var writer = new StreamWriter(mapKmlFilename))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                    writer.WriteLine("  <Document>");
                    writer.WriteLine($"    <name>{mapName}</name>");

                    // Style for towers (blue pushpin)
                    writer.WriteLine("    <Style id=\"blue_pin_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/pushpin/blue-pushpin.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"20\" y=\"2\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Style for standalone points (red balloon)
                    writer.WriteLine("    <Style id=\"red_balloon_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/red-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Style for tower member points (blue balloon)
                    writer.WriteLine("    <Style id=\"blue_balloon_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/blu-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Style for low confidence points (yellow balloon)
                    writer.WriteLine("    <Style id=\"yellow_balloon_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/ylw-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Write a placemark for every result
                    foreach (var result in estimationResults)
                    {
                        if (!result.ContainsKey("est_Lat2") || !result.ContainsKey("est_Lon2"))
                        {
                            continue;
                        }

                        string lat = result["est_Lat2"];
                        string lon = result["est_Lon2"];
                        string cellId = result.GetValueOrDefault("CellId", "");
                        string cellIdentity = result.GetValueOrDefault("cellIdentity", "");
                        string confidence = result.GetValueOrDefault("Confidence", "N/A");
                        string type = result.GetValueOrDefault("Type", "Sector"); // Default to Sector
                        string beamInfo = result.ContainsKey("BeamIndex") ? $", Beam: {result["BeamIndex"]}" : "";

                        writer.WriteLine("    <Placemark>");
                        writer.WriteLine($"      <name>{cellId}</name>");
                        writer.WriteLine("      <description>");

                        string description;
                        string styleUrl;

                        if (type == "Tower")
                        {
                            description = $"        <![CDATA[Tower containing Cell IDs: {cellId.Replace("/", ", ")}<br/>Cell Identities: {cellIdentity.Replace("_", ", ")}]]>";
                            styleUrl = "#blue_pin_style";
                        }
                        else if (confidence == "Low")
                        {
                            description = $"        <![CDATA[Cell ID: {cellId}{beamInfo}<br/>Cell Identity: {cellIdentity}<br/>Confidence: {confidence}]]>";
                            styleUrl = "#yellow_balloon_style";
                        }
                        else if (towerMemberCellIds.Contains(cellId))
                        {
                            description = $"        <![CDATA[Cell ID: {cellId}{beamInfo}<br/>Cell Identity: {cellIdentity}<br/>Confidence: {confidence}]]>";
                            styleUrl = "#blue_balloon_style";
                        }
                        else
                        {
                            description = $"        <![CDATA[Cell ID: {cellId}{beamInfo}<br/>Cell Identity: {cellIdentity}<br/>Confidence: {confidence}]]>";
                            styleUrl = "#red_balloon_style";
                        }
                        writer.WriteLine(description);

                        writer.WriteLine("      </description>");
                        writer.WriteLine($"      <styleUrl>{styleUrl}</styleUrl>");
                        writer.WriteLine("      <Point>");
                        writer.WriteLine($"        <coordinates>{lon},{lat},0</coordinates>");
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

        public static void map_cellid(List<Dictionary<string, string>> allData, string channel, string cellId, string color)
        {
            string baseOutputFilename = $"map_ch{channel}_cell{cellId}";
            string csvOutputFilename = baseOutputFilename + ".csv";
            string kmlOutputFilename = baseOutputFilename + ".kml";

            var filteredData = allData
                .Where(row => row.GetValueOrDefault("channel", "") == channel && row.GetValueOrDefault("cellId", "") == cellId)
                .ToList();

            if (!filteredData.Any())
            {
                Console.WriteLine($"No data found for Channel {channel} and CellID {cellId}.");
                return;
            }

            // --- CSV Generation ---
            try
            {
                // Define headers using the correct keys from ExtractChannelCellMap
                var headers = new List<string> { "latitude", "longitude", "cellIdentity", "RSSI", "cinr" };
                
                // Determine if there's any beam index data to decide on the header
                bool hasBeamIndex = filteredData.Any(p => p.ContainsKey("beamIndex"));
                if (hasBeamIndex)
                {
                    headers.Add("beamIndex");
                }

                using (var writer = new StreamWriter(csvOutputFilename))
                {
                    // Write the header row
                    writer.WriteLine(string.Join(",", headers));

                    // Write data rows
                    foreach (var point in filteredData)
                    {
                        var values = headers.Select(header => point.GetValueOrDefault(header, ""));
                        writer.WriteLine(string.Join(",", values));
                    }
                }
                Console.WriteLine($"Cell map data saved to {csvOutputFilename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating cell map CSV: {ex.Message}");
            }

            // --- KML Generation ---
            try
            {
                string styleId;
                string iconHref;

                switch (color.ToLower())
                {
                    case "red":
                        styleId = "red_balloon_style";
                        iconHref = "http://maps.google.com/mapfiles/kml/paddle/red-circle.png";
                        break;
                    case "green":
                        styleId = "green_balloon_style";
                        iconHref = "http://maps.google.com/mapfiles/kml/paddle/grn-circle.png";
                        break;
                    case "blue":
                    default:
                        styleId = "blue_balloon_style";
                        iconHref = "http://maps.google.com/mapfiles/kml/paddle/blu-circle.png";
                        break;
                }

                using (var writer = new StreamWriter(kmlOutputFilename))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                    writer.WriteLine("  <Document>");
                    writer.WriteLine($"    <name>Map for Channel {channel}, Cell ID {cellId}</name>");

                    // Style for balloon points
                    writer.WriteLine($"    <Style id=\"{styleId}\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine($"          <href>{iconHref}</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Write a placemark for every data point
                    foreach (var point in filteredData)
                    {
                        if (!point.ContainsKey("latitude") || !point.ContainsKey("longitude"))
                        {
                            continue;
                        }

                        string lat = point["latitude"];
                        string lon = point["longitude"];
                        string cellIdentity = point.GetValueOrDefault("cellIdentity", "N/A");
                        string rssi = point.GetValueOrDefault("RSSI", "N/A");
                        string cinr = point.GetValueOrDefault("cinr", "N/A");
                        string beamInfo = point.ContainsKey("beamIndex") ? $", Beam: {point["beamIndex"]}" : "";
                        string rowNum = point.GetValueOrDefault("rowNumber", "N/A");

                        writer.WriteLine("    <Placemark>");
                        writer.WriteLine($"      <name>Point {rowNum}</name>");
                        writer.WriteLine("      <description>");
                        writer.WriteLine($"        <![CDATA[Cell Identity: {cellIdentity}<br/>RSSI: {rssi}<br/>CINR: {cinr}{beamInfo}]]>");
                        writer.WriteLine("      </description>");
                        writer.WriteLine($"      <styleUrl>#{styleId}</styleUrl>");
                        writer.WriteLine("      <Point>");
                        writer.WriteLine($"        <coordinates>{lon},{lat},0</coordinates>");
                        writer.WriteLine("      </Point>");
                        writer.WriteLine("    </Placemark>");
                    }

                    writer.WriteLine("  </Document>");
                    writer.WriteLine("</kml>");
                }
                Console.WriteLine($"Cell map KML saved to {kmlOutputFilename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating cell map KML: {ex.Message}");
            }
        }
    }
}
