# MQTT Configuration with User Secrets

## Overview

The MQTT configuration has been refactored to use .NET's configuration system with support for User Secrets, instead of directly coupling to environment variables.

## Configuration Hierarchy

The MQTT password is loaded in the following order (later sources override earlier):

1. `appsettings.json` - Default configuration (password should be `null`)
2. `appsettings.Development.json` - Development-specific settings
3. **User Secrets** - Secure local storage (Development environment only) ? **Recommended**
4. **Environment Variables** - Fallback for production/CI/CD
5. Command-line arguments

## Setting Up User Secrets (Recommended for Development)

### Quick Setup

```bash
# Navigate to the main project directory
cd node-api

# Set the MQTT password via User Secrets
dotnet user-secrets set "MqttSettings:Password" "your-password-here"

# Verify it was set
dotnet user-secrets list
```

### What Gets Stored

User Secrets are stored in:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

The `UserSecretsId` is defined in `node-api.csproj`:
```xml
<UserSecretsId>6471d754-eb95-4d79-9bc3-fc026e9852bf</UserSecretsId>
```

## Configuration Structure

### appsettings.json (Committed to Git)

```json
{
  "MqttSettings": {
    "Host": "node-api.packet.oarc.uk",
    "Port": 1883,
    "Username": "writer",
    "Password": null,
    "ClientIdPrefix": "node-api",
    "AutoReconnectDelaySeconds": 5,
    "CleanSession": true
  }
}
```

### User Secrets (NOT in Git)

```json
{
  "MqttSettings": {
    "Password": "your-actual-password"
  }
}
```

## Environment Variable Fallback

For production or CI/CD environments where User Secrets aren't available:

```bash
# Linux/macOS
export MQTT_WRITER_PASSWORD="your-password"

# Windows (PowerShell)
$env:MQTT_WRITER_PASSWORD="your-password"

# Docker
docker run -e MQTT_WRITER_PASSWORD="your-password" ...
```

The code checks configuration first, then falls back to `MQTT_WRITER_PASSWORD` environment variable.

## Testing Configuration

### Unit Tests

Tests use `IOptions<MqttSettings>` with mock configuration:

```csharp
var mqttSettings = Options.Create(new MqttSettings
{
    Host = "node-api.packet.oarc.uk",
    Port = 1883,
    Username = "writer",
    Password = "test-password",
    ClientIdPrefix = "test",
    AutoReconnectDelaySeconds = 5,
    CleanSession = true
});

var provider = new MqttClientProvider(logger, mqttSettings);
```

### Integration Tests

Integration tests use `TestWebApplicationFactory` which automatically provides mock MQTT configuration. **No MQTT password setup required!**

The factory configures mock MQTT settings:
```csharp
services.Configure<MqttSettings>(options =>
{
    options.Host = "test-broker";
    options.Port = 1883;
    options.Username = "test-user";
    options.Password = "test-password"; // Mock password
    options.ClientIdPrefix = "test";
    options.AutoReconnectDelaySeconds = 5;
    options.CleanSession = true;
});
```

**Result**: Integration tests run without requiring real MQTT credentials or User Secrets.

## Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Host` | string | `"node-api.packet.oarc.uk"` | MQTT broker hostname |
| `Port` | int | `1883` | MQTT broker port |
| `Username` | string | `"writer"` | MQTT username for write access |
| `Password` | string? | `null` | MQTT password (set via User Secrets or env var) |
| `ClientIdPrefix` | string | `"node-api"` | Prefix for MQTT client IDs |
| `AutoReconnectDelaySeconds` | int | `5` | Delay between reconnection attempts |
| `CleanSession` | bool | `true` | Whether to use clean sessions |

## Services Using MQTT Configuration

The following services now use `IOptions<MqttSettings>`:

1. **MqttClientProvider** - Main MQTT client provider (requires password)
2. **SystemMetricsPublisher** - Publishes system metrics (requires password)
3. **MqttStateSubscriber** - Subscribes to events (read-only, no password needed)

## Migration from Environment Variables

### Old Code (Direct Environment Variable)

```csharp
var password = Environment.GetEnvironmentVariable("MQTT_WRITER_PASSWORD") 
    ?? throw new InvalidOperationException("MQTT_WRITER_PASSWORD not set");
```

### New Code (Configuration with Fallback)

```csharp
public MqttClientProvider(
    ILogger<MqttClientProvider> logger,
    IOptions<MqttSettings> mqttSettings)
{
    _mqttSettings = mqttSettings.Value;
}
```

Configuration setup in `Program.cs`:

```csharp
builder.Services.Configure<MqttSettings>(options =>
{
    builder.Configuration.GetSection("MqttSettings").Bind(options);
    
    // Fallback to environment variable if password not set in config
    if (string.IsNullOrWhiteSpace(options.Password))
    {
        options.Password = Environment.GetEnvironmentVariable("MQTT_WRITER_PASSWORD");
    }
});
```

## Troubleshooting

### Error: "MQTT password is not configured"

**Solution**: Set the password using one of these methods:

1. **User Secrets** (recommended for development):
   ```bash
   cd node-api
   dotnet user-secrets set "MqttSettings:Password" "your-password"
   ```

2. **Environment Variable** (production/CI):
   ```bash
   export MQTT_WRITER_PASSWORD="your-password"
   ```

3. **appsettings.Development.json** (NOT recommended - can be accidentally committed):
   ```json
   {
     "MqttSettings": {
       "Password": "your-password"
     }
   }
   ```

### Tests Failing with "MQTT password not set"

**Solution**: Tests now use mock `IOptions<MqttSettings>` with test credentials. If tests still fail:

1. Check test is creating proper mock configuration:
   ```csharp
   var mqttSettings = Options.Create(new MqttSettings { Password = "test-password" });
   ```

2. For integration tests using `TestWebApplicationFactory`, ensure mock configuration is registered.

### User Secrets Not Loading

**Verification steps**:

1. Check User Secrets are set:
   ```bash
   cd node-api
   dotnet user-secrets list
   ```

2. Check User Secrets ID matches in `.csproj`:
   ```xml
   <UserSecretsId>6471d754-eb95-4d79-9bc3-fc026e9852bf</UserSecretsId>
   ```

3. Ensure running in Development environment:
   ```bash
   # Check current environment
   echo $ASPNETCORE_ENVIRONMENT  # Linux/macOS
   echo %ASPNETCORE_ENVIRONMENT%  # Windows CMD
   $env:ASPNETCORE_ENVIRONMENT    # Windows PowerShell
   ```

4. User Secrets only load in **Development** environment. For other environments, use environment variables or other configuration providers.

## Benefits of This Approach

? **Secure** - Secrets not committed to Git  
? **Flexible** - Supports multiple configuration sources  
? **Testable** - Easy to mock in unit tests  
? **Documented** - Clear configuration structure  
? **Compatible** - Still supports environment variables as fallback  
? **Type-safe** - Configuration mapped to strongly-typed `MqttSettings` class  

## See Also

- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- Database configuration: See `docs/DATABASE_CONFIGURATION.md`
