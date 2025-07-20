using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Irony.Parsing;
using Silk.NET.Core.Native;
using static EleCho.AetherTex.Utilities.ColorExpressionParser;

namespace EleCho.AetherTex.Utilities
{
    internal static partial class ColorExpressionParser
    {
        private static readonly ColorFilterExpressionGrammar s_grammar;
        private static readonly Parser s_parser;

        static ColorExpressionParser()
        {
            s_grammar = new();
            s_parser = new Parser(s_grammar);
        }

        public record struct ValueNodeInfo(string Text, int Components, ColorSpace ColorSpace, Variable[] Members);

        private static IEnumerable<ValueNodeInfo> ExpressionListNodeInfo(ParseTreeNode node, VectorVariable[] availableVariables, VectorFunction[] availableFunctions)
        {
            ParseTreeNode? nextNode = node;

            while (nextNode is not null)
            {
                switch (nextNode.ChildNodes)
                {
                    case [var expression]:
                        yield return ExpressionNodeInfo(expression, availableVariables, availableFunctions);
                        yield break;
                    case [var expressionList, _, var expression]:
                        foreach (var before in ExpressionListNodeInfo(expressionList, availableVariables, availableFunctions))
                        {
                            yield return before;
                        }

                        yield return ExpressionNodeInfo(expression, availableVariables, availableFunctions);
                        yield break;
                }
            }
        }
        private static IEnumerable<ValueNodeInfo> ArgumentListNodeInfo(ParseTreeNode node, Variable[] availableVariables, VectorFunction[] availableFunctions)
        {
            ParseTreeNode? nextNode = node;

            while (nextNode is not null)
            {
                switch (nextNode.ChildNodes)
                {
                    case [var expression]:
                        yield return ExpressionNodeInfo(expression, availableVariables, availableFunctions);
                        yield break;
                    case [var expressionList, _, var expression]:
                        foreach (var before in ArgumentListNodeInfo(expressionList, availableVariables, availableFunctions))
                        {
                            yield return before;
                        }

                        yield return ExpressionNodeInfo(expression, availableVariables, availableFunctions);
                        yield break;
                }
            }
        }

        private static ValueNodeInfo ExpressionNodeInfo(ParseTreeNode node, Variable[] availableVariables, VectorFunction[] availableFunctions)
        {
            switch (node.Term.Name)
            {
                case "number":
                    return new ValueNodeInfo(node.Token.Text, 1, ColorSpace.RGB, VectorVariable.EnumerateMembersOfVector(ColorSpace.RGB, 1).ToArray());

                case "memberAccess":
                    return MemberAccessNodeInfo(node, availableVariables, availableFunctions);

                case "functionCall":
                    return FunctionCallNodeInfo(node, availableVariables, availableFunctions);

                case "factor":
                    switch (node.ChildNodes.Count)
                    {
                        case 1:
                            return ExpressionNodeInfo(node.ChildNodes[0], availableVariables, availableFunctions);
                        case 3:
                            var childInfo = ExpressionNodeInfo(node.ChildNodes[1], availableVariables, availableFunctions);
                            return new ValueNodeInfo($"({childInfo.Text})", childInfo.Components, ColorSpace.RGB, childInfo.Members);
                        default:
                            throw new InvalidOperationException();
                    }

                case "term":
                    switch (node.ChildNodes.Count)
                    {
                        case 1:
                            return ExpressionNodeInfo(node.ChildNodes[0], availableVariables, availableFunctions);
                        case 3:
                            var childInfo1 = ExpressionNodeInfo(node.ChildNodes[0], availableVariables, availableFunctions);
                            var childInfo2 = ExpressionNodeInfo(node.ChildNodes[2], availableVariables, availableFunctions);
                            if (childInfo1.Components != 1 &&
                                childInfo2.Components != 1)
                            {
                                throw new ArgumentException("Invalid term");
                            }

                            var op = node.ChildNodes[1].Token.Text;
                            return new ValueNodeInfo($"{childInfo1.Text} {op} {childInfo2.Text}", childInfo1.Components * childInfo2.Components, ColorSpace.RGB, childInfo2.Members);
                        default:
                            throw new InvalidOperationException();
                    }

                case "expression":
                    switch (node.ChildNodes.Count)
                    {
                        case 1:
                            return ExpressionNodeInfo(node.ChildNodes[0], availableVariables, availableFunctions);
                        case 3:
                            var childInfo1 = ExpressionNodeInfo(node.ChildNodes[0], availableVariables, availableFunctions);
                            var childInfo2 = ExpressionNodeInfo(node.ChildNodes[2], availableVariables, availableFunctions);
                            if (childInfo1.Components != childInfo2.Components)
                            {
                                throw new ArgumentException("Invalid term");
                            }

                            var op = node.ChildNodes[1].Token.Text;
                            return new ValueNodeInfo($"{childInfo1.Text} {op} {childInfo2.Text}", childInfo1.Components, ColorSpace.RGB, childInfo1.Members);
                        default:
                            throw new InvalidOperationException();
                    }

                default:
                    throw new InvalidOperationException();
            }
        }

