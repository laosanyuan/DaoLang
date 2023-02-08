using System.IO;
using System.Linq;

namespace DaoLang.SourceGenerators.Utils
{
    internal static class DirectoryUtil
    {
        /// <summary>
        /// 获取项目.csproj文件路径
        /// </summary>
        /// <param name="filePath">项目种包含的某一文件路径</param>
        /// <returns></returns>
        public static string GetProjectFilePath(string filePath)
        {
            var currentPath = Path.GetDirectoryName(filePath);
            var realPath = CheckParentPath(currentPath!);
            if (string.IsNullOrEmpty(realPath))
            {
                return default!;
            }
            else
            {
                var files = Directory.GetFiles(realPath);
                return files.FirstOrDefault(t => t.Contains(".csproj")) ?? string.Empty;
            }

            string CheckParentPath(string directory)
            {
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    return default!;
                }

                var files = Directory.GetFiles(directory);
                if (files?.Any(t => t.Contains(".csproj")) == true)
                {
                    return directory;
                }
                else
                {
                    DirectoryInfo info = new(directory);
                    return CheckParentPath(info.Parent?.FullName!);
                }
            }
        }
    }
}
