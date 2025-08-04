using Oceyra.Dbml.Parser.Models;
using System.Text.RegularExpressions;

namespace Oceyra.Dbml.Parser;

public static partial class DbmlParser
{
    #region Regex Patterns
    // Compiled regex patterns for performance
    private static readonly Regex ProjectPattern = new(@"Project\s+(?<name>\w+|\""[^""]+\"")\s*\{\s*(?<content>(?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex SchemaTablePattern = new(@"Table\s+(?:(?<schema>\w+)\.)?(?<table>\w+|\""[^""]+\"")(?:\s+as\s+(?<alias>\w+))?\s*(?<settings>\[[^\]]*\])?\s*\{\s*(?<content>(?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ColumnPattern = new(@"^\s*(?<name>\w+|\""[^""]+\"")\s+(?<type>[\w\(\),\s]+?)(?<settings>(?:\s*\[[^\[\]]*\])*)\s*(?://.*)?$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex IndexPattern = new(@"indexes\s*\{\s*(?<content>(?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex IndexItemPattern = new(@"^\s*(?:\((?<columns>(?:`[^`]*`|[^,)]+)(?:\s*,\s*(?:`[^`]*`|[^,)]+))*)\)|(?<column>`[^`]+`|\w+))\s*(?<settings>\[[^\]]*\])?\s*(?://.*)?$", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

    private static readonly Regex RelationshipBlockPattern = new(@"Ref\s*\{\s*(?<content>[^}]*)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex RelationshipLinePattern = new(@"^\s*(?<left>(?:\w+|\""[^\""]+\"")\.(?:\((?<leftcols>[^)]+)\)|(?<leftcol>\w+|\""[^\""]+\"")))\s*(?<relation>[<>-]|<>)\s*(?<right>(?:\w+|\""[^\""]+\"")\.(?:\((?<rightcols>[^)]+)\)|(?<rightcol>\w+|\""[^\""]+\"")))\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex RelationshipLongPattern = new(@"Ref\s*(?<name>\w+|\""[^""]+\"")?\s*\{\s*(?<left>(?:\[(?:[^\]]+)\]|(?:\w+\.)?(?:\w+|""[^""]+"")\.(?:\w+|""[^""]+"")))\s*(?<relation>[<>-]|<>)\s*(?<right>(?:\[(?:[^\]]+)\]|(?:\w+\.)?(?:\w+|""[^""]+"")\.(?:\w+|""[^""]+"")))\s*(?<settings>\[[^\]]*\])?\s*\}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex RelationshipShortPattern = new(@"Ref\s*(?<name>\w+|\""[^""]+\"")?:\s*(?<left>(?:\w+|\""[^""]+\"")\.\((?:\w+|\""[^""]+\"")(?:\s*,\s*(?:\w+|\""[^""]+\""))*\)|(?:\w+|\""[^""]+\"")\.(?:\w+|\""[^""]+\""))\s*(?<relation>[<>-]|<>)\s*(?<right>(?:\w+|\""[^""]+\"")\.\((?:\w+|\""[^""]+\"")(?:\s*,\s*(?:\w+|\""[^""]+\""))*\)|(?:\w+|\""[^""]+\"")\.(?:\w+|\""[^""]+\""))\s*(\[(?<settings>[^\]]*)\])?", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex InlineRefPattern = new(@"\s*(?<relation><>|<|>|-)\s*(?<target>(?:(?:\w+|""[^""]+"")\.){1,2}(?:\w+|""[^""]+""))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EnumPattern = new(@"enum\s+(?:(?<schema>\w+)\.)?(?<name>\w+|\""[^""]+\"")\s*\{\s*(?<content>(?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex EnumValuePattern = new(@"^\s*(?<value>\w+|\""[^""]+\"")(?:\s*(?<settings>\[[^\]]*\]))?\s*(?://.*)?$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex TableGroupPattern = new(@"TableGroup\s+(?<name>\w+|\""[^""]+\"")\s*(?<settings>\[[^\]]*\])?\s*\{\s*(?<content>(?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex TablePartialPattern = new(@"TablePartial\s+(?<name>\w+|\""[^""]+\"")\s*(?<settings>\[[^\]]*\])?\s*\{\s*(?<content>(?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex StickyNotePattern = new(@"Note\s+(?<name>\w+|\""[^""]+\"")\s*\{\s*(?<content>(?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex NotePattern = new(@"Note:\s*(?<content>'''(?:[^']|'(?!''))*'''|'[^']*')", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex SettingsPattern = new(@"(?<key>\w+):\s*(?<value>'''(?:[^']|'(?!''))*'''|`[^`]*`|'[^']*'|[^,\]]+)|(?<flag>\w+(?:\s+\w+)*)(?=\s*[,\]])", RegexOptions.Compiled);

    private static readonly Regex CommentPattern = new(@"//.*$|/\*[\s\S]*?\*/", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex PartialInjectionPattern = new(@"^\s*~(?<partial>\w+)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex WhitespacePattern = new(@"\s+");
    #endregion Regex Patterns

    public static DatabaseModel Parse(string dbmlContent)
    {
        var document = new DatabaseModel();

        // Remove comments first
        var cleanContent = CommentPattern.Replace(dbmlContent, "");

        // Parse project
        document.Project = ParseProject(cleanContent);

        // Parse table partials first (needed for injection)
        document.TablePartials = ParseTablePartials(cleanContent);

        // Parse tables
        document.Tables = ParseTables(cleanContent, document.TablePartials);

        // Parse enums
        document.Enums = ParseEnums(cleanContent);

        // Parse relationships
        document.Relationships = ParseRelationships(cleanContent);

        // Parse table groups
        document.TableGroups = ParseTableGroups(cleanContent);

        // Parse sticky notes
        document.StickyNotes = ParseStickyNotes(cleanContent);

        var aliasToTableName = document.Tables
            .Where(t => !string.IsNullOrEmpty(t.Alias))
            .ToDictionary(t => t.Alias!, t => t.Name!);

        document.Relationships.AddRange(document.Tables
            .SelectMany(t => t.Columns
                .Where(c => c.InlineRef != null)
                .Select(c =>
                {
                    var relationship = c.InlineRef!;
                    relationship.LeftTable = t.Name!;
                    relationship.LeftColumns.Add(c.Name!);
                    return relationship;
                })));

        // Resolve relationships using aliases
        foreach (var relationship in document.Relationships)
        {
            if (aliasToTableName.TryGetValue(relationship.LeftTable!, out var realLeftTable))
            {
                relationship.LeftTable = realLeftTable;
            }

            if (aliasToTableName.TryGetValue(relationship.RightTable!, out var realRightTable))
            {
                relationship.RightTable = realRightTable;
            }
        }

        return document;
    }

    private static ProjectModel? ParseProject(string content)
    {
        var match = ProjectPattern.Match(content);
        if (!match.Success) return null;

        var project = new ProjectModel
        {
            Name = CleanQuotes(match.Groups["name"].Value)
        };

        var projectContent = match.Groups["content"].Value;

        // Parse settings and notes
        ParseSettings(projectContent, project.Settings);
        project.Note = ParseNote(projectContent);

        return project;
    }

    private static List<TableModel> ParseTables(string content, List<TablePartialModel> partials)
    {
        var tables = new List<TableModel>();
        var matches = SchemaTablePattern.Matches(content);

        foreach (Match match in matches)
        {
            var table = new TableModel
            {
                Schema = string.IsNullOrEmpty(match.Groups["schema"].Value) ? "public" : match.Groups["schema"].Value,
                Name = CleanQuotes(match.Groups["table"].Value),
                Alias = CleanQuotes(match.Groups["alias"].Value)
            };

            // Parse table settings
            if (match.Groups["settings"].Success)
            {
                ParseSettings(match.Groups["settings"].Value, table.Settings);
            }

            var tableContent = match.Groups["content"].Value;

            // Parse note
            table.Note = ParseNote(tableContent);

            // Parse columns and partial injections
            ParseTableContent(tableContent, table, partials);

            // Parse indexes
            table.Indexes = ParseIndexes(tableContent);

            tables.Add(table);
        }

        return tables;
    }

    private static void ParseTableContent(string content, TableModel table, List<TablePartialModel> partials)
    {
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//") ||
                trimmedLine.StartsWith("Note") || trimmedLine.StartsWith("indexes"))
                continue;

            // Check for partial injection
            var partialMatch = PartialInjectionPattern.Match(trimmedLine);
            if (partialMatch.Success)
            {
                var partialName = partialMatch.Groups["partial"].Value;
                table.InjectedPartials.Add(partialName);

                // Inject partial content
                var partial = partials.FirstOrDefault(p => p.Name == partialName);
                if (partial != null)
                {
                    // Merge settings
                    foreach (var setting in partial.Settings.Where(s => table.Settings.ContainsKey(s.Key)))
                    {
                        table.Settings[setting.Key] = setting.Value;
                    }

                    // Add columns
                    table.Columns.AddRange(partial.Columns);

                    // Add indexes
                    table.Indexes.AddRange(partial.Indexes);
                }
                continue;
            }

            // Parse column
            var columnMatch = ColumnPattern.Match(trimmedLine);
            if (columnMatch.Success)
            {
                var column = new ColumnModel
                {
                    Name = CleanQuotes(columnMatch.Groups["name"].Value),
                    Type = columnMatch.Groups["type"].Value.Trim()
                };

                var settingsText = columnMatch.Groups["settings"].Value;
                foreach (Match settingMatch in Regex.Matches(settingsText, @"\[[^\[\]]*\]"))
                {
                    ParseColumnSettings(settingMatch.Value, column); // or whatever target model you use
                }

                table.Columns.Add(column);
            }
        }
    }

    private static List<IndexModel> ParseIndexes(string content)
    {
        var indexes = new List<IndexModel>();
        var indexMatch = IndexPattern.Match(content);

        if (!indexMatch.Success) return indexes;

        var indexContent = indexMatch.Groups["content"].Value;
        var lines = indexContent.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                continue;

            var itemMatch = IndexItemPattern.Match(trimmedLine);
            if (itemMatch.Success)
            {
                var index = new IndexModel();

                if (itemMatch.Groups["columns"].Success)
                {
                    // Multiple columns
                    var columns = itemMatch.Groups["columns"].Value.Split(',');
                    index.Columns.AddRange(columns.Select(c => c.Trim()));
                }
                else if (itemMatch.Groups["column"].Success)
                {
                    // Single column
                    var column = itemMatch.Groups["column"].Value.Trim();
                    index.Columns.Add(column);
                    index.IsExpression = column.StartsWith("`") && column.EndsWith("`");
                }

                if (itemMatch.Groups["settings"].Success)
                {
                    ParseSettings(itemMatch.Groups["settings"].Value, index.Settings, index);
                }

                indexes.Add(index);
            }
        }

        return indexes;
    }

    private static List<RelationshipModel> ParseRelationships(string content)
    {
        var relationships = new List<RelationshipModel>();

        // 1) Parse Ref blocks with multiple composite FKs
        foreach (Match blockMatch in RelationshipBlockPattern.Matches(content))
        {
            var blockContent = blockMatch.Groups["content"].Value;

            var lines = blockContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var lineMatch = RelationshipLinePattern.Match(line.Trim());
                if (lineMatch.Success)
                {
                    var relationship = new RelationshipModel
                    {
                        LeftTable = CleanQuotes(lineMatch.Groups["left"].Value.Split('.')[0]),
                        RightTable = CleanQuotes(lineMatch.Groups["right"].Value.Split('.')[0])
                    };

                    if (lineMatch.Groups["leftcols"].Success)
                    {
                        var leftCols = lineMatch.Groups["leftcols"].Value.Split([','], StringSplitOptions.RemoveEmptyEntries);
                        relationship.LeftColumns = [.. leftCols.Select(c => CleanQuotes(c.Trim()))];
                    }
                    else if (lineMatch.Groups["leftcol"].Success)
                    {
                        relationship.LeftColumns = [CleanQuotes(lineMatch.Groups["leftcol"].Value)];
                    }

                    if (lineMatch.Groups["rightcols"].Success)
                    {
                        var rightCols = lineMatch.Groups["rightcols"].Value.Split([','], StringSplitOptions.RemoveEmptyEntries);
                        relationship.RightColumns = [.. rightCols.Select(c => CleanQuotes(c.Trim()))];
                    }
                    else if (lineMatch.Groups["rightcol"].Success)
                    {
                        relationship.RightColumns = [CleanQuotes(lineMatch.Groups["rightcol"].Value)];
                    }

                    relationship.RelationshipType = ParseRelationshipType(lineMatch.Groups["relation"].Value);

                    relationships.Add(relationship);
                }
            }
        }

        // 2. Parse existing long form relationships (single line)
        var longMatches = RelationshipLongPattern.Matches(content);
        foreach (Match match in longMatches)
        {
            relationships.Add(ParseRelationshipMatch(match));
        }

        // 3. Parse short form relationships
        var shortMatches = RelationshipShortPattern.Matches(content);
        foreach (Match match in shortMatches)
        {
            relationships.Add(ParseRelationshipMatch(match));
        }

        return relationships;
    }

    private static RelationshipType ParseRelationshipType(string symbol)
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

    private static RelationshipModel ParseRelationshipMatch(Match match, string? blockName = null)
    {
        var relationship = new RelationshipModel
        {
            Name = string.IsNullOrEmpty(match.Groups["name"].Value) ? blockName : CleanQuotes(match.Groups["name"].Value),
            RelationshipType = ParseRelationshipType(match.Groups["relation"].Value)
        };

        var left = match.Groups["left"].Value;
        var right = match.Groups["right"].Value;

        var leftParts = left.Split('.');
        relationship.LeftTable = CleanQuotes(leftParts[0]);
        relationship.LeftColumns = ExtractColumns(left);

        var rightParts = right.Split('.');
        relationship.RightTable = CleanQuotes(rightParts[0]);
        relationship.RightColumns = ExtractColumns(right);

        if (match.Groups["settings"].Success)
        {
            ParseSettings(match.Groups["settings"].Value, relationship.Settings);
        }

        return relationship;
    }

    private static List<string> ExtractColumns(string side)
    {
        var firstParen = side.IndexOf('(');
        if (firstParen >= 0)
        {
            // composite columns
            var colsPart = side.Substring(firstParen + 1, side.LastIndexOf(')') - firstParen - 1);
            return [.. colsPart.Split(',').Select(c => CleanQuotes(c.Trim()))];
        }
        else
        {
            // single column
            var parts = side.Split('.');
            return [CleanQuotes(parts[parts.Count() - 1])];
        }
    }

    private static List<EnumModel> ParseEnums(string content)
    {
        var enums = new List<EnumModel>();
        var matches = EnumPattern.Matches(content);

        foreach (Match match in matches)
        {
            var dbmlEnum = new EnumModel
            {
                Schema = string.IsNullOrEmpty(match.Groups["schema"].Value) ? "public" : match.Groups["schema"].Value,
                Name = CleanQuotes(match.Groups["name"].Value)
            };

            var enumContent = match.Groups["content"].Value;
            var lines = enumContent.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;

                var valueMatch = EnumValuePattern.Match(trimmedLine);
                if (valueMatch.Success)
                {
                    var enumValue = new EnumValueModel
                    {
                        Value = CleanQuotes(valueMatch.Groups["value"].Value)
                    };

                    if (valueMatch.Groups["settings"].Success)
                    {
                        ParseSettings(valueMatch.Groups["settings"].Value, enumValue.Settings);
                        enumValue.Note = enumValue.Settings.TryGetValue("note", out string? value) ? value : null;
                    }

                    dbmlEnum.Values.Add(enumValue);
                }
            }

            enums.Add(dbmlEnum);
        }

        return enums;
    }

    private static List<TableGroupModel> ParseTableGroups(string content)
    {
        var groups = new List<TableGroupModel>();
        var matches = TableGroupPattern.Matches(content);

        foreach (Match match in matches)
        {
            var group = new TableGroupModel
            {
                Name = CleanQuotes(match.Groups["name"].Value)
            };

            if (match.Groups["settings"].Success)
            {
                ParseSettings(match.Groups["settings"].Value, group.Settings);
            }

            var groupContent = match.Groups["content"].Value;
            group.Note = ParseNote(groupContent);

            var lines = groupContent.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("//") &&
                    !trimmedLine.StartsWith("Note") && !trimmedLine.Contains(':'))
                {
                    group.Tables.Add(trimmedLine);
                }
            }

            groups.Add(group);
        }

