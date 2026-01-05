using System.Runtime.CompilerServices;
using EleCho.AetherTex.Converters;
using EleCho.AetherTex.Internal;
using EleCho.AetherTex.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace EleCho.AetherTex.Processing
{
    internal class Bgra8888OrRgba8888ToYuv420TileWriter : ITileWriter
    {
        private bool _disposedValue;
        private readonly ComPtr<ID3D11Device> _device;
        private readonly ComPtr<ID3D11DeviceContext> _deviceContext;
        private readonly PixelProcessor _ySplitProcessor;
        private readonly PixelProcessor _uvSplitProcessor;

        public TextureFormat Source { get; }

        public TextureFormat Target => TextureFormat.Yuv420;

        public Bgra8888OrRgba8888ToYuv420TileWriter(ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> deviceContext, TextureFormat source)
        {
            _device = device;
            _deviceContext = deviceContext;
            _ySplitProcessor = new PixelProcessor(device, deviceContext, "color",
                "0.299 * color.r + 0.587 * color.g + 0.114 * color.b, 0, 0, 1");
            _uvSplitProcessor = new PixelProcessor(device, deviceContext, "color",
                "-0.169 * color.r - 0.331 * color.g + 0.5 * color.b + 0.5, 0.5 * color.r - 0.419 * color.g - 0.081 * color.b + 0.5, 0, 1");

            Source = source;
        }

        public void WriteTile(TextureData textureData, ComPtr<ID3D11Texture2D> tileTexture1, ComPtr<ID3D11Texture2D>? tileTexture2, uint subResource)
        {
            if (textureData.Format != Source)
            {
                throw new ArgumentException("Invalid texture data, only bgra8888 is supported");
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
                format: Format.FormatB8G8R8A8Unorm,
                viewDimension: D3DSrvDimension.D3D11SrvDimensionTexture2D,
                texture2D: new Tex2DSrv(
                    mipLevels: 1,
                    mostDetailedMip: 0)));

            var copySrcBox = new Box(0, 0, 0, (uint)textureData.Width, (uint)textureData.Height, 1);

            using (var texture1Buffer = DxUtils.CreateTexture2D(_device, new Texture2DDesc()
            {
                Width = (uint)textureData.Width,
                Height = (uint)textureData.Height,
                ArraySize = 1,
                BindFlags = (uint)BindFlag.RenderTarget,
                CPUAccessFlags = 0,
                Format = Format.FormatR8Unorm,
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
            }))
            {
                using var rt = DxUtils.CreateRenderTargetView(_device, texture1Buffer);
                _ySplitProcessor.Process(shaderResourceView, rt, textureData.Width, textureData.Height);
                _deviceContext.CopySubresourceRegion(tileTexture1, subResource, 0, 0, 0, texture1Buffer, 0, in copySrcBox);
            }

            if (!tileTexture2.HasValue)
            {
                return;
            }

            using (var texture2Buffer = DxUtils.CreateTexture2D(_device, new Texture2DDesc()
            {
                Width = (uint)(textureData.Width / 2),
                Height = (uint)(textureData.Height / 2),
                ArraySize = 1,
                BindFlags = (uint)BindFlag.RenderTarget,
                CPUAccessFlags = 0,
                Format = Format.FormatR8G8Unorm,
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
            }))
            {
                using var rt = DxUtils.CreateRenderTargetView(_device, texture2Buffer);
                _uvSplitProcessor.Process(shaderResourceView, rt, textureData.Width / 2, textureData.Height / 2);
                _deviceContext.CopySubresourceRegion(tileTexture2.Value, subResource, 0, 0, 0, texture2Buffer, 0, in copySrcBox);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _ySplitProcessor.Dispose();
                _uvSplitProcessor.Dispose();
                _disposedValue = true;
            }
        }

        ~Bgra8888OrRgba8888ToYuv420TileWriter()
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
