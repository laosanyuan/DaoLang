using DaoLang.Shared.Enums;
using DaoLang.Shared.Models;
using DaoLang.Shared.Utils;
using DaoLang.SourceGeneration.Utils;
using DaoLang.SourceGenerators.Components;
using DaoLang.SourceGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DaoLang.SourceGeneration
{
    [Generator(LanguageNames.CSharp)]
    public class FileSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (s, _) => GeneratorUtils.IsClassHasAttribute(s),
                    static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)!;

            IncrementalValueProvider<(Compilation Compilation, ImmutableArray<TypeDeclarationSyntax> TypeDeclarationSyntaxes)> compilationAndTypes =
                context.CompilationProvider.Combine(typeDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndTypes, static (spc, source) =>
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
                    // 筛选主语言
                    if (SupportAttributes.MainLanguageAttributeName.Equals(attributeSymbol.ContainingType.ToDisplayString()))
                    {
                        return typeDeclarationSyntax;
                    }
                }
            }

            return null!;
        }

        /// <summary>
        /// 对语言Attribute标注生成字段内容
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="types"></param>
        private static void Execute(
            Compilation compilation,
            ImmutableArray<TypeDeclarationSyntax> types,
            SourceProductionContext context)
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

                var classFileName = typeDeclarationSyntax.SyntaxTree.FilePath;
                var csprojFile = DirectoryUtil.GetProjectFilePath(classFileName);

                var attributes = typeSymbol.GetAttributes();
                var main = attributes.FirstOrDefault(t =>
                    t.AttributeClass!.ToDisplayString().Equals(SupportAttributes.MainLanguageAttributeName));

                // 只有设置了主语言才生成；仅有副语言无效
                if (main == null)
                {
                    continue;
                }

                var directory = main.ConstructorArguments[0].Value as string;
                var fileFlag = main.ConstructorArguments[1].Value as string;
                var tmpType = main.ConstructorArguments[2].Value;
                if (tmpType == null)
                {
                    continue;
                }
                var type = (LanguageType)tmpType;

                // 只有三个参数时，默认嵌入式
                var generationType = FileGenerationType.Embedded;
                if (main.ConstructorArguments.Length > 3)
                {
                    generationType = (FileGenerationType)(main.ConstructorArguments[3].Value ?? FileGenerationType.Embedded);
                }

                var info = new FileGenerationInfo()
                {
                    Directory = directory ?? string.Empty,
                    FileFlag = fileFlag ?? string.Empty,
                    MainLanguageType = type,
                    FileGenerationType = generationType,
                    CsprojFile = csprojFile,
                };

                // 字段内容更新
                UpdateFields(typeDeclarationSyntax, typeSymbol, semanticModel, info, context);
            }
        }

        /// <summary>
        /// 按条件更新字段
        /// </summary>
        /// <param name="typeDeclarationSyntax"></param>
        /// <param name="typeSymbol"></param>
        /// <param name="info"></param>
        private static void UpdateFields(
            TypeDeclarationSyntax typeDeclarationSyntax,
            INamespaceOrTypeSymbol typeSymbol,
            SemanticModel semanticModel,
            FileGenerationInfo info,
            SourceProductionContext context)
        {
            var valueDic = new Dictionary<string, string>();

            foreach (var member in typeDeclarationSyntax.Members)
            {
                if (member is not FieldDeclarationSyntax field)
                {
                    continue;
                }

                if (!TryGetEntryFieldValue(field, semanticModel, context, out var fieldName, out var fieldValue))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(fieldName)
                    && !valueDic.ContainsKey(fieldName)
                    && fieldValue is not null)
                {
                    valueDic.Add(fieldName, fieldValue);
                }
            }

            // 更新全部文件
            UpdateLanguageFiles(typeSymbol, info, valueDic);
        }

        /// <summary>
        /// 字段集合更新,按类别更新资源
        /// </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="info"></param>
        /// <param name="fieldsValueDic"></param>
        /// <param name="onlyUpdateMain"></param>
        /// <returns></returns>
        private static void UpdateLanguageFiles(
            INamespaceOrTypeSymbol typeSymbol,
            FileGenerationInfo info,
            Dictionary<string, string> fieldsValueDic,
            bool onlyUpdateMain = false)
        {
            var sources = new List<string>();
            var sourceName = SourceReader.GetFileName(info.Directory, info.FileFlag, info.MainLanguageType);
            sources.Add(sourceName);
            var attributes = typeSymbol.GetAttributes();
            var projectDirectory = Path.GetDirectoryName(info.CsprojFile);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                return;
            }

            // 生成主语言文件
            GeneratorFile(
                typeSymbol,
                Path.Combine(projectDirectory, sourceName),
                info.MainLanguageType,
                fieldsValueDic,
                true);

            if (onlyUpdateMain)
            {
                return;
            }

            // 生成副语言文件
            foreach (var attribute in attributes.Where(
                         t => t.AttributeClass?.ToDisplayString().Equals(SupportAttributes.SecondaryLanguageAttributeName) == true))
            {
                var type = (LanguageType)attribute.ConstructorArguments[0].Value!;
                sourceName = SourceReader.GetFileName(info.Directory, info.FileFlag, type);
                sources.Add(sourceName);
                GeneratorFile(
                    typeSymbol,
                    Path.Combine(projectDirectory, sourceName),
                    type);
            }

            // 更新.csproj文件
            CsprojUtil.AddFileToOutput(info.CsprojFile, sources, info.FileGenerationType);
            WriteStaleLanguageFilesManifest(projectDirectory, info, sources);
        }

        /// <summary>
        /// 生成语言资源文件
        /// </summary>
        /// <param name="typeSymbol">roslyn类信息</param>
        /// <param name="fileName">资源文件名称</param>
        /// <param name="type">语言类型</param>
        /// <param name="defaultValues">主语言默认字段值</param>
        /// <param name="isMainLanguage">是否为主语言</param>
        private static void GeneratorFile(
            INamespaceOrTypeSymbol typeSymbol,
            string fileName,
            LanguageType type,
            Dictionary<string, string>? defaultValues = null,
            bool isMainLanguage = false)
        {
            var isExistFile = SourceReader.Load(fileName, out Language? existSource);
            var result = new Language { IsMainLanguage = isMainLanguage, LanguageType = type };

            foreach (var field in typeSymbol.GetMembers().Where(t => t is { Kind: SymbolKind.Field }))
            {
                var attributes = field.GetAttributes();
                var entry = attributes.FirstOrDefault(t => t.AttributeClass?.ToDisplayString().Equals(SupportAttributes.EntryAttributeName) == true);
                if (entry == null)
                {
                    continue;
                }

                var item = new LanguageItem
                {
                    Key = GeneratorUtils.GetGeneratedPropertyName((IFieldSymbol)field),
                };

                // 主语言使用字段默认值作为资源值
                // 副语言如果文件已填充过资源则沿用，否则留空待填
                if (defaultValues != null && defaultValues.TryGetValue(field.Name, out var value))
                {
                    item.Content = value;
                }
                else
                {
                    var existingItem = existSource?[item.Key];
                    if (isExistFile && existingItem is not null)
                    {
                        item.Content = existingItem.Content;
                    }
                }
                result.Add(item);
            }

            SourceReader.Save(result, fileName);
        }

        private static void WriteStaleLanguageFilesManifest(string projectDirectory, FileGenerationInfo info, IEnumerable<string> currentSources)
        {
            CsprojUtil.WriteCleanupManifest(projectDirectory, GetStaleLanguageFiles(projectDirectory, info, currentSources));
        }

        private static IEnumerable<string> GetStaleLanguageFiles(string projectDirectory, FileGenerationInfo info, IEnumerable<string> currentSources)
        {
            var sourceDirectory = Path.Combine(projectDirectory, info.Directory ?? string.Empty);
            if (!Directory.Exists(sourceDirectory))
            {
                yield break;
            }

            var expectedFiles = new HashSet<string>(
                currentSources.Select(source => Path.GetFullPath(Path.Combine(projectDirectory, source))),
                System.StringComparer.OrdinalIgnoreCase);

            var escapedFlag = Regex.Escape(info.FileFlag);
            var pattern = new Regex($"^{escapedFlag}\\.[\\w]{{2}}([_-])[\\w]{{2}}\\.xml$", RegexOptions.IgnoreCase);

            foreach (var file in Directory.EnumerateFiles(sourceDirectory, $"{info.FileFlag}.*.xml", SearchOption.TopDirectoryOnly))
            {
                var fullPath = Path.GetFullPath(file);
                if (expectedFiles.Contains(fullPath))
                {
                    continue;
                }

                if (!pattern.IsMatch(Path.GetFileName(file)))
                {
                    continue;
                }

                yield return fullPath;
            }
        }

        private static bool TryGetEntryFieldValue(
            FieldDeclarationSyntax field,
            SemanticModel semanticModel,
            SourceProductionContext context,
            out string fieldName,
            out string? fieldValue)
        {
            fieldName = string.Empty;
            fieldValue = null;

            if (field.Declaration.Variables.Count != 1)
            {
                if (HasEntryAttribute(field, semanticModel))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GeneratorDiagnostics.EntryFieldMustBePrivateStaticString,
                        field.Declaration.GetLocation(),
                        field.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? "<unknown>"));
                }
                return false;
            }

            var variable = field.Declaration.Variables[0];
            if (semanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
            {
                return false;
            }

            if (!HasEntryAttribute(fieldSymbol))
            {
                return false;
            }

            fieldName = variable.Identifier.ValueText;

            if (!IsValidEntryField(fieldSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GeneratorDiagnostics.EntryFieldMustBePrivateStaticString,
                    field.Declaration.GetLocation(),
                    fieldName));
                return false;
            }

            if (variable.Initializer?.Value is not LiteralExpressionSyntax literal
                || !literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GeneratorDiagnostics.EntryFieldMustUseStringLiteralInitializer,
                    variable.GetLocation(),
                    fieldName));
                return false;
            }

            fieldValue = literal.Token.ValueText;
            return true;
        }

        private static bool IsValidEntryField(IFieldSymbol fieldSymbol)
        {
            return fieldSymbol.DeclaredAccessibility == Accessibility.Private
                && fieldSymbol.IsStatic
                && fieldSymbol.Type.SpecialType == SpecialType.System_String;
        }

        private static bool HasEntryAttribute(FieldDeclarationSyntax field, SemanticModel semanticModel)
        {
            return field.AttributeLists
                .SelectMany(t => t.Attributes)
                .Any(attribute =>
                    semanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol
                    && SupportAttributes.EntryAttributeName.Equals(attributeSymbol.ContainingType.ToDisplayString()));
        }

        private static bool HasEntryAttribute(IFieldSymbol fieldSymbol)
        {
            return fieldSymbol.GetAttributes()
                .Any(attribute => attribute.AttributeClass?.ToDisplayString().Equals(SupportAttributes.EntryAttributeName) == true);
        }
        #endregion

        #region  [Classes]
        internal record FileGenerationInfo
        {
            public string Directory { get; set; } = null!;
            public string FileFlag { get; set; } = null!;
            public string CsprojFile { get; set; } = null!;
            public LanguageType MainLanguageType { get; set; }
            public FileGenerationType FileGenerationType { get; set; }
        }
        #endregion
    }
}
