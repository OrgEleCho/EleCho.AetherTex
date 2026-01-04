using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace EleCho.AetherTex.Processing
{
    internal class Bgra8888ToYuvTileWriter : ITileWriter
    {
        public TextureFormat Source => TextureFormat.Bgra8888;

        public TextureFormat Target => TextureFormat.Yuv420;

        public void WriteTile(TextureData textureData, ComPtr<ID3D11Texture2D> tileTexture1, ComPtr<ID3D11Texture2D> tileTexture2)
        {

        }
    }
}
