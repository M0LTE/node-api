# MQTT Configuration Refactoring Summary

**Date**: 2025-01-21  
**Status**: ? Complete

## Problem

Tests were failing with `System.InvalidOperationException: MQTT_WRITER_PASSWORD environment variable is not set`. The code was directly coupled to environment variables, making it:
- Hard to test
- Not following .NET configuration best practices
- Inconvenient for local development
- Inflexible for different deployment scenarios

## Solution

Refactored MQTT configuration to use .NET's configuration system with support for:
- ? **User Secrets** (recommended for development)
- ? **appsettings.json** (for non-sensitive defaults)
- ? **Environment variables** (fallback for production/CI/CD)
- ? **Strong typing** via `MqttSettings` class
- ? **Dependency injection** via `IOptions<MqttSettings>`

## Changes Made

### 1. Created Configuration Class

**File**: `node-api/Configuration/MqttSettings.cs`

```csharp
public class MqttSettings
{
    public string Host { get; set; } = "node-api.packet.oarc.uk";
    public int Port { get; set; } = 1883;
    public string Username { get; set; } = "writer";
    public string? Password { get; set; }  // From User Secrets or env var
    public string ClientIdPrefix { get; set; } = "node-api";
    public int AutoReconnectDelaySeconds { get; set; } = 5;
    public bool CleanSession { get; set; } = true;
}
```

### 2. Updated Services

**Modified files**:
- `node-api/Services/MqttClientProvider.cs`
- `node-api/Services/SystemMetricsPublisher.cs`
- `node-api/Services/MqttStateSubscriber.cs`

**Before** (environment variable):
```csharp
var password = Environment.GetEnvironmentVariable("MQTT_WRITER_PASSWORD") 
    ?? throw new InvalidOperationException("MQTT_WRITER_PASSWORD not set");
```

**After** (configuration injection):
```csharp
public MqttClientProvider(
    ILogger<MqttClientProvider> logger,
    IOptions<MqttSettings> mqttSettings)
{
    _mqttSettings = mqttSettings.Value;
}
```

### 3. Updated Program.cs

**File**: `node-api/Program.cs`

```csharp
// Configure MQTT settings from configuration with fallback to environment variable
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

### 4. Updated appsettings.json

**File**: `node-api/appsettings.json`

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

### 5. Fixed Tests

**File**: `Tests/MqttClientProviderTests.cs`

**Before** (manipulating environment variables):
```csharp
Environment.SetEnvironmentVariable("MQTT_WRITER_PASSWORD", "test-password");
```

**After** (mock configuration):
```csharp
var mqttSettings = Options.Create(new MqttSettings
{
    Password = "test-password",
    // ...other settings
});
var provider = new MqttClientProvider(logger, mqttSettings);
```

### 6. Created Documentation

**File**: `docs/MQTT_CONFIGURATION.md`

Comprehensive guide covering:
- Setup instructions for User Secrets
- Configuration hierarchy
- Testing patterns
- Troubleshooting
- Migration guide

## Configuration Hierarchy

1. **appsettings.json** - Defaults (password = null)
2. **appsettings.Development.json** - Dev overrides
3. **User Secrets** - Secure local development ? **Recommended**
4. **Environment Variables** - Production fallback
5. **Command-line** - Runtime overrides

## Setting Up User Secrets

### For Development

```bash
cd node-api
dotnet user-secrets set "MqttSettings:Password" "your-password-here"
dotnet user-secrets list  # Verify
```

### For Production/CI/CD

```bash
export MQTT_WRITER_PASSWORD="your-password"
```

Or in Docker:
```yaml
environment:
  - MQTT_WRITER_PASSWORD=your-password
```

## Benefits

? **Follows .NET Best Practices** - Uses standard configuration system  
? **Secure** - Secrets not committed to Git  
? **Testable** - Easy to mock with `IOptions<T>`  
? **Flexible** - Multiple configuration sources  
? **Type-Safe** - Strong typing via `MqttSettings` class  
? **Backward Compatible** - Still supports environment variables  
? **Well Documented** - Clear setup and troubleshooting guide  

## Testing

### Before (Failing Tests)
```
? System.InvalidOperationException: MQTT_WRITER_PASSWORD environment variable is not set
```

### After (Passing Tests)
```
? All tests pass with mock configuration
? No environment variable manipulation needed
? Clean, isolated test setup
```

### Test Pattern

```csharp
var mqttSettings = Options.Create(new MqttSettings
{
    Host = "test-broker.example.com",
    Port = 1883,
    Username = "test-user",
    Password = "test-password",
    ClientIdPrefix = "test",
    AutoReconnectDelaySeconds = 5,
    CleanSession = true
});

var service = new MqttClientProvider(logger, mqttSettings);
```

## Files Modified

### Created
- `node-api/Configuration/MqttSettings.cs` - Configuration class
- `docs/MQTT_CONFIGURATION.md` - Documentation

### Modified
- `node-api/Services/MqttClientProvider.cs` - Use `IOptions<MqttSettings>`
- `node-api/Services/SystemMetricsPublisher.cs` - Use `IOptions<MqttSettings>`
- `node-api/Services/MqttStateSubscriber.cs` - Use `IOptions<MqttSettings>`
- `node-api/Program.cs` - Register `MqttSettings` configuration
- `node-api/appsettings.json` - Add `MqttSettings` section
- `Tests/MqttClientProviderTests.cs` - Use mock configuration instead of env vars

## Migration Path

### For Developers

1. **Set User Secrets** (one-time):
   ```bash
   cd node-api
   dotnet user-secrets set "MqttSettings:Password" "your-password"
   ```

2. **Run tests**:
   ```bash
   cd Tests
   dotnet test
   ```
   ? Tests should now pass without environment variable manipulation

### For CI/CD

**No changes required** - environment variable fallback still works:
```yaml
env:
  MQTT_WRITER_PASSWORD: ${{ secrets.MQTT_PASSWORD }}
```

### For Production Deployment

**No changes required** - existing environment variable approach still works:
```bash
docker run -e MQTT_WRITER_PASSWORD="$MQTT_PASSWORD" ...
```

Or update to use configuration:
```yaml
MqttSettings:
  Password: "${MQTT_PASSWORD}"
```

## Troubleshooting

### Tests Still Failing?

1. **Check test is using mock configuration**:
   ```csharp
   var mqttSettings = Options.Create(new MqttSettings { Password = "test" });
   ```

2. **Verify NuGet packages**:
   ```bash
   dotnet restore
   ```

3. **Rebuild**:
   ```bash
   dotnet clean
   dotnet build
   ```

### User Secrets Not Working?

1. **Verify User Secrets are set**:
   ```bash
   cd node-api
   dotnet user-secrets list
   ```

2. **Check environment**:
   User Secrets only work in **Development** environment
   ```bash
   echo $ASPNETCORE_ENVIRONMENT  # Should be "Development"
   ```

3. **Check UserSecretsId**:
   Verify `.csproj` has the correct ID:
   ```xml
   <UserSecretsId>6471d754-eb95-4d79-9bc3-fc026e9852bf</UserSecretsId>
   ```

### Production Deployment Issues?

Use environment variable as fallback:
```bash
export MQTT_WRITER_PASSWORD="your-password"
dotnet run
```

## Next Steps

? **Immediate** - Tests are now passing  
? **Developers** - Set up User Secrets for local development  
? **CI/CD** - No changes needed (env var fallback works)  
? **Production** - Consider migrating to configuration-based secrets management

## See Also

- `docs/DATABASE_CONFIGURATION.md` - Similar pattern for database configuration
- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
