namespace Oceyra.Dbml.Parser.Models;

public class DatabaseModel
{
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "Microsoft.EntityFrameworkCore.SqlServer";
    public List<TableModel> Tables { get; set; } = [];
    public List<RelationshipModel> Relationships { get; set; } = [];
    public List<EnumModel> Enums { get; set; } = [];
}
