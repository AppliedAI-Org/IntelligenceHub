using System.Diagnostics;

namespace IntelligenceHub.Host.Logging
{
    /// <summary>
    /// Middleware for logging request and response details.
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        /// <summary>
        /// Constructor for the logging middleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance to log request and response details.</param>
        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Logs request and response details.
        /// </summary>
        /// <param name="context">The context of the request or response.</param>
        /// <returns>An awaitable task.</returns>
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