using DaoLang.Shared.Enums;
using DaoLang.Shared.Extensions;
using DaoLang.Shared.Models;
using DaoLang.Shared.Utils;
using DaoLang.SourceGenerators.Components;
using DaoLang.SourceGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace DaoLang.SourceGenerators
{
    [Generator(LanguageNames.CSharp)]
    public class FileSourceGenerator : IIncrementalGenerator
    {
        #region [Fields]
        /// <summary>
        /// 主语言标记特性
        /// </summary>
        private const string MainLanguageAttributeName = "DaoLang.Attributes.MainLanguageAttribute";
        /// <summary>
        /// 词条标记特性
        /// </summary>
        private const string EntryAttributeName = "DaoLang.Attributes.EntryAttribute";
        /// <summary>
        /// 副语言标记特性
        /// </summary>
        private const string SecondaryLanguageAttributeName = "DaoLang.Attributes.SecondaryLanguageAttribute";
        #endregion

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
                    if (MainLanguageAttributeName.Equals(attributeSymbol.ContainingType.ToDisplayString()))
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
                    t.AttributeClass!.ToDisplayString().Equals(MainLanguageAttributeName));

                // 只有设置了主语言才生成；仅有副语言无效
                if (main == null)
                {
                    continue;
                }

                var directory = (string)main.ConstructorArguments[0].Value;
                var fileFlag = (string)main.ConstructorArguments[1].Value;
                var tmpType = main.ConstructorArguments[2].Value;
                if (tmpType == null)
                {
                    continue;
                }

                var type = (LanguageType)tmpType;

                // 字段内容更新
                UpdateFields(typeDeclarationSyntax, typeSymbol, type, directory, fileFlag, csprojFile);
                // 语言类别更新
                UpdateLanguages(typeSymbol, directory, fileFlag, csprojFile);
            }
        }

        /// <summary>
        /// 按条件更新字段
        /// </summary>
        /// <param name="typeDeclarationSyntax"></param>
        /// <param name="typeSymbol"></param>
        /// <param name="type"></param>
        /// <param name="directory"></param>
        /// <param name="fileFlag"></param>
        /// <param name="csprojFile"></param>
        private static void UpdateFields(
            TypeDeclarationSyntax typeDeclarationSyntax,
            INamespaceOrTypeSymbol typeSymbol,
            LanguageType type,
            string directory,
            string fileFlag,
            string csprojFile)
        {
            var valueDic = new Dictionary<string, string>();
            var key = directory + fileFlag;
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
                                    if (tmp.ArgumentList?.Arguments.Count == 3 || tmp.ArgumentList?.Arguments.Count == 4)
                                    {
                                        var folder = ((LiteralExpressionSyntax)tmp.ArgumentList.Arguments[0].Expression)
                                            .Token
                                            .ValueText;
                                        var flag = ((LiteralExpressionSyntax)tmp.ArgumentList.Arguments[1].Expression)
                                            .Token
                                            .ValueText;
                                        if ((folder + flag).Equals(key))
                                        {
                                            return true;
                                        }
                                    }
                                    return false;
                                });
                            });
                        }
                        return false;
                    }) is not ClassDeclarationSyntax classNode)
            {
                return;
            }

            foreach (var member in classNode?.Members)
            {
                if (member is FieldDeclarationSyntax field)
                {
                    var fieldName = field.Declaration.Variables[0].Identifier.ValueText;
                    var fieldValue = ((LiteralExpressionSyntax)field.Declaration.Variables[0].Initializer.Value)
                        .Token
                        .ValueText;
                    if (fieldName != null && !valueDic.ContainsKey(fieldName))
                    {
                        valueDic.Add(fieldName, fieldValue);
                    }
                }
            }

            // 更新全部文件
            UpdateLanguageFiles(typeSymbol, type, directory, fileFlag, csprojFile, valueDic);
        }

        /// <summary>
        /// 更新语言文件种类
        /// </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="reader"></param>
        /// <param name="fileType"></param>
        /// <param name="directory"></param>
        /// <param name="fileFlag"></param>
        /// <param name="csprojFile"></param>
        private static void UpdateLanguages(
            INamespaceOrTypeSymbol typeSymbol,
            string directory,
            string fileFlag,
            string csprojFile)
        {
            var attributes = typeSymbol.GetAttributes();
            var sources = new List<string>();
            var languages = new List<LanguageType>();
            var key = directory + fileFlag;
            foreach (var attribute in attributes.Where(
                         t => t.AttributeClass?.ToDisplayString()
                             .Equals(SecondaryLanguageAttributeName) == true))
            {
                if (attribute.ConstructorArguments == null
                    || attribute.ConstructorArguments.Length <= 0)
                {
                    continue;
                }

                var type = (LanguageType)attribute.ConstructorArguments[0].Value;
                languages.Add(type);

                var sourceName = SourceReader.GetFileName(directory, fileFlag, type);

                sources.Add(sourceName);
                // 生成新添加的副语言文件
                GeneratorFile(
                    typeSymbol,
                    Path.Combine(Path.GetDirectoryName(csprojFile), sourceName),
                    type);
            }

            if (sources.Count > 0)
            {
                // 更新.csproj文件
                CsprojUtil.AddFileToOutput(csprojFile, sources);
            }
        }

        /// <summary>
        /// 字段集合更新,按类别更新资源
        /// </summary>
        /// <param name="typeSymbol"></param>
        /// <param name="type"></param>
        /// <param name="fileType"></param>
        /// <param name="reader"></param>
        /// <param name="directory"></param>
        /// <param name="fileFlag"></param>
        /// <param name="csprojFile"></param>
        /// <param name="fieldsValueDic"></param>
        /// <returns></returns>
        private static void UpdateLanguageFiles(
            INamespaceOrTypeSymbol typeSymbol,
            LanguageType type,
            string directory,
            string fileFlag,
            string csprojFile,
            Dictionary<string, string> fieldsValueDic,
            bool onlyUpdateMain = false)
        {
            var sources = new List<string>();
            var sourceName = SourceReader.GetFileName(directory, fileFlag, type);
            sources.Add(sourceName);
            var attributes = typeSymbol.GetAttributes();

            // 生成主语言文件
            GeneratorFile(
                typeSymbol,
                Path.Combine(Path.GetDirectoryName(csprojFile), sourceName),
                type,
                fieldsValueDic,
                true);

            if (onlyUpdateMain)
            {
                return;
            }

            var languages = new List<LanguageType>();
            var key = directory + fileFlag;
            // 生成副语言文件
            foreach (var attribute in attributes.Where(
                         t => t.AttributeClass?.ToDisplayString()
                             .Equals(SecondaryLanguageAttributeName) == true))
            {
                type = (LanguageType)attribute.ConstructorArguments[0].Value;
                languages.Add(type);
                sourceName = SourceReader.GetFileName(directory, fileFlag, type);
                sources.Add(sourceName);
                GeneratorFile(
                    typeSymbol,
                    Path.Combine(Path.GetDirectoryName(csprojFile), sourceName),
                    type);
            }

            // 更新.csproj文件
            CsprojUtil.AddFileToOutput(csprojFile, sources);
        }

        /// <summary>
        /// 生成语言资源文件
        /// </summary>
        /// <param name="typeSymbol">roslyn类信息</param>
        /// <param name="fileName">资源文件名称</param>
        /// <param name="type">语言类型</param>
        /// <param name="defalutValue">主语言默认字段值</param>
        /// <param name="sourceFileType">生成文件类型</param>
        private static void GeneratorFile(
            INamespaceOrTypeSymbol typeSymbol,
            string fileName,
            LanguageType type,
            Dictionary<string, string> defalutValue = null,
            bool isMainLanguage = false)
        {
            var isExistFile = SourceReader.Load(fileName, out Language existSource);
            var result = new Language { IsMainLanguage = isMainLanguage };

            foreach (var field in typeSymbol.GetMembers().Where(t => t is { Kind: SymbolKind.Field }))
            {
                var attributes = field.GetAttributes();
                var entry = attributes.FirstOrDefault(t => t.AttributeClass?.ToDisplayString().Equals(EntryAttributeName) == true);
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
    }
}
