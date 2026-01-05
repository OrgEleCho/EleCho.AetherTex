using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EleCho.AetherTex.Converters;
using EleCho.AetherTex.Internal;
using EleCho.AetherTex.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace EleCho.AetherTex.Processing
{
    internal interface ITileWriter : IDisposable
    {
        public TextureFormat Source { get; }
        public TextureFormat Target { get; }

        public void WriteTile(
            TextureData textureData,
            ComPtr<ID3D11Texture2D> tileTexture1,
            ComPtr<ID3D11Texture2D>? tileTexture2,
            uint subResource);
    }
}
