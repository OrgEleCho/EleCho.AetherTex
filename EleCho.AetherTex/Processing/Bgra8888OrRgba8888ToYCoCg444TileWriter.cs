using System.Runtime.CompilerServices;
using EleCho.AetherTex.Converters;
using EleCho.AetherTex.Internal;
using EleCho.AetherTex.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace EleCho.AetherTex.Processing
{
    internal class Bgra8888OrRgba8888ToYCoCg444TileWriter : ITileWriter
    {
        private bool _disposedValue;
        private readonly ComPtr<ID3D11Device> _device;
        private readonly ComPtr<ID3D11DeviceContext> _deviceContext;
        private readonly PixelProcessor _ycocgProcessor;

        public TextureFormat Source { get; }

        public TextureFormat Target => TextureFormat.YCoCg444;

        public Bgra8888OrRgba8888ToYCoCg444TileWriter(ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> deviceContext, TextureFormat source)
        {
            _device = device;
            _deviceContext = deviceContext;
            
            // YCoCg-R formulas packed into RGBA:
            // Y  = B + (R - B) * 0.5 + (G - B - (R - B) * 0.5) * 0.5
            // Co = (R - B) * 0.5 + 0.5
            // Cg = (G - B - (R - B) * 0.5) * 0.5 + 0.5
            
            _ycocgProcessor = new PixelProcessor(device, deviceContext, "color",
                "color.b + (color.r - color.b) * 0.5 + (color.g - (color.b + (color.r - color.b) * 0.5)) * 0.5, (color.r - color.b) * 0.5 + 0.5, (color.g - (color.b + (color.r - color.b) * 0.5)) * 0.5 + 0.5, 1");

            Source = source;
        }

        public void WriteTile(TextureData textureData, ComPtr<ID3D11Texture2D> tileTexture1, ComPtr<ID3D11Texture2D>? tileTexture2, uint subResource)
        {
            if (textureData.Format != Source)
            {
                throw new ArgumentException($"Invalid texture data, only {Source} is supported");
            }

            using var texture = DxUtils.CreateTexture2D(_device, textureData, new Texture2DDesc()
            {
                Width = (uint)textureData.Width,
                Height = (uint)textureData.Height,
                ArraySize = (uint)1,
                BindFlags = (uint)BindFlag.ShaderResource,
                CPUAccessFlags = 0,
                Format = Source.ToDirectX(),
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
            });

            using var shaderResourceView = DxUtils.CreateShaderResourceView(_device, texture, new ShaderResourceViewDesc(
                format: Source.ToDirectX(),
                viewDimension: D3DSrvDimension.D3D11SrvDimensionTexture2D,
                texture2D: new Tex2DSrv(
                    mipLevels: 1,
                    mostDetailedMip: 0)));

            var copySrcBox = new Box(0, 0, 0, (uint)textureData.Width, (uint)textureData.Height, 1);

            using (var textureBuffer = DxUtils.CreateTexture2D(_device, new Texture2DDesc()
            {
                Width = (uint)textureData.Width,
                Height = (uint)textureData.Height,
                ArraySize = 1,
                BindFlags = (uint)BindFlag.RenderTarget,
                CPUAccessFlags = 0,
                Format = Format.FormatR8G8B8A8Unorm,
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
            }))
            {
                using var rt = DxUtils.CreateRenderTargetView(_device, textureBuffer);
                _ycocgProcessor.Process(shaderResourceView, rt, textureData.Width, textureData.Height);
                _deviceContext.CopySubresourceRegion(tileTexture1, subResource, 0, 0, 0, textureBuffer, 0, in copySrcBox);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _ycocgProcessor.Dispose();
                _disposedValue = true;
            }
        }

        ~Bgra8888OrRgba8888ToYCoCg444TileWriter()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
