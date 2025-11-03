# Database Integration Tests

## Overview

The `DatabaseIntegrationTests.cs` file contains comprehensive integration tests that verify the node-api codebase is compatible with the deployed MySQL database schema and configuration.

**These tests are NOT run as part of the normal test suite.** They require a live database connection and should be run manually after:

- Schema migrations
- Database configuration changes  
- Connection string parameter changes
- Major refactoring of repository code
- Deployment to a new environment

## Prerequisites

### Quick Setup: User Secrets (Recommended)

The Tests project **shares User Secrets with the main node-api project**, so you only need to set credentials once.

#### One-Time Setup

```bash
# Navigate to either project directory (they share secrets)
cd node-api
# OR
cd Tests

# Set your database credentials (only need to do this once)
dotnet user-secrets set "DB_HOST" "your-mysql-host"
dotnet user-secrets set "DB_PORT" "3306"
dotnet user-secrets set "DB_USER" "your-username"
dotnet user-secrets set "DB_PASSWORD" "your-password"
dotnet user-secrets set "DB_NAME" "your-database"

# Verify secrets are set
dotnet user-secrets list
```

That's it! Both the main application and tests will use these credentials.

**Where are secrets stored?**
- Windows: `%APPDATA%\Microsoft\UserSecrets\6471d754-eb95-4d79-9bc3-fc026e9852bf\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/6471d754-eb95-4d79-9bc3-fc026e9852bf/secrets.json`

#### Manage Secrets

```bash
# View all secrets (from either project)
dotnet user-secrets list

# Update a secret
dotnet user-secrets set "DB_PASSWORD" "new-password"

# Remove a secret
dotnet user-secrets remove "DB_PASSWORD"

# Clear all secrets
dotnet user-secrets clear
```

### Alternative: Environment Variables

If you prefer, set environment variables before running tests:

```bash
# Windows (PowerShell)
$env:DB_HOST="your-mysql-host"
$env:DB_PORT="3306"
$env:DB_USER="your-username"
$env:DB_PASSWORD="your-password"
$env:DB_NAME="your-database"

# Linux/macOS
export DB_HOST="your-mysql-host"
export DB_PORT="3306"
export DB_USER="your-username"
export DB_PASSWORD="your-password"
export DB_NAME="your-database"
```

### Using a Separate Test Database

If you want tests to use a different database than the main application:

**Option 1: Override with environment variables**
```bash
# Set DB_NAME to a different database just for tests
DB_NAME=test_database dotnet test --filter "Category=DatabaseIntegration"
```

**Option 2: Use separate User Secrets for tests**

Edit `Tests/Tests.csproj` and change the `<UserSecretsId>` to a different GUID, then set separate secrets for the Tests project.

## Running the Tests

### Command Line

#### Run all database integration tests:
```bash
dotnet test --filter "Category=DatabaseIntegration"
```

#### Run with verbose output:
```bash
dotnet test --filter "Category=DatabaseIntegration" --logger "console;verbosity=detailed"
```

#### Run a specific test:
```bash
dotnet test --filter "FullyQualifiedName~TraceRepository_Should_Insert_And_Retrieve_Trace"
```

### Visual Studio

1. Open **Test Explorer** (Test > Test Explorer)
2. Click the **filter** icon
3. Select **Traits**
4. Check **Category: DatabaseIntegration**
5. Click **Run All**

### Visual Studio Code

1. Install the **.NET Core Test Explorer** extension
2. Open Test Explorer in the sidebar
3. Filter tests by typing `@Category=DatabaseIntegration` in the search box
4. Click **Run All Tests**

## Test Categories

The tests are organized into the following categories:

### Connection Tests
- Verify connection string configuration
- Test basic connectivity
- Validate connection pooling behavior
- Ensure reconnection works after connection close

### Repository Tests

#### Trace Repository
- Insert and retrieve L2 trace records
- Pagination support
- Schema compatibility with all indexed columns
- Complex filtering queries

#### Event Repository  
- Insert and retrieve event records (LinkUp, LinkDown, etc.)
- Pagination support
- Schema compatibility with event-specific indexes

#### Network State Repository
- **Nodes**: Insert, update, and retrieve node state
- **Links**: Insert, update, and retrieve link state including flapping metrics
- **Circuits**: Insert, update, and retrieve circuit state
- Schema compatibility with GeoIP columns
- Schema compatibility with link flapping detection columns

#### Errored Message Repository
- Insert validation errors
- Insert generic errors

### Resilience Tests
- Multiple sequential database operations
- Concurrent read operations
- Long-running queries
- Connection pooling under load

### Schema Compatibility Tests
- Comprehensive schema validation across all repositories
- Ensures no missing columns, type mismatches, or constraint violations

## What Gets Tested

