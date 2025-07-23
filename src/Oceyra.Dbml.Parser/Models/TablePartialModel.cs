namespace Oceyra.Dbml.Parser.Models;

public class TablePartialModel
{
    public string? Name { get; set; }
    public Dictionary<string, string> Settings { get; set; } = [];
    public List<ColumnModel> Columns { get; set; } = [];
    public List<IndexModel> Indexes { get; set; } = [];
}
