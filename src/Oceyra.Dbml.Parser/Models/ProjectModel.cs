namespace Oceyra.Dbml.Parser.Models;

// Data structures to hold parsed results
public class ProjectModel
{
    public string? Name { get; set; }
    public Dictionary<string, string> Settings { get; set; } = [];
    public string? Note { get; set; }
}
