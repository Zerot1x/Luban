using System.Text.RegularExpressions;
using Luban.CodeFormat;
using Luban.Datas;
using Luban.Defs;
using Luban.Types;
using Luban.Utils;
using Luban.Validator;

namespace Luban.DataValidator.Builtin.Collection;

[Validator("codename")]
public class CodeNameValidator : DataValidatorBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private List<string> _tables;
    public override void Compile(DefField field, TType type)
    {
        this._tables = DefUtil.TrimBracePairs(Args).Split(',').Select(s => s.Trim()).ToList();
        if (type is not TString)
        {
            throw new Exception($"field:{field} text validator supports string type only");
        }
    }

    public override void Validate(DataValidatorContext ctx, TType type, DType data)
    {
        var index = DataValidatorContext.CurrentVisitor.CurrentValidateRecord.Data.Fields[0].ToString();
        var table = DataValidatorContext.CurrentVisitor.CurrentValidateRecord.Data.Type;
        string result = Regex.Replace(data.ToString(), "^\"|\"$", "");
        result = CodeFormatManager.Ins.CsharpDefaultCodeStyle.FormatNamespace(result);
        if (!table.CodeNameMap.TryAdd(result, index))
        {
            s_logger.Error("记录 {}:{} (来自文件:{}) codename 加载失败", DataValidatorContext.CurrentRecordPath, data, Source);
        }
    }
}
