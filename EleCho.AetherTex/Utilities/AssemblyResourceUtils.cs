using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EleCho.AetherTex.Utilities
{
    internal static class AssemblyResourceUtils
    {
        private static readonly Assembly s_currentAssembly = Assembly.GetExecutingAssembly();
        private static readonly string s_currentAssemblyName = s_currentAssembly.GetName().Name!;

        public static byte[]? GetShaderBytes(string fileName)
        {
            var stream = s_currentAssembly.GetManifestResourceStream($"{s_currentAssemblyName}.Shaders.{fileName}");
            if (stream is null)
            {
                return null;
            }

            using (stream)
            {
                MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
