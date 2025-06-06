using Luban.CodeFormat;
using Luban.CodeFormat.CodeStyles;
using Luban.DataLoader.Builtin.Excel;
using Luban.Defs;
using Luban.RawDefs;
using Luban.Utils;

namespace Luban.Schema.Builtin;

[TableImporter("next-custom")]
public class NextCustomTableImporter : ITableImporter
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 从配置的数据目录加载并解析所有符合要求的表格文件，生成原始表结构信息列表
    /// 
    /// 主要功能：
    /// 1. 遍历输入数据目录及其子目录中的所有Excel/CSV文件
    /// 2. 通过正则匹配文件名提取表名信息
    /// 3. 根据配置规则生成命名空间、表名、值类型名称
    /// 4. 收集文件信息并构建原始表结构定义
    /// </summary>
    /// <returns>解析得到的原始表结构信息列表</returns>
    public List<RawTable> LoadImportTables()
    {
        string dataDir = GenerationContext.GlobalConf.InputDataDir;
        
        // 其他配置保持原样
        string tableNamespaceFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "tableNamespaceFormat", false, "{0}");
        string tableNameFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "tableNameFormat", false, "{0}");
        string valueTypeNameFormatStr = EnvManager.Current.GetOptionOrDefault("tableImporter", "valueTypeNameFormat", false, "{0}Config");
        var excelExts = new HashSet<string> { "xlsx", "xls", "xlsm", "csv" };
        
        var tables = new List<RawTable>();
        foreach (string file in Directory.GetFiles(dataDir, "*", SearchOption.AllDirectories))
        {
            if (FileUtil.IsIgnoreFile(dataDir, file)) continue;
            
            string ext = Path.GetExtension(file).TrimStart('.');
            if (!excelExts.Contains(ext)) continue;
            
            // 获取文件相对路径（用于命名空间生成）
            string relativePath = file.Substring(dataDir.Length + 1).TrimStart('\\', '/');
            string namespaceFromRelativePath = Path.GetDirectoryName(relativePath)?.Replace('/', '.').Replace('\\', '.');
            
            // 新增：读取Excel文件的所有Sheet名称
            var sheetNames = GetExcelSheetNames(file); // 需要实现Excel读取逻辑
            
            foreach (var sheetName in sheetNames)
            {
                string rawTableName = sheetName;
                string formatSheetName = CodeFormatManager.Ins.CsharpDefaultCodeStyle.FormatNamespace(rawTableName);
                // 根据配置规则生成最终名称
                string tableNamespace = string.Format(tableNamespaceFormatStr, namespaceFromRelativePath);
                string valueTypeFullName = TypeUtil.MakeFullName(tableNamespace, string.Format(valueTypeNameFormatStr, formatSheetName));
                string tableName = string.Format(tableNameFormatStr, formatSheetName);
                var input = $"{rawTableName}@{relativePath}";
                // s_logger.Info($"import table tableNamespace:{tableNamespace}, tableName:{tableName}, valueTypeFullName:{valueTypeFullName}, relativePath:{relativePath}");

                tables.Add(new RawTable
                {
                    Namespace = tableNamespace,
                    Name = tableName,
                    Index = "", // 索引字段（留空需后续处理）
                    ValueType = valueTypeFullName, // 关联的值类型
                    ReadSchemaFromFile = true, // 标记需要从文件读取schema
                    Mode = TableMode.MAP, // 默认使用Map模式（键值对结构）
                    Comment = "", // 注释信息
                    Groups = new List<string> { }, // 表分组信息
                    InputFiles = new List<string> { input }, // 关联的输入文件路径
                    OutputFile = "", // 输出文件路径（后续生成）
                });
            }
        }

        return tables;
    }
    
    private List<string> GetExcelSheetNames(string filePath)
    {
        var sheets = new List<string>();
        foreach (string sheetName in SheetLoadUtil.LoadRaw(filePath, new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
        {
            sheets.Add(sheetName);
        }
        return sheets;
    }
}
