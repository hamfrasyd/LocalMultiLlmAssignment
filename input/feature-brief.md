@'
# Todo API feature

Implement an in-memory Todo feature in the existing DemoApi ASP.NET Core project.

## Required behavior

The API must support:

### POST /api/todos

Request:

```json
{
  "title": "Write assignment"
}
```

Requirements:

- title is required
- title must be between 1 and 100 characters
- creates a Todo item with:
  - integer ID
  - title
  - completed flag
- a newly created Todo is not completed
- return an appropriate successful HTTP status

### GET /api/todos/{id}

Requirements:

- returns the Todo when it exists
- returns 404 when it does not exist

## Technical boundaries

- use C#
- use the existing ASP.NET Core project
- no database
- no Entity Framework
- no authentication
- no external services
- use an in-memory repository or service
- keep responsibilities separated
- add automated tests
- update documentation
- the solution must build with `dotnet build`
- tests must run with `dotnet test`

## Deployment requirement

The project must be publishable with:

```text
dotnet publish
```

Document the environment/configuration needed to run it.
'@ | Set-Content -Path ".\input\feature-brief.md" -Encoding utf8