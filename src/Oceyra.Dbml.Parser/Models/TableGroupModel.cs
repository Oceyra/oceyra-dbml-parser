namespace Oceyra.Dbml.Parser.Models;

public class TableGroupModel
{
    public string? Name { get; set; }
    public List<string> Tables { get; set; } = [];
    public Dictionary<string, string> Settings { get; set; } = [];
    public string? Note { get; set; }
}
