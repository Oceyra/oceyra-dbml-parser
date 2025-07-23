namespace Oceyra.Dbml.Parser.Models;

public class EnumValueModel
{
    public string? Value { get; set; }
    public Dictionary<string, string> Settings { get; set; } = [];
    public string? Note { get; set; }
}
