namespace Oceyra.Dbml.Parser.Models;

public class IndexModel
{
    public List<string> Columns { get; set; } = [];
    public Dictionary<string, string> Settings { get; set; } = [];
    public bool IsExpression { get; set; }

    // Individual flag properties
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
}
