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

        private static void generate_map_kml(List<Dictionary<string, string>> estimationResults, string mapKmlFilename)
        {
            try
            {
                string mapName = Path.GetFileNameWithoutExtension(mapKmlFilename);
                using (var writer = new StreamWriter(mapKmlFilename))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                    writer.WriteLine("  <Document>");
                    writer.WriteLine($"    <name>{mapName}</name>");
                    writer.WriteLine("    <Style id=\"red_balloon\">");
                    writer.WriteLine("      <IconStyle>");
                    writer.WriteLine("        <Icon>");
                    writer.WriteLine("          <href>http://maps.google.com/mapfiles/kml/paddle/red-circle.png</href>");
                    writer.WriteLine("        </Icon>");
                    writer.WriteLine("        <hotSpot x=\"32\" y=\"1\" xunits=\"pixels\" yunits=\"pixels\"/>");
                    writer.WriteLine("      </IconStyle>");
                    writer.WriteLine("    </Style>");

                    foreach (var result in estimationResults)
                    {
                        string lat = result.GetValueOrDefault("est_Lat2", "");
                        string lon = result.GetValueOrDefault("est_Lon2", "");
                        string cellId = result.GetValueOrDefault("CellId", "");
                        string cellIdentity = result.GetValueOrDefault("cellIdentity", "");
                        string beamInfo = result.ContainsKey("BeamIndex") ? $", Beam: {result["BeamIndex"]}" : "";

                        writer.WriteLine("    <Placemark>");
                        writer.WriteLine($"      <name>{cellId}</name>");
                        writer.WriteLine("      <description>");
                        writer.WriteLine($"        <![CDATA[Cell ID: {cellId}{beamInfo}<br/>Cell Identity: {cellIdentity}]]>");
                        writer.WriteLine("      </description>");
                        writer.WriteLine("      <styleUrl>#red_balloon</styleUrl>");
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
