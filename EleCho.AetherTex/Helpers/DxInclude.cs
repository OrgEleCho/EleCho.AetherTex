using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EleCho.AetherTex.Utilities;
using Silk.NET.Core.Native;

namespace EleCho.AetherTex.Helpers
{
    internal unsafe class DxInclude : IDisposable
    {
        private bool _disposedValue;
        private readonly ComPtr<ID3DInclude> _include;
        private Delegate _funcIncludeOpen;
        private Delegate _funcIncludeClose;
        private readonly Dictionary<string, (nint DataPointer, int DataSize)> _openedShaderSources;

        public ComPtr<ID3DInclude> Include => _include;

        public static DxInclude Instance { get; } = new DxInclude();

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

        private DxInclude()
        {
            var includeFunctions = (nint*)NativeMemory.Alloc((nuint)(sizeof(nint) * 2));
            _funcIncludeOpen = D3DIncludeOpen;
            _funcIncludeClose = D3DIncludeClose;
            includeFunctions[0] = Marshal.GetFunctionPointerForDelegate(_funcIncludeOpen);
            includeFunctions[1] = Marshal.GetFunctionPointerForDelegate(_funcIncludeClose);

            var include = (ID3DInclude*)NativeMemory.Alloc((nuint)sizeof(ID3DInclude));
            include[0] = new ID3DInclude((void**)includeFunctions);

            _openedShaderSources = new();
            _include = new ComPtr<ID3DInclude>(include);
        }

        protected virtual void Dispose(bool disposing)
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
                _disposedValue = true;
            }
        }

        ~DxInclude()
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
