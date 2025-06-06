using Luban.Defs;
using Luban.RawDefs;
using Luban.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Luban.Schema.Builtin;

[TableImporter("default")]
public class DefaultTableImporter : ITableImporter
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    public List<RawTable> LoadImportTables()
    {
        string dataDir = GenerationContext.GlobalConf.InputDataDir;
        
        string fileNamePatternStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "filePattern", false, "(.*)"); //"#(.*)"
        string tableNamespaceFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "tableNamespaceFormat", false, "{0}");
        string tableNameFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "tableNameFormat", false, "Tb{0}");
        string valueTypeNameFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "valueTypeNameFormat", false, "{0}");
        var fileNamePattern = new Regex(fileNamePatternStr);
        var excelExts = new HashSet<string> { "xlsx", "xls", "xlsm", "csv" };

        var tables = new List<RawTable>();
        foreach (string file in Directory.GetFiles(dataDir, "*", SearchOption.AllDirectories))
        {
            // 跳过忽略文件
            if (FileUtil.IsIgnoreFile(dataDir, file))
            {
                s_logger.Info($"跳过忽略文件 ...");
                continue;
            }
            
            // 解析文件基本信息
            string fileName = Path.GetFileName(file);
            string ext = Path.GetExtension(fileName).TrimStart('.');
            
            // 过滤非表格文件类型
            if (!excelExts.Contains(ext))
            {
                s_logger.Info($"过滤非表格文件类型 ...");
                continue;
            }
            
            // 使用正则匹配文件名提取表标识
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var match = fileNamePattern.Match(fileNameWithoutExt);
            if (!match.Success || match.Groups.Count <= 1)
            {
                s_logger.Info($"跳过不符合命名规则的文件 ...");
                continue;
            }

            string relativePath = file.Substring(dataDir.Length + 1).TrimStart('\\').TrimStart('/');
            string namespaceFromRelativePath = Path.GetDirectoryName(relativePath)?.Replace('/', '.').Replace('\\', '.'); // 将路径转换为命名空间格式

            // 解析原始表名信息
            string rawTableFullName = match.Groups[1].Value;  // 从正则匹配组获取完整表名
            string rawTableNamespace = TypeUtil.GetNamespace(rawTableFullName); // 分离命名空间部分
            string rawTableName = TypeUtil.GetName(rawTableFullName); // 分离表名部分
            
            // 根据配置规则生成最终名称
            string tableNamespace = TypeUtil.MakeFullName(namespaceFromRelativePath, string.Format(tableNamespaceFormatStr, rawTableNamespace)); // 组合路径命名空间和配置格式
            string tableName = string.Format(tableNameFormatStr, rawTableName); // 应用表名格式
            string valueTypeFullName = TypeUtil.MakeFullName(tableNamespace, string.Format(valueTypeNameFormatStr, rawTableName)); // 生成值类型全名
            
            var table = new RawTable()
            {
                Namespace = tableNamespace,
                Name = tableName,
                Index = "", // 索引字段（留空需后续处理）
                ValueType = valueTypeFullName, // 关联的值类型
                ReadSchemaFromFile = true, // 标记需要从文件读取schema
                Mode = TableMode.MAP, // 默认使用Map模式（键值对结构）
                Comment = "", // 注释信息
                Groups = new List<string> { }, // 表分组信息
                InputFiles = new List<string> { relativePath }, // 关联的输入文件路径
                OutputFile = "", // 输出文件路径（后续生成）,
                Tags = new Dictionary<string, string>(),
            };
            // 记录调试日志
            s_logger.Debug("import table file:{@}", table);
            tables.Add(table);
        }


        return tables;
    }
}
