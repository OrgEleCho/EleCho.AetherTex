using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EleCho.AetherTex;

namespace AetherTex.Viewer.Extensions
{
    internal static class TextureDataExtensions
    {
        public static unsafe void NormalizeFloat(this TextureData data, float? min, float? max)
        {
            if (data.Format != TextureFormat.Float32)
            {
                throw new InvalidOperationException("TextureData must be in Float32 format to normalize.");
            }

            float actualMin = min ?? float.NaN;
            float actualMax = max ?? float.NaN;

            if (float.IsNaN(actualMin))
            {
                if (float.IsNaN(actualMax))
                {
                    actualMin = float.MaxValue;
                    actualMax = float.MinValue;
                    for (int y = 0; y < data.Height; y++)
                    {
                        var rowPixels = new Span<float>((void*)(data.BaseAddress + y * data.RowBytes), data.Width);
                        for (int x = 0; x < data.Width; x++)
                        {
                            var pixel = rowPixels[x];
                            if (pixel < actualMin)
                            {
                                actualMin = pixel;
                            }
                            if (pixel > actualMax)
                            {
                                actualMax = pixel;
                            }
                        }
                    }
                }
                else
                {
                    actualMin = float.MaxValue;
                    for (int y = 0; y < data.Height; y++)
                    {
                        var rowPixels = new Span<float>((void*)(data.BaseAddress + y * data.RowBytes), data.Width);
                        for (int x = 0; x < data.Width; x++)
                        {
                            var pixel = rowPixels[x];
                            if (pixel < actualMin)
                            {
                                actualMin = pixel;
                            }
                        }
                    }
                }
            }
            else if (float.IsNaN(actualMax))
            {
                actualMax = float.MinValue;
                for (int y = 0; y < data.Height; y++)
                {
                    var rowPixels = new Span<float>((void*)(data.BaseAddress + y * data.RowBytes), data.Width);
                    for (int x = 0; x < data.Width; x++)
                    {
                        var pixel = rowPixels[x];
                        if (pixel > actualMax)
                        {
                            actualMax = pixel;
                        }
                    }
                }
            }

            if (actualMin > actualMax)
            {
                throw new ArgumentException("Min value must be less than Max value for normalization.");
            }

            var range = actualMax - actualMin;

            if (range == 0)
            {
                // If range is zero, all pixels are the same, set them to 0
                for (int y = 0; y < data.Height; y++)
                {
                    var rowPixels = new Span<float>((void*)(data.BaseAddress + y * data.RowBytes), data.Width);
                    foreach (ref var pixel in rowPixels)
                    {
                        pixel = 0f;
                    }
                }
            }
            else
            {
                for (int y = 0; y < data.Height; y++)
                {
                    var rowPixels = new Span<float>((void*)(data.BaseAddress + y * data.RowBytes), data.Width);
                    foreach (ref var pixel in rowPixels)
                    {
                        pixel = (pixel - actualMin) / range;
                    }
                }
            }
        }
    }
}
