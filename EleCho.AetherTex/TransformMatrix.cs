using System.Diagnostics;
using System.Numerics;

namespace EleCho.AetherTex
{
    /// <summary>
    /// Transform Matrix. <br />
    /// [ ScaleX, SkewY, PerspX; <br />
    ///   SkewX, ScaleY, PerspY; <br />
    ///   TransX, TransY, PerspZ ]
    /// </summary>
    /// <param name="ScaleX">Row1 Column1, ScaleX</param>
    /// <param name="SkewY">Row1 Column2, SkewY</param>
    /// <param name="PerspX">Row1 Column3, Perspective X</param>
    /// <param name="SkewX">Row2 Column1, SkewX</param>
    /// <param name="ScaleY">Row2 Column2, ScaleY</param>
    /// <param name="PerspY">Row2 Column3, Perspective Y</param>
    /// <param name="TransX">Row3 Column1, Translate X</param>
    /// <param name="TransY">Row3 Column2, Translate Y</param>
    /// <param name="PerspZ">Row3 Column3, Perspective Z</param>
    public record struct TransformMatrix(
        float ScaleX, float SkewY, float PerspX,
        float SkewX, float ScaleY, float PerspY,
        float TransX, float TransY, float PerspZ)
    {
        public void Append(TransformMatrix other)
        {
            this = Multiply(this, other);
        }

        public void Prepend(TransformMatrix other)
        {
            this = Multiply(other, this);
        }

        public void Translate(float x, float y)
        {
            Append(new TransformMatrix(1, 0, 0, 0, 1, 0, x, y, 1));
        }

        public void Rotate(float rotation)
        {
#if NET6_0_OR_GREATER
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);
#else
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
#endif

            // Create rotation matrix
            var rotationMatrix = new TransformMatrix(
                cos, -sin, 0,  // Row 1
                sin, cos, 0,  // Row 2
                0, 0, 1   // Row 3
            );

            // Multiply this matrix with the rotation matrix
            Append(rotationMatrix);
        }

        public void Scale(float x, float y)
        {
            // Create scaling matrix
            var scalingMatrix = new TransformMatrix(
                x, 0, 0,  // Row 1
                0, y, 0,  // Row 2
                0, 0, 1   // Row 3
            );

            // Multiply this matrix with the scaling matrix
            Append(scalingMatrix);
        }

        public void Scale(float value)
        {
            Scale(value, value);
        }

        /// <summary>
        /// Inverts this matrix.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the matrix is non-invertible.</exception>
        public void Invert()
        {
            if (!TryInvert(out this))
            {
                throw new InvalidOperationException("Matrix is not invertible.");
            }
        }

        /// <summary>
        /// Attempts to invert this matrix and returns a value indicating whether the operation succeeded.
        /// </summary>
        /// <param name="result">The inverted matrix if the operation succeeded, or the original matrix if it failed.</param>
        /// <returns>true if the matrix was successfully inverted; otherwise, false.</returns>
        public bool TryInvert(out TransformMatrix result)
        {
            // Calculate the determinant of the matrix
            float determinant = ScaleX * (ScaleY * PerspZ - PerspY * TransY) -
                                SkewY * (SkewX * PerspZ - PerspY * TransX) +
                                PerspX * (SkewX * TransY - ScaleY * TransX);

            // Check if the determinant is zero (or very close to it)
            // A matrix with a determinant of zero is singular and has no inverse.
            if (Math.Abs(determinant) < 1e-8f) // Use a small epsilon for floating-point comparison
            {
                result = this; // Return the original matrix
                return false;
            }

            float invDet = 1.0f / determinant;
            // Calculate the components of the adjugate matrix and multiply by the inverse of the determinant
            result = new TransformMatrix(
                // Row 1
                (ScaleY * PerspZ - PerspY * TransY) * invDet,
                (PerspX * TransY - SkewY * PerspZ) * invDet,
                (SkewY * PerspY - PerspX * ScaleY) * invDet,
                // Row 2
                (PerspY * TransX - SkewX * PerspZ) * invDet,
                (ScaleX * PerspZ - PerspX * TransX) * invDet,
                (PerspX * SkewX - ScaleX * PerspY) * invDet,
                // Row 3
                (SkewX * TransY - ScaleY * TransX) * invDet,
                (ScaleX * TransY - SkewY * TransX) * invDet,
                (ScaleX * ScaleY - SkewY * SkewX) * invDet
            );

            return true;
        }

