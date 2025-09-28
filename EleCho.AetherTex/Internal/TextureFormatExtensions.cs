using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.DXGI;

namespace EleCho.AetherTex.Internal
{
    internal static class TextureFormatExtensions
    {
        public static Format ToDxFormat(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Bgra8888 => Format.FormatB8G8R8A8Unorm,
                TextureFormat.Rgba8888 => Format.FormatR8G8B8A8Unorm,

                TextureFormat.Gray8 => Format.FormatR8Unorm,
                TextureFormat.Gray16 => Format.FormatR16Unorm,

                TextureFormat.BayerRggb => Format.FormatR8Unorm,
                TextureFormat.BayerBggr => Format.FormatR8Unorm,
                TextureFormat.BayerGbrg => Format.FormatR8Unorm,
                TextureFormat.BayerGrbg => Format.FormatR8Unorm,

                TextureFormat.Float32 => Format.FormatR32Float,

                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported texture format")
            };
        }

        public static int GetPixelBytes(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Bgra8888 => 4,
                TextureFormat.Rgba8888 => 4,

                TextureFormat.Gray8 => 1,
                TextureFormat.Gray16 => 2,
                TextureFormat.Float32 => 4,

                TextureFormat.BayerRggb => 1,
                TextureFormat.BayerBggr => 1,
                TextureFormat.BayerGbrg => 1,
                TextureFormat.BayerGrbg => 1,

                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported texture format")
            };
        }

        public static bool IsBayer(this TextureFormat format)
        {
            return format is 
                TextureFormat.BayerRggb or 
                TextureFormat.BayerBggr or 
                TextureFormat.BayerGbrg or 
                TextureFormat.BayerGrbg;
        }
    }
}
