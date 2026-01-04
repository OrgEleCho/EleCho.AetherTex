using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using EleCho.AetherTex.Helpers;
using EleCho.AetherTex.Internal;
using EleCho.AetherTex.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace EleCho.AetherTex
{
    public sealed unsafe partial class AetherTexImage : IDisposable
    {
        const int TileMaxWidth = 8192;
        const int TileMaxHeight = 8192;
        const ulong FormatTag = 0x41525458494D4745; // ARTXIMGE
        const int CurrentFormatVersion = 1;

        private readonly string[] _sources;
        private readonly Dictionary<string, (nint DataPointer, int DataSize)> _openedShaderSources;
        private readonly bool _keepDeviceAlive;
        private Delegate _funcIncludeOpen;
        private Delegate _funcIncludeClose;

        private ExprSource? _defaultSource;

        private D3DCompiler _compiler;

        private Texture2DDesc _texture2DDesc;
        private Texture2DDesc? _texture2DDesc2;
        private BufferDesc _vertexBufferDesc;
        private BufferDesc _constantBufferDesc;
        private SamplerDesc _samplerDesc;

        private ComPtr<ID3DInclude> _include;
        private ComPtr<ID3D11Device> _device;
        private ComPtr<ID3D11DeviceContext> _deviceContext;
        private ComPtr<ID3D11Texture2D>[] _textures;
        private ComPtr<ID3D11Texture2D>[]? _textures2;
        private ComPtr<ID3D11ShaderResourceView>[] _textureViews;
        private ComPtr<ID3D11ShaderResourceView>[]? _textureViews2;
        private ComPtr<ID3D11Buffer> _vertexBuffer;
        private ComPtr<ID3D11Buffer> _constantBuffer;
        private ComPtr<ID3D10Blob> _vertexShaderBlob;
        private ComPtr<ID3D11VertexShader> _vertexShader;
        private ComPtr<ID3D11InputLayout> _inputLayout;
        private ComPtr<ID3D11SamplerState> _samplerState;

        // rendering cache
        private int _lastRenderWidth;
        private int _lastRenderHeight;
        private TransformMatrix? _lastRenderTransform;
        private ComPtr<ID3D11Texture2D> _renderTarget;
        private ComPtr<ID3D11RenderTargetView> _renderTargetView;

        private float[] _background = [0, 0, 0, 0];
        private bool _disposedValue;

        public AetherTexImageOptions Options { get; }

        public TextureFormat Format { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public int EdgeSize { get; }
        public int Rows { get; }
        public int Columns { get; }

        public int Width => (TileWidth - EdgeSize - EdgeSize) * Columns;
        public int Height => (TileHeight - EdgeSize - EdgeSize) * Rows;

        public IReadOnlyList<string> Sources { get; }

        public ExprSource DefaultSource
            => _defaultSource ??= new ExprSource(this, _sources[0]);

        public QuadVectors FullQuad { get; }

        private static ComPtr<ID3D11DeviceContext> CreateDeviceContext()
        {
            var api = D3D11.GetApi(null, false);

            ComPtr<ID3D11Device> device = default;
            ComPtr<ID3D11DeviceContext> deviceContext = default;

            int createDeviceError = api.CreateDevice(ref Unsafe.NullRef<IDXGIAdapter>(), D3DDriverType.Hardware, 0, (uint)CreateDeviceFlag.Debug, ref Unsafe.NullRef<D3DFeatureLevel>(), 0, D3D11.SdkVersion, ref device, null, ref deviceContext);
            if (createDeviceError != 0)
            {
                throw new InvalidOperationException("Failed to create device");
            }

            return deviceContext;
        }

        private static void VerifyTileSize(TextureFormat format, int tileWidth, int tileHeight)
        {
            if (format.TileSizeMustBeEven())
            {
                if (tileWidth % 2 != 0 ||
                    tileHeight % 2 != 0)
                {
                    throw new ArgumentException("Tile width and height must be even numbers for specified format");
                }
            }
        }

        private static string GetShaderEntryPointFileName(TextureFormat textureFormat)
        {
            return textureFormat switch
            {
                TextureFormat.BayerRggb => "AetherTexImageBayerRggb.hlsl",
                TextureFormat.BayerBggr => "AetherTexImageBayerBggr.hlsl",
                TextureFormat.BayerGrbg => "AetherTexImageBayerGrbg.hlsl",
                TextureFormat.BayerGbrg => "AetherTexImageBayerGbrg.hlsl",
                TextureFormat.I444 => "AetherTexImageI444.hlsl",
                TextureFormat.I422 => "AetherTexImageI422.hlsl",
                TextureFormat.I420 => "AetherTexImageI420.hlsl",
                TextureFormat.Yuv420 => "AetherTexImageYuv420.hlsl",
                _ => "AetherTexImage.hlsl",
            };
        }

        /// <summary>
        /// 获取第一个纹理描述
        /// </summary>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="arraySize"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static Texture2DDesc GetTextureDesc(TextureFormat format, int width, int height, int arraySize)
        {
            var dxPixelFormat = format switch
            {
                TextureFormat.Bgra8888 => Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm,
                TextureFormat.Rgba8888 => Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm,

                TextureFormat.Gray8 => Silk.NET.DXGI.Format.FormatR8Unorm,
                TextureFormat.Gray16 => Silk.NET.DXGI.Format.FormatR16Unorm,

                TextureFormat.BayerRggb => Silk.NET.DXGI.Format.FormatR8Unorm,
                TextureFormat.BayerBggr => Silk.NET.DXGI.Format.FormatR8Unorm,
                TextureFormat.BayerGbrg => Silk.NET.DXGI.Format.FormatR8Unorm,
                TextureFormat.BayerGrbg => Silk.NET.DXGI.Format.FormatR8Unorm,

                TextureFormat.I444 => Silk.NET.DXGI.Format.FormatR8Unorm,
                TextureFormat.I422 => Silk.NET.DXGI.Format.FormatR8Unorm,
                TextureFormat.I420 => Silk.NET.DXGI.Format.FormatR8Unorm,

                TextureFormat.Yuv420 => Silk.NET.DXGI.Format.FormatR8Unorm,

                TextureFormat.Float32 => Silk.NET.DXGI.Format.FormatR32Float,

                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported texture format")
            };

            return format switch
            {
                TextureFormat.I444 => new Texture2DDesc()
                {
                    Width = (uint)width,
                    Height = (uint)(height * 3),
                    ArraySize = (uint)arraySize,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    CPUAccessFlags = 0,
                    Format = dxPixelFormat,
                    MipLevels = 1,
                    MiscFlags = 0,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                },
                TextureFormat.I422 => new Texture2DDesc()
                {
                    Width = (uint)width,
                    Height = (uint)(height * 2),
                    ArraySize = (uint)arraySize,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    CPUAccessFlags = 0,
                    Format = dxPixelFormat,
                    MipLevels = 1,
                    MiscFlags = 0,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                },
                TextureFormat.I420 => new Texture2DDesc()
                {
                    Width = (uint)width,
                    Height = (uint)(height + height / 2),
                    ArraySize = (uint)arraySize,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    CPUAccessFlags = 0,
                    Format = dxPixelFormat,
                    MipLevels = 1,
                    MiscFlags = 0,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                },
                TextureFormat.Yuv420 => new Texture2DDesc()
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    ArraySize = (uint)arraySize,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    CPUAccessFlags = 0,
                    Format = dxPixelFormat,
                    MipLevels = 1,
                    MiscFlags = 0,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                },
                _ => new Texture2DDesc()
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    ArraySize = (uint)arraySize,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    CPUAccessFlags = 0,
                    Format = dxPixelFormat,
                    MipLevels = 1,
                    MiscFlags = 0,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                }
            };
        }

        /// <summary>
        /// 获取第二个纹理描述 (需要多个不同大小或格式的纹理组合的图像格式才需要实现这个. 例如 YUV420)
        /// </summary>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="arraySize"></param>
        /// <returns></returns>
        private static Texture2DDesc? GetTextureDesc2(TextureFormat format, int width, int height, int arraySize)
        {
            return format switch
            {
                TextureFormat.Yuv420 => new Texture2DDesc()
                {
                    Width = (uint)(width / 2),
                    Height = (uint)(height / 2),
                    ArraySize = (uint)arraySize,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    CPUAccessFlags = 0,
                    Format = Silk.NET.DXGI.Format.FormatR8G8Unorm,
                    MipLevels = 1,
                    MiscFlags = 0,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                },
                _ => null,
            };
        }

        private static void InitializeTexturesAndShaderResourceViews(
            ComPtr<ID3D11Device> device,
            Texture2DDesc texture2DDesc,
            int count,
            out ComPtr<ID3D11Texture2D>[] textures,
            out ComPtr<ID3D11ShaderResourceView>[] shaderResourceViews)
        {
            ShaderResourceViewDesc inputTextureShaderResourceViewDesc = new ShaderResourceViewDesc(
                format: texture2DDesc.Format,
                viewDimension: D3DSrvDimension.D3D11SrvDimensionTexture2D,
                texture2D: new Tex2DSrv(
                    mipLevels: 1,
                    mostDetailedMip: 0));

            textures = new ComPtr<ID3D11Texture2D>[count];
            shaderResourceViews = new ComPtr<ID3D11ShaderResourceView>[count];

            for (int i = 0; i < count; i++)
            {
                textures[i] = DxUtils.CreateTexture2D(device, texture2DDesc);
                device.CreateShaderResourceView(textures[i], in inputTextureShaderResourceViewDesc, ref shaderResourceViews[i]);
            }
        }

        private int D3DIncludeOpen(
            ID3DInclude* self, D3DIncludeType includeType, byte* pFileName, void* pParentData, void** ppData, uint* pBytesPtr)
        {
            if (includeType != D3DIncludeType.D3DIncludeSystem)
            {
                return unchecked((int)0x80004001); // E_NOTIMPL
            }

            string fileName = Marshal.PtrToStringAnsi((nint)pFileName) ?? string.Empty;
            if (_openedShaderSources.TryGetValue(fileName, out var existData))
            {
                *ppData = (void*)existData.DataPointer;
                *pBytesPtr = (uint)existData.DataSize;
                return 0; // S_OK
            }

            var bytes = AssemblyResourceUtils.GetShaderBytes(fileName);
            if (bytes is null)
            {
                return unchecked((int)0x80070002); // HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)
            }

            var nativeMem = NativeMemory.AllocZeroed((nuint)bytes.Length);
            fixed (byte* pBytes = bytes)
            {
                NativeMemory.Copy(pBytes, nativeMem, (nuint)bytes.Length);
            }

            _openedShaderSources[fileName] = ((nint)nativeMem, bytes.Length);

            *ppData = nativeMem;
            *pBytesPtr = (uint)bytes.Length;
            return 0; // S_OK
        }

        private int D3DIncludeClose(ID3DInclude* self, void* pData)
        {
            var dataPointer = (nint)pData;
            var item = _openedShaderSources.FirstOrDefault(kv => kv.Value.DataPointer == dataPointer);
            if (item.Key is not null)
            {
                NativeMemory.Free((void*)dataPointer);
                _openedShaderSources.Remove(item.Key);
            }

            return 0; // S_OK
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceContext"></param>
        /// <param name="keepDeviceAlive"></param>
        /// <param name="format"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        /// <param name="edgeSize"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <param name="sources"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AetherTexImage(
            ComPtr<ID3D11DeviceContext> deviceContext, bool keepDeviceAlive,
            TextureFormat format, int tileWidth, int tileHeight, int edgeSize, int rows, int columns,
            IEnumerable<string> sources)
        {
            if (deviceContext.Handle == null)
            {
                throw new ArgumentNullException(nameof(deviceContext));
            }

            deviceContext.GetDevice(ref _device);
            _deviceContext = deviceContext;
            _keepDeviceAlive = keepDeviceAlive;

            // ensure tile size valid
            VerifyTileSize(format, tileWidth, tileHeight);

            _sources = sources.ToArray();
            _openedShaderSources = new();

            Options = new AetherTexImageOptions(this);
            Format = format;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            EdgeSize = edgeSize;
            Rows = rows;
            Columns = columns;
            Sources = _sources.AsReadOnly();

            FullQuad = new QuadVectors(
                new Vector2(0, 0),
                new Vector2(Width, 0),
                new Vector2(Width, Height),
                new Vector2(0, Height));

            var includeFunctions = (nint*)NativeMemory.Alloc((nuint)(sizeof(nint) * 2));
            _funcIncludeOpen = D3DIncludeOpen;
            _funcIncludeClose = D3DIncludeClose;
            includeFunctions[0] = Marshal.GetFunctionPointerForDelegate(_funcIncludeOpen);
            includeFunctions[1] = Marshal.GetFunctionPointerForDelegate(_funcIncludeClose);

            var include = (ID3DInclude*)NativeMemory.Alloc((nuint)sizeof(ID3DInclude));
            include[0] = new ID3DInclude((void**)includeFunctions);

            _include = new ComPtr<ID3DInclude>(include);

            _texture2DDesc = GetTextureDesc(format, tileWidth, tileHeight, rows * columns);
            _texture2DDesc2 = GetTextureDesc2(format, tileWidth, tileHeight, rows * columns);

            _vertexBufferDesc = new BufferDesc()
            {
                BindFlags = (uint)BindFlag.VertexBuffer,
                ByteWidth = (uint)(sizeof(InputVertex) * 4),
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
                MiscFlags = 0,
                Usage = Usage.Dynamic
            };

            _constantBufferDesc = new BufferDesc()
            {
                BindFlags = (uint)BindFlag.ConstantBuffer,
                ByteWidth = (uint)(sizeof(float) * 4 * 3),
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
                MiscFlags = 0,
                Usage = Usage.Dynamic
            };

            ShaderResourceViewDesc inputTextureShaderResourceViewDesc = new ShaderResourceViewDesc(
                format: _texture2DDesc.Format,
                viewDimension: D3DSrvDimension.D3D11SrvDimensionTexture2D,
                texture2D: new Tex2DSrv(
                    mipLevels: 1,
                    mostDetailedMip: 0));

            _compiler = D3DCompiler.GetApi();


            InitializeTexturesAndShaderResourceViews(_device, _texture2DDesc, _sources.Length, out _textures, out _textureViews);
            if (_texture2DDesc2 is { } texture2DDesc2)
            {
                InitializeTexturesAndShaderResourceViews(_device, texture2DDesc2, _sources.Length, out _textures2, out _textureViews2);
            }

            _vertexBuffer = DxUtils.CreateBuffer(_device, _vertexBufferDesc);
            _constantBuffer = DxUtils.CreateBuffer(_device, _constantBufferDesc);

            _vertexShaderBlob = DxUtils.Compile(_compiler, "shader", "vs_main", "vs_5_0", AssemblyResourceUtils.GetShaderBytes("AetherTexImage.hlsl"), new Dictionary<string, string>()
            {
                ["SourceCount"] = _sources.Length.ToString(),
            }, _include);

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
        /// <param name="sources"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public AetherTexImage(
            TextureFormat format, int tileWidth, int tileHeight, int edgeSize, int rows, int columns,
            IEnumerable<string> sources) : this(CreateDeviceContext(), false, format, tileWidth, tileHeight, edgeSize, rows, columns, sources)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        /// <param name="edgeSize"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        public AetherTexImage(
            TextureFormat format, int tileWidth, int tileHeight, int edgeSize, int rows, int columns)
            : this(format, tileWidth, tileHeight, edgeSize, rows, columns, ["color"])
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <param name="sources"></param>
        public AetherTexImage(
            TextureFormat format, int tileWidth, int tileHeight, int rows, int columns,
            IEnumerable<string> sources)
            : this(format, tileWidth, tileHeight, 0, rows, columns, sources)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        public AetherTexImage(
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
        public AetherTexImage(
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
        /// Creates a AetherTexImage with the specified format, width, and height.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public AetherTexImage(
            TextureFormat format, int width, int height) : this(format, width, height, ["color"])
        {

        }

        private void EnsureNotDisposed()
        {
            if (_disposedValue)
            {
                throw new InvalidOperationException("Object disposed");
            }
        }

        private Filter GetSamplerFilter()
        {
            return Options.UsePointSampling ?
                Filter.MinMagMipPoint :
                Filter.MinMagMipLinear;
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

        private void UpdateTransform(TransformMatrix transformMatrix)
        {
            if (_lastRenderTransform == transformMatrix)
            {
                return;
            }

            _lastRenderTransform = transformMatrix;

            // 因为需要对图像进行变换, 而着色器里面是对采样点位置做变换
            // 所以这里需要向 DX 传入逆矩阵
            transformMatrix.Invert();

            MappedSubresource mappedSubResource = default;
            _deviceContext.Map(_constantBuffer, 0, Map.WriteDiscard, 0, ref mappedSubResource);

            float* pMatrixElements = (float*)mappedSubResource.PData;

            pMatrixElements[0] = transformMatrix.ScaleX;
            pMatrixElements[1] = transformMatrix.SkewX;
            pMatrixElements[2] = transformMatrix.TransX;
            pMatrixElements[3] = 0;

            pMatrixElements[4] = transformMatrix.SkewY;
            pMatrixElements[5] = transformMatrix.ScaleY;
            pMatrixElements[6] = transformMatrix.TransY;
            pMatrixElements[7] = 0;

            pMatrixElements[8] = transformMatrix.PerspX;
            pMatrixElements[9] = transformMatrix.PerspY;
            pMatrixElements[10] = transformMatrix.PerspZ;
            pMatrixElements[11] = 0;

            _deviceContext.Unmap(_constantBuffer, 0);
        }

        private void Render(ExprSource source, TextureData buffer)
        {
            if (!ReferenceEquals(source.Owner, this))
            {
                throw new ArgumentException("Source is not from current texture");
            }

            var dxPixelFormat = buffer.Format switch
            {
                TextureFormat.Bgra8888 => Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm,
                TextureFormat.Rgba8888 => Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm,

                TextureFormat.Gray8 => Silk.NET.DXGI.Format.FormatR8Unorm,
                TextureFormat.Gray16 => Silk.NET.DXGI.Format.FormatR16Unorm,

                TextureFormat.Float32 => Silk.NET.DXGI.Format.FormatR32Float,

                _ => throw new ArgumentOutOfRangeException(nameof(buffer), buffer, "Unsupported texture format")
            };

            var viewport = new Viewport(0, 0, buffer.Width, buffer.Height, 0, 1);
            var samplerFilter = GetSamplerFilter();

            if (_samplerDesc.Filter != samplerFilter)
            {
                _samplerState.Dispose();
                _samplerState = default;
            }

            if (buffer.Width != _lastRenderWidth ||
                buffer.Height != _lastRenderHeight)
            {
                _renderTargetView.Dispose();
                _renderTargetView = default;

                _renderTarget.Dispose();
                _renderTarget = default;
            }

            if (_samplerState.Handle is null)
            {
                _samplerDesc = new SamplerDesc
                {
                    Filter = samplerFilter,
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Clamp,
                };

                _samplerState = DxUtils.CreateSamplerState(_device, _samplerDesc);
            }

            if (_renderTarget.Handle is null)
            {
                _renderTarget = DxUtils.CreateTexture2D(_device, new Texture2DDesc()
                {
                    Width = (uint)buffer.Width,
                    Height = (uint)buffer.Height,
                    ArraySize = 1,
                    BindFlags = (uint)BindFlag.RenderTarget,
                    CPUAccessFlags = 0,
                    Format = dxPixelFormat,
                    MipLevels = 1,
                    MiscFlags = 0,
                    SampleDesc = new SampleDesc(1, 0),
                    Usage = Usage.Default,
                });

                _renderTargetView = DxUtils.CreateRenderTargetView(_device, _renderTarget, in Unsafe.NullRef<RenderTargetViewDesc>());
            }

            // clear as transparent
            _deviceContext.ClearRenderTargetView(_renderTargetView, ref _background[0]);

            _deviceContext.RSSetViewports(1, in viewport);
            _deviceContext.OMSetRenderTargets(1, ref _renderTargetView, ref Unsafe.NullRef<ID3D11DepthStencilView>());

            _deviceContext.VSSetShader(_vertexShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
            source.ApplyPixelShader(_deviceContext);

            uint vertexStride = sizeof(float) * 4;
            uint vertexOffset = 0;
            _deviceContext.IASetVertexBuffers(0, 1, ref _vertexBuffer, in vertexStride, in vertexOffset);
            _deviceContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglestrip);
            _deviceContext.IASetInputLayout(_inputLayout);

            _deviceContext.PSSetShaderResources(0, (uint)_textureViews.Length, ref _textureViews[0]);
            if (_textureViews2 is { })
            {
                _deviceContext.PSSetShaderResources(1, (uint)_textureViews2.Length, ref _textureViews2[0]);
            }

            _deviceContext.PSSetSamplers(0, 1, ref _samplerState);
            _deviceContext.PSSetConstantBuffers(0, 1, ref _constantBuffer);

            _deviceContext.Draw(4, 0);

            DxUtils.CopyTexture(_device, _deviceContext, _renderTarget, 0, buffer);

            _deviceContext.Flush();
            _deviceContext.ClearState();

            if (!Options.EnableRenderBufferCaching)
            {
                _renderTargetView.Dispose();
                _renderTarget.Dispose();

                _renderTargetView = default;
                _renderTarget = default;
            }

            _lastRenderWidth = buffer.Width;
            _lastRenderHeight = buffer.Height;
        }

        public ExprSource CreateSource(string expression)
        {
            EnsureNotDisposed();

            return new ExprSource(this, expression);
        }

        public void Read(ExprSource source, TransformMatrix transform, QuadVectors quad, TextureData buffer)
        {
            EnsureNotDisposed();

            UpdateVertices(quad);
            UpdateTransform(transform);
            Render(source, buffer);
        }

        public void Read(ExprSource source, QuadVectors quad, TextureData buffer)
            => Read(source, TransformMatrix.Identity, quad, buffer);

        public void Read(TransformMatrix transform, QuadVectors quad, TextureData buffer)
            => Read(DefaultSource, transform, quad, buffer);

        public void Read(QuadVectors quad, TextureData buffer)
            => Read(DefaultSource, TransformMatrix.Identity, quad, buffer);

        private void EnsureCanWrite(TextureFormat dataFormat, int dataWidth, int dataHeight)
        {
            if (dataFormat != Format)
            {
                throw new InvalidOperationException("Data format not match");
            }

            if (Format is TextureFormat.I420 or TextureFormat.I422 or TextureFormat.I444)
            {
                if (dataWidth != TileWidth ||
                    dataHeight != TileHeight)
                {
                    throw new InvalidOperationException("Data size must equals tile size if texture format is yuv planar");
                }
            }
            else if (Format.IsBayer())
            {
                if (dataWidth % 2 != 0 ||
                    dataHeight % 2 != 0)
                {
                    throw new InvalidOperationException("Invalid bayer data, data size must be even number");
                }
            }
        }

        private Box GetBoxForWriting(int dataWidth, int dataHeight, nint dataRowBytes, out nint depthPitch)
        {
            depthPitch = Format switch
            {
                TextureFormat.I444 => dataRowBytes * (dataHeight * 3),
                TextureFormat.I422 => dataRowBytes * (dataHeight * 2),
                TextureFormat.I420 => dataRowBytes * (dataHeight + dataHeight / 2),
                _ => dataRowBytes * dataHeight,
            };

            return Format switch
            {
                TextureFormat.I444 => new Box(0, 0, 0, (uint)dataWidth, (uint)(dataHeight * 3), 1),
                TextureFormat.I422 => new Box(0, 0, 0, (uint)dataWidth, (uint)(dataHeight * 2), 1),
                TextureFormat.I420 => new Box(0, 0, 0, (uint)dataWidth, (uint)(dataHeight + dataHeight / 2), 1),
                _ => new Box(0, 0, 0, (uint)Math.Min(TileWidth, dataWidth), (uint)Math.Min(TileHeight, dataHeight), 1)
            };
        }

        public void Write(TextureData data, int sourceIndex, int column, int row)
        {
            EnsureNotDisposed();
            EnsureCanWrite(data.Format, data.Width, data.Height);

            if (sourceIndex < 0 ||
                sourceIndex >= _textures.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            var texture = _textures[sourceIndex];
            var subResource = (uint)(row * Columns + column);

            var box = GetBoxForWriting(data.Width, data.Height, data.RowBytes, out var depthPitch);
            _deviceContext.UpdateSubresource(texture, subResource, in box, (void*)data.BaseAddress, (uint)data.RowBytes, (uint)depthPitch);
        }

        public void Write(TextureData data, string source, int column, int row)
        {
            EnsureNotDisposed();

            var sourceIndex = Array.IndexOf(_sources, source);
            if (sourceIndex == -1)
            {
                throw new ArgumentException("Source not exist", nameof(source));
            }

            Write(data, sourceIndex, column, row);
        }

        public void Write(TextureData data, int column, int row)
            => Write(data, _sources[0], column, row);

        private void ReadTile(int sourceIndex, TextureData buffer, int column, int row)
        {
            EnsureNotDisposed();

            if (sourceIndex < 0 ||
                sourceIndex >= _textures.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            if (column < 0 || column >= Columns ||
                row < 0 || row >= Rows)
            {
                throw new ArgumentOutOfRangeException("Column or row out of range");
            }

            var texture = _textures[sourceIndex];
            var subResource = (uint)(row * Columns + column);
            var box = new Box(0, 0, 0, (uint)Math.Min(TileWidth, buffer.Width), (uint)Math.Min(TileHeight, buffer.Height), 1);

            DxUtils.CopyTexture(_device, _deviceContext, texture, subResource, buffer);
        }

        private static ISerializer GetSerializer(int version)
        {
            return version switch
            {
                1 => SerializerV1.Instance,
                _ => throw new ArgumentException($"No serializer for version {version}", nameof(version))
            };
        }

        public static void Serialize(AetherTexImage image, Stream destination)
        {
            var version = CurrentFormatVersion;
            var serializer = GetSerializer(version);

            BinaryWriter binaryWriter = new BinaryWriter(destination, Encoding.UTF8, true);
            binaryWriter.Write(FormatTag);
            binaryWriter.Write((byte)version);

            serializer.Serialize(image, binaryWriter);
        }

        public static AetherTexImage Deserialize(Stream source)
        {
            BinaryReader binaryReader = new BinaryReader(source);
            ulong formatTag = binaryReader.ReadUInt64();
            if (formatTag != FormatTag)
            {
                throw new InvalidDataException($"Invalid format tag: {formatTag}");
            }

            var version = binaryReader.ReadByte();
            var serializer = GetSerializer(version);

            return serializer.Deserialize(binaryReader);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                foreach (var openedShaderSource in _openedShaderSources.Values)
                {
                    NativeMemory.Free((void*)openedShaderSource.DataPointer);
                }
                _openedShaderSources.Clear();

                var includeValue = *_include.Handle;
                NativeMemory.Free(includeValue.LpVtbl);
                NativeMemory.Free(_include.Handle);

                _deviceContext.Dispose();
                _samplerState.Dispose();
                _inputLayout.Dispose();
                _vertexShader.Dispose();
                _vertexShaderBlob.Dispose();
                _vertexBuffer.Dispose();

                for (int i = 0; i < _textures.Length; i++)
                {
                    _textureViews[i].Dispose();
                    _textures[i].Dispose();
                }

                if (_textures2 is { })
                {
                    for (int i = 0; i < _textures.Length; i++)
                    {
                        _textureViews2![i].Dispose();
                        _textures2[i].Dispose();
                    }
                }

                _device.Dispose();
                _disposedValue = true;
            }
        }

        ~AetherTexImage()
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
