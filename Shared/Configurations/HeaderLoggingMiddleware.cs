public class HeaderLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HeaderLoggingMiddleware> _logger;

    public HeaderLoggingMiddleware(RequestDelegate next, ILogger<HeaderLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation($"Request Path: {context.Request.Path}");
        _logger.LogInformation("Request Headers:");
        foreach (var header in context.Request.Headers)
        {
            _logger.LogInformation($"{header.Key}: {header.Value}");
        }

        await _next(context);
    }
}