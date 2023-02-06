using DaoLang.SourceGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace DaoLang.SourceGenerators
{
    /// <summary>
    /// 词条代码生成器
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public class EntrySourceGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// 主语言标记特性
        /// </summary>
        private const string MainLanguageAttributeName = "DaoLang.Attributes.MainLanguageAttribute";
        /// <summary>
        /// 词条标记特性
        /// </summary>
        private const string EntryAttributeName = "DaoLang.Attributes.EntryAttribute";

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

        #region [Private Methods]
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

            return null!;
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

                // 同种attribute只判断一遍
                var usedAttributes = new Dictionary<string, List<AttributeData>>();

                // 遍历class上每个Attribute
                foreach (var attribute in typeSymbol.GetAttributes())
                {
                    var attributeName = attribute.AttributeClass!.ToDisplayString();

                    // 仅处理MajorLanguage标注的类
                    if (!MainLanguageAttributeName.Equals(attributeName))
                    {
                        continue;
                    }
                    if (usedAttributes.ContainsKey(attributeName))
                    {
                        usedAttributes[attributeName].Add(attribute);
                    }
                    else
                    {
                        usedAttributes[attributeName] = new List<AttributeData> { attribute };
                    }
                }

                foreach (var usedAttribute in usedAttributes)
                {
                    if (GenerateLanguageClass(typeDeclarationSyntax, typeSymbol, usedAttribute.Value) is { } source)
                    {
                        context.AddSource(
                            $"{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.g.cs",
                            source);
                    }
                }
            }
        }

        /// <summary>
        /// 生成语言类
        /// </summary>
        /// <param name="typeDeclaration"></param>
        /// <param name="typeSymbol"></param>
        /// <param name="attributeList"></param>
        /// <returns></returns>
        private static string GenerateLanguageClass(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol typeSymbol, List<AttributeData> attributeList)
        {
            var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var namespaces = new HashSet<string> { "DaoLang" };

            var usedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var classBegin = @$"namespace {typeSymbol.ContainingNamespace.ToDisplayString()}
{{

    partial class {name} : LanguageResource
    {{";
            var propertyEntrys = new List<string>();
            const string classEnd = CodePart.Enter + CodePart.Tab + "}" + CodePart.Enter + "}";

            //循环生成词条属性
            foreach (var field in typeSymbol.GetMembers().Where(t => t is { Kind: SymbolKind.Field }))
            {
                var attributes = field.GetAttributes();
                var entry = attributes.FirstOrDefault(t => t.AttributeClass?.ToDisplayString().Equals(EntryAttributeName) == true);
                if (entry != null)
                {
                    var fieldType = ((IFieldSymbol)field).Type;
                    namespaces.UseNamespace(usedTypes, typeSymbol, fieldType);

                    var propertyName = GeneratorUtils.GetGeneratedPropertyName((IFieldSymbol)field);
                    var comment = entry.ConstructorArguments.FirstOrDefault().Value;
                    // 拼装内容
                    propertyEntrys.Add($@"
        /// <summary>
        /// {comment}
        /// </summary>
        public static {fieldType.Name} {propertyName} => {field.Name};

        /// <summary>
        /// {comment} - 用于绑定资源
        /// </summary>        
        public static {fieldType.Name} {propertyName}Key => ""{propertyName}"";");
                }

            }

            var namespaceNames = namespaces.Aggregate("", (current, ns) => current + $"using {ns};{CodePart.Enter}");
            var allPropertyEntrys = propertyEntrys.Aggregate(CodePart.Enter, (current, ps) => current + $"{ps}{CodePart.Enter}");
            allPropertyEntrys = allPropertyEntrys.Substring(0, allPropertyEntrys.Length - 1);
            var compilationUnit = namespaceNames + CodePart.Enter + classBegin + allPropertyEntrys + classEnd;
            return compilationUnit;
        }
        #endregion
    }
}
