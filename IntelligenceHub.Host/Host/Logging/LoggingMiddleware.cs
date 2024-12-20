using System.Diagnostics;

namespace IntelligenceHub.Host.Logging
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Log request details
            _logger.LogInformation("Received '{Method}' request for '{Path}' from '{IPAddress}'",
                context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);

            await _next(context);  // Call the next middleware

            stopwatch.Stop();

            // Log response status and duration
            _logger.LogInformation("Responded with status '{StatusCode}' in '{ElapsedMilliseconds}ms'",
                context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}