        private static ValueNodeInfo MemberAccessNodeInfo(ParseTreeNode node, Variable[] availableVariables, VectorFunction[] availableFunctions)
        {
            if (node.ChildNodes is [var identifier])
            {
                var identifierVariable = availableVariables.FirstOrDefault(v => v.Name == identifier.Token.Text)
                    ?? throw new ArgumentException($"No variable like '{identifier.Token.Text}'");

                availableVariables = identifierVariable.Members.ToArray();
                return identifierVariable.Resolve(null);
            }
            else if (node.ChildNodes is [var memberAccess, _, var identifierToAccess])
            {
                var parent = MemberAccessNodeInfo(memberAccess, availableVariables, availableFunctions);

                if (parent.Members.FirstOrDefault(v => v.Name == identifierToAccess.Token.Text) is { } matchedVariable)
                {
                    availableVariables = matchedVariable.Members.ToArray();
                    return matchedVariable.Resolve(parent);
                }
                else if (availableFunctions.FirstOrDefault(f => f.Name == identifierToAccess.Token.Text) is { } matchedFunction)
                {
                    if (matchedFunction.Overrides
                        .FirstOrDefault(ovrd => ovrd.ArgumentComponents.Count == 1 && ovrd.ArgumentComponents[0] == parent.Components)
                        is { } matchedOverride)
                    {
                        var colorSpace = matchedFunction.ReturningColorSpace ?? parent.ColorSpace;
                        var members = VectorVariable.EnumerateMembersOfVector(colorSpace, 1).ToArray();
                        return new ValueNodeInfo($"{matchedFunction.Name}({parent.Text})", matchedOverride.ReturnComponents, colorSpace, members);
                    }
                    else
                    {
                        throw new ArgumentException($"Function '{identifierToAccess.Token.Text}' no override accepts {parent.Components} components");
                    }
                }
                else
                {
                    throw new ArgumentException($"No variable or function like '{identifierToAccess.Token.Text}'");
                }
            }

            throw new InvalidOperationException("Invalid node");
        }

