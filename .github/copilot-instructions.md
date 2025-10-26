# GitHub Copilot Instructions for node-api

## Project Overview

This is a .NET 9.0 ASP.NET Core Web API service that provides packet network monitoring capabilities. The service:

- Listens for UDP datagrams on port 13579 containing network event data
- Validates and processes various event types (nodes, links, circuits, traces)
- Publishes processed events to MQTT topics
- Persists network state and events to MySQL database
- Exposes REST API endpoints for querying network state
- Provides OpenAPI/Scalar documentation at `/scalar`

## Technology Stack

- **.NET 9.0** - Target framework
- **ASP.NET Core** - Web framework with minimal API
- **FluentValidation** - Input validation framework
- **Dapper** - Lightweight ORM for MySQL access
- **MQTTnet** - MQTT client library
- **xUnit** - Testing framework
- **Docker** - Container support with `mcr.microsoft.com/dotnet/aspnet:9.0` base image

## Project Structure

- **`/node-api`** - Main application project
  - `Controllers/` - REST API controllers for nodes, links, circuits, events, traces, diagnostics
  - `Models/` - Domain models and event types
  - `Services/` - Background services (UDP listener, MQTT subscriber, persistence, metrics)
  - `Validators/` - FluentValidation validators for all event types
  - `Converters/` - JSON converters
  - `Utilities/` - Helper utilities
  - `Constants/` - Application constants
  - `Program.cs` - Application startup and DI configuration
- **`/Tests`** - Unit and integration tests (691 tests)
- **`/SmokeTests`** - End-to-end smoke tests for deployed instances
- **`/schema`** - Database schema definitions

## Code Style Guidelines

### General Principles

- Enable **nullable reference types** (`<Nullable>enable</Nullable>`)
- Use **implicit usings** (`<ImplicitUsings>enable</ImplicitUsings>`)
- Follow C# naming conventions (PascalCase for classes/methods, camelCase for parameters)
- Keep methods focused and single-purpose
- Prefer dependency injection over static methods

### Async/Await

- Use async methods for I/O operations (database, network, file)
- Background services should implement `BackgroundService` base class
- Avoid blocking calls - use `await` instead of `.Result` or `.Wait()`

### Validation

- All input models should have corresponding FluentValidation validators
- Validators are registered as singletons in DI container
- Use `DatagramValidationService` for centralized validation logic
- Return proper validation error messages mapped to JSON property names

### Dependency Injection

- Register services in `Program.cs`
- Use appropriate lifetimes:
  - Singleton: State services, repositories, validators
  - Scoped: Request-scoped services
  - Transient: Stateless services
- Inject interfaces, not concrete types

### Testing

- **Unit tests** go in `/Tests` - currently 691 passing tests
- **Smoke tests** go in `/SmokeTests` - for deployed instance validation
- Use xUnit as the test framework
- Mock repositories using interfaces like `ITraceRepository`, `IEventRepository`
- Test validators thoroughly with edge cases
- Follow existing test patterns (see `L2TraceValidatorTests.cs` for examples)

### Error Handling

- Log errors using `ILogger<T>`
- Use structured logging with `ConsoleFormatterNames.Systemd`
- Handle validation errors gracefully with proper HTTP status codes
- Invalid UDP datagrams should be logged but not crash the service

### Database

- Use **Dapper** for database operations (see `MySqlTraceRepository.cs`)
- Connection strings from configuration: `ConnectionStrings:DefaultConnection`
- Parameterize all queries to prevent SQL injection
- Use transactions for multi-statement operations
- MySQL-specific features are acceptable

### MQTT

- Use `ManagedMqttClient` for automatic reconnection
- Topic structure: `in/udp`, `out/{EventType}`, `err/validation`
- Publish system metrics to `metrics/system/{hostname}`
- Handle connection failures gracefully

### API Design

- Controllers should be thin - delegate to services
- Return appropriate HTTP status codes
- Use OpenAPI attributes for documentation
- Support CORS for web clients
- Add cache control headers: `no-cache, no-store, must-revalidate`

## Building and Testing

```bash
# Build the solution
dotnet build

# Run all unit tests
dotnet test Tests/

# Run smoke tests (requires running service)
dotnet test SmokeTests/

# Run the service locally
cd node-api
dotnet run

# Service will be available at:
# - http://localhost:5000
# - OpenAPI docs at http://localhost:5000/scalar
```

## Important Configuration

- **UDP Port**: 13579 (configured in `UdpNodeInfoListener`)
- **MQTT**: Configured via `appsettings.json` under `MqttSettings`
- **Database**: MySQL connection string in `ConnectionStrings:DefaultConnection`
- **CORS**: Allows any origin (configured for development)
- **Forwarded Headers**: Configured for Docker/proxy scenarios (X-Forwarded-For, X-Forwarded-Proto)

## Common Patterns

### Adding a New Event Type

1. Create model in `/Models` with `@type` discriminator property
2. Create FluentValidation validator in `/Validators`
3. Register validator in `Program.cs` DI container
4. Add to `DatagramValidationService` validation logic
5. Update `UdpNodeInfoJsonDatagramDeserialiser` if needed
6. Add comprehensive tests in `/Tests`

### Adding a New API Endpoint

1. Create or update controller in `/Controllers`
2. Inject required services via constructor
3. Add OpenAPI attributes for documentation
4. Return proper status codes and content types
5. Add integration tests in `/Tests`

## Known Issues and Warnings

- Some nullable reference type warnings exist (CS8620, CS8603) - these are being addressed
- Environment-specific logic: `DbWriter` service is disabled for development machine `PRECISION3660` - consider making this configuration-driven rather than hard-coded
- Performance test warnings about blocking operations (xUnit1031) are acceptable for perf tests

## Security Considerations

- All database queries use parameterization
- Input validation via FluentValidation before processing
- No authentication currently implemented (service runs in trusted network)
- **CORS**: Currently allows any origin for development
  - For production: Restrict to specific origins in `Program.cs`:
    ```csharp
    policy.WithOrigins("https://yourdomain.com")
          .AllowAnyMethod()
          .AllowAnyHeader();
    ```
- **Forwarded Headers**: Trusts Docker's default network (172.17.0.0/16)
  - Adjust `KnownNetworks` in `Program.cs` for different deployment scenarios
  - Add additional trusted proxy networks as needed

## Tips for Contributors

- Run `dotnet build` early to catch compilation issues
- Write tests first when adding validation rules
- Check existing validators for patterns before creating new ones
- Use `MockTraceRepository` and `MockEventRepository` for testing
- Smoke tests require a running instance - unit tests don't
- The service is designed to be resilient - failing to write to DB or MQTT shouldn't crash UDP listener
