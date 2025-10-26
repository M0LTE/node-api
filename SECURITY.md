# Security Configuration Guide

This document describes the security features implemented in node-api and how to configure them for production use.

## Overview

The node-api service has multiple layers of security protection:

1. **UDP Security**: IP filtering, rate limiting, and input validation
2. **API Security**: Rate limiting, security headers, and CORS control
3. **Input Validation**: Comprehensive validation of all UDP datagrams and API inputs
4. **Cursor Security**: Strict validation of pagination cursors
5. **Logging Security**: Sanitized logs to prevent information disclosure

## Configuration

All security settings are configured in `appsettings.json` under the `Security` section:

```json
{
  "Security": {
    "UdpAllowedSourceNetworks": [
      "0.0.0.0/0"
    ],
    "ApiRateLimiting": {
      "Enabled": true,
      "PermitLimit": 100,
      "Window": 60
    },
    "UdpRateLimiting": {
      "Enabled": true,
      "MaxPacketsPerSecondPerIp": 10,
      "MaxTotalPacketsPerSecond": 1000
    },
    "CorsAllowedOrigins": [
      "*"
    ]
  }
}
```

## UDP Security

### IP Filtering

Control which IP addresses can send UDP datagrams to the service.

**Configuration:**
```json
"UdpAllowedSourceNetworks": [
  "192.168.1.0/24",
  "10.0.0.0/8"
]
```

**Options:**
- Use CIDR notation (e.g., `192.168.1.0/24`)
- `0.0.0.0/0` allows all IPs (default, for development)
- Multiple networks can be specified
- Invalid IPs are logged and published to MQTT topic `in/udp/errored/blockedip`

**Production Recommendation:**
Restrict to only known trusted networks.

### UDP Rate Limiting

Prevents DoS attacks by limiting the number of UDP packets processed per second.

**Configuration:**
```json
"UdpRateLimiting": {
  "Enabled": true,
  "MaxPacketsPerSecondPerIp": 10,
  "MaxTotalPacketsPerSecond": 1000
}
```

**Parameters:**
- `Enabled`: Set to `true` to enable rate limiting
- `MaxPacketsPerSecondPerIp`: Maximum packets per second from a single IP
- `MaxTotalPacketsPerSecond`: Global maximum across all IPs

**Rate limited packets are:**
- Logged with a warning
- Published to MQTT topic `in/udp/errored/ratelimit`
- Not processed further

### UDP Input Validation

**Automatic protections:**
- Maximum datagram size: 65,507 bytes (UDP maximum)
- All datagrams are validated against FluentValidation rules
- Oversized datagrams are rejected and logged

## API Security

### API Rate Limiting

Prevents API abuse by limiting requests per IP address.

**Configuration:**
```json
"ApiRateLimiting": {
  "Enabled": true,
  "PermitLimit": 100,
  "Window": 60
}
```

**Parameters:**
- `Enabled`: Set to `true` to enable
- `PermitLimit`: Maximum requests per window
- `Window`: Time window in seconds

**When limit exceeded:**
- HTTP 429 (Too Many Requests) response
- `Retry-After` header indicates wait time
- Separate limits per IP address

### Security Headers

The following security headers are automatically added to all responses:

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Frame-Options` | `DENY` | Prevent clickjacking |
| `X-Content-Type-Options` | `nosniff` | Prevent MIME sniffing |
| `X-XSS-Protection` | `1; mode=block` | Enable XSS protection |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Control referrer information |
| `Content-Security-Policy` | (See below) | Prevent XSS and injection attacks |
| `Permissions-Policy` | (Restrictive) | Restrict browser features |

**Content Security Policy (CSP):**
```
default-src 'self';
script-src 'self' 'unsafe-inline' https://unpkg.com;
style-src 'self' 'unsafe-inline';
connect-src 'self' wss://node-api.packet.oarc.uk:443;
img-src 'self' data:;
font-src 'self';
object-src 'none';
base-uri 'self';
form-action 'self';
```

### CORS Configuration

Control which origins can access the API.

**Configuration:**
```json
"CorsAllowedOrigins": [
  "https://myapp.example.com",
  "https://another.example.com"
]
```

**Options:**
- `["*"]`: Allow any origin (development only)
- Specific URLs: Production mode with credentials support

**Production Recommendation:**
Always specify exact origins in production.

### Request Size Limits

**Maximum request body size: 10 MB**

This prevents DoS attacks via large request bodies.

## Cursor Security

Pagination cursors are validated to prevent injection attacks:

- Maximum length: 200 characters
- Must be valid Base64
- Decoded content limited to 150 characters
- Timestamp must be valid ISO 8601 format
- ID must be a positive integer
- Invalid cursors return HTTP 400

## Logging Security

To prevent information disclosure:

- UDP datagram content is not logged in production
- Only metadata (IP, size, type) is logged
- Log content is truncated for unknown types
- No sensitive data in log messages

## Monitoring

Security events are published to MQTT topics:

| Topic | Event |
|-------|-------|
| `in/udp/errored/blockedip` | Blocked IP attempts |
| `in/udp/errored/ratelimit` | Rate limited packets |
| `in/udp/errored/validation` | Validation failures |
| `in/udp/errored/badjson` | JSON parse failures |
| `in/udp/errored/badtype` | Unknown datagram types |

## Production Deployment Checklist

- [ ] Configure `UdpAllowedSourceNetworks` to trusted IPs only
- [ ] Set appropriate rate limits based on expected traffic
- [ ] Configure `CorsAllowedOrigins` with specific domains
- [ ] Review and adjust `PermitLimit` for API rate limiting
- [ ] Enable HTTPS and uncomment HSTS header in `SecurityHeadersMiddleware`
- [ ] Monitor MQTT error topics for security events
- [ ] Review logs for security warnings
- [ ] Consider adding authentication for sensitive endpoints

## Future Security Enhancements

Potential improvements not yet implemented:

1. **API Authentication**
   - API key authentication
   - JWT token support
   - Role-based access control

2. **Enhanced Monitoring**
   - Security event aggregation
   - Automated alerting
   - IP reputation checking

3. **Additional Headers**
   - HSTS for HTTPS (requires HTTPS deployment)
   - Certificate Transparency (Expect-CT)

## Support

For security concerns or to report vulnerabilities, please contact the repository maintainers through GitHub Issues (mark as security-sensitive).
