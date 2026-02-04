namespace RiskRadar.Api.Middlewares;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var cid = ctx.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(cid))
            cid = Guid.NewGuid().ToString("N");

        ctx.Items[HeaderName] = cid;
        ctx.Response.Headers[HeaderName] = cid;

        await _next(ctx);
    }
}
