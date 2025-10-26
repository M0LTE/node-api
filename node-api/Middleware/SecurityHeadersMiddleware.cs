namespace node_api.Middleware;

/// <summary>
/// Middleware to add security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context.Response);
        await _next(context);
    }

    private void AddSecurityHeaders(HttpResponse response)
    {
        // Prevent clickjacking attacks
        if (!response.Headers.ContainsKey("X-Frame-Options"))
            response.Headers["X-Frame-Options"] = "DENY";

        // Prevent MIME type sniffing
        if (!response.Headers.ContainsKey("X-Content-Type-Options"))
            response.Headers["X-Content-Type-Options"] = "nosniff";

        // Enable XSS protection
        if (!response.Headers.ContainsKey("X-XSS-Protection"))
            response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer policy - don't leak information
        if (!response.Headers.ContainsKey("Referrer-Policy"))
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy - prevent XSS and data injection attacks
        if (!response.Headers.ContainsKey("Content-Security-Policy"))
        {
            response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://unpkg.com; " + // Allow MQTT library from unpkg
                "style-src 'self' 'unsafe-inline'; " +
                "connect-src 'self' wss://node-api.packet.oarc.uk:443; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";
        }

        // Permissions Policy - restrict browser features
        if (!response.Headers.ContainsKey("Permissions-Policy"))
        {
            response.Headers["Permissions-Policy"] = 
                "geolocation=(), " +
                "microphone=(), " +
                "camera=(), " +
                "payment=(), " +
                "usb=(), " +
                "magnetometer=(), " +
                "accelerometer=(), " +
                "gyroscope=()";
        }

        // HSTS - enforce HTTPS (only add if request is HTTPS)
        // Note: Commented out as this should only be enabled in production with HTTPS
        // if (context.Request.IsHttps && !response.Headers.ContainsKey("Strict-Transport-Security"))
        //     response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
}
