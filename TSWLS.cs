using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BTS_Location_Estimation
{
    public static class TSWLS
    {
        /***************************************************************************************************
        *
        *   Function:       WLS_Matlab
        *
        *   Description:    C# implementation of the WLS (Weighted Least Squares) function from MATLAB.
        *                   It solves the linear system z = (G' * inv(W) * G)^-1 * G' * inv(W) * h.
        *
        *   Input:          G (Matrix<double>) - The design matrix.
        *                   W (Matrix<double>) - The weighting matrix.
        *                   h (Vector<double>) - The observation vector.
        *
        *   Output:         The result vector z, or null if a matrix is singular and cannot be inverted.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static Vector<double>? WLS_Matlab(Matrix<double> G, Matrix<double> W, Vector<double> h)
        {
            try
            {
                Matrix<double> Winv = W.Inverse();
                Matrix<double> G_transpose = G.Transpose();
                Matrix<double> temp1 = G_transpose * Winv;
                Matrix<double> temp2 = temp1 * G;
                Matrix<double> covZ = temp2.Inverse();
                
                Vector<double> temp3 = temp1 * h;
                Vector<double> result = covZ * temp3;
                
                return result;
            }
            catch (System.InvalidOperationException ex)
            {
                Console.Error.WriteLine($"Error: A matrix in WLS_Matlab is singular and cannot be inverted. Details: {ex.Message}");
                return null;
            }
        }

        /***************************************************************************************************
        *
        *   Function:       tswls2
        *
        *   Description:    C# implementation of the core TSWLS (Two-Stage Weighted Least Squares) algorithm.
        *                   This function performs multiple WLS passes to estimate the x, y location.
        *
        *   Input:          N (int) - The number of data points.
        *                   sx (Vector<double>) - Vector of x-coordinates for the measurement points.
        *                   sy (Vector<double>) - Vector of y-coordinates for the measurement points.
        *                   ts (Vector<double>) - Vector of time-of-arrival measurements.
        *                   c (double) - The speed of light.
        *
        *   Output:         A vector containing the estimated xHat, yHat, and r_0 (distance),
        *                   or null if any of the internal WLS passes fail.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static Vector<double>? tswls2(int N, Vector<double> sx, Vector<double> sy, Vector<double> ts, double c)
        {
            // Step 1: First WLS Pass
            var Ga = Matrix<double>.Build.Dense(N - 1, 3);
            var h = Vector<double>.Build.Dense(N - 1);
            double K0 = sx[0] * sx[0] + sy[0] * sy[0];

            for (int i = 0; i < N - 1; ++i)
            {
                Ga[i, 0] = sx[i + 1] - sx[0];
                Ga[i, 1] = sy[i + 1] - sy[0];
                double r_i0 = c * (ts[i + 1] - ts[0]);
                Ga[i, 2] = r_i0;
                double Ki = sx[i + 1] * sx[i + 1] + sy[i + 1] * sy[i + 1];
                h[i] = 0.5 * (Ki - K0 - r_i0 * r_i0);
            }

            var Q = Matrix<double>.Build.Dense(N - 1, N - 1, 0.5);
            for (int i = 0; i < N - 1; ++i)
            {
                Q[i, i] += 0.5;
            }

            Vector<double>? z1 = WLS_Matlab(Ga, Q, h);
            if (z1 == null || z1.Count < 2)
            {
                Console.Error.WriteLine("First pass of WLS failed.");
                return null;
            }
            double x = z1[0];
            double y = z1[1];

            // Step 2: Second WLS Pass
            var B = Matrix<double>.Build.DenseDiagonal(N - 1, N - 1, 0.0);
            for (int i = 0; i < N - 1; ++i)
            {
                B[i, i] = Math.Sqrt(Math.Pow(x - sx[i + 1], 2) + Math.Pow(y - sy[i + 1], 2));
            }
            Matrix<double> W2 = B * Q * B;

            Vector<double>? z2 = WLS_Matlab(Ga, W2, h);
            if (z2 == null || z2.Count < 3)
            {
                Console.Error.WriteLine("Second pass of WLS failed.");
                return null;
            }

            // Step 3: Covariance and Final WLS Pass
            Matrix<double> covZ2;
            try
            {
                covZ2 = (Ga.Transpose() * W2 * Ga).Inverse();
            }
            catch (System.InvalidOperationException)
            {
                Console.Error.WriteLine("Failed to calculate covariance matrix.");
                return null;
            }

            var Gap = Matrix<double>.Build.DenseOfArray(new double[,] { { 1, 0 }, { 0, 1 }, { 1, 1 } });
            x = z2[0];
            y = z2[1];
            var hp = Vector<double>.Build.Dense(new double[] {
                (x - sx[0]) * (x - sx[0]),
                (y - sy[0]) * (y - sy[0]),
                z2[2] * z2[2]
            });

            var Bp = Matrix<double>.Build.DenseDiagonal(3, 3, 0.0);
            Bp[0, 0] = x - sx[0];
            Bp[1, 1] = y - sy[0];
            Bp[2, 2] = z2[2];

            Matrix<double> Wp_inv = 4 * Bp * covZ2 * Bp;
            Matrix<double> Wp;
            try
            {
                Wp = Wp_inv.Inverse();
            }
            catch (System.InvalidOperationException)
            {
                Console.Error.WriteLine("Failed to calculate final weighting matrix.");
                return null;
            }

            Vector<double>? zp = WLS_Matlab(Gap, Wp, hp);
            if (zp == null || zp.Count < 2)
            {
                Console.Error.WriteLine("Final pass of WLS failed.");
                return null;
            }

            // Step 4: Final Result Calculation
            double xHat, yHat;
            if (zp[0] >= 0 && zp[1] >= 0)
            {
                xHat = (x >= 0) ? Math.Sqrt(zp[0]) + sx[0] : -Math.Sqrt(zp[0]) + sx[0];
                yHat = (y >= 0) ? Math.Sqrt(zp[1]) + sy[0] : -Math.Sqrt(zp[1]) + sy[0];
            }
            else
            {
                xHat = x;
                yHat = y;
            }

            double r_0 = Math.Sqrt(xHat * xHat + yHat * yHat);

            return Vector<double>.Build.Dense(new double[] { xHat, yHat, r_0 });
        }

        /***************************************************************************************************
        *
        *   Function:       run_tswls
        *
        *   Description:    Main workflow function for the TSWLS process. It converts latitude/longitude
        *                   to a local x/y coordinate system, runs the core `tswls2` algorithm,
        *                   and then refines the result using a grid search.
        *
        *   Input:          points (List<...>) - The list of filtered and time-adjusted data points.
        *                   minimum_points_for_TSWLS (int) - The minimum number of points required.
        *                   c (double) - The speed of light constant.
        *                   metersPerDegree (double) - Conversion factor for lat/lon to meters.
        *                   range (double) - The search range for the grid search.
        *                   step (double) - The step size for the grid search.
        *
        *   Output:         A vector containing the initial estimate (xHat, yHat) and the refined
        *                   grid search estimate (xHat2, yHat2), or null on failure.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static Vector<double>? run_tswls(List<Dictionary<string, string>> points, int minimum_points_for_TSWLS, double c, double metersPerDegree, double range, double step)
        {
            if (points.Count < minimum_points_for_TSWLS)
            {
                Console.WriteLine($"TSWLS requires at least {minimum_points_for_TSWLS} points. Cannot run with {points.Count} points.");
                return null;
            }

            int N = points.Count;
            var sx = Vector<double>.Build.Dense(N);
            var sy = Vector<double>.Build.Dense(N);
            var ts = Vector<double>.Build.Dense(N);

            // Convert lat/lon to local cartesian x/y coordinates.
            const double PI = Math.PI;
            
            double latRef = double.Parse(points[0]["latitude"]);
            double lonRef = double.Parse(points[0]["longitude"]);
            
            double xRef = lonRef * metersPerDegree * Math.Cos(latRef * PI / 180.0);
            double yRef = latRef * metersPerDegree;

            for (int i = 0; i < N; i++)
            {
                double lat = double.Parse(points[i]["latitude"]);
                double lon = double.Parse(points[i]["longitude"]);
                sx[i] = lon * metersPerDegree * Math.Cos(latRef * PI / 180.0) - xRef;
                sy[i] = lat * metersPerDegree - yRef;
                ts[i] = double.Parse(points[i]["TimeOffset"]);
            }

            Vector<double>? tswls_results = tswls2(N, sx, sy, ts, c);

            if (tswls_results == null)
            {
                Console.WriteLine("TSWLS algorithm failed to produce a result.");
                return null;
            }

            double xHat = tswls_results[0];
            double yHat = tswls_results[1];
            double r_0 = tswls_results[2];

            Console.WriteLine("\n--- TSWLS Algorithm Results ---");
            Console.WriteLine($"Estimated Location (xHat, yHat): ({xHat:F4}, {yHat:F4})");
            Console.WriteLine($"Estimated Distance (r_0): {r_0:F4} meters");

            // Call GridSearch to refine the result
            Console.WriteLine("\n--- Running Grid Search ---");
            Vector<double> grid_search_results = GridSearch2(N, sx, sy, ts, c, xHat, yHat, range, step);

            double xHat2 = 0.0;
            double yHat2 = 0.0;
            if (grid_search_results != null && grid_search_results.Count > 1)
            {
                xHat2 = grid_search_results[0];
                yHat2 = grid_search_results[1];
                Console.WriteLine($"Grid Search Location (xHat2, yHat2): ({xHat2:F4}, {yHat2:F4})");
            }

            return Vector<double>.Build.Dense(new double[] { xHat, yHat, xHat2, yHat2 });
        }

        /***************************************************************************************************
        *
        *   Function:       CalcAME
        *
        *   Description:    C# implementation of the CalcAME (Calculate Absolute Mean Error) function.
        *                   It computes the error for a candidate location (bx, by).
        *
        *   Input:          N (int) - The number of data points.
        *                   sx, sy (Vector<double>) - Vectors of measurement point coordinates.
        *                   ts (Vector<double>) - Vector of time-of-arrival measurements.
        *                   c (double) - The speed of light.
        *                   bx, by (double) - The candidate x and y coordinates to evaluate.
        *
        *   Output:         The calculated absolute mean error (double).
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static double CalcAME(int N, Vector<double> sx, Vector<double> sy, Vector<double> ts, double c, double bx, double by)
        {
            double ame = 0.0;
            var dist = Vector<double>.Build.Dense(N);

            dist[0] = Math.Sqrt(Math.Pow(sx[0] - bx, 2) + Math.Pow(sy[0] - by, 2));
            for (int n = 1; n < N; ++n)
            {
                dist[n] = Math.Sqrt(Math.Pow(sx[n] - bx, 2) + Math.Pow(sy[n] - by, 2));
                double distDiff = dist[n] - dist[0];
                double tdistDiff = (ts[n] - ts[0]) * c;
                ame += Math.Abs(distDiff - tdistDiff);
            }

            if (N > 1)
            {
                ame /= (N - 1);
            }
            return ame;
        }

        /***************************************************************************************************
        *
        *   Function:       GridSearch2
        *
        *   Description:    C# implementation of the GridSearch2 function. It performs a grid search
        *                   around an initial estimate (x, y) to find a location with a lower
        *                   Absolute Mean Error (AME), refining the TSWLS result.
        *
        *   Input:          N (int) - The number of data points.
        *                   sx, sy (Vector<double>) - Vectors of measurement point coordinates.
        *                   ts (Vector<double>) - Vector of time-of-arrival measurements.
        *                   c (double) - The speed of light.
        *                   x, y (double) - The initial estimated coordinates.
        *                   range (double) - The search range for the grid.
        *                   step (double) - The step size for the grid.
        *
        *   Output:         A vector containing the refined xHat, yHat, and r_0.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static Vector<double> GridSearch2(int N, Vector<double> sx, Vector<double> sy, Vector<double> ts, double c, double x, double y, double range, double step)
        {
            int M = (int)Math.Ceiling(range / step);
            int gridSize = 2 * M + 1;
            var rmse = Matrix<double>.Build.Dense(gridSize, gridSize);

            Console.WriteLine("Populating grid for search...");
            for (int xcnt = -M; xcnt <= M; ++xcnt)
            {
                for (int ycnt = -M; ycnt <= M; ++ycnt)
                {
                    double xTmp = x + xcnt * step;
                    double yTmp = y + ycnt * step;
                    double rmseTmp = CalcAME(N, sx, sy, ts, c, xTmp, yTmp);
                    rmse[ycnt + M, xcnt + M] = rmseTmp;
                }
            }
            Console.WriteLine("Grid populated.");

            // Walk search to find the minimum
            Console.WriteLine("Performing walk search...");
            var wk = (Row: M, Col: M); // Start walk from the center [row, col] -> [y, x]
            while (true)
            {
                int wy = wk.Row;
                int wx = wk.Col;
                
                var wkNew = wk;
                double wVue = rmse[wy, wx];

                // Search neighbors (Up, Down, Left, Right)
                int[] dx = { 0, 0, -1, 1 }; 
                int[] dy = { -1, 1, 0, 0 }; 

                for (int i = 0; i < 4; ++i)
                {
                    int wxTmp = wx + dx[i];
                    int wyTmp = wy + dy[i];

                    if (wxTmp >= 0 && wxTmp < gridSize && wyTmp >= 0 && wyTmp < gridSize)
                    {
                        double wVueTmp = rmse[wyTmp, wxTmp];
                        if (wVueTmp < wVue)
                        {
                            wVue = wVueTmp;
                            wkNew = (wyTmp, wxTmp);
                        }
                    }
                }

                if (wk == wkNew)
                {
                    break; // No change, found local minimum
                }
                wk = wkNew;
            }
            Console.WriteLine("Walk search complete.");

            double yAdj = (wk.Row - M) * step;
            double xAdj = (wk.Col - M) * step;
            double xHat = x + xAdj;
            double yHat = y + yAdj;
            double r_0 = Math.Sqrt(xHat * xHat + yHat * yHat);

            return Vector<double>.Build.Dense(new double[] { xHat, yHat, r_0 });
        }

        /***************************************************************************************************
        *
        *   Function:       xy2LatLon
        *
        *   Description:    C# implementation of the xy2LatLon function. It converts local
        *                   x, y coordinates back to latitude and longitude based on a reference point.
        *
        *   Input:          x, y (double) - The local coordinates to convert.
        *                   latRef, lonRef (double) - The reference latitude and longitude.
        *                   metersPerDegree (double) - The conversion factor for meters to degrees.
        *
        *   Output:         A tuple containing the calculated Latitude and Longitude.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static (double Lat, double Lon) xy2LatLon(double x, double y, double latRef, double lonRef, double metersPerDegree)
        {
            const double PI = Math.PI;
            double lon = (x / metersPerDegree) / Math.Cos(latRef * PI / 180.0) + lonRef;
            double lat = y / metersPerDegree + latRef;
            return (lat, lon);
        }

        /***************************************************************************************************
        *
        *   Function:       ProcessTswlsResult
        *
        *   Description:    Processes the successful result from the TSWLS algorithm for a single cell.
        *                   It converts the estimated x,y coordinates to latitude/longitude,
        *                   calculates a confidence level, and formats the output for saving.
        *
        *   Input:          tswlsResult (Vector<double>) - Vector with estimated x,y coordinates.
        *                   timeAdjustedPoints (List<...>) - Data points used for the estimation.
        *                   group (IGrouping<...>) - Grouping metadata (Channel, CellId).
        *                   maxCinr (double) - Maximum CINR for the cell.
        *                   estimationResults (List<...>) - The list to which the final result is added.
        *                   fileType (int) - The type of the processed file.
        *                   metersPerDegree (double) - Conversion factor for lat/lon to meters.
        *                   wcdmaFileTypeCsv (int) - Constant for WCDMA CSV file type.
        *                   wcdmaFileTypeDtr (int) - Constant for WCDMA DTR file type.
        *                   confidenceMinPointsWcdma (int) - Minimum points for WCDMA confidence.
        *                   confidenceMinEcioWcdma (double) - Minimum EC/IO for WCDMA confidence.
        *                   confidenceMinPointsLteNr (int) - Minimum points for LTE/NR confidence.
        *                   confidenceMinCinrLteNr (double) - Minimum CINR for LTE/NR confidence.
        *
        *   Output:         None (void). Modifies the 'estimationResults' list by adding a new
        *                   dictionary containing the full estimation result for the cell.
        *
        *   Author:         Amir Soltanian
        *
        *   Date:           September 4, 2025
        *
        ***************************************************************************************************/
        public static void ProcessTswlsResult(
            Vector<double> tswlsResult,
            List<Dictionary<string, string>> timeAdjustedPoints,
            IGrouping<dynamic, Dictionary<string, string>> group,
            double maxCinr,
            List<Dictionary<string, string>> estimationResults,
            int fileType,
            double metersPerDegree,
            int wcdmaFileTypeCsv,
            int wcdmaFileTypeDtr,
            int confidenceMinPointsWcdma,
            double confidenceMinEcioWcdma,
            int confidenceMinPointsLteNr,
            double confidenceMinCinrLteNr)
        {
            double xhat1 = tswlsResult[0];
            double yhat1 = tswlsResult[1];
            double xhat2 = tswlsResult[2];
            double yhat2 = tswlsResult[3];

            double latRef = double.Parse(timeAdjustedPoints[0]["latitude"], CultureInfo.InvariantCulture);
            double lonRef = double.Parse(timeAdjustedPoints[0]["longitude"], CultureInfo.InvariantCulture);

            var (est_Lat1, est_Lon1) = xy2LatLon(xhat1, yhat1, latRef, lonRef, metersPerDegree);
            var (est_Lat2, est_Lon2) = xy2LatLon(xhat2, yhat2, latRef, lonRef, metersPerDegree);

            Console.WriteLine($"Estimated Final Location for Cell {group.Key.CellId} (Lat, Lon): ({est_Lat2:F6}, {est_Lon2:F6})");

            // Extract and combine unique cellIdentity values for the group
            var cellIdentities = group
                .Where(p => p.ContainsKey("cellIdentity") && !string.IsNullOrWhiteSpace(p["cellIdentity"]))
                .Select(p => p["cellIdentity"])
                .Distinct()
                .ToList();
            string combinedCellIdentity = string.Join("-", cellIdentities);

            string confidence = "High";
            bool isWcdma = fileType == wcdmaFileTypeCsv || fileType == wcdmaFileTypeDtr;
            if (isWcdma)
            {
                if (timeAdjustedPoints.Count < confidenceMinPointsWcdma && maxCinr < confidenceMinEcioWcdma)
                {
                    confidence = "Low";
                }
            }
            else // LTE and NR
            {
                if (timeAdjustedPoints.Count < confidenceMinPointsLteNr && maxCinr < confidenceMinCinrLteNr)
                {
                    confidence = "Low";
                }
            }

            var resultDict = new Dictionary<string, string>
            {
                { "Channel", group.Key.Channel },
                { "CellId", group.Key.CellId },
                { "cellIdentity", combinedCellIdentity },
            };
            // Add mnc and mcc after cellIdentity
            var mnc = group.Select(p => p.ContainsKey("mnc") ? p["mnc"] : "").FirstOrDefault();
            var mcc = group.Select(p => p.ContainsKey("mcc") ? p["mcc"] : "").FirstOrDefault();
            resultDict["mnc"] = mnc ?? "";
            resultDict["mcc"] = mcc ?? "";
            // Add remaining fields
            resultDict["xhat1"] = xhat1.ToString("F4", CultureInfo.InvariantCulture);
            resultDict["yhat1"] = yhat1.ToString("F4", CultureInfo.InvariantCulture);
            resultDict["xhat2"] = xhat2.ToString("F4", CultureInfo.InvariantCulture);
            resultDict["yhat2"] = yhat2.ToString("F4", CultureInfo.InvariantCulture);
            resultDict["est_Lat1"] = est_Lat1.ToString("F6", CultureInfo.InvariantCulture);
            resultDict["est_Lon1"] = est_Lon1.ToString("F6", CultureInfo.InvariantCulture);
            resultDict["est_Lat2"] = est_Lat2.ToString("F6", CultureInfo.InvariantCulture);
            resultDict["est_Lon2"] = est_Lon2.ToString("F6", CultureInfo.InvariantCulture);
            resultDict["Max_cinr"] = maxCinr.ToString("F2", CultureInfo.InvariantCulture);
            resultDict["Num_points"] = timeAdjustedPoints.Count.ToString();
            resultDict["Confidence"] = confidence;
            estimationResults.Add(resultDict);
        }
    }
}

