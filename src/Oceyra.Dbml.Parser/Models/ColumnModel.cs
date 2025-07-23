namespace Oceyra.Dbml.Parser.Models;

public class ColumnModel
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? DefaultValue { get; set; }
    public Dictionary<string, string> Settings { get; set; } = [];
    public string? Note { get; set; }
    public RelationshipModel? InlineRef { get; set; }

    // Individual flag properties
    public bool IsPrimaryKey { get; set; }
    public bool IsNull { get; set; } = true; // Default is nullable
    public bool IsNotNull { get; set; }
    public bool IsUnique { get; set; }
    public bool IsIncrement { get; set; }
}