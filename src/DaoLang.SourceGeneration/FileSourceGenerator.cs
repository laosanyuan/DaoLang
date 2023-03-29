using DaoLang.Shared.Enums;
using DaoLang.Shared.Models;
using DaoLang.Shared.Utils;
using DaoLang.SourceGeneration.Utils;
using DaoLang.SourceGenerators.Components;
using DaoLang.SourceGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

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
                Execute(source.Compilation, source.TypeDeclarationSyntaxes));
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

            return null;
        }

        /// <summary>
        /// 对语言Attribute标注生成字段内容
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="types"></param>
        private static void Execute(
            Compilation compilation,
            ImmutableArray<TypeDeclarationSyntax> types)
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
                    Directory = directory,
                    FileFlag = fileFlag,
                    MainLanguageType = type,
                    FileGenerationType = generationType,
                    CsprojFile = csprojFile,
                };

                // 字段内容更新
                UpdateFields(typeDeclarationSyntax, typeSymbol, info);
            }
        }

        /// <summary>
        /// 按条件更新字段
        /// </summary>
        /// <param name="typeDeclarationSyntax"></param>
        /// <param name="typeSymbol"></param>
        /// <param name="info"></param>
        private static void UpdateFields(
            SyntaxNode typeDeclarationSyntax,
            INamespaceOrTypeSymbol typeSymbol,
            FileGenerationInfo info)
        {
            var valueDic = new Dictionary<string, string>();
            var key = info.Directory + info.FileFlag;
            // 获取文件中的目标类，针对同一文件中存在多个类的情况
            if (typeDeclarationSyntax.SyntaxTree.GetRoot().DescendantNodes()
                    .FirstOrDefault(t =>
                    {
                        if (t is ClassDeclarationSyntax { AttributeLists.Count: > 0 } cds)
                        {
                            return cds.AttributeLists.Any(s =>
                            {
                                return s.Attributes.Any(tmp =>
                                {
                                    if (tmp.ArgumentList?.Arguments.Count != 3 &&
                                        tmp.ArgumentList?.Arguments.Count != 4)
                                    {
                                        return false;
                                    }
                                    var folder = ((LiteralExpressionSyntax)tmp.ArgumentList.Arguments[0].Expression)
                                        .Token
                                        .ValueText;
                                    var flag = ((LiteralExpressionSyntax)tmp.ArgumentList.Arguments[1].Expression)
                                        .Token
                                        .ValueText;
                                    return (folder + flag).Equals(key);
                                });
                            });
                        }
                        return false;
                    }) is not ClassDeclarationSyntax classNode)
            {
                return;
            }

            foreach (var member in classNode.Members)
            {
                if (member is not FieldDeclarationSyntax field)
                {
                    continue;
                }
                var fieldName = field.Declaration.Variables[0].Identifier.ValueText;
                var fieldValue = (field.Declaration.Variables[0].Initializer?.Value as LiteralExpressionSyntax)
                    ?.Token
                    .ValueText;
                if (!string.IsNullOrEmpty(fieldName)
                    && !valueDic.ContainsKey(fieldName)
                    && fieldValue != null)
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

            // 生成主语言文件
            GeneratorFile(
                typeSymbol,
                Path.Combine(Path.GetDirectoryName(info.CsprojFile), sourceName),
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
                    Path.Combine(Path.GetDirectoryName(info.CsprojFile)!, sourceName),
                    type);
            }

            // 更新.csproj文件
            CsprojUtil.AddFileToOutput(info.CsprojFile, sources, info.FileGenerationType);
        }

        /// <summary>
        /// 生成语言资源文件
        /// </summary>
        /// <param name="typeSymbol">roslyn类信息</param>
        /// <param name="fileName">资源文件名称</param>
        /// <param name="type">语言类型</param>
        /// <param name="defalutValue">主语言默认字段值</param>
        /// <param name="isMainLanguage">是否为主语言</param>
        private static void GeneratorFile(
            INamespaceOrTypeSymbol typeSymbol,
            string fileName,
            LanguageType type,
            Dictionary<string, string> defalutValue = null,
            bool isMainLanguage = false)
        {
            var isExistFile = SourceReader.Load(fileName, out Language existSource);
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
                if (defalutValue != null && defalutValue.TryGetValue(field.Name, out var value))
                {
                    item.Content = value;
                }
                else
                {
                    if (isExistFile && existSource?[item.Key] is not null)
                    {
                        item.Content = existSource[item.Key].Content;
                    }
                }
                result.Add(item);
            }

            SourceReader.Save(result, fileName);
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
