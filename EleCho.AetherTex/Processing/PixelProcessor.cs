using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EleCho.AetherTex.Helpers;
using EleCho.AetherTex.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace EleCho.AetherTex.Converters
{
    internal class PixelProcessor : IDisposable
    {
        private bool _disposedValue;
        private readonly ComPtr<ID3D11DeviceContext> _deviceContext;
        private readonly ComPtr<ID3D10Blob> _vertexShaderBlob;
        private readonly ComPtr<ID3D10Blob> _pixelShaderBlob;
        private readonly ComPtr<ID3D11VertexShader> _vertexShader;
        private readonly ComPtr<ID3D11PixelShader> _pixelShader;
        private readonly ComPtr<ID3D11InputLayout> _inputLayout;
        private ComPtr<ID3D11Buffer> _vertexBuffer;

        public unsafe PixelProcessor(
            ComPtr<ID3D11Device> device,
            ComPtr<ID3D11DeviceContext> deviceContext,
            string sourceName,
            string expression)
        {
            var compiler = D3DCompiler.GetApi();
            var shaderExpression = ColorExpressionParser.GetShaderExpressionForSourceExpr(expression, new[] { sourceName });

            _deviceContext = deviceContext;
            _vertexShaderBlob = DxUtils.Compile(compiler, "shader", "vs_main", "vs_5_0", AssemblyResourceUtils.GetShaderBytes("PixelProcessor.hlsl"), DxInclude.Instance.Include);
            _pixelShaderBlob = DxUtils.Compile(compiler, "shader", "ps_main", "ps_5_0", AssemblyResourceUtils.GetShaderBytes("PixelProcessor.hlsl"), new Dictionary<string, string>()
            {
                ["SourceExpr"] = shaderExpression
            }, DxInclude.Instance.Include);

            _vertexShader = DxUtils.CreateVertexShader(device, _vertexShaderBlob);
            _pixelShader = DxUtils.CreatePixelShader(device, _pixelShaderBlob);

            using var sematicTexcoord0 = new NativeString("MT_POSITION");

            _inputLayout = DxUtils.CreateInputLayout(device, _vertexShaderBlob, new[]
            {
                new InputElementDesc()
                {
                    SemanticName = (byte*)sematicTexcoord0.ASCII,
                    SemanticIndex = 0,
                    Format = Silk.NET.DXGI.Format.FormatR32G32Float,
                    InputSlot = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                }
            });

            float* vertexBufferData = stackalloc float[8]
            {
                -1, 1,
                1, 1,
                -1, -1,
                1, -1
            };

            _vertexBuffer = DxUtils.CreateBuffer(device, new BufferDesc()
            {
                BindFlags = (uint)BindFlag.VertexBuffer,
                ByteWidth = (uint)(sizeof(float) * 8),
                CPUAccessFlags = (uint)CpuAccessFlag.None,
                MiscFlags = 0,
                Usage = Usage.Default
            }, new SubresourceData()
            {
                PSysMem = vertexBufferData,
                SysMemPitch = sizeof(float) * 8
            });
        }

        public unsafe void Process(
            ComPtr<ID3D11ShaderResourceView> input,
            ComPtr<ID3D11RenderTargetView> renderTargetView,
            int width,
            int height)
        {
            var viewport = new Viewport(0, 0, width, height, 0, 1);

            float* background = stackalloc float[4];

            // clear as transparent
            _deviceContext.ClearRenderTargetView(renderTargetView, ref background[0]);

            _deviceContext.RSSetViewports(1, in viewport);
            _deviceContext.OMSetRenderTargets(1, ref renderTargetView, ref Unsafe.NullRef<ID3D11DepthStencilView>());

            _deviceContext.VSSetShader(_vertexShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
            _deviceContext.PSSetShader(_pixelShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);

            uint vertexStride = sizeof(float) * 2;
            uint vertexOffset = 0;
            _deviceContext.IASetVertexBuffers(0, 1, ref _vertexBuffer, in vertexStride, in vertexOffset);
            _deviceContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3D10PrimitiveTopologyTrianglestrip);
            _deviceContext.IASetInputLayout(_inputLayout);

            _deviceContext.PSSetShaderResources(0, 1, ref input);

            _deviceContext.Draw(4, 0);

            _deviceContext.Flush();
            _deviceContext.ClearState();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                _vertexShaderBlob.Dispose();
                _pixelShaderBlob.Dispose();
                _vertexShader.Dispose();
                _pixelShader.Dispose();
                _inputLayout.Dispose();
                _vertexBuffer.Dispose();
                _disposedValue = true;
            }
        }

        ~PixelProcessor()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
