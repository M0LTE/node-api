# Database Configuration Guide

## Quick Setup (Recommended)

Both the main application and tests **share the same User Secrets**, so you only need to set credentials once.

### One-Time Setup

```sh
# Navigate to either project directory (they share the same secrets)
cd node-api
# OR
cd Tests

# Set your database credentials (ONLY ONCE)
dotnet user-secrets set "DB_HOST" "your-mysql-host.com"
dotnet user-secrets set "DB_PORT" "3306"
dotnet user-secrets set "DB_USER" "your_username"
dotnet user-secrets set "DB_PASSWORD" "your_password"
dotnet user-secrets set "DB_NAME" "your_database"

# Verify they're set
dotnet user-secrets list
```

That's it! Both projects will now use these same credentials.

### Using Different Credentials for Tests (Optional)

If you want tests to use a different database (e.g., local test DB), you have two options:

**Option 1: Use environment variables for tests**
```sh
# Set before running tests
export DB_HOST="localhost"
export DB_NAME="test_database"

dotnet test --filter "Category=DatabaseIntegration"
```

**Option 2: Use a separate User Secrets ID for tests**

Edit `Tests/Tests.csproj` and change the `<UserSecretsId>` to a different GUID, then set separate secrets for tests.

## How It Works

### Shared Secrets

Both `node-api.csproj` and `Tests.csproj` use the same `<UserSecretsId>`:
```xml
<UserSecretsId>6471d754-eb95-4d79-9bc3-fc026e9852bf</UserSecretsId>
```

This means they read from the **same secrets file** at:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\6471d754-eb95-4d79-9bc3-fc026e9852bf\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/6471d754-eb95-4d79-9bc3-fc026e9852bf/secrets.json`

### Priority Order

The application reads configuration in this order (later sources override earlier):

1. `appsettings.json` (checked into Git ?)
2. `appsettings.Development.json` (checked into Git ?)
3. **User Secrets** (NOT in Git ? - Development environment only)
4. Environment Variables (NOT in Git ?)
5. Command-line arguments

### What Gets Checked Into Git?

? **Committed to Git:**
- `appsettings.json` - Default configuration, no secrets
- `.csproj` files with `<UserSecretsId>` - Just the ID, not the secrets

? **NOT Committed to Git:**
- User Secrets (stored in your user profile)
- Environment variables
- Local `.env` files (if you use them)

## Managing Secrets

### View All Secrets

```sh
# From either project directory
cd node-api  # or Tests
dotnet user-secrets list
```

### Update a Secret

```sh
# From either project directory - changes apply to both
dotnet user-secrets set "DB_PASSWORD" "new_password"
```

### Remove a Secret

```sh
dotnet user-secrets remove "DB_PASSWORD"
```

### Clear All Secrets

```sh
dotnet user-secrets clear
```

## Production Deployment

In production (Docker, Azure, etc.), use environment variables instead:

```sh
# Docker run example
docker run -e DB_HOST=prod-mysql.example.com \
           -e DB_PORT=3306 \
           -e DB_USER=prod_user \
           -e DB_PASSWORD=prod_password \
           -e DB_NAME=prod_database \
           m0lte/node-api:latest
```

Or in Docker Compose:

```yaml
services:
  node-api:
    image: m0lte/node-api:latest
    environment:
      - DB_HOST=mysql
      - DB_PORT=3306
      - DB_USER=prod_user
      - DB_PASSWORD=prod_password
      - DB_NAME=prod_database
```

## Testing Your Configuration

### Test Main Application

```sh
cd node-api
dotnet run

# Check logs for database connection
# Should see: "Connection successful" or queries being logged
```

### Test Integration Tests

```sh
cd Tests

# Run database integration tests
dotnet test --filter "Category=DatabaseIntegration"

# You should see tests passing, not connection errors
```

## Troubleshooting

### "Database configuration is missing" Error

This means the app can't find your database credentials.

**Solutions:**

1. Check if User Secrets are set:
   ```sh
   cd node-api  # or Tests
   dotnet user-secrets list
   ```
   If it says "No secrets configured", run the setup commands above.

2. Check if all 5 values are set:
   - `DB_HOST`
   - `DB_PORT`
   - `DB_USER`
   - `DB_PASSWORD`
   - `DB_NAME`

3. Make sure you're running in Development environment (User Secrets only load in Development).

### User Secrets Not Loading

User Secrets only load in **Development** environment.

**Check your environment:**

```sh
# Windows (PowerShell)
$env:ASPNETCORE_ENVIRONMENT

# Linux/macOS
echo $ASPNETCORE_ENVIRONMENT
```

If it's not set or not "Development":

```sh
# Windows (PowerShell)
$env:ASPNETCORE_ENVIRONMENT="Development"

# Linux/macOS
export ASPNETCORE_ENVIRONMENT=Development
```

### Verify Secrets File Location

You can manually check the secrets file exists:

```sh
# Windows
type %APPDATA%\Microsoft\UserSecrets\6471d754-eb95-4d79-9bc3-fc026e9852bf\secrets.json

# Linux/macOS
cat ~/.microsoft/usersecrets/6471d754-eb95-4d79-9bc3-fc026e9852bf/secrets.json
```

You should see something like:
```json
{
  "DB_HOST": "your-host",
  "DB_PORT": "3306",
  "DB_USER": "your-user",
  "DB_PASSWORD": "your-password",
  "DB_NAME": "your-database"
}
```

### Still Not Working?

Use environment variables as a fallback:

```sh
# Windows (PowerShell)
$env:DB_HOST="your-host"
$env:DB_PORT="3306"
$env:DB_USER="your-user"
$env:DB_PASSWORD="your-password"
$env:DB_NAME="your-database"

dotnet run

# Linux/macOS
export DB_HOST="your-host"
export DB_PORT="3306"
export DB_USER="your-user"
export DB_PASSWORD="your-password"
export DB_NAME="your-database"

dotnet run
```

## Security Best Practices

? **DO:**
- Use User Secrets for local development
- Use environment variables or secret managers (Azure Key Vault, AWS Secrets Manager) in production
- Never commit database credentials to Git
- Rotate passwords regularly
- Use different credentials for development and production

? **DON'T:**
- Put credentials in `appsettings.json`
- Commit `.env` files
- Share your User Secrets file
- Use production credentials locally

## Advanced: Separate Test Database

If you want tests to use a completely separate database:

### Option 1: Environment Variables Override

Set environment variables before running tests:

```sh
# Windows (PowerShell)
$env:DB_NAME="test_database"
dotnet test --filter "Category=DatabaseIntegration"

# Linux/macOS
DB_NAME=test_database dotnet test --filter "Category=DatabaseIntegration"
```

### Option 2: Separate UserSecretsId for Tests

1. Edit `Tests/Tests.csproj` and change the `<UserSecretsId>` to a new GUID:
   ```xml
   <UserSecretsId>a1b2c3d4-e5f6-7890-abcd-ef1234567890</UserSecretsId>
   ```

2. Set secrets for tests separately:
   ```sh
   cd Tests
   dotnet user-secrets set "DB_HOST" "localhost"
   dotnet user-secrets set "DB_NAME" "test_database"
   # ... etc
   ```

## Additional Resources

- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
