using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Oceyra.Dbml.Parser.Models;

namespace Oceyra.Dbml.Parser;


public class DbmlParser
{
    private static readonly Regex TableHeaderRegex = new Regex(@"Table\s+(""[^""]+""|\w+)(?:\s+as\s+(""[^""]+""|\w+))?\s*{", RegexOptions.IgnoreCase);

    private static readonly Regex TableRegex = new Regex(@"Table\s+(""[^""]+""|\w+)(?:\s+as\s+(""[^""]+""|\w+))?\s*{([^}]*)}", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex FieldRegex = new Regex(@"(""[^""]+""|\w+)\s+(\w+(?:\([^)]*\))?(?:\[\])?)((?:\s*\[[^\]]*\])*)\s*(?://.*)?", RegexOptions.Multiline);
    private static readonly Regex RefRegex = new Regex(@"Ref:\s*(""[^""]+""|\w+)\.(""[^""]+""|\w+)\s*([<>-]+)\s*(""[^""]+""|\w+)\.(""[^""]+""|\w+)", RegexOptions.IgnoreCase);
    private static readonly Regex EnumRegex = new Regex(@"enum\s+(""[^""]+""|\w+)\s*{([^}]*)}", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex ProjectRegex = new Regex(@"Project\s+(""[^""]+""|\w+)\s*{([^}]*)}", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex IndexBlockRegex = new Regex(@"Indexes\s*{([^}]*)}", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex IndexEntryRegex = new Regex(@"\(([^)]+)\)\s*\[([^\]]*)\]", RegexOptions.Multiline);


    private readonly List<RelationshipModel> _inlineRelationships = new();

    public DatabaseModel Parse(string dbmlContent)
    {
        var database = new DatabaseModel { Name = string.Empty };

        // Remove comments
        dbmlContent = RemoveComments(dbmlContent);

        // Parse project info (optional)
        ParseProjectInfo(dbmlContent, database);

        // Parse enums
        ParseEnums(dbmlContent, database);

        // Parse tables
        ParseTables(dbmlContent, database);

        // Parse references (foreign keys)
        ParseReferences(dbmlContent, database);
        database.Relationships.AddRange(_inlineRelationships);

        return database;
    }

    private string RemoveComments(string content)
    {
        // Remove single-line comments
        content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);

        // Remove multi-line comments
        content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);

        return content;
    }

    private void ParseProjectInfo(string content, DatabaseModel database)
    {
        var projectMatch = ProjectRegex.Match(content);
        if (projectMatch.Success)
        {
            database.Name = Unquote(projectMatch.Groups[1].Value);
            var projectBody = Unquote(projectMatch.Groups[2].Value);

            // Parse database_type if present
            var dbTypeMatch = Regex.Match(projectBody, @"database_type:\s*['""]([^'""]*)['""]", RegexOptions.IgnoreCase);
            if (dbTypeMatch.Success)
            {
                database.Provider = MapDatabaseType(Unquote(dbTypeMatch.Groups[1].Value));
            }
        }
    }

    private void ParseEnums(string content, DatabaseModel database)
    {
        var enumMatches = EnumRegex.Matches(content);
        foreach (Match match in enumMatches)
        {
            var enumModel = new EnumModel
            {
                Name = Unquote(match.Groups[1].Value),
                Values = new List<string>()
            };

            var enumBody = Unquote(match.Groups[2].Value);
            var values = enumBody.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(v => v.Trim())
                                 .Where(v => !string.IsNullOrEmpty(v))
                                 .ToList();

            foreach (var value in values)
            {
                var cleanValue = Regex.Replace(value, @"['""]", "").Trim();
                if (!string.IsNullOrEmpty(cleanValue))
                {
                    enumModel.Values.Add(cleanValue);
                }
            }

            database.Enums.Add(enumModel);
        }
    }

    private void ParseTables(string content, DatabaseModel database)
    {
        var matches = TableHeaderRegex.Matches(content);

        foreach (Match match in matches)
        {
            var startIndex = match.Index;
            var bodyStart = content.IndexOf('{', startIndex);
            if (bodyStart < 0) continue;

            int bodyEnd = FindMatchingBrace(content, bodyStart);
            if (bodyEnd < 0) continue; // Malformed

            string tableBody = content.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);

            var table = new TableModel
            {
                Name = Unquote(match.Groups[1].Value),
                ClassName = match.Groups[2].Success ? Unquote(match.Groups[2].Value) : Unquote(match.Groups[1].Value),
                Columns = new List<ColumnModel>()
            };

            ParseTableFields(tableBody, table);
            ParseTableIndexes(tableBody, table);

            database.Tables.Add(table);
        }
    }

    private int FindMatchingBrace(string content, int startIndex)
    {
        int depth = 0;
        for (int i = startIndex; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1; // No matching brace
    }

    private void ParseTableFields(string tableBody, TableModel table)
    {
        var fieldMatches = FieldRegex.Matches(tableBody);

        foreach (Match match in fieldMatches)
        {
            var column = new ColumnModel
            {
                Name = Unquote(match.Groups[1].Value),
                Member = Unquote(match.Groups[1].Value),
                Type = Unquote(match.Groups[2].Value),
                CanBeNull = true
            };

            if (match.Groups[3].Success)
            {
                var attributes = match.Groups[3].Value;
                ParseFieldAttributes(attributes, column, table);
            }

            table.Columns.Add(column);
        }
    }

    private void ParseFieldAttributes(string attributes, ColumnModel column, TableModel table)
    {
        // Extract each [ ... ] block from string
        var attrBlocks = Regex.Matches(attributes, @"\[[^\]]*\]")
            .Cast<Match>()
            .Select(m => m.Value.Trim('[', ']'))
            .ToList();

        foreach (var block in attrBlocks)
        {
            var attrs = block.Split(',').Select(a => a.Trim()).ToList();

            foreach (var attr in attrs)
            {
                var lowerAttr = attr.ToLower();

                if (lowerAttr == "pk" || lowerAttr == "primary key")
                {
                    column.IsPrimaryKey = true;
                }
                else if (lowerAttr == "not null")
                {
                    column.CanBeNull = false;
                }
                else if (lowerAttr == "null")
                {
                    column.CanBeNull = true;
                }
                else if (lowerAttr == "increment" || lowerAttr == "auto-increment")
                {
                    column.IsDbGenerated = true;
                }
                else if (lowerAttr.StartsWith("default:"))
                {
                    column.DefaultValue = lowerAttr.Substring(8).Trim();
                }
                else if (lowerAttr.StartsWith("note:"))
                {
                    column.Note = lowerAttr.Substring(5).Trim('\'', '"');
                }
                else if (lowerAttr.StartsWith("ref:"))
                {
                    var refMatch = Regex.Match(attr, @"ref:\s*([<>-]+)\s*(""[^""]+""|\w+)\.(""[^""]+""|\w+)", RegexOptions.IgnoreCase);
                    if (refMatch.Success)
                    {
                        var relationship = new RelationshipModel
                        {
                            FromTable = table.Name,
                            FromColumn = column.Name,
                            ToTable = Unquote(refMatch.Groups[2].Value),
                            ToColumn = Unquote(refMatch.Groups[3].Value),
                            RelationshipType = ParseRelationshipType(refMatch.Groups[1].Value)
                        };
                        _inlineRelationships.Add(relationship);
                    }
                }
            }
        }
    }

    private void ParseTableIndexes(string tableBody, TableModel table)
    {
        var indexBlockMatch = IndexBlockRegex.Match(tableBody);
        if (!indexBlockMatch.Success)
            return;

        var indexBody = indexBlockMatch.Groups[1].Value;
        var indexEntries = IndexEntryRegex.Matches(indexBody);

        foreach (Match match in indexEntries)
        {
            var fields = match.Groups[1].Value
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(f => Unquote(f.Trim()))
                .ToList();

            var attributes = match.Groups[2].Value
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim().ToLower())
                .ToList();

            var index = new IndexModel
            {
                Columns = fields,
                IsUnique = attributes.Contains("unique"),
                Name = Unquote( attributes.FirstOrDefault(a => a.StartsWith("name:"))?
                    .Substring("name:".Length)
                    .Trim('\'', '"') ?? string.Empty)
            };

            table.Indexes.Add(index);
        }
    }

    private void ParseReferences(string content, DatabaseModel database)
    {
        var refMatches = RefRegex.Matches(content);

        foreach (Match match in refMatches)
        {
            var relationship = new RelationshipModel
            {
                FromTable = Unquote(match.Groups[1].Value),
                FromColumn = Unquote(match.Groups[2].Value),
                ToTable = Unquote(match.Groups[4].Value),
                ToColumn = Unquote(match.Groups[5].Value),
                RelationshipType = ParseRelationshipType(Unquote(match.Groups[3].Value))
            };

            database.Relationships.Add(relationship);
        }
    }

    private RelationshipType ParseRelationshipType(string symbol)
    {
        return symbol.Trim() switch
        {
            "<" => RelationshipType.OneToMany,
            ">" => RelationshipType.ManyToOne,
            "-" => RelationshipType.OneToOne,
            "<>" => RelationshipType.ManyToMany,
            _ => RelationshipType.OneToMany
        };
    }

    private string MapDatabaseType(string dbType)
    {
        return dbType.ToLower() switch
        {
            "postgresql" => "Npgsql.EntityFrameworkCore.PostgreSQL",
            "mysql" => "Pomelo.EntityFrameworkCore.MySql",
            "sqlite" => "Microsoft.EntityFrameworkCore.Sqlite",
            "sqlserver" => "Microsoft.EntityFrameworkCore.SqlServer",
            _ => "Microsoft.EntityFrameworkCore.SqlServer"
        };
    }
    private string Unquote(string value) =>
    value.Trim().Trim('"');

}