### ? Connection String Parameters
- `Pooling=true` is set
- `Connection Lifetime=300` for automatic connection refresh
- `Connection Timeout=10` for fast failure detection
- `Default Command Timeout=30` to prevent hung queries

### ? Connection Pooling
- Multiple concurrent connections work correctly
- Pool doesn't exhaust under normal load
- Connections are properly disposed

### ? CRUD Operations
- **Create**: Insert operations succeed
- **Read**: Retrieval operations return expected data
- **Update**: Upsert operations properly update existing records
- **Delete**: Delete operations work (where applicable)

### ? Schema Compatibility
- All expected columns exist
- Data types match expectations
- Indexes support expected query patterns
- No constraint violations on insert/update

### ? Advanced Features
- Pagination (cursor-based keyset pagination)
- Complex filtering (multiple WHERE clauses)
- Aggregate queries (COUNT for total results)
- JSON column storage and retrieval
- Generated/computed columns

## Test Data Cleanup

The tests insert test data with identifiable prefixes:
- Nodes: `TEST-XX` (e.g., `TEST-99`, `TEST-98`)
- Links: `TEST-L1<->TEST-L2`
- Circuits: `TEST-C1:0001<->TEST-C2:0002`
- Traces/Events: `reportFrom` or `node` = `TEST-INTEGRATION`

You may want to periodically clean up test data:

```sql
-- Clean up test nodes
DELETE FROM nodes WHERE callsign LIKE 'TEST-%';

-- Clean up test links  
DELETE FROM links WHERE canonical_key LIKE 'TEST-%';

-- Clean up test circuits
DELETE FROM circuits WHERE canonical_key LIKE 'TEST-%';

-- Clean up test traces (older than 1 day)
DELETE FROM traces 
WHERE json LIKE '%TEST-INTEGRATION%' 
  AND timestamp < DATE_SUB(NOW(), INTERVAL 1 DAY);

-- Clean up test events (older than 1 day)
DELETE FROM events 
WHERE json LIKE '%TEST-%' 
  AND timestamp < DATE_SUB(NOW(), INTERVAL 1 DAY);
```

## Interpreting Results

### All Tests Pass ?
Your database schema and configuration are compatible with the current codebase.

### Tests Fail ?
Investigate the failure:

1. **Connection failures**: Check environment variables and network connectivity
2. **Schema errors** (missing column, type mismatch): Your database schema may be out of sync
3. **Timeout errors**: Increase `Default Command Timeout` or check database performance
4. **Constraint violations**: May indicate schema changes (new NOT NULL columns, etc.)

## Common Issues

### "Connection string not set" error
Set the required environment variables before running tests.

### "Table doesn't exist" error  
Run your database migrations first.

### "Unknown column" error
Your database schema is missing expected columns. Check for pending migrations.

### "Lock wait timeout exceeded"
Another process has locked the tables. Wait and retry, or kill the blocking transaction.

### Connection pool exhausted
Increase `Max Pool Size` in the connection string (currently 100).

## Best Practices

1. **Run before deployment**: Always run these tests against your staging database before deploying schema changes
2. **Run after migrations**: Verify schema migrations didn't break compatibility
3. **Run in CI/CD**: Configure these to run in a separate pipeline with database credentials
4. **Keep tests up to date**: Add new tests when you add new database tables or columns
5. **Monitor execution time**: Slow tests may indicate missing indexes

## Adding New Tests

When adding new database functionality:

1. Add the repository method to the appropriate test category
2. Test both insert and retrieval
3. Test edge cases (null values, empty results, etc.)
4. Ensure proper cleanup or use identifiable test data
5. Mark the test with `[Trait("Category", "DatabaseIntegration")]`

Example:
```csharp
[Fact]
[Trait("Category", "DatabaseIntegration")]
public async Task NewRepository_Should_Do_Something()
{
    // Arrange
    var repo = new MyNewRepository(_logger, _tracker);
    
    // Act
    var result = await repo.MyNewMethodAsync();
    
    // Assert
    result.Should().NotBeNull();
}
```

## CI/CD Integration

To run these tests in your CI/CD pipeline:

```yaml
# GitHub Actions example
- name: Run Database Integration Tests
  run: dotnet test --filter "Category=DatabaseIntegration"
  env:
    DB_HOST: ${{ secrets.DB_HOST }}
    DB_PORT: ${{ secrets.DB_PORT }}
    DB_USER: ${{ secrets.DB_USER }}
    DB_PASSWORD: ${{ secrets.DB_PASSWORD }}
    DB_NAME: ${{ secrets.DB_NAME }}
```

## Support

If tests fail and you need help:

1. Check the test output for the specific error message
2. Review recent schema changes
3. Check database logs for errors
4. Verify connection string parameters
5. Ensure the database user has appropriate permissions (SELECT, INSERT, UPDATE, DELETE)
