namespace Oceyra.Dbml.Parser.Models;

public class RelationshipModel
{
    public string? Name { get; set; }
    public string? LeftTable { get; set; }
    public string? LeftColumn { get; set; }
    public string? RightTable { get; set; }
    public string? RightColumn { get; set; }
    public RelationshipType RelationshipType { get; set; } // <, >, -, <>
    public Dictionary<string, string> Settings { get; set; } = [];
}
