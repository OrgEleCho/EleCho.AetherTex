namespace EleCho.MegaTextures.Utilities
{
    internal static partial class ColorExpressionParser
    {
        public abstract class Variable
        {
            public abstract string Name { get; }

            public abstract ValueNodeInfo Resolve(ValueNodeInfo? parent);

            public abstract IEnumerable<Variable> Members { get; }
        }
    }
}
