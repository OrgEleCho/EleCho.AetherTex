namespace EleCho.AetherTex.Utilities
{
    internal static partial class ColorExpressionParser
    {
        public class FunctionVariable : Variable
        {
            private readonly VectorFunction _function;

            public FunctionVariable(VectorFunction function)
            {
                ArgumentNullException.ThrowIfNull(function);

                Name = function.Name;
                _function = function;
            }

            public override string Name { get; }

            public override IEnumerable<Variable> Members
            {
                get
                {
                    yield break;
                }
            }

            public override ValueNodeInfo Resolve(ValueNodeInfo? parent)
            {
                throw new ArgumentException($"No arguments specified for function {_function.Name}.");
            }
        }

        public class VectorVariable : Variable
        {
            private readonly char[] _componentNames;

            public VectorVariable(string name, string nameInShader, char[] componentNames, ColorSpace colorSpace)
            {
                ArgumentNullException.ThrowIfNull(name);
                ArgumentNullException.ThrowIfNull(nameInShader);
                ArgumentNullException.ThrowIfNull(componentNames);

                if (componentNames.Length == 0 ||
                    componentNames.Length > 4)
                {
                    throw new ArgumentException(nameof(componentNames));
                }

                Name = name;
                NameInShader = nameInShader;
                Components = componentNames.Length;

                _componentNames = componentNames;
            }

            public VectorVariable(string name, string nameInShader, ColorSpace colorSpace)
                : this(name, nameInShader, GetComponentNames(colorSpace, 4), colorSpace)
            {

            }

            public override string Name { get; }
            public string NameInShader { get; }
            public int Components { get; }

            public override IEnumerable<Variable> Members => EnumerateMembersOfVector(_componentNames);

            private static IEnumerable<VectorVariable> GenerateCombinationsRecursive(char[] chars, string current, string currentInShader, int length)
            {
                if (current.Length == length)
                {
                    // 如果当前长度达到目标长度，将结果存储起来
                    yield return new VectorVariable(current, currentInShader, current.ToCharArray(), ColorSpace.RGB);
                    yield break;
                }

                for (int i = 0; i < chars.Length; i++)
                {
                    char c = chars[i];
                    char cInShader = "xyzw"[i];
                    // 对每一个字符继续递归，生成更多的组合

                    foreach (var item in GenerateCombinationsRecursive(chars, current + c, currentInShader + cInShader, length))
                    {
                        yield return item;
                    }
                }
            }

            public override ValueNodeInfo Resolve(ValueNodeInfo? parent)
            {
                if (parent is null)
                {
                    return new ValueNodeInfo(NameInShader, Components, ColorSpace.RGB, Members.ToArray());
                }

                return new ValueNodeInfo($"{parent.Value.Text}.{NameInShader}", Components, ColorSpace.RGB, Members.ToArray());
            }

            public static char[] GetComponentNames(ColorSpace colorSpace, int components)
            {
                char[] componentNames = new char[components];
                for (int i = 0; i < components; i++)
                {
                    componentNames[i] = colorSpace switch
                    {
                        ColorSpace.RGB => "rgba"[i],
                        ColorSpace.HSV => "hsva"[i],
                        ColorSpace.HSL => "hsla"[i],
                        ColorSpace.LUV => "luva"[i],
                        ColorSpace.XYZ => "xyza"[i],

                        _ => "rgba"[i]
                    };
                }

                return componentNames;
            }

            public static IEnumerable<Variable> EnumerateMembersOfVector(char[] components)
            {
                for (int len = 1; len <= components.Length; len++)
                {
                    foreach (var item in GenerateCombinationsRecursive(components, string.Empty, string.Empty, len))
                    {
                        yield return item;
                    }
                }
            }

            public static IEnumerable<Variable> EnumerateMembersOfVector(ColorSpace colorSpace, int components)
            {
                if (components < 1 || components > 4)
                {
                    throw new ArgumentOutOfRangeException(nameof(components));
                }

                var componentNames = GetComponentNames(colorSpace, components);
                return EnumerateMembersOfVector(componentNames);
            }
        }
    }
}
