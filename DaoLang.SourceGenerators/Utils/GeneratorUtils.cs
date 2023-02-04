using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Globalization;

namespace DaoLang.SourceGenerators.Utils
{
    internal static class GeneratorUtils
    {
        /// <summary>
        /// Class类型且带有特性
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal static bool IsClassHasAttribute(SyntaxNode node)
        {
            return node is TypeDeclarationSyntax { AttributeLists.Count: > 0 } and ClassDeclarationSyntax;
        }

        /// <summary>
        /// 获取某type的namespace并加入namespaces集合
        /// </summary>
        /// <param name="namespaces">namespaces集合</param>
        /// <param name="usedTypes">已判断过的types</param>
        /// <param name="baseClass">type的基类</param>
        /// <param name="symbol">type的symbol</param>
        internal static void UseNamespace(this HashSet<string> namespaces, HashSet<ITypeSymbol> usedTypes, INamedTypeSymbol baseClass, ITypeSymbol symbol)
        {
            if (usedTypes.Contains(symbol))
            {
                return;
            }

            usedTypes.Add(symbol);

            var ns = symbol.ContainingNamespace;
            if (!SymbolEqualityComparer.Default.Equals(ns, baseClass.ContainingNamespace))
            {
                namespaces.Add(ns.ToDisplayString());
            }

            if (symbol is INamedTypeSymbol { IsGenericType: true } genericSymbol)
            {
                foreach (var a in genericSymbol.TypeArguments)
                {
                    namespaces.UseNamespace(usedTypes, baseClass, a);
                }
            }
        }

        /// <summary>
        /// 根据字段名称生成对应的属性名
        /// </summary>
        /// <param name="fieldSymbol"></param>
        /// <returns></returns>
        public static string GetGeneratedPropertyName(IFieldSymbol fieldSymbol)
        {
            string propertyName = fieldSymbol.Name;

            if (propertyName.StartsWith("_"))
            {
                propertyName = propertyName.TrimStart('_');
            }

            return $"{char.ToUpper(propertyName[0], CultureInfo.InvariantCulture)}{propertyName.Substring(1)}";
        }
    }
}
