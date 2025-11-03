# Quick Reference: Shared User Secrets Setup

## ? One-Time Setup (Both Projects)

Both `node-api` and `Tests` share the same User Secrets ID, so you only need to configure once:

```sh
# From EITHER directory (node-api OR Tests)
cd node-api

# Set credentials (applies to both projects)
dotnet user-secrets set "DB_HOST" "your-host.com"
dotnet user-secrets set "DB_PORT" "3306"
dotnet user-secrets set "DB_USER" "your_username"
dotnet user-secrets set "DB_PASSWORD" "your_password"
dotnet user-secrets set "DB_NAME" "your_database"

# Verify
dotnet user-secrets list
```

## ?? Shared Secrets Location

Both projects read from the same file:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\6471d754-eb95-4d79-9bc3-fc026e9852bf\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/6471d754-eb95-4d79-9bc3-fc026e9852bf/secrets.json`

## ?? Run the Application

```sh
cd node-api
dotnet run

# Automatically loads secrets from User Secrets
# Falls back to environment variables in production
```

## ?? Run Integration Tests

```sh
cd Tests

# Run database integration tests
dotnet test --filter "Category=DatabaseIntegration"

# Uses the same secrets as the main app
```

## ?? Common Commands

```sh
# View secrets (from either project)
dotnet user-secrets list

# Update a secret (affects both projects)
dotnet user-secrets set "DB_PASSWORD" "new-password"

# Remove a secret
dotnet user-secrets remove "DB_HOST"

# Clear all secrets
dotnet user-secrets clear
```

## ?? Using Different Databases for Tests

### Quick Override with Environment Variables

```sh
# Override just the database name for tests
DB_NAME=test_database dotnet test --filter "Category=DatabaseIntegration"
```

### Permanent Separate Test Database

1. Edit `Tests/Tests.csproj`
2. Change `<UserSecretsId>` to a new GUID
3. Run `dotnet user-secrets init` in Tests directory
4. Set separate secrets for tests

## ?? Security

? **Safe** - User Secrets are stored in your user profile, NOT in the project
? **Not in Git** - The `.csproj` only contains the ID, not the actual secrets
? **Development Only** - User Secrets only load in Development environment
? **Production** - Use environment variables or Azure Key Vault in production

## ? Troubleshooting

### Secrets not loading?

```sh
# Check environment (must be Development for User Secrets)
echo $ASPNETCORE_ENVIRONMENT

# Set to Development if needed
export ASPNETCORE_ENVIRONMENT=Development  # Linux/macOS
$env:ASPNETCORE_ENVIRONMENT="Development"  # Windows PowerShell
```

### Verify secrets file exists:

```sh
# Windows
type %APPDATA%\Microsoft\UserSecrets\6471d754-eb95-4d79-9bc3-fc026e9852bf\secrets.json

# Linux/macOS
cat ~/.microsoft/usersecrets/6471d754-eb95-4d79-9bc3-fc026e9852bf/secrets.json
```

### Still not working?

Use environment variables as fallback:

```sh
# Windows PowerShell
$env:DB_HOST="your-host"
$env:DB_PASSWORD="your-password"
# ... etc

# Linux/macOS
export DB_HOST="your-host"
export DB_PASSWORD="your-password"
# ... etc
```

## ?? Full Documentation

See `docs/DATABASE_CONFIGURATION.md` for complete details.