        private static ValueNodeInfo FunctionCallNodeInfo(ParseTreeNode node, Variable[] availableVariables, VectorFunction[] availableFunctions)
        {
            switch (node.ChildNodes)
            {
                case [var identifier, _, var argumentList, _]:
                    var identifierText = identifier.Token.Text;
                    var argumentListNodeInfos = ArgumentListNodeInfo(argumentList, availableVariables, availableFunctions).ToArray();
                    if (availableFunctions.FirstOrDefault(f => f.Name == identifierText) is not { } matchedFunc)
                    {
                        throw new ArgumentException($"No function like '{identifierText}'");
                    }

                    foreach (var funcOverride in matchedFunc.Overrides)
                    {
                        if (funcOverride.ArgumentComponents.Count != argumentListNodeInfos.Length)
                        {
                            continue;
                        }

                        for (int i = 0; i < funcOverride.ArgumentComponents.Count; i++)
                        {
                            var currentArgumentComponent = funcOverride.ArgumentComponents[i];
                            if (currentArgumentComponent != argumentListNodeInfos[i].Components)
                            {
                                throw new ArgumentException($"Function '{identifierText}' with {argumentListNodeInfos.Length} arguments override, argument index {i}, component count not match, required: {currentArgumentComponent}, actual: {argumentListNodeInfos[i].Components}");
                            }
                        }

                        var returnColorSpace = matchedFunc.ReturningColorSpace ?? argumentListNodeInfos.First().ColorSpace;
                        var members = VectorVariable.EnumerateMembersOfVector(returnColorSpace, funcOverride.ReturnComponents).ToArray();
                        return new ValueNodeInfo($"{identifierText}({string.Join(", ", argumentListNodeInfos.Select(nodeInfo => nodeInfo.Text))})", funcOverride.ReturnComponents, returnColorSpace, members);
                    }

                    throw new ArgumentException($"No matched override of function '{identifierText}'.");

                default:
                    throw new InvalidOperationException();
            }
        }


        public delegate string ShaderExpressionDefaultComponentResolver(ValueNodeInfo[] Values, int RequiredComponentIndex);

        public static string GetShaderExpression(string expression, VectorVariable[] availableVariables, VectorFunction[] availableFunctions, ShaderExpressionDefaultComponentResolver defaultComponentResolver)
        {
            var parseTree = s_parser.Parse(expression);
            if (parseTree.HasErrors())
            {
                throw new ArgumentException("Invalid expression");
            }

            var nodeInfos = ExpressionListNodeInfo(parseTree.Root, availableVariables, availableFunctions).ToArray();

            if (nodeInfos.Length == 0)
            {
                throw new ArgumentException("Invalid expression");
            }
            else if (nodeInfos.Length == 1)
            {
                var onePart = nodeInfos[0];
                return onePart.Components switch
                {
                    1 => $"float4({onePart.Text}, {defaultComponentResolver.Invoke(nodeInfos, 1)}, {defaultComponentResolver.Invoke(nodeInfos, 2)}, {defaultComponentResolver.Invoke(nodeInfos, 3)})",
                    2 => $"float4({onePart.Text}, {defaultComponentResolver.Invoke(nodeInfos, 2)}, {defaultComponentResolver.Invoke(nodeInfos, 3)})",
                    3 => $"float4({onePart.Text}, {defaultComponentResolver.Invoke(nodeInfos, 3)})",
                    4 => $"{onePart.Text}",
                    _ => throw new ArgumentException("Invalid expression")
                };
            }
            else
            {
                var totalComponents = 0;
                for (int i = 0; i < nodeInfos.Length; i++)
                {
                    var part = nodeInfos[i];
                    if (part.Components + totalComponents > 4)
                    {
                        throw new ArgumentException("Invalid expression");
                    }

                    totalComponents += part.Components;
                }

                if (totalComponents < 4)
                {
                    var componentsToFill = Enumerable.Range(totalComponents, 4 - totalComponents)
                        .Select(requiredComponentIndex => defaultComponentResolver.Invoke(nodeInfos, requiredComponentIndex));

                    return $"float4({string.Join(", ", nodeInfos.Select(part => part.Text))}, {string.Join(", ", componentsToFill)})";
                }
                else
                {
                    return $"float4({string.Join(", ", nodeInfos.Select(part => part.Text))})";
                }
            }
        }

