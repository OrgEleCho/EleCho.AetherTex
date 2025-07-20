using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EleCho.MegaTextures.Helpers;
using EleCho.MegaTextures.Internal;
using EleCho.MegaTextures.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace EleCho.MegaTextures
{
    public sealed unsafe partial class MegaTexture
    {
        const int TileMaxWidth = 8192;
        const int TileMaxHeight = 8192;

        private string[] _sources;
        private ExprSource? _defaultSource;

        private D3D11 _api;
        private D3DCompiler _compiler;

        private Texture2DDesc _texture2DDesc;
        private BufferDesc _vertexBufferDesc;

        private ComPtr<ID3D11Device> _device;
        private ComPtr<ID3D11DeviceContext> _deviceContext;
        private ComPtr<ID3D11Texture2D>[] _textures;
        private ComPtr<ID3D11Buffer> _vertexBuffer;
        private ComPtr<ID3D11Buffer> _indexBuffer;
        private ComPtr<ID3D10Blob> _vertexShaderBlob;
        private ComPtr<ID3D11VertexShader> _vertexShader;
        private ComPtr<ID3D11InputLayout> _inputLayout;

        private float[] _background = [1, 0, 0, 1];

        public TextureFormat Format { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public int Rows { get; }
        public int Columns { get; }

        public int Width => TileWidth * Columns;
        public int Height => TileHeight * Rows;

        public IReadOnlyList<string> Sources { get; }

        public ExprSource DefaultSource
            => _defaultSource ??= new ExprSource(this, _sources[0]);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <param name="sources"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public MegaTexture(
            TextureFormat format, int tileWidth, int tileHeight, int rows, int columns,
            IEnumerable<string> sources)
        {
            _sources = sources.ToArray();

            Format = format;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            Rows = rows;
            Columns = columns;
            Sources = _sources.AsReadOnly();

            _texture2DDesc = new Texture2DDesc()
            {
                Width = (uint)tileWidth,
                Height = (uint)tileHeight,
                ArraySize = (uint)(rows * columns),
                BindFlags = (uint)BindFlag.ShaderResource,
                CPUAccessFlags = 0,
                Format = format.ToDxFormat(),
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
            };

            _vertexBufferDesc = new BufferDesc()
            {
                BindFlags = (uint)BindFlag.VertexBuffer,
                ByteWidth = (uint)(sizeof(InputVertex) * 4),
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
                MiscFlags = 0,
                Usage = Usage.Dynamic
            };

            _api = D3D11.GetApi(null, false);
            _compiler = D3DCompiler.GetApi();

            int createDeviceError = _api.CreateDevice(ref Unsafe.NullRef<IDXGIAdapter>(), D3DDriverType.Hardware, 0, (uint)CreateDeviceFlag.Debug, ref Unsafe.NullRef<D3DFeatureLevel>(), 0, D3D11.SdkVersion, ref _device, null, ref _deviceContext);
            if (createDeviceError != 0)
            {
                throw new InvalidOperationException("Failed to create device");
            }

            _textures = new ComPtr<ID3D11Texture2D>[_sources.Length];
            for (int i = 0; i < _sources.Length; i++)
            {
                _textures[i] = DxUtils.CreateTexture2D(_device, _texture2DDesc);
            }

            _vertexBuffer = DxUtils.CreateBuffer(_device, _vertexBufferDesc);

            _vertexShaderBlob = DxUtils.Compile(_compiler, "shader", "vs_main", "vs_5_0", AssemblyResourceUtils.GetShaderBytes("MegaTexture.hlsl"), new Dictionary<string, string>()
            {
                ["SourceCount"] = _sources.Length.ToString(),
            });

            _vertexShader = DxUtils.CreateVertexShader(_device, _vertexShaderBlob);

            using var sematicPosition = new NativeString("MT_POSITION");
            using var sematicTexcoord = new NativeString("MT_TEXCOORD");


            ReadOnlySpan<InputElementDesc> inputElementDescSpan =
            [
                new InputElementDesc
                {
                    SemanticName = (byte*)sematicPosition.ASCII,
                    SemanticIndex = 0,
                    Format = Silk.NET.DXGI.Format.FormatR32G32Float,
                    InputSlot = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                },
                new InputElementDesc
                {
                    SemanticName = (byte*)sematicTexcoord.ASCII,
                    SemanticIndex = 0,
                    Format = Silk.NET.DXGI.Format.FormatR32G32Float,
                    InputSlot = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    AlignedByteOffset = 8,
                }
            ];

            _inputLayout = DxUtils.CreateInputLayout(_device, _vertexShaderBlob, inputElementDescSpan);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        public MegaTexture(
            TextureFormat format, int tileWidth, int tileHeight, int rows, int columns)
            : this(format, tileWidth, tileHeight, rows, columns, ["color"])
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="sources"></param>
        public MegaTexture(
            TextureFormat format, int width, int height,
            IEnumerable<string> sources) : this(
                format,
                Math.Min(width, TileMaxWidth),
                Math.Min(height, TileMaxHeight),
                (width + TileMaxWidth - 1) / TileMaxWidth,
                (height + TileMaxHeight - 1) / TileMaxHeight,
                sources)
        {

        }

        /// <summary>
        /// Creates a MegaTexture with the specified format, width, and height.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public MegaTexture(
            TextureFormat format, int width, int height) : this(format, width, height, ["color"])
        {

        }

        public ExprSource CreateSource(string expression)
        {
            return new ExprSource(this, expression);
        }

        private void UpdateVertices(QuadVectors quad)
        {
            MappedSubresource mappedSubResource = default;
            _deviceContext.Map(_vertexBuffer, 0, Map.WriteDiscard, 0, ref mappedSubResource);

            InputVertex* pVertices = (InputVertex*)mappedSubResource.PData;

            pVertices[0] = new InputVertex(-1, 1, quad.LeftTop.X, quad.LeftTop.Y);
            pVertices[1] = new InputVertex(1, 1, quad.RightTop.X, quad.RightTop.Y);
            pVertices[2] = new InputVertex(-1, -1, quad.LeftBottom.X, quad.LeftBottom.Y);
            pVertices[3] = new InputVertex(1, -1, quad.RightBottom.X, quad.RightBottom.Y);

            _deviceContext.Unmap(_vertexBuffer, 0);
        }

        private void Render(ExprSource source, TextureData buffer)
        {
            ShaderResourceViewDesc inputTextureShaderResourceViewDesc = new ShaderResourceViewDesc
            {
                Format = _texture2DDesc.Format,
                ViewDimension = D3DSrvDimension.D3D11SrvDimensionTexture2D,
            };

            inputTextureShaderResourceViewDesc.Texture2D.MipLevels = 1;
            inputTextureShaderResourceViewDesc.Texture2D.MostDetailedMip = 0;

            ComPtr<ID3D11ShaderResourceView>[] inputTextureShaderResourceViews = new ComPtr<ID3D11ShaderResourceView>[_textures.Length];
            for (int i = 0; i < _textures.Length; i++)
            {
                _device.CreateShaderResourceView(_textures[i], in inputTextureShaderResourceViewDesc, ref inputTextureShaderResourceViews[i]);
            }

            SamplerDesc samplerDesc = new SamplerDesc
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
            };

            ComPtr<ID3D11SamplerState> samplerState = default;
            _device.CreateSamplerState(in samplerDesc, ref samplerState);

            var viewport = new Viewport(0, 0, buffer.Width, buffer.Height, 0, 1);

            using var renderTargetTexture = DxUtils.CreateTexture2D(_device, new Texture2DDesc()
            {
                Width = (uint)buffer.Width,
                Height = (uint)buffer.Height,
                ArraySize = 1,
                BindFlags = (uint)BindFlag.RenderTarget,
                CPUAccessFlags = 0,
                Format = buffer.Format.ToDxFormat(),
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
            });

            var renderTargetView = DxUtils.CreateRenderTargetView(_device, renderTargetTexture, in Unsafe.NullRef<RenderTargetViewDesc>());

            _deviceContext.RSSetViewports(1, in viewport);
            _deviceContext.OMSetRenderTargets(1, ref renderTargetView, ref Unsafe.NullRef<ID3D11DepthStencilView>());

            _deviceContext.VSSetShader(_vertexShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
            source.ApplyPixelShader(_deviceContext);

            uint vertexStride = sizeof(float) * 4;
            uint vertexOffset = 0;
            _deviceContext.IASetVertexBuffers(0, 1, ref _vertexBuffer, in vertexStride, in vertexOffset);
            _deviceContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglestrip);
            _deviceContext.IASetInputLayout(_inputLayout);

            _deviceContext.PSSetShaderResources(0, (uint)inputTextureShaderResourceViews.Length, ref inputTextureShaderResourceViews[0]);
            _deviceContext.PSSetSamplers(0, 1, ref samplerState);

            _deviceContext.Draw(4, 0);

            DxUtils.CopyTexture(_device, _deviceContext, renderTargetTexture, 0, buffer);

            renderTargetView.Dispose();
        }

        public void Read(ExprSource source, QuadVectors quad, TextureData buffer)
        {
            UpdateVertices(quad);
            Render(source, buffer);
        }

        public void Read(QuadVectors quad, TextureData buffer)
            => Read(DefaultSource, quad, buffer);

        public void Write(TextureData data, int sourceIndex, int column, int row)
        {
            if (sourceIndex < 0 ||
                sourceIndex >= _textures.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            var texture = _textures[sourceIndex];
            var subResource = (uint)(row * Columns + column);

            var box = new Box(0, 0, 0, (uint)data.Width, (uint)data.Height, 1);
            _deviceContext.UpdateSubresource(texture, subResource, in box, (void*)data.BaseAddress, (uint)data.RowBytes, (uint)(data.RowBytes * data.Height));
        }

        public void Write(TextureData data, string source, int column, int row)
        {
            var sourceIndex = Array.IndexOf(_sources, source);
            if (sourceIndex == -1)
            {
                throw new ArgumentException("Source not exist", nameof(source));
            }

            Write(data, sourceIndex, column, row);
        }

        public void Write(TextureData data, int column, int row)
            => Write(data, _sources[0], column, row);
    }
}
