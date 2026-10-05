### Runbook

#### Deployment Requirements
1. Build the solution with `dotnet build`
2. Publish the application with `dotnet publish`
3. Run the published application

#### Environment Configuration
- Use `appsettings.Development.json` for logging
- Use `appsettings.json` for allowed hosts and logging levels
- Use `launchSettings.json` for application URL and environment variables

#### Operational Steps
1. Run `dotnet run` to start the application
2. Use `dotnet test` to run tests
3. Publish with `dotnet publish` for deployment