namespace Oceyra.Dbml.Parser.Models;

public class EnumModel
{
    public string Schema { get; set; } = "public";
    public string? Name { get; set; }
    public List<EnumValueModel> Values { get; set; } = [];
}
