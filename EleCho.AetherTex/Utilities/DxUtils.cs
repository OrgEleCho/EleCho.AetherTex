using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EleCho.AetherTex.Helpers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace EleCho.AetherTex.Utilities
{
    internal static class DxUtils
    {
        private static unsafe void CopyReadableTexture(
            ComPtr<ID3D11Device> device,
            ComPtr<ID3D11DeviceContext> deviceContext,
            ComPtr<ID3D11Texture2D> texture,
            uint subResource,
            TextureData buffer)
        {
            Texture2DDesc desc = default;
            texture.GetDesc(ref desc);

            MappedSubresource mappedSubResource = default;
            var hr = deviceContext.Map(texture, subResource, Map.Read, 0, ref mappedSubResource);
            DxUtils.ThrowHResult(hr);

            var rowsToCopy = Math.Min(desc.Height, buffer.Height);
            var rowBytesToCopy = Math.Min(mappedSubResource.RowPitch, buffer.RowBytes);

            for (int y = 0; y < rowsToCopy; y++)
            {
                NativeMemory.Copy(
                    (void*)((nint)mappedSubResource.PData + mappedSubResource.RowPitch * y),
                    (void*)(buffer.BaseAddress + buffer.RowBytes * y),
                    (nuint)rowBytesToCopy);
            }

            deviceContext.Unmap(texture, subResource);
        }

        public static ComPtr<ID3D11Texture2D> CreateTexture2D(ComPtr<ID3D11Device> device, in SubresourceData initialData, in Texture2DDesc desc)
        {
            ComPtr<ID3D11Texture2D> texture = default;
            var hr = device.CreateTexture2D(in desc, in initialData, ref texture);
            DxUtils.ThrowHResult(hr);

            return texture;
        }

        public static ComPtr<ID3D11Texture2D> CreateTexture2D(ComPtr<ID3D11Device> device, in Texture2DDesc desc)
            => CreateTexture2D(device, in Unsafe.NullRef<SubresourceData>(), in desc);

        public static ComPtr<ID3D11Buffer> CreateBuffer(ComPtr<ID3D11Device> device, in BufferDesc desc, in SubresourceData initialData)
        {
            ComPtr<ID3D11Buffer> buffer = default;
            var hr = device.CreateBuffer(in desc, in initialData, ref buffer);
            DxUtils.ThrowHResult(hr);

            return buffer;
        }

        public static ComPtr<ID3D11Buffer> CreateBuffer(ComPtr<ID3D11Device> device, in BufferDesc desc)
            => CreateBuffer(device, in desc, in Unsafe.NullRef<SubresourceData>());

        public static ComPtr<ID3D11RenderTargetView> CreateRenderTargetView(ComPtr<ID3D11Device> device, ComPtr<ID3D11Texture2D> texture, in RenderTargetViewDesc desc)
        {
            ComPtr<ID3D11RenderTargetView> renderTarget = default;
            var hr = device.CreateRenderTargetView(texture, in desc, ref renderTarget);
            DxUtils.ThrowHResult(hr);

            return renderTarget;
        }

        public static void CopyTexture(
            ComPtr<ID3D11Device> device,
            ComPtr<ID3D11DeviceContext> deviceContext,
            ComPtr<ID3D11Texture2D> texture,
            uint subResource,
            TextureData buffer)
        {
            Texture2DDesc desc = default;
            texture.GetDesc(ref desc);

            if ((desc.CPUAccessFlags & (uint)CpuAccessFlag.Read) != 0)
            {
                CopyReadableTexture(device, deviceContext, texture, subResource, buffer);
                return;
            }

            using var outputBuffer = CreateTexture2D(device, new Texture2DDesc()
            {
                Width = desc.Width,
                Height = desc.Height,
                ArraySize = 1,
                BindFlags = 0,
                CPUAccessFlags = (uint)CpuAccessFlag.Read,
                Format = desc.Format,
                MipLevels = 1,
                MiscFlags = 0,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Staging,
            });

            if (subResource != 0 || desc.ArraySize != 1)
            {
                var srcBox = new Box(0, 0, 0, desc.Width, desc.Height, 1);
                deviceContext.CopySubresourceRegion(outputBuffer, 0, 0, 0, 0, texture, subResource, in srcBox);
            }
            else
            {
                deviceContext.CopyResource(outputBuffer, texture);
            }

            CopyReadableTexture(device, deviceContext, outputBuffer, 0, buffer);
        }

        public static unsafe ComPtr<ID3D10Blob> Compile(D3DCompiler compiler, string sourceName, string entryPoint, string target, ReadOnlySpan<byte> shaderSource, ReadOnlySpan<D3DShaderMacro> macros, ComPtr<ID3DInclude> include)
        {
            D3DShaderMacro* pMacros = null;
            ID3DInclude* pIncludes = null;

            if (macros.Length > 0)
            {
                pMacros = (D3DShaderMacro*)NativeMemory.AllocZeroed((nuint)(sizeof(D3DShaderMacro) * (macros.Length + 1)));

                fixed (D3DShaderMacro* p = macros)
                {
                    NativeMemory.Copy(p, pMacros, (nuint)(sizeof(D3DShaderMacro) * macros.Length));
                }
            }

            fixed (byte* pShaderSource = shaderSource)
            {
                ComPtr<ID3D10Blob> shader = default;
                ComPtr<ID3D10Blob> errorMsgs = null;
                var ok = compiler.Compile(pShaderSource, (nuint)(shaderSource.Length), sourceName, pMacros, include, entryPoint, target, 0, 0, ref shader, ref errorMsgs);
                if (ok != 0 &&
                    errorMsgs.Handle != null)
                {
                    string error = Encoding.ASCII.GetString((byte*)errorMsgs.GetBufferPointer(), (int)errorMsgs.GetBufferSize());
                    throw new InvalidOperationException(error);
                }

                if (pMacros is not null)
                {
                    NativeMemory.Free(pMacros);
                }

                return shader;
            }
        }

        public static unsafe ComPtr<ID3D10Blob> Compile(D3DCompiler compiler, string sourceName, string entryPoint, string target, ReadOnlySpan<byte> shaderSource, ReadOnlySpan<D3DShaderMacro> macros)
            => Compile(compiler, sourceName, entryPoint, target, shaderSource, macros, default);

        public static unsafe ComPtr<ID3D10Blob> Compile(D3DCompiler compiler, string sourceName, string entryPoint, string target, ReadOnlySpan<byte> shaderSource, IEnumerable<KeyValuePair<string, string>> macros)
        {
            KeyValuePair<NativeString, NativeString>[] nativeMacros = macros
                .Select(kv => new KeyValuePair<NativeString, NativeString>(new NativeString(kv.Key), new NativeString(kv.Value)))
                .ToArray();

            D3DShaderMacro[] d3dMacros = nativeMacros
                .Select(kv => new D3DShaderMacro((byte*)kv.Key.ASCII, (byte*)kv.Value.ASCII))
                .ToArray();

            var shader = Compile(compiler, sourceName, entryPoint, target, shaderSource, d3dMacros);

            foreach (var kv in nativeMacros)
            {
                kv.Key.Dispose();
                kv.Value.Dispose();
            }

            return shader;
        }

        public static unsafe ComPtr<ID3D10Blob> Compile(D3DCompiler compiler, string sourceName, string entryPoint, string target, ReadOnlySpan<byte> shaderSource)
            => Compile(compiler, sourceName, entryPoint, target, shaderSource, Array.Empty<D3DShaderMacro>());

        public static unsafe ComPtr<ID3D11VertexShader> CreateVertexShader(ComPtr<ID3D11Device> device, ComPtr<ID3D10Blob> shaderBlob, ComPtr<ID3D11ClassLinkage> classLinkage)
        {
            ComPtr<ID3D11VertexShader> shader = default;
            var hr = device.CreateVertexShader(shaderBlob.GetBufferPointer(), shaderBlob.GetBufferSize(), classLinkage, ref shader);
            DxUtils.ThrowHResult(hr);

            return shader;
        }

        public static unsafe ComPtr<ID3D11PixelShader> CreatePixelShader(ComPtr<ID3D11Device> device, ComPtr<ID3D10Blob> shaderBlob, ComPtr<ID3D11ClassLinkage> classLinkage)
        {
            ComPtr<ID3D11PixelShader> shader = default;
            var hr = device.CreatePixelShader(shaderBlob.GetBufferPointer(), shaderBlob.GetBufferSize(), classLinkage, ref shader);
            DxUtils.ThrowHResult(hr);

            return shader;
        }

        public static unsafe ComPtr<ID3D11VertexShader> CreateVertexShader(ComPtr<ID3D11Device> device, ComPtr<ID3D10Blob> shaderBlob)
            => CreateVertexShader(device, shaderBlob, default);

        public static unsafe ComPtr<ID3D11PixelShader> CreatePixelShader(ComPtr<ID3D11Device> device, ComPtr<ID3D10Blob> shaderBlob)
            => CreatePixelShader(device, shaderBlob, default);

        public static unsafe ComPtr<ID3D11InputLayout> CreateInputLayout(ComPtr<ID3D11Device> device, ComPtr<ID3D10Blob> shaderBlob, ReadOnlySpan<InputElementDesc> inputElements)
        {
            ComPtr<ID3D11InputLayout> inputLayout = default;
            var hr = device.CreateInputLayout(in inputElements[0], (uint)inputElements.Length, shaderBlob.GetBufferPointer(), shaderBlob.GetBufferSize(), ref inputLayout);
            DxUtils.ThrowHResult(hr);

            return inputLayout;
        }

        public static unsafe ComPtr<ID3D11SamplerState> CreateSamplerState(ComPtr<ID3D11Device> device, in SamplerDesc desc)
        {
            ComPtr<ID3D11SamplerState> samplerState = default;
            var hr = device.CreateSamplerState(in desc, ref samplerState);
            DxUtils.ThrowHResult(hr);

            return samplerState;
        }

        public static void ThrowHResult(int hr)
        {
            // TODO: Some check

            SilkMarshal.ThrowHResult(hr);
        }
    }
}
