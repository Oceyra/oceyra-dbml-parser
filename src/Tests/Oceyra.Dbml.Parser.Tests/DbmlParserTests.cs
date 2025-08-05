using System;
using System.Xml;
using Oceyra.Dbml.Parser;
using Oceyra.Dbml.Parser.Models;
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

            Ref ""foreignkey_name_fk"": posts.user_id > users.id // many-to-one";

        var model = DbmlParser.Parse(dbmlContent);

        model.ShouldNotBeNull();
        model.Project.ShouldNotBeNull();
        model.Project.Name.ShouldBe("project_name");
        model.Tables.Count.ShouldBe(2);
        model.Tables[0].Name.ShouldBe("users");
        model.Tables[0].Columns.Count.ShouldBe(4);
        model.Tables[0].Columns[0].IsPrimaryKey.ShouldBe(false);

        model.Tables[1].Name.ShouldBe("posts");
        model.Tables[1].Columns.Count.ShouldBe(5);
        model.Tables[1].Columns[0].IsPrimaryKey.ShouldBe(true);

        model.Relationships[0].RelationshipType.ShouldBe(RelationshipType.ManyToOne);
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

        var model = DbmlParser.Parse(dbmlContent);

        model.ShouldNotBeNull();
        model.Project.ShouldNotBeNull();
        model.Project.Name.ShouldBe("project_name");
        model.Tables.Count.ShouldBe(2);
        model.Tables[0].Name.ShouldBe("users");
        model.Tables[0].Columns.Count.ShouldBe(4);
        model.Tables[0].Columns[0].IsPrimaryKey.ShouldBe(false);

        model.Tables[1].Name.ShouldBe("posts");
        model.Tables[1].Columns.Count.ShouldBe(5);
        model.Tables[1].Columns[0].IsPrimaryKey.ShouldBe(true);

        model.Relationships[0].LeftTable.ShouldBe("users");
        model.Relationships[0].RightTable.ShouldBe("posts");
        model.Relationships[0].RelationshipType.ShouldBe(RelationshipType.OneToMany);
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

        var model = DbmlParser.Parse(dbmlContent);

        model.ShouldNotBeNull();
        model.Project.ShouldBeNull();
        model.Tables.Count.ShouldBe(1);
        model.Tables[0].Name.ShouldBe("users");
        model.Tables[0].Columns.Count.ShouldBe(7);
        model.Tables[0].Columns[4].DefaultValue.ShouldBe("direct");
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

        var model = DbmlParser.Parse(dbmlContent);

        model.ShouldNotBeNull();
        model.Project.ShouldBeNull();
        model.Tables.Count.ShouldBe(1);
        model.Tables[0].Name.ShouldBe("bookings");
        model.Tables[0].Indexes.Count.ShouldBe(8);
    }

    [Fact]
    public void DbmlParser_WithTableGroup_ReturnTableGroupModel()
    {
        var dbmlContent = @"
            TableGroup user_management {
              users
              orders
            }";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Project.ShouldBeNull();
        model.TableGroups.Count.ShouldBe(1);
        model.TableGroups[0].Name.ShouldBe("user_management");
        model.TableGroups[0].Tables.Count.ShouldBe(2);
        model.TableGroups[0].Tables[0].ShouldBe("users");
        model.TableGroups[0].Tables[1].ShouldBe("orders");
    }

    [Fact]
    public void DbmlParser_WithTablePartial_ReturnTablePartialModel()
    {
        var dbmlContent = @"
            TablePartial user_partial {
              Note: 'Partial for user table'
              Columns {
                id integer [primary key]
                username varchar(50) [not null, unique]
              }
              Indexes {
                username [unique]
              }
            }";
        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Project.ShouldBeNull();
        model.TablePartials.Count.ShouldBe(1);
        model.TablePartials[0].Name.ShouldBe("user_partial");
        model.TablePartials[0].Columns.Count.ShouldBe(2);
        model.TablePartials[0].Indexes.Count.ShouldBe(1);
    }

    [Fact]
    public void DbmlParser_WithComplexDbml_ReturnComplexModel()
    {
        var dbmlContent = @"
            Project ecommerce_db {
              database_type: 'PostgreSQL'
              Note: 'E-commerce database schema'
            }
            enum order_status {
              created [note: 'Order created']
              processing
              shipped
              delivered
              cancelled
            }
            Table users as U {
              id integer [primary key, increment]
              username varchar(50) [not null, unique]
              email varchar(100) [not null, unique]
              created_at timestamp [default: `now()`]
              Note: 'User accounts table'
            }
            Table orders {
              id integer [pk, increment]
              user_id integer [ref: > U.id, not null]
              status order_status [default: 'created']
              total decimal(10,2) [not null]
              created_at timestamp [default: `now()`]
              indexes {
                user_id
                (user_id, created_at) [name: 'user_orders_idx']
              }
            }
            Ref: orders.user_id > users.id [delete: cascade]
            TableGroup user_management {
              users
              orders
            }";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Project.ShouldNotBeNull();
        model.Project.Name.ShouldBe("ecommerce_db");
        model.Tables.Count.ShouldBe(2);
        model.Enums.Count.ShouldBe(1);
        model.Relationships.Count.ShouldBe(2);
        model.TableGroups.Count.ShouldBe(1);
    }

    [Fact]
    public void DbmlParser_WithQuotedDbDbml_ReturnFieldWithoutQuote()
    {
        var dbmlContent = @"
Table ""projects"" {
  ""project_id"" varchar(500) [pk, not null] [ref: < ""task_results"".""project_id""]
  ""project_name"" varchar(500) [not null]
  ""requirements"" varchar(500)
  ""task_plan"" varchar(500)
  ""status"" varchar(500) [not null]
  ""created_at"" timestamp [not null]
  ""updated_at"" timestamp
}

Table ""task_results"" {
  ""project_id"" varchar(500) [not null]
  ""task_id"" varchar(500) [not null] [ref: < ""task_dependencies"".""dependency_task_id""]
  ""name"" varchar(500) [not null]
  ""description"" varchar(500)
  ""agent_type"" varchar(500) [not null]
  ""priority"" varchar(500)
  ""estimated_hours"" bigint
  ""result_data"" varchar(500)
  ""status"" varchar(500) [not null]
  ""created_at"" timestamp [not null]
  ""updated_at"" timestamp

  Indexes {
    (task_id, agent_type, project_id) [unique, name: ""public_idx_task_results_task_id_agent_type""]
  }
}

Table ""task_dependencies"" {
  ""id"" bigint [pk, not null]
  ""project_id"" varchar(500) [not null]
  ""task_id"" varchar(500) [not null]
  ""dependency_task_id"" varchar(500) [not null]
  ""created_at"" timestamp
  ""updated_at"" timestamp

  Indexes {
    (task_id, dependency_task_id, project_id) [unique, name: ""public_index_1""]
  }
}
";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Project.ShouldBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Tables[2].Name.ShouldBe("task_dependencies");
        model.Tables[1].Indexes.Count.ShouldBe(1);
        model.Enums.Count.ShouldBe(0);
        model.Relationships.Count.ShouldBe(2);
        model.Relationships[0].LeftTable.ShouldBe("projects");
        model.Relationships[0].RightTable.ShouldBe("task_results");
        model.TableGroups.Count.ShouldBe(0);
    }

    [Fact]
    public void DbmlParser_WithComplexChartDbDbml_ReturnComplexModel()
    {

        var dbmlContent = @"Project DBML {
  Note: '''
  # DBML - Database Markup Language
  DBML (database markup language) is a simple, readable DSL language designed to define database structures.

  ## Benefits

  * It is simple, flexible and highly human-readable
  * It is database agnostic, focusing on the essential database structure definition without worrying about the detailed syntaxes of each database
  * Comes with a free, simple database visualiser at [dbdiagram.io](http://dbdiagram.io)
  '''
}