        public Vector2 Transform(Vector2 vector)
        {
            Vector3 result = new Vector3(
                vector.X * ScaleX + vector.Y * SkewX + TransX,
                vector.X * SkewY + vector.Y * ScaleY + TransY,
                vector.X * PerspX + vector.Y * PerspY + PerspZ);

            if (result.Z == 0)
            {
                return new Vector2(result.X, result.Y);
            }
            else
            {
                return new Vector2(
                    result.X / result.Z,
                    result.Y / result.Z);
            }
        }

        public static TransformMatrix Identity { get; } = new TransformMatrix(1, 0, 0, 0, 1, 0, 0, 0, 1);

        /// <summary>
        /// Calculates a linear transformation matrix from two pairs of corresponding points.
        /// A linear transform includes scale, skew, and rotation, but no translation.
        /// The origin (0,0) remains fixed.
        /// </summary>
        public static TransformMatrix LinearTransform(
            Vector2 p1Before, Vector2 p1After,
            Vector2 p2Before, Vector2 p2After)
        {
            // We need to solve for 4 unknowns: ScaleX, SkewX, SkewY, ScaleY.
            // x' = ScaleX * x + SkewX * y
            // y' = SkewY * x + ScaleY * y

            // System for ScaleX, SkewX:
            // p1After.X = ScaleX * p1Before.X + SkewX * p1Before.Y
            // p2After.X = ScaleX * p2Before.X + SkewX * p2Before.Y
            var A_x = new float[,] {
                { p1Before.X, p1Before.Y },
                { p2Before.X, p2Before.Y }
            };

            var b_x = new float[] { p1After.X, p2After.X };
            var x_params = LinearEquationSolver.Solve(A_x, b_x);
            // System for SkewY, ScaleY:
            // p1After.Y = SkewY * p1Before.X + ScaleY * p1Before.Y
            // p2After.Y = SkewY * p2Before.X + ScaleY * p2Before.Y
            var A_y = new float[,] {
                { p1Before.X, p1Before.Y },
                { p2Before.X, p2Before.Y }
            };

            var b_y = new float[] { p1After.Y, p2After.Y };
            var y_params = LinearEquationSolver.Solve(A_y, b_y);
            if (x_params == null || y_params == null)
            {
                // Points are collinear and a unique linear transform cannot be determined.
                // Return identity matrix as a fallback.
                return new TransformMatrix(1, 0, 0, 0, 1, 0, 0, 0, 1);
            }
            // x_params = { ScaleX, SkewX }
            // y_params = { SkewY, ScaleY }
            return new TransformMatrix(
                x_params[0], y_params[0], 0,
                x_params[1], y_params[1], 0,
                0, 0, 1
            );
        }

