#if NET461

using System.Collections.ObjectModel;

namespace System.Runtime.InteropServices
{
    static class NativeMemory
    {
        private static nint? _heapHandle;
        private static nint HeapHandle => _heapHandle ??= GetProcessHeap();

        [DllImport("Kernel32", EntryPoint = "GetProcessHeap")]
        private extern static nint GetProcessHeap();

        [DllImport("Kernel32", EntryPoint = "HeapAlloc")]
        private extern static nint HeapAlloc(nint heap, uint flags, nuint size);

        [DllImport("Kernel32", EntryPoint = "HeapFree")]
        private extern static bool HeapFree(nint heap, uint flags, nint memoryPointer);

        [DllImport("Kernel32", EntryPoint = "CopyMemory")]
        private extern static void CopyMemory(nint destination, nint source, nuint length);

        public static unsafe void* Alloc(nuint size)
        {
            var ptr = HeapAlloc(HeapHandle, 0, size);
            if (ptr == 0)
            {
                throw new OutOfMemoryException();
            }

            return (void*)ptr;
        }

        public static unsafe void* AllocZeroed(nuint size)
        {
            var flags = 0x00000008u;
            var ptr = HeapAlloc(HeapHandle, flags, size);
            if (ptr == 0)
            {
                throw new OutOfMemoryException();
            }

            return (void*)ptr;
        }

        public static unsafe void Free(void* ptr)
        {
            HeapFree(HeapHandle, 0, (nint)ptr);
        }

        public static unsafe void Copy(void* source, void* destination, nuint byteCount)
        {
            CopyMemory((nint)destination, (nint)source, byteCount);
        }
    }
}

namespace System.Collections.Generic
{
    internal static class CollectionExtensions
    {
        public static ReadOnlyCollection<T> AsReadOnly<T>(this IList<T> list)
        {
            return new ReadOnlyCollection<T>(list);
        }
    }
}

#endif
