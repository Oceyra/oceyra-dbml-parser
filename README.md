# Oceyra DBML Parser
This project is used to parse a DBML from DbDiagram or ChartDb and return the model of the database.

[![Build status](https://github.com/oceyra/oceyra-dbml-parser/actions/workflows/publish.yaml/badge.svg?branch=main&event=push)](https://github.com/oceyra/oceyra-dbml-parser/actions/workflows/publish.yaml?query=branch%3Amain+event%3Apush)

## Usage Sample
```c#
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

var dbModel = DbmlParser.Parse(dbmlContent);
```

## Sponsors

[![Buy Me a Coffee](https://raw.githubusercontent.com/calimero100582/calimero100582.github.io/refs/heads/main/images/sponsors/buymeacoffee/default-blue.png)](https://www.buymeacoffee.com/pierduchp)