Table ""projects"" {
  ""project_id"" varchar(500) [pk, not null] [ref: < ""task_results"".""project_id""]
  ""project_name"" varchar(500) [not null]
  ""requirements"" varchar(500)
  ""task_plan"" varchar(500)
  ""status"" varchar(500) [not null]
  ""created_at"" timestamp [not null]
  ""updated_at"" timestamp
}

Table ""task_results"" {
  ""project_id"" varchar(500) [not null]
  ""task_id"" varchar(500) [not null] [ref: < ""task_dependencies"".""dependency_task_id""]
  ""name"" varchar(500) [not null]
  ""description"" varchar(500)
  ""agent_type"" varchar(500) [not null]
  ""priority"" varchar(500)
  ""estimated_hours"" bigint
  ""result_data"" varchar(500)
  ""status"" varchar(500) [not null]
  ""created_at"" timestamp [not null]
  ""updated_at"" timestamp

  Indexes {
    (task_id, agent_type, project_id) [unique, name: ""public_idx_task_results_task_id_agent_type""]
  }
}

Table ""task_dependencies"" {
  ""id"" bigint [pk, not null]
  ""project_id"" varchar(500) [not null]
  ""task_id"" varchar(500) [not null]
  ""dependency_task_id"" varchar(500) [not null]
  ""created_at"" timestamp
  ""updated_at"" timestamp

  Indexes {
    (task_id, dependency_task_id, project_id) [unique, name: ""public_index_1""]
  }
}";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Project.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Tables[0].Indexes.Count.ShouldBe(0);
        model.Tables[1].Indexes.Count.ShouldBe(1);
        model.Tables[2].Indexes.Count.ShouldBe(1);
        model.Relationships.Count.ShouldBe(2);

        // Helper local function to resolve table name from alias or return as-is
        static void TestQuotesCleaned(string? testString)
        {
            testString.ShouldNotBeNull();
            testString.ShouldNotContain("\'");
            testString.ShouldNotContain("\"");
            testString.ShouldNotContain("`");
        }

        TestQuotesCleaned(model.Project.Note);

        foreach (var table in model.Tables)
        {
            TestQuotesCleaned(table.Name);

            foreach (var column in table.Columns)
            {
                TestQuotesCleaned(column.Name);
            }

            foreach (var index in table.Indexes)
            {
                if (index.Settings.TryGetValue("name", out var name))
                {
                    TestQuotesCleaned(name);
                }
            }
        }

        foreach (var relationship in model.Relationships)
        {
            TestQuotesCleaned(relationship.LeftTable);
            TestQuotesCleaned(relationship.RightTable);

            foreach (var column in relationship.LeftColumns)
            {
                TestQuotesCleaned(column);
            }

            foreach (var column in relationship.RightColumns)
            {
                TestQuotesCleaned(column);
            }
        }
    }

    [Fact]
    public void DbmlParser_WithSimpleCompositeForeignKey_ReturnsTwoRelationships()
    {
        var dbmlContent = @"Table orders {
  order_id integer [pk]
  customer_id integer
  store_id integer

  indexes {
    (order_id, customer_id)
  }
}

