using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.DXGI;

namespace EleCho.MegaTextures.Internal
{
    internal static class TextureFormatExtensions
    {
        public static Format ToDxFormat(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Bgra8888 => Format.FormatB8G8R8A8Unorm,
                TextureFormat.Rgba8888 => Format.FormatR8G8B8A8Unorm,

                TextureFormat.UInt8 => Format.FormatR8Uint,
                TextureFormat.UInt16 => Format.FormatR16Uint,
                TextureFormat.Float32 => Format.FormatR32Float,

                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported texture format")
            };
        }
    }
}
