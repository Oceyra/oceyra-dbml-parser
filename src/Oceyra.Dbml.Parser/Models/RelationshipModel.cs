namespace Oceyra.Dbml.Parser.Models;

public class RelationshipModel
{
    public string FromTable { get; set; } = "";
    public string FromColumn { get; set; } = "";
    public string ToTable { get; set; } = "";
    public string ToColumn { get; set; } = "";
    public RelationshipType RelationshipType { get; set; }
}