Table customers {
  customer_id integer [pk]
  store_id integer [pk]
  name varchar
}

Ref: orders.(customer_id, store_id) > customers.(customer_id, store_id)";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(2);
        model.Relationships.Count.ShouldBe(1);
        model.Relationships[0].LeftColumns.Count.ShouldBe(2);
    }

    [Fact]
    public void DbmlParser_WithCompositeRefInBlock_ReturnsTwoRelationships()
    {
        var dbmlContent = @"Table invoices {
  invoice_id integer [pk]
  client_id integer
  region_id integer
}

Table clients {
  client_id integer [pk]
  region_id integer [pk]
  name varchar
}

Ref {
  invoices.client_id > clients.client_id
  invoices.region_id > clients.region_id
}";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(2);
        model.Relationships.Count.ShouldBe(2);
    }

    [Fact]
    public void DbmlParser_WithMultiColumnIndexAndCompositeKey_ReturnsExpectedIndexes()
    {
        var dbmlContent = @"Table product_orders {
  order_id int
  product_id int
  quantity int

  Indexes {
    (order_id, product_id) [unique]
  }
}

Table products {
  product_id int [pk]
  name varchar
}

Table orders {
  order_id int [pk]
  date timestamp
}";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Tables[0].Indexes.Count.ShouldBe(1);
        model.Relationships.Count.ShouldBe(0);
    }

    [Fact]
    public void DbmlParser_WithCompositeForeignKeysAcrossMultipleTables_ReturnsExpectedRelationships()
    {
        var dbmlContent = @"Table enrollments {
  student_id int
  course_id int
  semester varchar
}

