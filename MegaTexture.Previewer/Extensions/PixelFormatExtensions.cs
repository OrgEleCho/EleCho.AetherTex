using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using EleCho.AetherTex;

namespace AetherTex.Viewer.Extensions
{
    internal static class PixelFormatExtensions
    {
        public static PixelFormat ToWpf(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Bgra8888 => PixelFormats.Bgra32,
                TextureFormat.Rgba8888 => PixelFormats.Bgra32, // WPF does not support RGBA natively, so we use BGRA,

                TextureFormat.Gray8 => PixelFormats.Gray8,
                TextureFormat.Gray16 => PixelFormats.Gray16,

                TextureFormat.Float32 => PixelFormats.Gray32Float,
            };
        }
    }
}
