using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BTS_Location_Estimation
{
    
    public static class SaveHelper
    {

        public static void DeleteOutputFiles(string directoryPath)
        {
            try
            {
                // Find all files with .csv or .kml extensions
                var filesToDelete = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));

                // Loop through and delete each file
                foreach (string file in filesToDelete)
                {
                    File.Delete(file);
                    Console.WriteLine($"Deleted: {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during file cleanup: {ex.Message}");
            }
        }


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
                "Channel", "CellId", "cellIdentity", "mnc", "mcc", "xhat1", "yhat1", "xhat2", "yhat2",
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

                // Define a list of styles to cycle through for different towers
                var towerStyles = new List<(string pin, string balloon)>
                {
                    ("#blue_pin_style", "#blue_balloon_style"),
                    ("#green_pin_style", "#green_balloon_style"),
                    ("#ltblu_pin_style", "#ltblu_balloon_style"),
                    ("#purple_pin_style", "#purple_balloon_style")
                };

                // --- Pass 1: Identify towers and assign a color style to each tower and its members ---
                var sectorToStyleMap = new Dictionary<string, string>();
                var towerToStyleMap = new Dictionary<string, string>();
                int styleIndex = 0;

                var towers = estimationResults.Where(r => r.GetValueOrDefault("Type") == "Tower").ToList();
                foreach (var tower in towers)
                {
                    // Get the next style from the list, cycling through
                    var currentStyle = towerStyles[styleIndex % towerStyles.Count];
                    styleIndex++;

                    string towerCellId = tower.GetValueOrDefault("CellId", "");
                    towerToStyleMap[towerCellId] = currentStyle.pin;

                    string beamIndex = tower.GetValueOrDefault("BeamIndex", "");

                    // For LTE towers, the CellId is composite (e.g., "101/102/103").
                    if (towerCellId.Contains("/"))
                    {
                        var individualCellIds = towerCellId.Split('/');
                        foreach (var id in individualCellIds)
                        {
                            sectorToStyleMap[id] = currentStyle.balloon;
                        }
                    }
                    // For NR towers, the CellId is the common PCI.
                    else if (beamIndex.Contains("/") && !string.IsNullOrEmpty(towerCellId))
                    {
                        // All sectors with the same CellId (PCI) belong to this tower
                        var memberSectors = estimationResults
                            .Where(r => r.GetValueOrDefault("Type") == "Sector" && r.GetValueOrDefault("CellId") == towerCellId);
                        foreach (var sector in memberSectors)
                        {
                            // For NR, sectors share a CellId but have unique BeamIndex.
                            // We create a unique key for the style map to avoid overwriting.
                            string sectorKey = $"{sector.GetValueOrDefault("CellId", "")}_{sector.GetValueOrDefault("BeamIndex", "")}";
                            sectorToStyleMap[sectorKey] = currentStyle.balloon;
                        }
                    }
                }

                using (var writer = new StreamWriter(mapKmlFilename))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                    writer.WriteLine("  <Document>");
                    writer.WriteLine($"    <name>{mapName}</name>");

                    // --- Define All Styles ---

                    // Blue Styles
                    writer.WriteLine("    <Style id=\"blue_pin_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pushpin/blue-pushpin.png</href></Icon><hotSpot x=\"20\" y=\"2\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");
                    writer.WriteLine("    <Style id=\"blue_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/blu-circle.png</href></Icon><hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");

                    // Green Styles
                    writer.WriteLine("    <Style id=\"green_pin_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pushpin/grn-pushpin.png</href></Icon><hotSpot x=\"20\" y=\"2\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");
                    writer.WriteLine("    <Style id=\"green_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/grn-circle.png</href></Icon><hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");

                    // Light Blue Styles
                    writer.WriteLine("    <Style id=\"ltblu_pin_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pushpin/ltblu-pushpin.png</href></Icon><hotSpot x=\"20\" y=\"2\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");
                    writer.WriteLine("    <Style id=\"ltblu_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/ltblu-circle.png</href></Icon><hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");

                    // Purple Styles
                    writer.WriteLine("    <Style id=\"purple_pin_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pushpin/purple-pushpin.png</href></Icon><hotSpot x=\"20\" y=\"2\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");
                    writer.WriteLine("    <Style id=\"purple_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/purple-circle.png</href></Icon><hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");

                    // Standalone/Default Styles
                    writer.WriteLine("    <Style id=\"red_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/red-circle.png</href></Icon><hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");
                    writer.WriteLine("    <Style id=\"yellow_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/ylw-circle.png</href></Icon><hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle></Style>");


                    // --- Pass 2: Write a placemark for every result ---
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
                        string type = result.GetValueOrDefault("Type", "Sector");
                        string beamInfo = result.GetValueOrDefault("BeamIndex", "");

                        writer.WriteLine("    <Placemark>");
                        writer.WriteLine($"      <name>{cellId}</name>");
                        writer.WriteLine("      <description>");

                        string description;
                        string styleUrl;

                        if (type == "Tower")
                        {
                            styleUrl = towerToStyleMap.GetValueOrDefault(cellId, "#blue_pin_style"); // Default to blue if not found
                            string towerDesc = $"        <![CDATA[Tower containing Cell IDs: {cellId.Replace("/", ", ")}<br/>Cell Identities: {cellIdentity.Replace("_", ", ")}";
                            if (beamInfo.Contains("/"))
                            {
                                towerDesc += $"<br/>Beams: {beamInfo.Replace("/", ", ")}";
                            }
                            description = towerDesc + "]]>";
                        }
                        else // It's a Sector
                        {
                            // Create the same unique key as in Pass 1 for NR sectors
                            string sectorKey = $"{cellId}_{beamInfo}";

                            if (confidence == "Low")
                            {
                                styleUrl = "#yellow_balloon_style";
                            }
                            // Check for LTE style (key is just cellId) or NR style (key is cellId_beamInfo)
                            else if (sectorToStyleMap.TryGetValue(cellId, out var assignedStyle) || sectorToStyleMap.TryGetValue(sectorKey, out assignedStyle))
                            {
                                styleUrl = assignedStyle; // Use the color assigned from its tower
                            }
                            else
                            {
                                styleUrl = "#red_balloon_style"; // Standalone sector
                            }
                            description = $"        <![CDATA[Cell ID: {cellId}, Beam: {beamInfo}<br/>Cell Identity: {cellIdentity}<br/>Confidence: {confidence}]]>";
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

        public static void ClusterProcessing()
        {
            string[] estimateFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "Estimate*.csv");
            string outputFile = "ALL_Estimate.csv";
            string header = "Technology,Channel,CellId,BeamIndex,Type,cellIdentity,mnc,mcc,xhat1,yhat1,xhat2,yhat2,est_Lat1,est_Lon1,est_Lat2,est_Lon2,Max_cinr,Num_points,Confidence";
            var allRows = new List<Dictionary<string, string>>();
            using (var writer = new StreamWriter(outputFile))
            {
                writer.WriteLine(header);
                foreach (string file in estimateFiles)
                {
                    string tech = "LTE";
                    if (file.Contains("_NR_")) tech = "NR";
                    else if (file.Contains("WCDMA")) tech = "WCDMA";
                    else if (file.Contains("ColorCode")) tech = "GSM";
                    var lines = File.ReadAllLines(file);
                    var fileHeader = lines[0].Split(',');
                    for (int i = 1; i < lines.Length; i++) // skip header
                    {
                        var columns = lines[i].Split(',');
                        string[] rowArr;
                        if (tech == "NR")
                        {
                            rowArr = new string[] { tech }.Concat(columns).ToArray();
                        }
                        else
                        {
                            rowArr = new string[] { tech }
                                .Concat(columns.Take(2))
                                .Concat(new string[] { "" })
                                .Concat(columns.Skip(2)).ToArray();
                        }
                        writer.WriteLine(string.Join(",", rowArr));
                        // Build dictionary for clustering
                        var rowDict = new Dictionary<string, string>();
                        for (int c = 0; c < header.Split(',').Length && c < rowArr.Length; c++)
                        {
                            rowDict[header.Split(',')[c]] = rowArr[c];
                        }
                        allRows.Add(rowDict);
                    }
                }
                // Cluster and append cluster entries
                var clusterEntries = DataBaseProc.DBSCAN_Cluster(allRows, 0.5, 4);
                foreach (var entry in clusterEntries)
                {
                    writer.WriteLine(string.Join(",", header.Split(',').Select(h => entry.GetValueOrDefault(h, ""))));
                }
            }
            Console.WriteLine("ALL_Estimate.csv created with Technology column and cluster entries.");
        }

        public static void save_cluster(string inputFile = "ALL_Estimate.csv", string outputFile = "ALL_map.csv")
        {
            var headers = new List<string> { "Latitude", "Longitude", "CellID", "CellIdentity", "mnc", "mcc", "BeamIndex", "Type" };
            var rows = new List<Dictionary<string, string>>();
            using (var reader = new StreamReader(inputFile))
            using (var writer = new StreamWriter(outputFile))
            {
                writer.WriteLine(string.Join(",", headers));
                var allHeaders = reader.ReadLine()?.Split(',') ?? new string[0]; // Read header from ALL_Estimate.csv
                int latIdx = Array.IndexOf(allHeaders, "est_Lat2");
                int lonIdx = Array.IndexOf(allHeaders, "est_Lon2");
                int cellIdIdx = Array.IndexOf(allHeaders, "CellId");
                int cellIdentityIdx = Array.IndexOf(allHeaders, "cellIdentity");
                int mncIdx = Array.IndexOf(allHeaders, "mnc");
                int mccIdx = Array.IndexOf(allHeaders, "mcc");
                int beamIdxIdx = Array.IndexOf(allHeaders, "BeamIndex");
                int typeIdx = Array.IndexOf(allHeaders, "Type");
                while (reader.ReadLine() is string lineNotNull)
                {
                    var cols = lineNotNull.Split(',');
                    var row = new List<string>
                    {
                        latIdx >= 0 ? cols[latIdx] : "",
                        lonIdx >= 0 ? cols[lonIdx] : "",
                        cellIdIdx >= 0 ? cols[cellIdIdx] : "",
                        cellIdentityIdx >= 0 ? cols[cellIdentityIdx] : "",
                        mncIdx >= 0 ? cols[mncIdx] : "",
                        mccIdx >= 0 ? cols[mccIdx] : "",
                        beamIdxIdx >= 0 ? cols[beamIdxIdx] : "",
                        typeIdx >= 0 ? cols[typeIdx] : ""
                    };
                    writer.WriteLine(string.Join(",", row));
                    // Build dictionary for KML
                    var dict = new Dictionary<string, string>
                    {
                        {"Latitude", latIdx >= 0 ? cols[latIdx] : ""},
                        {"Longitude", lonIdx >= 0 ? cols[lonIdx] : ""},
                        {"CellID", cellIdIdx >= 0 ? cols[cellIdIdx] : ""},
                        {"CellIdentity", cellIdentityIdx >= 0 ? cols[cellIdentityIdx] : ""},
                        {"mnc", mncIdx >= 0 ? cols[mncIdx] : ""},
                        {"mcc", mccIdx >= 0 ? cols[mccIdx] : ""},
                        {"BeamIndex", beamIdxIdx >= 0 ? cols[beamIdxIdx] : ""},
                        {"Type", typeIdx >= 0 ? cols[typeIdx] : ""}
                    };
                    rows.Add(dict);
                }
            }
            Console.WriteLine("ALL_map.csv created from ALL_Estimate.csv.");
            // Also generate KML
            generate_all_map_kml(rows, Path.ChangeExtension(outputFile, ".kml"));
        }

        // Helper to generate ALL_map.kml from the parsed rows
        private static void generate_all_map_kml(List<Dictionary<string, string>> rows, string kmlFilename)
        {
            // Color styles: blue, green, light blue, purple (no red)
            var colorStyles = new List<(string pin, string balloon)>
            {
                ("#blue_pin_style", "#blue_balloon_style"),
                ("#green_pin_style", "#green_balloon_style"),
                ("#ltblu_pin_style", "#ltblu_balloon_style"),
                ("#purple_pin_style", "#purple_balloon_style")
            };
            // Assign colors to clusters and sectors
            var clusterColors = new Dictionary<string, (string pin, string balloon)>();
            int colorIdx = 0;
            // Find all cluster entries
            var clusters = rows.Where(r => r.GetValueOrDefault("Type") == "cluster entry").ToList();
            foreach (var cluster in clusters)
            {
                var clusterId = cluster.GetValueOrDefault("CellID", "") + "_" + cluster.GetValueOrDefault("BeamIndex", "");
                clusterColors[clusterId] = colorStyles[colorIdx % colorStyles.Count];
                colorIdx++;
            }
            // Map sectors to clusters
            var sectorToCluster = new Dictionary<string, string>();
            foreach (var cluster in clusters)
            {
                var cellIds = cluster.GetValueOrDefault("CellID", "").Split('/');
                var beamIndices = cluster.GetValueOrDefault("BeamIndex", "").Split('/');
                foreach (var cellId in cellIds)
                {
                    sectorToCluster[cellId] = cluster.GetValueOrDefault("CellID", "") + "_" + cluster.GetValueOrDefault("BeamIndex", "");
                }
            }
            using (var writer = new StreamWriter(kmlFilename))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                writer.WriteLine("  <Document>");
                writer.WriteLine("    <name>ALL_map</name>");
                // Styles
                writer.WriteLine("    <Style id=\"blue_pin_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pushpin/blue-pushpin.png</href></Icon></IconStyle></Style>");
                writer.WriteLine("    <Style id=\"blue_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/blu-circle.png</href></Icon></IconStyle></Style>");
                writer.WriteLine("    <Style id=\"green_pin_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pushpin/grn-pushpin.png</href></Icon></IconStyle></Style>");
                writer.WriteLine("    <Style id=\"green_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/grn-circle.png</href></Icon></IconStyle></Style>");
                writer.WriteLine("    <Style id=\"ltblu_pin_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pushpin/ltblu-pushpin.png</href></Icon></IconStyle></Style>");
                writer.WriteLine("    <Style id=\"ltblu_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/ltblu-circle.png</href></Icon></IconStyle></Style>");
                writer.WriteLine("    <Style id=\"purple_pin_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pushpin/purple-pushpin.png</href></Icon></IconStyle></Style>");
                writer.WriteLine("    <Style id=\"purple_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/purple-circle.png</href></Icon></IconStyle></Style>");
                writer.WriteLine("    <Style id=\"red_balloon_style\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/red-circle.png</href></Icon></IconStyle></Style>");
                // Placemarks
                foreach (var row in rows)
                {
                    string lat = row.GetValueOrDefault("Latitude", "");
                    string lon = row.GetValueOrDefault("Longitude", "");
                    string cellId = row.GetValueOrDefault("CellID", "");
                    string cellIdentity = row.GetValueOrDefault("CellIdentity", "");
                    string mnc = row.GetValueOrDefault("mnc", "");
                    string mcc = row.GetValueOrDefault("mcc", "");
                    string beamIndex = row.GetValueOrDefault("BeamIndex", "");
                    string type = row.GetValueOrDefault("Type", "");
                    string styleUrl = "";
                    string name = cellId;
                    string desc = "";
                    if (type == "cluster entry")
                    {
                        // Pushpin, color by cluster
                        var clusterId = cellId + "_" + beamIndex;
                        styleUrl = clusterColors.ContainsKey(clusterId) ? clusterColors[clusterId].pin : "#blue_pin_style";
                        desc = $"<![CDATA[Cluster: {cellId}<br/>Beam: {beamIndex}<br/>CellIdentity: {cellIdentity}<br/>MNC: {mnc}<br/>MCC: {mcc}]]>";
                    }
                    else if (type == "Sector")
                    {
                        // Balloon, color by cluster if associated, else red
                        string sectorKey = cellId;
                        if (sectorToCluster.ContainsKey(sectorKey) && clusterColors.ContainsKey(sectorToCluster[sectorKey]))
                        {
                            styleUrl = clusterColors[sectorToCluster[sectorKey]].balloon;
                        }
                        else
                        {
                            styleUrl = "#red_balloon_style";
                        }
                        desc = $"<![CDATA[Sector: {cellId}<br/>Beam: {beamIndex}<br/>CellIdentity: {cellIdentity}<br/>MNC: {mnc}<br/>MCC: {mcc}]]>";
                    }
                    else
                    {
                        styleUrl = "#red_balloon_style";
                        desc = $"<![CDATA[{type}: {cellId}<br/>Beam: {beamIndex}<br/>CellIdentity: {cellIdentity}<br/>MNC: {mnc}<br/>MCC: {mcc}]]>";
                    }
                    writer.WriteLine("    <Placemark>");
                    writer.WriteLine($"      <name>{name}</name>");
                    writer.WriteLine("      <description>");
                    writer.WriteLine($"        {desc}");
                    writer.WriteLine("      </description>");
                    writer.WriteLine($"      <styleUrl>{styleUrl}");
                    writer.WriteLine("      <Point>");
                    writer.WriteLine($"        <coordinates>{lon},{lat},0</coordinates>");
                    writer.WriteLine("      </Point>");
                    writer.WriteLine("    </Placemark>");
                }
                writer.WriteLine("  </Document>");
                writer.WriteLine("</kml>");
            }
            Console.WriteLine($"ALL_map.kml created at {kmlFilename}");
        }
    }
}