        public static string GetShaderExpressionForSourceExpr(string expression, string[] sources, ShaderExpressionDefaultComponentResolver defaultComponentResolver)
        {
            VectorVariable[] vectorVariables = sources
                .Select((source, i) => new VectorVariable(source, $"sources[{i}]", ColorSpace.RGB))
                .ToArray();

            VectorFunction[] vectorFunctions =
            [
                new VectorFunction("abs", "abs", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("sin", "sin", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("cos", "cos", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("tan", "tan", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("asin", "asin", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("acos", "acos", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("atan", "atan", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("log", "log", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("log2", "log2", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("log10", "log10", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("sqrt", "sqrt", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(2, [2]),
                    new VectorFunctionOverride(3, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("pow", "pow", [
                    new VectorFunctionOverride(1, [1, 1]),
                    new VectorFunctionOverride(2, [2, 2]),
                    new VectorFunctionOverride(3, [3, 3]),
                    new VectorFunctionOverride(4, [4, 4]),
                ]),

                new VectorFunction("min", "min", [
                    new VectorFunctionOverride(1, [1, 1]),
                    new VectorFunctionOverride(2, [2, 2]),
                    new VectorFunctionOverride(3, [3, 3]),
                    new VectorFunctionOverride(4, [4, 4]),
                ]),

                new VectorFunction("max", "max", [
                    new VectorFunctionOverride(1, [1, 1]),
                    new VectorFunctionOverride(2, [2, 2]),
                    new VectorFunctionOverride(3, [3, 3]),
                    new VectorFunctionOverride(4, [4, 4]),
                ]),

                new VectorFunction("lerp", "lerp", [
                    new VectorFunctionOverride(1, [1, 1, 1]),
                    new VectorFunctionOverride(2, [2, 2, 2]),
                    new VectorFunctionOverride(3, [3, 3, 3]),
                    new VectorFunctionOverride(4, [4, 4, 4]),
                ]),

                new VectorFunction("clamp", "clamp", [
                    new VectorFunctionOverride(1, [1, 1, 1]),
                    new VectorFunctionOverride(2, [2, 2, 2]),
                    new VectorFunctionOverride(3, [3, 3, 3]),
                    new VectorFunctionOverride(4, [4, 4, 4]),
                ]),

                new VectorFunction("color", "color", [
                    new VectorFunctionOverride(4, [1]),
                    new VectorFunctionOverride(4, [2]),
                    new VectorFunctionOverride(4, [3]),
                    new VectorFunctionOverride(4, [4]),
                ]),

                new VectorFunction("lum", "lum", [
                    new VectorFunctionOverride(1, [1]),
                    new VectorFunctionOverride(1, [2]),
                    new VectorFunctionOverride(1, [3]),
                    new VectorFunctionOverride(1, [4]),
                ]),
            ];

            return GetShaderExpression(expression, vectorVariables, vectorFunctions, defaultComponentResolver);
        }

        public static string GetShaderExpressionForSourceExpr(string expression, string[] sources)
        {
            ShaderExpressionDefaultComponentResolver resolver = (componentsExists, requiredComponentIndex) =>
            {
                if (componentsExists.Length == 0)
                {
                    if (requiredComponentIndex == 3)
                    {
                        return "1";
                    }

                    return "0";
                }

                var colorSpace = componentsExists[0].ColorSpace;

                if (colorSpace == ColorSpace.RGB)
                {
                    if (componentsExists.Length == 1 &&
                        componentsExists[0].Components == 1)
                    {
                        return componentsExists[0].Text;
                    }

                    return "1";
                }
                else if (colorSpace == ColorSpace.HSV)
                {
                    return requiredComponentIndex switch
                    {
                        1 => "1",
                        2 => "1",
                        3 => "1",
                        _ => "0"
                    };
                }
                else if (colorSpace == ColorSpace.HSL)
                {
                    return requiredComponentIndex switch
                    {
                        1 => "1",
                        2 => "0.5",
                        3 => "1",
                        _ => "0"
                    };
                }
                else
                {
                    return "0.5";
                }
            };

            return GetShaderExpressionForSourceExpr(expression, sources, resolver);
        }
    }
}
