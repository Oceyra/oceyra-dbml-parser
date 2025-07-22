namespace Oceyra.Dbml.Parser.Models;

public class ColumnModel
{
    public string Name { get; set; } = "";
    public string Member { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsPrimaryKey { get; set; }
    public bool IsDbGenerated { get; set; }
    public bool CanBeNull { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? Note { get; set; }
}