Table students {
  student_id int [pk]
  name varchar
}

Table courses {
  course_id int [pk]
  title varchar
}

Ref: enrollments.student_id > students.student_id
Ref: enrollments.course_id > courses.course_id";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Relationships.Count.ShouldBe(2);
    }

    [Fact]
    public void DbmlParser_WithNestedCompositeKeys_ReturnsCorrectSchema()
    {
        var dbmlContent = @"Table schedule {
  class_id int
  room_id int
  time_slot varchar
}

Table classes {
  class_id int [pk]
  subject varchar
}

Table rooms {
  room_id int [pk]
  location varchar
}

Ref {
  schedule.class_id > classes.class_id
  schedule.room_id > rooms.room_id
}";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Relationships.Count.ShouldBe(2);
        model.Relationships.Count.ShouldBe(2);
        model.Relationships.Count.ShouldBe(2);
    }

    [Fact]
    public void DbmlParser_WithSimpleCompositeForeignKey_ReturnsOneRelationship()
    {
        var dbmlContent = @"
Table orders {
  order_id integer [pk]
  customer_id integer
  store_id integer

  indexes {
    (order_id, customer_id)
  }
}

Table customers {
  customer_id integer [pk]
  store_id integer [pk]
  name varchar
}

Ref: orders.(customer_id, store_id) > customers.(customer_id, store_id)
";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(2);
        model.Relationships.Count.ShouldBe(1);

        var rel = model.Relationships[0];
        rel.LeftTable.ShouldBe("orders");
        rel.RightTable.ShouldBe("customers");
        rel.LeftColumns.ShouldBe(["customer_id", "store_id"]);
        rel.RightColumns.ShouldBe(["customer_id", "store_id"]);
    }

    [Fact]
    public void DbmlParser_WithCompositeRefInBlock_ReturnsOneRelationship()
    {
        var dbmlContent = @"
Table invoices {
  invoice_id integer [pk]
  client_id integer
  region_id integer
}

Table clients {
  client_id integer [pk]
  region_id integer [pk]
  name varchar
}

Ref {
  invoices.(client_id, region_id) > clients.(client_id, region_id)
}
";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(2);
        model.Relationships.Count.ShouldBe(1);

        var rel = model.Relationships[0];
        rel.LeftTable.ShouldBe("invoices");
        rel.RightTable.ShouldBe("clients");
        rel.LeftColumns.ShouldBe(["client_id", "region_id"]);
        rel.RightColumns.ShouldBe(["client_id", "region_id"]);
    }

    [Fact]
    public void DbmlParser_WithMultiColumnIndexAndCompositeKey2_ReturnsExpectedIndexes()
    {
        var dbmlContent = @"
Table product_orders {
  order_id int
  product_id int
  quantity int

  Indexes {
    (order_id, product_id) [unique]
  }
}

Table products {
  product_id int [pk]
  name varchar
}

Table orders {
  order_id int [pk]
  date timestamp
}
";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Tables[0].Indexes.Count.ShouldBe(1);
        model.Relationships.Count.ShouldBe(0);
    }

    [Fact]
    public void DbmlParser_WithCompositeForeignKeysAcrossMultipleTables_ReturnsTwoRelationships()
    {
        var dbmlContent = @"
Table enrollments {
  student_id int
  course_id int
  semester varchar
}

Table students {
  student_id int [pk]
  name varchar
}

Table courses {
  course_id int [pk]
  title varchar
}

Ref: enrollments.student_id > students.student_id
Ref: enrollments.course_id > courses.course_id
";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Relationships.Count.ShouldBe(2);

        // This test keeps multiple separate foreign keys because it's testing multiple relationships on different columns.
        // No change needed here unless you want to test composite FK combining student_id and course_id.
    }

    [Fact]
    public void DbmlParser_WithNestedCompositeKeys2_ReturnsCorrectSchema()
    {
        var dbmlContent = @"
Table schedule {
  class_id int
  room_id int
  time_slot varchar
}

Table classes {
  class_id int [pk]
  subject varchar
}

Table rooms {
  room_id int [pk]
  location varchar
}

Ref {
  schedule.(class_id, room_id) > classes.(class_id, room_id)
}
";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Relationships.Count.ShouldBe(1);

        var rel = model.Relationships[0];
        rel.LeftTable.ShouldBe("schedule");
        rel.RightTable.ShouldBe("classes");
        rel.LeftColumns.ShouldBe(["class_id", "room_id"]);
        rel.RightColumns.ShouldBe(["class_id", "room_id"]);
    }


    [Fact]
    public void DbmlParser_WithRelationship_ReturnsCorrectRelationship()
    {
        var dbmlContent = @"
            Table ""nuget_packages"" {
              ""id"" int[pk, not null]
              ""package_id"" varchar(500) [unique, not null]
              ""package_version"" varchar(500) [not null]
            }

            Table ""datasource_providers"" {
              ""id"" int[pk, not null]
              ""name"" varchar(500)
              ""nuget_package_id"" int[not null]
              ""use_method"" varchar(500) [not null]
            }

            Table ""datasources"" {
              ""id"" int [pk, not null]
            ""name"" int[unique, not null]
              ""datasource_provider_id"" int [not null]
            ""connection"" varchar(500)
              ""username"" varchar(500)
              ""password"" varchar(500)

              Indexes {
                name [unique, name: ""idx_name""]
              }
            }

            Ref ""fk_0_datasource_providers_nuget_package_id_fk"":""nuget_packages"".""id"" < ""datasource_providers"".""nuget_package_id""

            Ref ""fk_1_datasources_datasource_provider_id_fk"":""datasource_providers"".""id"" < ""datasources"".""datasource_provider_id""
            ";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Relationships.Count.ShouldBe(2);

        var rel = model.Relationships[0];
        rel.LeftTable.ShouldBe("nuget_packages");
        rel.RightTable.ShouldBe("datasource_providers");
        rel.LeftColumns.ShouldBe(["id"]);
        rel.RightColumns.ShouldBe(["nuget_package_id"]);
    }

    [Fact]
    public void DbmlParser_WithInlineRelationship_ReturnsCorrectRelationship()
    {
        var dbmlContent = @"
            Table ""public"".""nuget_packages"" {
                ""id"" integer [pk, not null]
                ""package_id"" varchar(500)[unique, not null]
                ""package_version"" varchar(500)[not null]
                ""created_at"" timestamp[not null]
                ""created_by"" varchar(500)[not null]
                ""updated_at"" timestamp[not null]
                ""updated_by"" varchar(500)[not null]
            }

            Table ""public"".""datasource_providers"" {
                ""id"" integer[pk, not null]
                ""name"" varchar(500)
                ""description"" varchar(500)
                ""nuget_package_id"" integer[ref: < ""public"".""nuget_packages"".""id""]
                ""use_method"" varchar(500) [not null]
                ""created_at"" timestamp[not null]
                ""created_by"" varchar(500) [not null]
                ""updated_at"" timestamp[not null]
                ""updated_by"" varchar(500) [not null]

                Indexes {
                name[name: ""idx_name""]
                }
            }

            Table ""public"".""datasources"" {
                ""id"" integer [pk, not null]
                ""name"" integer[unique, not null]
                ""description"" varchar(500)
                ""enabled"" bool
                ""datasource_provider_id"" integer [not null, ref: < ""public"".""datasource_providers"".""id""]
                ""connection"" varchar(500)
                ""username"" varchar(500)
                ""password"" varchar(500)
                ""created_at"" timestamp[not null]
                ""created_by"" varchar(500)[not null]
                ""updated_at"" timestamp[not null]
                ""updated_by"" varchar(500)[not null]

                Indexes {
                name [unique, name: ""idx_name""]
                }
            }";

        var model = DbmlParser.Parse(dbmlContent);
        model.ShouldNotBeNull();
        model.Tables.Count.ShouldBe(3);
        model.Relationships.Count.ShouldBe(2);

        var rel = model.Relationships[0];
        rel.LeftTable.ShouldBe("datasource_providers");
        rel.RightTable.ShouldBe("nuget_packages");
        rel.LeftColumns.ShouldBe(["nuget_package_id"]);
        rel.RightColumns.ShouldBe(["id"]);
    }
}