        /// <summary>
        /// Calculates an affine transformation matrix from three pairs of corresponding points.
        /// An affine transform includes scale, skew, rotation, and translation. Parallel lines remain parallel.
        /// </summary>
        public static TransformMatrix AffineTransform(
            Vector2 p1Before, Vector2 p1After,
            Vector2 p2Before, Vector2 p2After,
            Vector2 p3Before, Vector2 p3After)
        {
            // We need to solve for 6 unknowns: ScaleX, SkewX, TransX, SkewY, ScaleY, TransY
            // x' = ScaleX * x + SkewX * y + TransX
            // y' = SkewY * x + ScaleY * y + TransY
            // System for ScaleX, SkewX, TransX
            var A = new float[,] {
                { p1Before.X, p1Before.Y, 1 },
                { p2Before.X, p2Before.Y, 1 },
                { p3Before.X, p3Before.Y, 1 }
            };

            var b_x = new float[] { p1After.X, p2After.X, p3After.X };
            var x_params = LinearEquationSolver.Solve((float[,])A.Clone(), b_x);

            var b_y = new float[] { p1After.Y, p2After.Y, p3After.Y };
            var y_params = LinearEquationSolver.Solve((float[,])A.Clone(), b_y);
            if (x_params == null || y_params == null)
            {
                // Points are collinear and a unique affine transform cannot be determined.
                // Return identity matrix as a fallback.
                return new TransformMatrix(1, 0, 0, 0, 1, 0, 0, 0, 1);
            }
            // x_params = { ScaleX, SkewX, TransX }
            // y_params = { SkewY, ScaleY, TransY }
            return new TransformMatrix(
                x_params[0], y_params[0], 0,
                x_params[1], y_params[1], 0,
                x_params[2], y_params[2], 1
            );
        }
        /// <summary>
        /// Calculates a perspective transformation matrix from four pairs of corresponding points.
        /// </summary>
        public static TransformMatrix PerspectiveTransform(
            Vector2 p1Before, Vector2 p1After,
            Vector2 p2Before, Vector2 p2After,
            Vector2 p3Before, Vector2 p3After,
            Vector2 p4Before, Vector2 p4After)
        {
            // To solve for the 8 unknowns of a perspective matrix, we set up a system of 8 linear equations.
            // The transformation equations are:
            // x' = (a*x + b*y + c) / (g*x + h*y + 1)
            // y' = (d*x + e*y + f) / (g*x + h*y + 1)
            // We rearrange them to avoid division:
            // a*x + b*y + c - g*x*x' - h*y*x' = x'
            // d*x + e*y + f - g*x*y' - h*y*y' = y'
            // Where the unknowns are {a,b,c,d,e,f,g,h} corresponding to {ScaleX, SkewX, TransX, SkewY, ScaleY, TransY, PerspX, PerspY}.
            // PerspZ is assumed to be 1.

            var A = new float[8, 8];
            var b = new float[8];
            var pointsBefore = new[] { p1Before, p2Before, p3Before, p4Before };
            var pointsAfter = new[] { p1After, p2After, p3After, p4After };
            for (int i = 0; i < 4; i++)
            {
                var pb = pointsBefore[i];
                var pa = pointsAfter[i];
                int r1 = i * 2;
                int r2 = r1 + 1;
                // Row for the x' equation
                A[r1, 0] = pb.X; A[r1, 1] = pb.Y; A[r1, 2] = 1;
                A[r1, 3] = 0; A[r1, 4] = 0; A[r1, 5] = 0;
                A[r1, 6] = -pb.X * pa.X;
                A[r1, 7] = -pb.Y * pa.X;
                b[r1] = pa.X;
                // Row for the y' equation
                A[r2, 0] = 0; A[r2, 1] = 0; A[r2, 2] = 0;
                A[r2, 3] = pb.X; A[r2, 4] = pb.Y; A[r2, 5] = 1;
                A[r2, 6] = -pb.X * pa.Y;
                A[r2, 7] = -pb.Y * pa.Y;
                b[r2] = pa.Y;
            }
            var x = LinearEquationSolver.Solve(A, b);
            if (x == null)
            {
                // Points are degenerate (e.g., three are collinear)
                // and a unique perspective transform cannot be determined.
                return new TransformMatrix(1, 0, 0, 0, 1, 0, 0, 0, 1);
            }
            // x = {a, b, c, d, e, f, g, h}
            return new TransformMatrix(
                x[0], x[3], x[6], // ScaleX, SkewY, PerspX
                x[1], x[4], x[7], // SkewX,  ScaleY, PerspY
                x[2], x[5], 1     // TransX, TransY, PerspZ
            );
        }

        public static TransformMatrix Multiply(TransformMatrix left, TransformMatrix right)
        {
            return new TransformMatrix(
                // First Row
                left.ScaleX * right.ScaleX + left.SkewY * right.SkewX + left.PerspX * right.TransX,
                left.ScaleX * right.SkewY + left.SkewY * right.ScaleY + left.PerspX * right.TransY,
                left.ScaleX * right.PerspX + left.SkewY * right.PerspY + left.PerspX * right.PerspZ,
                // Second Row
                left.SkewX * right.ScaleX + left.ScaleY * right.SkewX + left.PerspY * right.TransX,
                left.SkewX * right.SkewY + left.ScaleY * right.ScaleY + left.PerspY * right.TransY,
                left.SkewX * right.PerspX + left.ScaleY * right.PerspY + left.PerspY * right.PerspZ,
                // Third Row
                left.TransX * right.ScaleX + left.TransY * right.SkewX + left.PerspZ * right.TransX,
                left.TransX * right.SkewY + left.TransY * right.ScaleY + left.PerspZ * right.TransY,
                left.TransX * right.PerspX + left.TransY * right.PerspY + left.PerspZ * right.PerspZ);
        }

        public override string ToString()
        {
            return $"[ {ScaleX}, {SkewY}, {PerspX}; {SkewX}, {ScaleY}, {PerspY}; {TransX}, {TransY}, {PerspZ} ]";
        }
    }
}
