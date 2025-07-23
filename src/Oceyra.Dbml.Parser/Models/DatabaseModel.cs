namespace Oceyra.Dbml.Parser.Models;

public class DatabaseModel
{
    public ProjectModel? Project { get; set; }
    public List<TableModel> Tables { get; set; } = [];
    public List<EnumModel> Enums { get; set; } = [];
    public List<RelationshipModel> Relationships { get; set; } = [];
    public List<TableGroupModel> TableGroups { get; set; } = [];
    public List<TablePartialModel> TablePartials { get; set; } = [];
    public List<StickyNoteModel> StickyNotes { get; set; } = [];
}
