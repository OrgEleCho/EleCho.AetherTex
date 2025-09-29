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
                TextureFormat.I420;
        }
    }
}
