### Design Documentation

#### Architecture
- Controller: Handles HTTP requests (POST /api/todos, GET /api/todos/{id})
- Service: Manages business logic (Todo creation, retrieval, status management)
- Infrastructure: In-memory data storage (no database, no EF)

#### Interfaces
- `ITodoService`: Defines contract for Todo operations (create, retrieve)
- `IController`: Defines contract for HTTP request handling (POST /api/todos, GET /api/todos/{id})

#### Technical Boundaries
- Use C# with .NET 10.0
- No database, no EF
- No authentication
- In-memory repository
- Automated tests in DemoApi.Tests
- Documentation and tests included