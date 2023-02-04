using DaoLang.Shared.Enums;
using DaoLang.SourceGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DaoLang.SourceGenerators
{
    /// <summary>
    /// 语言资源构造函数生成器
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public class LanguageConstructorSourceGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// 主语言标记特性
        /// </summary>
        private const string MainLanguageAttributeName = "DaoLang.Attributes.MainLanguageAttribute";
        /// <summary>
        /// 副语言标记特性
        /// </summary>
        private const string SecondaryLanguageAttributeName = "DaoLang.Attributes.SecondaryLanguageAttribute";

        private static readonly Dictionary<string, string> SourceCache = new();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (s, _) => GeneratorUtils.IsClassHasAttribute(s),
                    static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)!;

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
                    if (MainLanguageAttributeName.Equals(attributeSymbol.ContainingType.ToDisplayString()))
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
                     where MainLanguageAttributeName.Equals(attributeName) || SecondaryLanguageAttributeName.Equals(attributeName)
                     select attribute)
                    .ToList();

                if (GenerateLanguageClass(typeSymbol, usedAttributes) is { } source)
                {
                    var name = typeSymbol.ToDisplayString();
                    if (SourceCache.ContainsKey(name))
                    {
                        if (SourceCache[name].Equals(source))
                        {
                            return;
                        }
                        SourceCache[name] = source;
                    }
                    else
                    {
                        SourceCache.Add(name, source);
                    }

                    context.AddSource($"{name}.g.cs", source);
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
            var main = attributeList.FirstOrDefault(t => t.AttributeClass!.ToDisplayString().Equals(MainLanguageAttributeName));
            if (main is null)
            {
                return string.Empty;
            }

            // 获取副语言并去重
            attributeList.Remove(main);
            attributeList.RemoveAll(t => t.ConstructorArguments[0].Equals(main.ConstructorArguments[2]));
            var secondaries = attributeList.Distinct().ToList();

            var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var namespaces = new HashSet<string> { "DaoLang" };

            // 副语言数组
            var secondaryStr = string.Empty;
            if (secondaries?.Any() == true)
            {
                secondaryStr = @"_secondaryLanguages = new LanguageType[]
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
            _sourceType = typeof({name});
            _folder = @""{main.ConstructorArguments[0].Value}"";
            _fileFlag = ""{main.ConstructorArguments[1].Value}"";
            _mainLanguage = LanguageType.{(LanguageType)(main.ConstructorArguments[2].Value ?? 0)};
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
