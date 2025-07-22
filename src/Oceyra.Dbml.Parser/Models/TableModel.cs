namespace Oceyra.Dbml.Parser.Models;

public class TableModel
{
    public string Name { get; set; } = "";
    public string ClassName { get; set; } = "";
    public List<ColumnModel> Columns { get; set; } = [];
    public List<IndexModel> Indexes { get; set; } = [];

}
