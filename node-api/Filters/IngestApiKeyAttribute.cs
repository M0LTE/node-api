using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace node_api.Filters;

/// <summary>
/// Requires a valid <c>X-Api-Key</c> header matching config <c>Ingest:PortFrequencyApiKey</c>. Secure by
/// default: if no key is configured the endpoint rejects everything. Constant-time comparison.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IngestApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configured = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Ingest:PortMetadataApiKey"];
        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(configured) || !FixedTimeEquals(provided, configured))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
