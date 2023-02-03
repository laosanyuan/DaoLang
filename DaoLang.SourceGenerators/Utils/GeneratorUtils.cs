using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    }
}
