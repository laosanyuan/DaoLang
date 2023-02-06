using DaoLang.Shared.Enums;
using DaoLang.SourceGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DaoLang.SourceGenerators.Components;

namespace DaoLang.SourceGenerators
{
    /// <summary>
    /// 语言资源构造函数生成器
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public class LanguageConstructorSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (s, _) => GeneratorUtils.IsClassHasAttribute(s),
                    static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation Compilation, ImmutableArray<TypeDeclarationSyntax> TypeDeclarationSyntaxes)> compilationAndTypes =
                context.CompilationProvider.Combine(typeDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndTypes, (spc, source) =>
                Execute(source.Compilation, source.TypeDeclarationSyntaxes, spc));
        }

        /// <summary>
        /// 获取TypeDeclarationSyntax
        /// </summary>
        private static TypeDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;
            // 不用Linq，用foreach保证速度
            foreach (var attributeListSyntax in typeDeclarationSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }
                    if (SupportAttributes.MainLanguageAttributeName.Equals(attributeSymbol.ContainingType.ToDisplayString()))
                    {
                        return typeDeclarationSyntax;
                    }
                }
            }

            return default!;
        }

        /// <summary>
        /// 对获取的每个type和Attribute进行生成
        /// </summary>
        private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> types, SourceProductionContext context)
        {
            if (types.IsDefaultOrEmpty)
            {
                return;
            }

            // 遍历每个class
            foreach (var typeDeclarationSyntax in types)
            {
                var semanticModel = compilation.GetSemanticModel(typeDeclarationSyntax.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol typeSymbol)
                {
                    continue;
                }

                var usedAttributes =
                    (from attribute in typeSymbol.GetAttributes()
                     let attributeName = attribute.AttributeClass!.ToDisplayString()
                     where SupportAttributes.MainLanguageAttributeName.Equals(attributeName) || SupportAttributes.SecondaryLanguageAttributeName.Equals(attributeName)
                     select attribute)
                    .ToList();

                if (GenerateLanguageClass(typeSymbol, usedAttributes) is { } source)
                {
                    context.AddSource($"{typeSymbol.ToDisplayString()}.g.cs", source);
                }
            }
        }

        /// <summary>
        /// 生成语言类
        /// </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="attributeList"></param>
        /// <returns></returns>
        private static string GenerateLanguageClass(ISymbol typeSymbol, List<AttributeData> attributeList)
        {
            // 获取主语言
            var main = attributeList.FirstOrDefault(t => t.AttributeClass!.ToDisplayString().Equals(SupportAttributes.MainLanguageAttributeName));
            if (main is null)
            {
                return string.Empty;
            }

            // 获取副语言并去重
            attributeList.Remove(main);
            attributeList.RemoveAll(t => t.ConstructorArguments[0].Equals(main.ConstructorArguments[2]));
            var secondaries = attributeList.Distinct().ToList();

            var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var namespaces = new HashSet<string> { "DaoLang", "DaoLang.Shared.Enums" };

            // 副语言数组
            var secondaryStr = string.Empty;
            if (secondaries?.Any() == true)
            {
                secondaryStr = @"SecondaryLanguages = new LanguageType[]
            {";
                secondaryStr += CodePart.Enter;
                secondaryStr = secondaries.Aggregate(
                    secondaryStr,
                    (current, minor)
                        => current + $"{CodePart.Tabs(4)}LanguageType.{(LanguageType)(minor.ConstructorArguments[0].Value ?? 0)},{CodePart.Enter}");
                secondaryStr += $"{CodePart.Tabs(3)}}};";
            }

            // 函数主体
            var body = @$"namespace {typeSymbol.ContainingNamespace.ToDisplayString()}
{{

    partial class {name}
    {{
        private {name}()
        {{
        }}

        public static void Init()
        {{
            SourceType = typeof({name});
            Folder = @""{main.ConstructorArguments[0].Value}"";
            FileFlag = ""{main.ConstructorArguments[1].Value}"";
            MainLanguage = LanguageType.{(LanguageType)(main.ConstructorArguments[2].Value ?? 0)};
            {secondaryStr}
        
            SetMainLanguage();
        }}
    }}
}}";

            var namespaceNames = namespaces.Aggregate("", (current, ns) => current + $"using {ns};{CodePart.Enter}");
            return namespaceNames + CodePart.Enter + body;
        }
    }
}
