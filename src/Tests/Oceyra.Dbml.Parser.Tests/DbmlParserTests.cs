using Shouldly;
using Xunit;

namespace Oceyra.Dbml.Parser.Tests;

public class DbmlParserTests
{
    [Fact]
    public void DbmlParser_WithBasicCase_ReturnBasicModel()
    {
        var dbmlContent = @"
            Project project_name {
              database_type: 'PostgreSQL'
              Note: 'Description of the project'
            }

            Table users {
              id integer
              username varchar
              role varchar
              created_at timestamp
            }

            Table posts {
              id integer [primary key]
              title varchar
              body text [note: 'Content of the post']
              user_id integer
              created_at timestamp
            }

            Ref: posts.user_id > users.id // many-to-one";

        DbmlParser parser = new();
        var model = parser.Parse(dbmlContent);

        model.ShouldNotBeNull();
        model.Name.ShouldBe("project_name");
        model.Tables.Count.ShouldBe(2);
        model.Tables[0].Name.ShouldBe("users");
        model.Tables[0].Columns.Count.ShouldBe(4);
        model.Tables[0].Columns[0].IsPrimaryKey.ShouldBe(false);

        model.Tables[1].Name.ShouldBe("posts");
        model.Tables[1].Columns.Count.ShouldBe(5);
        model.Tables[1].Columns[0].IsPrimaryKey.ShouldBe(true);

        model.Relationships[0].RelationshipType.ShouldBe(Models.RelationshipType.ManyToOne);
    }

    [Fact]
    public void DbmlParser_WithInvertedBasicCase_ReturnBasicModel()
    {
        var dbmlContent = @"
            Project project_name {
              database_type: 'PostgreSQL'
              Note: 'Description of the project'
            }

            Table users as U {
              id integer
              username varchar
              role varchar
              created_at timestamp
            }

            Table posts {
              id integer [primary key]
              title varchar
              body text [note: 'Content of the post']
              user_id integer
              created_at timestamp
            }

            Ref: U.id < posts.user_id // one-to-many";

        DbmlParser parser = new();
        var model = parser.Parse(dbmlContent);

        model.ShouldNotBeNull();
        model.Name.ShouldBe("project_name");
        model.Tables.Count.ShouldBe(2);
        model.Tables[0].Name.ShouldBe("users");
        model.Tables[0].Columns.Count.ShouldBe(4);
        model.Tables[0].Columns[0].IsPrimaryKey.ShouldBe(false);

        model.Tables[1].Name.ShouldBe("posts");
        model.Tables[1].Columns.Count.ShouldBe(5);
        model.Tables[1].Columns[0].IsPrimaryKey.ShouldBe(true);

        model.Relationships[0].FromTable.ShouldBe("users");
        model.Relationships[0].ToTable.ShouldBe("posts");
        model.Relationships[0].RelationshipType.ShouldBe(Models.RelationshipType.OneToMany);
    }


    [Fact]
    public void DbmlParser_WithSpecifiedDefaultValue_ReturnDefaultValue()
    {
        var dbmlContent = @"
            Table users {
              id integer [primary key]
              username varchar(255) [not null, unique]
              full_name varchar(255) [not null]
              gender varchar(1) [not null]
              source varchar(255) [default: 'direct']
              created_at timestamp [default: `now()`]
              rating integer [default: 10]
            }";

        DbmlParser parser = new();
        var model = parser.Parse(dbmlContent);

        model.ShouldNotBeNull();
        model.Name.ShouldBeEmpty();
        model.Tables.Count.ShouldBe(1);
        model.Tables[0].Name.ShouldBe("users");
        model.Tables[0].Columns.Count.ShouldBe(7);
        model.Tables[0].Columns[4].DefaultValue.ShouldBe("'direct'");
    }

    [Fact]
    public void DbmlParser_WithSpecifiedDefaultValues_ReturnDefaultValue()
    {
        var dbmlContent = @"
            Table bookings {
              id integer
              country varchar
              booking_date date
              created_at timestamp

              indexes {
                (id, country) [pk] // composite primary key
                created_at [name: 'created_at_index', note: 'Date']
                booking_date
                (country, booking_date) [unique]
                booking_date [type: hash]
                (`id*2`)
                (`id*3`,`getdate()`)
                (`id*3`,id)
              }
            }";

        DbmlParser parser = new();
        var model = parser.Parse(dbmlContent);

        model.ShouldNotBeNull();
        model.Name.ShouldBeEmpty();
        model.Tables.Count.ShouldBe(1);
        model.Tables[0].Name.ShouldBe("bookings");
        model.Tables[0].Indexes.Count.ShouldBe(8);
        model.Tables[0].Columns[4].DefaultValue.ShouldBe("'direct'");
    }


}
