using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.DXGI;

namespace EleCho.AetherTex.Internal
{
    public static class TextureFormatExtensions
    {
        internal static Format ToDirectX(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Rgba8888 => Format.FormatR8G8B8A8Unorm,
                TextureFormat.Bgra8888 => Format.FormatB8G8R8A8Unorm,
                TextureFormat.Gray8 => Format.FormatR8Unorm,
                TextureFormat.Gray16 => Format.FormatR16Unorm,
                TextureFormat.Float32 => Format.FormatR32Float,
                _ => throw new ArgumentException($"Unsupported texture format: {format}"),
            };
        }

        public static bool IsRgb(this TextureFormat format)
        {
            return format is
                TextureFormat.Rgba8888 or
                TextureFormat.Bgra8888;
        }

        public static bool IsBayer(this TextureFormat format)
        {
            return format is
                TextureFormat.BayerRggb or
                TextureFormat.BayerBggr or
                TextureFormat.BayerGbrg or
                TextureFormat.BayerGrbg;
        }

        public static bool IsYuv(this TextureFormat format)
        {
            return format is
                TextureFormat.I420 or
                TextureFormat.I422 or
                TextureFormat.I444 or
                TextureFormat.YCbCr420 or
                TextureFormat.YCbCr422 or
                TextureFormat.YCbCr444 or
                TextureFormat.YCoCg420 or
                TextureFormat.YCoCg422 or
                TextureFormat.YCoCg444;
        }

        public static bool TileSizeMustBeEven(this TextureFormat format)
        {
            return format is
                TextureFormat.BayerRggb or
                TextureFormat.BayerBggr or
                TextureFormat.BayerGbrg or
                TextureFormat.BayerGrbg or
                TextureFormat.I420 or
                TextureFormat.I422 or
                TextureFormat.YCbCr420 or
                TextureFormat.YCbCr422 or
                TextureFormat.YCoCg420 or
                TextureFormat.YCoCg422;
        }
    }
}