        return groups;
    }

    private static List<TablePartialModel> ParseTablePartials(string content)
    {
        var partials = new List<TablePartialModel>();
        var matches = TablePartialPattern.Matches(content);

        foreach (Match match in matches)
        {
            var partial = new TablePartialModel
            {
                Name = CleanQuotes(match.Groups["name"].Value)
            };

            if (match.Groups["settings"].Success)
            {
                ParseSettings(match.Groups["settings"].Value, partial.Settings);
            }

            var partialContent = match.Groups["content"].Value;

            // Parse columns (similar to table parsing)
            var lines = partialContent.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//") ||
                    trimmedLine.StartsWith("indexes"))
                    continue;

                var columnMatch = ColumnPattern.Match(trimmedLine);
                if (columnMatch.Success)
                {
                    var column = new ColumnModel
                    {
                        Name = CleanQuotes(columnMatch.Groups["name"].Value),
                        Type = columnMatch.Groups["type"].Value.Trim()
                    };

                    var settingsText = columnMatch.Groups["settings"].Value;
                    foreach (Match settingMatch in Regex.Matches(settingsText, @"\[[^\[\]]*\]"))
                    {
                        ParseColumnSettings(settingMatch.Value, column); // or whatever target model you use
                    }

                    partial.Columns.Add(column);
                }
            }

            // Parse indexes
            partial.Indexes = ParseIndexes(partialContent);

            partials.Add(partial);
        }

        return partials;
    }

    private static List<StickyNoteModel> ParseStickyNotes(string content)
    {
        var notes = new List<StickyNoteModel>();
        var matches = StickyNotePattern.Matches(content);

        foreach (Match match in matches)
        {
            var note = new StickyNoteModel
            {
                Name = CleanQuotes(match.Groups["name"].Value),
                Content = CleanStringValue(match.Groups["content"].Value)
            };

            notes.Add(note);
        }

        return notes;
    }

    private static void ParseColumnSettings(string settingsText, ColumnModel column)
    {
        var matches = SettingsPattern.Matches(settingsText);

        foreach (Match match in matches)
        {
            if (match.Groups["key"].Success && match.Groups["value"].Success)
            {
                var key = match.Groups["key"].Value;
                var value = CleanStringValue(match.Groups["value"].Value);
                column.Settings[key] = value;

                if (key == "note")
                {
                    column.Note = value;
                }
                else if (key == "default")
                {
                    column.DefaultValue = value;
                }
                else if (key == "ref")
                {
                    // Parse inline reference
                    var refMatch = InlineRefPattern.Match(value);
                    if (refMatch.Success)
                    {
                        column.InlineRef = new RelationshipModel
                        {
                            RelationshipType = ParseRelationshipType(refMatch.Groups["relation"].Value),
                        };

                        var targetParts = refMatch.Groups["target"].Value.Split('.');
                        if (targetParts.Length >= 2)
                        {
                            column.InlineRef.RightTable = CleanQuotes(targetParts[targetParts.Length - 2]);
                            column.InlineRef.RightColumns = [CleanQuotes(targetParts[targetParts.Length - 1])];
                        }
                    }
                }
            }
            else if (match.Groups["flag"].Success)
            {
                var flag = WhitespacePattern.Replace(match.Groups["flag"].Value.ToLower().Trim(), " ");
                switch (flag)
                {
                    case "pk":
                    case "primary key":
                        column.IsPrimaryKey = true;
                        column.IsNotNull = true; // Primary keys are implicitly not null
                        column.IsNull = false;
                        break;
                    case "null":
                        column.IsNull = true;
                        column.IsNotNull = false;
                        break;
                    case "not null":
                        column.IsNotNull = true;
                        column.IsNull = false;
                        break;
                    case "unique":
                        column.IsUnique = true;
                        break;
                    case "increment":
                        column.IsIncrement = true;
                        break;
                }
            }
        }
    }

    private static void ParseSettings(string settingsText, Dictionary<string, string> settings, IndexModel? index = null)
    {
        if (string.IsNullOrEmpty(settingsText)) return;

        var matches = SettingsPattern.Matches(settingsText);

        foreach (Match match in matches)
        {
            if (match.Groups["key"].Success && match.Groups["value"].Success)
            {
                var key = match.Groups["key"].Value;
                var value = CleanStringValue(match.Groups["value"].Value);
                settings[key] = value;
            }
            else if (match.Groups["flag"].Success && index != null)
            {
                var flag = WhitespacePattern.Replace(match.Groups["flag"].Value.ToLower().Trim(), " ");
                switch (flag)
                {
                    case "pk":
                    case "primary key":
                        index.IsPrimaryKey = true;
                        break;
                    case "unique":
                        index.IsUnique = true;
                        break;
                }
            }
        }
    }

    private static string? ParseNote(string content)
    {
        var match = NotePattern.Match(content);
        return match.Success ? CleanStringValue(match.Groups["content"].Value) : null;
    }

    private static string CleanQuotes(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        value = value.Trim();

        // Remove surrounding quotes if they match
        if (value.StartsWith("\"") && value.EndsWith("\"") ||
            value.StartsWith("\'") && value.EndsWith("\'") ||
            value.StartsWith("`") && value.EndsWith("`"))
        {
            return value.Substring(1, value.Length - 2);
        }
        return value;
    }

    private static string CleanStringValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        value = value.Trim();

        // Handle triple quotes (multiline strings)
        if (value.StartsWith("'''") && value.EndsWith("'''"))
        {
            return value.Substring(3, value.Length - 6).Trim();
        }

        return CleanQuotes(value);
    }
}
