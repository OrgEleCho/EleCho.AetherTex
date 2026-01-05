using EleCho.AetherTex.Converters;
using EleCho.AetherTex.Internal;
using EleCho.AetherTex.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace EleCho.AetherTex.Processing
{
    internal class Bgra8888OrRgba8888ToRgba8888OrBgra8888TileWriter : ITileWriter
    {
        private ComPtr<ID3D11Device> _device;
        private ComPtr<ID3D11DeviceContext> _deviceContext;
        private PixelProcessor _colorSpaceConvertionHelper;
        private bool _disposedValue;

        public TextureFormat Source { get; }
        public TextureFormat Target { get; }

        public Bgra8888OrRgba8888ToRgba8888OrBgra8888TileWriter(
            ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> deviceContext,
            TextureFormat source, TextureFormat target)
        {
            _device = device;
            _deviceContext = deviceContext;
            _colorSpaceConvertionHelper = new PixelProcessor(device, deviceContext, "color", "color");
            Source = source;
            Target = target;
        }

        public void WriteTile(TextureData textureData, ComPtr<ID3D11Texture2D> tileTexture1, ComPtr<ID3D11Texture2D>? tileTexture2, uint subResource)
        {
            if (textureData.Format != Source)
            {
                throw new ArgumentException($"Invalid texture data, only {Source} is supported");
            }

            var sourceDxTextureFormat = textureData.Format.ToDirectX();
            var targetDxTextureFormat = Target.ToDirectX();

            using var texture = DxUtils.CreateTexture2D(_device, textureData, new Texture2DDesc()
            {
                Width = (uint)textureData.Width,
                Height = (uint)textureData.Height,
                ArraySize = (uint)1,
                BindFlags = (uint)BindFlag.ShaderResource,
                CPUAccessFlags = 0,
                Format = sourceDxTextureFormat,
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
                Format = targetDxTextureFormat,
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
            }))
            {
                using var rt = DxUtils.CreateRenderTargetView(_device, texture1Buffer);
                _colorSpaceConvertionHelper.Process(shaderResourceView, rt, textureData.Width, textureData.Height);
                _deviceContext.CopySubresourceRegion(tileTexture1, subResource, 0, 0, 0, texture1Buffer, 0, in copySrcBox);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                _colorSpaceConvertionHelper.Dispose();
                _disposedValue = true;
            }
        }

        ~Bgra8888OrRgba8888ToRgba8888OrBgra8888TileWriter()
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
