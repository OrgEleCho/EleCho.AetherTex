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

                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported texture format")
            };
        }
    }
}
