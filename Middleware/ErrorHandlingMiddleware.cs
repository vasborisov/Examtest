using System.Net;
using System.Text.Json;

namespace proekt_za_6ca.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();

            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    errorResponse.Message = "Invalid request data provided.";
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case UnauthorizedAccessException:
                    errorResponse.Message = "You are not authorized to perform this action.";
                    errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;

                case FileNotFoundException:
                    errorResponse.Message = "The requested resource was not found.";
                    errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case TimeoutException:
                    errorResponse.Message = "The request timed out. Please try again.";
                    errorResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    break;

                default:
                    errorResponse.Message = _env.IsDevelopment() 
                        ? exception.Message 
                        : "An error occurred while processing your request.";
                    errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            if (_env.IsDevelopment())
            {
                errorResponse.Details = exception.StackTrace;
            }

            // If it's an AJAX request, return JSON
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                context.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                var jsonResponse = JsonSerializer.Serialize(errorResponse);
                await response.WriteAsync(jsonResponse);
            }
            else
            {
                // Redirect to error page for regular requests
                context.Items["ErrorMessage"] = errorResponse.Message;
                context.Items["StatusCode"] = errorResponse.StatusCode;
                
                response.Redirect("/Home/Error");
            }
        }
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = "An error occurred";
        public int StatusCode { get; set; }
        public string? Details { get; set; }
    }
}