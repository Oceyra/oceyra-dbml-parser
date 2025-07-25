﻿namespace Oceyra.Dbml.Parser.Models;

public class TableModel
{
    public string Schema { get; set; } = "public";
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public Dictionary<string, string> Settings { get; set; } = [];
    public List<ColumnModel> Columns { get; set; } = [];
    public List<IndexModel> Indexes { get; set; } = [];
    public string? Note { get; set; }
    public List<string> InjectedPartials { get; set; } = [];
}
