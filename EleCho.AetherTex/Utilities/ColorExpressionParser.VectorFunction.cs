namespace EleCho.AetherTex.Utilities
{
    internal static partial class ColorExpressionParser
    {
        public record VectorFunction(
            string Name, 
            string NameInShader, 
            IReadOnlyList<VectorFunctionOverride> Overrides, 
            ColorSpace? ReturningColorSpace = null);

        public record VectorFunctionOverride(
            int ReturnComponents, IReadOnlyList<int> ArgumentComponents);
    }
}
