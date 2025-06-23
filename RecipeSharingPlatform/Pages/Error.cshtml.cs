using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RecipeSharingPlatform.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string ErrorTitle { get; set; } = string.Empty;
        public string ErrorDescription { get; set; } = string.Empty;

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int? statusCode = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            StatusCode = statusCode ?? 500;

            // Set error details based on status code
            switch (StatusCode)
            {
                case 404:
                    ErrorTitle = "Page Not Found";
                    StatusMessage = "404 - Not Found";
                    ErrorDescription = "The page you're looking for doesn't exist. It might have been moved, deleted, or you entered the wrong URL.";
                    break;
                case 403:
                    ErrorTitle = "Access Forbidden";
                    StatusMessage = "403 - Forbidden";
                    ErrorDescription = "You don't have permission to access this resource.";
                    break;
                case 401:
                    ErrorTitle = "Unauthorized";
                    StatusMessage = "401 - Unauthorized";
                    ErrorDescription = "You need to be logged in to access this page.";
                    break;
                case 500:
                default:
                    ErrorTitle = "Internal Server Error";
                    StatusMessage = "500 - Server Error";
                    ErrorDescription = "Something went wrong on our end. We're working to fix it.";
                    break;
            }

            _logger.LogWarning("Error page displayed: {StatusCode} - {RequestId}", StatusCode, RequestId);
        }
    }
}