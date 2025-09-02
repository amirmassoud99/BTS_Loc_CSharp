using System;
using System.Collections.Generic;
using System.Linq;

public static class TSWLS
{
    // Helper: Create an identity matrix
    public static double[,] IdentityMatrix(int size)
    {
        var I = new double[size, size];
        for (int i = 0; i < size; i++) I[i, i] = 1.0;
        return I;
    }

    // Helper: Transpose a matrix
    public static double[,] Transpose(double[,] A)
    {
        int rows = A.GetLength(0), cols = A.GetLength(1);
        var T = new double[cols, rows];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                T[j, i] = A[i, j];
        return T;
    }

    // Helper: Multiply two matrices
    public static double[,] Multiply(double[,] A, double[,] B)
    {
        int rowsA = A.GetLength(0), colsA = A.GetLength(1), colsB = B.GetLength(1);
        var C = new double[rowsA, colsB];
        for (int i = 0; i < rowsA; i++)
            for (int j = 0; j < colsB; j++)
                for (int k = 0; k < colsA; k++)
                    C[i, j] += A[i, k] * B[k, j];
        return C;
    }

    // Helper: Multiply matrix by vector
    public static double[] Multiply(double[,] A, double[] v)
    {
        int rows = A.GetLength(0), cols = A.GetLength(1);
        var result = new double[rows];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                result[i] += A[i, j] * v[j];
        return result;
    }

    // Helper: Inverse of a square matrix (Gauss-Jordan, not for production)
    public static double[,] Inverse(double[,] A)
    {
        int n = A.GetLength(0);
        var I = IdentityMatrix(n);
        var copy = (double[,])A.Clone();
        for (int i = 0; i < n; i++)
        {
            // Find pivot
            int pivot = i;
            for (int j = i + 1; j < n; j++)
                if (Math.Abs(copy[j, i]) > Math.Abs(copy[pivot, i])) pivot = j;
            // Swap rows
            for (int k = 0; k < n; k++)
            {
                (copy[i, k], copy[pivot, k]) = (copy[pivot, k], copy[i, k]);
                (I[i, k], I[pivot, k]) = (I[pivot, k], I[i, k]);
            }
            if (Math.Abs(copy[i, i]) < 1e-12) throw new InvalidOperationException("Matrix is singular");
            double div = copy[i, i];
            for (int k = 0; k < n; k++)
            {
                copy[i, k] /= div;
                I[i, k] /= div;
            }
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                double mult = copy[j, i];
                for (int k = 0; k < n; k++)
                {
                    copy[j, k] -= mult * copy[i, k];
                    I[j, k] -= mult * I[i, k];
                }
            }
        }
        return I;
    }

    // Weighted Least Squares (AI interpretation)
    public static double[] WLS_AI(double[,] G, double[,] W, double[] h)
    {
        var GT = Transpose(G);
        var temp1 = Multiply(GT, W);
        var temp2 = Multiply(temp1, G);
        var tempInv = Inverse(temp2);
        var temp3 = Multiply(temp1, h);
        return Multiply(tempInv, temp3);
    }

    // Weighted Least Squares (Matlab style)
    public static double[] WLS_Matlab(double[,] G, double[,] W, double[] h)
    {
        var Winv = Inverse(W);
        var GT = Transpose(G);
        var temp1 = Multiply(GT, Winv);
        var temp2 = Multiply(temp1, G);
        var covZ = Inverse(temp2);
        var temp3 = Multiply(temp1, h);
        return Multiply(covZ, temp3);
    }

    // Two-Stage Weighted Least Squares (TSWLS2)
    public static double[] TSWLS2(int N, double[] sx, double[] sy, double[] ts, double c)
    {
        var Ga = new double[N - 1, 3];
        var h = new double[N - 1];
        double K0 = sx[0] * sx[0] + sy[0] * sy[0];
        for (int i = 0; i < N - 1; i++)
        {
            Ga[i, 0] = sx[i + 1] - sx[0];
            Ga[i, 1] = sy[i + 1] - sy[0];
            double r_i0 = c * (ts[i + 1] - ts[0]);
            Ga[i, 2] = r_i0;
            double Ki = sx[i + 1] * sx[i + 1] + sy[i + 1] * sy[i + 1];
            h[i] = 0.5 * (Ki - K0 - r_i0 * r_i0);
        }
        var Q = new double[N - 1, N - 1];
        for (int i = 0; i < N - 1; i++) Q[i, i] = 1.0;
        var z1 = WLS_Matlab(Ga, Q, h);
        if (z1.Length < 2) throw new Exception("First pass of WLS failed");
        double x = z1[0], y = z1[1];
        var B = new double[N - 1, N - 1];
        for (int i = 0; i < N - 1; i++) B[i, i] = Math.Sqrt(Math.Pow(x - sx[i + 1], 2) + Math.Pow(y - sy[i + 1], 2));
        var W2 = Multiply(Multiply(B, Q), B);
        var z2 = WLS_Matlab(Ga, W2, h);
        if (z2.Length < 3) throw new Exception("Second pass of WLS failed");
        // Final pass omitted for brevity
        return z2;
    }
}
