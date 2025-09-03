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
            // Delete the file if it exists to ensure a clean run
            if (File.Exists(outputFilename))
            {
                File.Delete(outputFilename);
            }

            using (var writer = new StreamWriter(outputFilename))
            {
                if (estimationResults.Any())
                {
                    var headers = estimationResults.First().Keys;
                    writer.WriteLine(string.Join(",", headers));

                    foreach (var result in estimationResults)
                    {
                        writer.WriteLine(string.Join(",", result.Values));
                    }
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
                // 1. Parse and sort the data
                var parsedData = estimationResults
                    .Select(r =>
                    {
                        int.TryParse(r.GetValueOrDefault("cellIdentity", "-1"), out int id);
                        return new
                        {
                            Result = r,
                            CellIdentityNum = id
                        };
                    })
                    .Where(x => x.CellIdentityNum != -1)
                    .OrderBy(x => x.CellIdentityNum)
                    .ToList();

                var placemarks = new List<Placemark>();
                var groupPlacemarks = new List<Placemark>(); // Separate list for averaged groups
                var processedIndices = new HashSet<int>();

                // 1. Create placemarks for all individual points first
                foreach (var dataPoint in parsedData)
                {
                    var result = dataPoint.Result;
                    placemarks.Add(new Placemark
                    {
                        Lat = result.GetValueOrDefault("est_Lat2", ""),
                        Lon = result.GetValueOrDefault("est_Lon2", ""),
                        CellId = result.GetValueOrDefault("CellId", ""),
                        CellIdentity = result.GetValueOrDefault("cellIdentity", ""),
                        BeamInfo = result.ContainsKey("BeamIndex") ? $", Beam: {result["BeamIndex"]}" : "",
                        Confidence = result.GetValueOrDefault("Confidence", "N/A"),
                        StyleId = "red_circle_style" // Default style
                    });
                }

                // 2. Identify groups of three, update styles, and create group placemark
                for (int i = 0; i <= parsedData.Count - 3; i++)
                {
                    if (processedIndices.Contains(i)) continue;

                    var p1 = parsedData[i];
                    var p2 = parsedData[i + 1];
                    var p3 = parsedData[i + 2];

                    int diff1 = p2.CellIdentityNum - p1.CellIdentityNum;
                    int diff2 = p3.CellIdentityNum - p2.CellIdentityNum;

                    if ((diff1 == 1 || diff1 == 2) && (diff2 == 1 || diff2 == 2))
                    {
                        var group = new[] { p1, p2, p3 };
                        double avgLat = group.Average(p => double.Parse(p.Result.GetValueOrDefault("est_Lat2", "0"), System.Globalization.CultureInfo.InvariantCulture));
                        double avgLon = group.Average(p => double.Parse(p.Result.GetValueOrDefault("est_Lon2", "0"), System.Globalization.CultureInfo.InvariantCulture));

                        groupPlacemarks.Add(new Placemark
                        {
                            Lat = avgLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
                            Lon = avgLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
                            CellId = string.Join("_", group.Select(p => p.Result.GetValueOrDefault("CellId", ""))),
                            CellIdentity = string.Join("_", group.Select(p => p.Result.GetValueOrDefault("cellIdentity", ""))),
                            StyleId = "pink_pin_style"
                        });

                        // Update style for individual points in the group
                        placemarks[i].StyleId = "pink_circle_style";
                        placemarks[i + 1].StyleId = "pink_circle_style";
                        placemarks[i + 2].StyleId = "pink_circle_style";

                        processedIndices.Add(i);
                        processedIndices.Add(i + 1);
                        processedIndices.Add(i + 2);
                    }
                }

                // 3. Identify groups of two, update styles, and create group placemark
                for (int i = 0; i <= parsedData.Count - 2; i++)
                {
                    if (processedIndices.Contains(i) || processedIndices.Contains(i + 1)) continue;

                    var p1 = parsedData[i];
                    var p2 = parsedData[i + 1];

                    int diff = p2.CellIdentityNum - p1.CellIdentityNum;

                    if (diff == 1 || diff == 2)
                    {
                        var group = new[] { p1, p2 };
                        double avgLat = group.Average(p => double.Parse(p.Result.GetValueOrDefault("est_Lat2", "0"), System.Globalization.CultureInfo.InvariantCulture));
                        double avgLon = group.Average(p => double.Parse(p.Result.GetValueOrDefault("est_Lon2", "0"), System.Globalization.CultureInfo.InvariantCulture));

                        groupPlacemarks.Add(new Placemark
                        {
                            Lat = avgLat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
                            Lon = avgLon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
                            CellId = string.Join("_", group.Select(p => p.Result.GetValueOrDefault("CellId", ""))),
                            CellIdentity = string.Join("_", group.Select(p => p.Result.GetValueOrDefault("cellIdentity", ""))),
                            StyleId = "pink_pin_style"
                        });

                        // Update style for individual points in the group
                        placemarks[i].StyleId = "pink_circle_style";
                        placemarks[i + 1].StyleId = "pink_circle_style";

                        processedIndices.Add(i);
                        processedIndices.Add(i + 1);
                    }
                }

                // 4. Combine all placemarks
                placemarks.AddRange(groupPlacemarks);

                string mapName = Path.GetFileNameWithoutExtension(mapKmlFilename);
                using (var writer = new StreamWriter(mapKmlFilename))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                    writer.WriteLine("  <Document>");
                    writer.WriteLine($"    <name>{mapName}</name>");

                    // Style for single points (red circle)
                    writer.WriteLine("    <Style id=\"red_circle_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/red-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Style for grouped points (pink circle for individual members)
                    writer.WriteLine("    <Style id=\"pink_circle_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/pink-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    // Style for grouped points (pink pushpin for average)
                    writer.WriteLine("    <Style id=\"pink_pin_style\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/pushpin/pink-pushpin.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"20\" y=\"2\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    foreach (var p in placemarks)
                    {
                        writer.WriteLine("    <Placemark>");
                        writer.WriteLine($"      <name>{p.CellId}</name>");
                        writer.WriteLine("      <description>");
                        if (p.StyleId == "pink_pin_style")
                        {
                            writer.WriteLine($"        <![CDATA[Grouped Cell Identities: {p.CellIdentity}]]>");
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
            // to a CSV file. It includes all the original data for the selected points
            // and can be used as input for the final location estimation algorithms.
            // The maximum CINR value is also available if needed for reporting.
            using (var writer = new StreamWriter(outputFilename))
            {
                if (finalPoints.Any())
                {
                    // Write header from the keys of the first point
                    var headers = finalPoints.First().Keys;
                    writer.WriteLine(string.Join(",", headers));

                    // Write data rows
                    foreach (var point in finalPoints)
                    {
                        writer.WriteLine(string.Join(",", point.Values));
                    }
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
    }
}
