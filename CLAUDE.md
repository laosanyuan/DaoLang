# CLAUDE.md

## 项目简介

DaoLang 是一个 .NET 客户端多语言本地化库。

核心机制：

- 用 Attribute 标记主语言、副语言和词条
- 用 Roslyn Source Generator 生成资源访问代码
- 自动维护 XML 语言资源文件

解决方案入口：

- [src/DaoLang.sln](./src/DaoLang.sln)

核心项目：

- [src/DaoLang.SourceGeneration/DaoLang.SourceGeneration.csproj](./src/DaoLang.SourceGeneration/DaoLang.SourceGeneration.csproj)
- [src/DaoLang.WPF/DaoLang.WPF.csproj](./src/DaoLang.WPF/DaoLang.WPF.csproj)
- [tests/DaoLang.Tests/DaoLang.Tests.csproj](./tests/DaoLang.Tests/DaoLang.Tests.csproj)

## 当前重点

最近重点改动是“删除副语言时同步删除资源文件和 `.csproj` 输出标记”。

相关代码：

- [src/DaoLang.SourceGeneration/FileSourceGenerator.cs](./src/DaoLang.SourceGeneration/FileSourceGenerator.cs)
- [src/DaoLang.SourceGeneration/Utils/CsprojUtil.cs](./src/DaoLang.SourceGeneration/Utils/CsprojUtil.cs)
- [tests/DaoLang.Tests/Generation/FileSynchronizationTests.cs](./tests/DaoLang.Tests/Generation/FileSynchronizationTests.cs)

当前已验证：

- `DaoLang.SourceGeneration` 可正常构建
- `DaoLang.WPF.Sample` 可正常 `Rebuild`
- `DaoLang.Tests` 已通过 `6/6`

## 当前 WPF Sample

WPF sample 走的是本地项目引用，不是 NuGet 包：

- [src/Samples/DaoLang.WPF.Sample/DaoLang.WPF.Sample.csproj](./src/Samples/DaoLang.WPF.Sample/DaoLang.WPF.Sample.csproj)

当前语言声明：

- 主语言：`EN_US`
- 副语言：`ZH_CN`

对应文件：

- [src/Samples/DaoLang.WPF.Sample/Localization.cs](./src/Samples/DaoLang.WPF.Sample/Localization.cs)

当前 sample 的资源文件应只有：

- `Source/Language.en_us.xml`
- `Source/Language.zh_cn.xml`

## 修改约定

- 不要把资源文件删除逻辑改回“生成器直接立刻删磁盘文件”
- 优先保留“生成器计算 + `.csproj`/target 执行清理”的结构
- 改 sample 语言声明时，要同时确认 `Localization.cs`、`Source` 目录和 `.csproj` 结果一致
- 改这块逻辑时，优先保证 [tests/DaoLang.Tests/Generation/FileSynchronizationTests.cs](./tests/DaoLang.Tests/Generation/FileSynchronizationTests.cs) 继续覆盖“删除副语言后同步清理”的场景

## 本地构建

推荐先设置：

```powershell
$env:APPDATA='D:\Codes\DaoLang\.appdata'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_HOME='D:\Codes\DaoLang\.dotnet'
```

并使用：

- [NuGet.Config](./NuGet.Config)

常用命令：

```powershell
dotnet build src\DaoLang.SourceGeneration\DaoLang.SourceGeneration.csproj --configfile NuGet.Config -p:NuGetAudit=false -v minimal
dotnet build src\Samples\DaoLang.WPF.Sample\DaoLang.WPF.Sample.csproj --configfile NuGet.Config -p:NuGetAudit=false -t:Rebuild -v minimal
dotnet restore tests\DaoLang.Tests\DaoLang.Tests.csproj --configfile NuGet.Config -p:NuGetAudit=false
dotnet test tests\DaoLang.Tests\DaoLang.Tests.csproj --no-restore --no-build -p:NuGetAudit=false -v minimal
```

## 已知注意点

- `dotnet test` 不要直接带 `--configfile`，先 `restore`，再 `test --no-restore`
- 测试工程有时会因为 `SourceLink` 或 `testhost` 文件锁失败，优先用 `build` 后再 `test --no-build`
- [tests/DaoLang.Tests/Generation/FileSynchronizationTests.cs](./tests/DaoLang.Tests/Generation/FileSynchronizationTests.cs) 会创建临时项目并执行 `dotnet build`，因此依赖 `nuget.org` 可访问

## 发布前最少检查

- `DaoLang.SourceGeneration` 构建通过
- `DaoLang.WPF.Sample` 重建通过
- `DaoLang.Tests` `6/6` 通过
