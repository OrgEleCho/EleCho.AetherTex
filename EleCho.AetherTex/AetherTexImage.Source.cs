using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EleCho.AetherTex.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;

namespace EleCho.AetherTex
{
    public sealed unsafe partial class AetherTexImage
    {
        public sealed class ExprSource : IDisposable
        {
            private ComPtr<ID3D10Blob> _pixelShaderBlob;
            private ComPtr<ID3D11PixelShader> _pixelShader;
            private bool _disposedValue;

            public AetherTexImage Owner { get; }

            public ExprSource(AetherTexImage owner, string expression)
            {
                var shaderExpression = ColorExpressionParser.GetShaderExpressionForSourceExpr(expression, owner._sources);

                _pixelShaderBlob = DxUtils.Compile(owner._compiler, "shader", "ps_main", "ps_5_0",
                    AssemblyResourceUtils.GetShaderBytes("AetherTexImage.hlsl"),
                    new Dictionary<string, string>()
                    {
                        ["SourceCount"] = owner._sources.Length.ToString(),
                        ["TileWidth"] = owner.TileWidth.ToString(),
                        ["TileHeight"] = owner.TileHeight.ToString(),
                        ["TileRows"] = owner.Rows.ToString(),
                        ["TileColumns"] = owner.Columns.ToString(),
                        ["SourceExpr"] = shaderExpression
                    });

                _pixelShader = DxUtils.CreatePixelShader(owner._device, _pixelShaderBlob);
                Owner = owner;
            }

            internal void ApplyPixelShader(ComPtr<ID3D11DeviceContext> deviceContext)
            {
                if (_disposedValue)
                {
                    throw new InvalidOperationException("Cannot use disposed ExprSource.");
                }

                deviceContext.PSSetShader(_pixelShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
            }

            private void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: 释放托管状态(托管对象)
                    }

                    _pixelShader.Dispose();
                    _pixelShaderBlob.Dispose();
                    _disposedValue = true;
                }
            }

            ~ExprSource()
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
}
