### README

#### Setup/Run Instructions
1. Install .NET 10.0
2. Clone the repository
3. Run `dotnet build` to build the solution
4. Run `dotnet run` to start the application

#### Environment Requirements
- `appsettings.Development.json` for logging
- `appsettings.json` for allowed hosts and logging levels
- `launchSettings.json` for application URL and environment variables

#### API Usage
- POST /api/todos with JSON body {"title": "Write assignment"}
- GET /api/todos/{id} to retrieve a todo

#### Operational Notes
- Application must be published with `dotnet publish`
- Tests run with `dotnet test`