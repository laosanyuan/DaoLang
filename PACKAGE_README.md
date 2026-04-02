# DaoLang

DaoLang is a .NET localization library for desktop and cross-platform UI apps.  
DaoLang 是一个面向 .NET 桌面与跨平台 UI 应用的本地化库。

It uses source generators to reduce manual localization work.  
它通过 Source Generator 减少手工维护多语言资源的成本。

## Features / 功能

- Generate strongly typed entry properties.  
  自动生成强类型词条属性。
- Generate and keep language resource files in sync.  
  自动生成并同步语言资源文件。
- Provide platform-specific resource dictionaries for binding.  
  为不同 UI 平台生成可直接绑定的资源字典。
- Support runtime language switching with main-language fallback.  
  支持运行时切换语言，并在缺失资源时回退到主语言。

## Packages / 包

- `DaoLang.WPF`
- `DaoLang.Avalonia`
- `DaoLang.WinUI3`
- `DaoLang.MAUI`

## Basic Usage / 基本使用

1. Install the package for your UI framework.  
   安装对应 UI 平台的 NuGet 包。
2. Define a localization class with DaoLang attributes.  
   使用 DaoLang 特性定义本地化资源类。
3. Mark entries with `[Entry]`.  
   使用 `[Entry]` 标记词条字段。
4. Build the project to generate code and resource files.  
   构建项目以生成代码和资源文件。
5. Initialize localization at startup and switch languages at runtime.  
   在启动时初始化，并在运行时切换语言。

## Repository / 仓库

- <https://github.com/laosanyuan/DaoLang>

## License / 许可证

- MIT
