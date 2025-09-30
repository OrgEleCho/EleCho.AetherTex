using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EleCho.AetherTex
{
    /// <summary>
    /// A simple solver for linear equations using Gaussian elimination.
    /// </summary>
    public static class LinearEquationSolver
    {
        public static float[]? Solve(float[,] A, float[] b)
        {
            int n = b.Length;

            for (int p = 0; p < n; p++)
            {
                // Find pivot row and swap
                int max = p;
                for (int i = p + 1; i < n; i++)
                {
                    if (Math.Abs(A[i, p]) > Math.Abs(A[max, p]))
                    {
                        max = i;
                    }
                }
                float[] temp = new float[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    // Swapping A and b in one go
                    if (i < n)
                    {
                        (A[p, i], A[max, i]) = (A[max, i], A[p, i]);
                    }
                    else
                    {
                        (b[p], b[max]) = (b[max], b[p]);
                    }
                }


                // Singular or nearly singular
                if (Math.Abs(A[p, p]) <= 1e-10)
                {
                    // This means the points are likely collinear or degenerate
                    // and a unique solution doesn't exist.
                    return null;
                }

                // Pivot within A and b
                for (int i = p + 1; i < n; i++)
                {
                    float alpha = A[i, p] / A[p, p];
                    b[i] -= alpha * b[p];
                    for (int j = p; j < n; j++)
                    {
                        A[i, j] -= alpha * A[p, j];
                    }
                }
            }

            // Back substitution
            float[] x = new float[n];
            for (int i = n - 1; i >= 0; i--)
            {
                float sum = 0.0f;
                for (int j = i + 1; j < n; j++)
                {
                    sum += A[i, j] * x[j];
                }
                x[i] = (b[i] - sum) / A[i, i];
            }
            return x;
        }
    }

}
