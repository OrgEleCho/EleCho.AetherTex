using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EleCho.MegaTextures.Helpers
{
    internal class NativeString : IDisposable
    {
        private readonly string _value;

        private nint? _default = null;
        private nint? _ascii = null;
        private nint? _utf8 = null;

        public nint Default => _default ??= AllocateNativeString(Encoding.Default);
        public nint ASCII => _ascii ??= AllocateNativeString(Encoding.ASCII);
        public nint UTF8 => _utf8 ??= AllocateNativeString(Encoding.UTF8);

        private unsafe nint AllocateNativeString(Encoding encoding)
        {
            var bytes = encoding.GetBytes(_value);
            var nativeMem = NativeMemory.AllocZeroed((nuint)bytes.Length + 1);

            fixed (byte* pBytes = bytes)
            {
                NativeMemory.Copy(pBytes, nativeMem, (nuint)bytes.Length);
            }

            return (nint)nativeMem;
        }

        public NativeString(string value)
        {
            _value = value;
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
            }

            if (_default is not null)
            {
                NativeMemory.Free((void*)_default.Value);
                _default = null;
            }

            if (_ascii is not null)
            {
                NativeMemory.Free((void*)_ascii.Value);
                _ascii = null;
            }

            if (_utf8 is not null)
            {
                NativeMemory.Free((void*)_utf8.Value);
                _utf8 = null;
            }
        }

        ~NativeString()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override bool Equals(object? obj)
        {
            return obj is NativeString @string &&
                   _value == @string._value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value);
        }
    }
}
