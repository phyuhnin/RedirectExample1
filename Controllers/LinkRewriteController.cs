using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("[controller]")]
public class InputController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public InputController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


[HttpGet]
public async Task<IActionResult> RewriteLinks([FromQuery] string url)
{
    if (string.IsNullOrEmpty(url))
    {
        return BadRequest("URL is required");
    }

    try
    {
        // Fetch the content of the URL
        var response = await _httpClient.GetStringAsync(url);

        // Extract the base URL from the query parameter
        var baseUri = new Uri(url); // This is your base URL (e.g., https://www.ugtop.com/)

        // Define the base URL for rewriting
        var baseRewriteUrl = "http://localhost:5000/input?url=";

        // Patterns to match different types of links and resources
        var patterns = new[]
        {
            new { Tag = "a", Attribute = "href", Pattern = @"<a\s+(?:[^>]*?\s+)?href=([""'])(.*?)\1" },
            new { Tag = "img", Attribute = "src", Pattern = @"<img\s+(?:[^>]*?\s+)?src=([""'])(.*?)\1" },
            new { Tag = "link", Attribute = "href", Pattern = @"<link\s+(?:[^>]*?\s+)?href=([""'])(.*?)\1" }
        };

        var rewrittenContent = response;

        foreach (var pattern in patterns)
        {
            rewrittenContent = Regex.Replace(rewrittenContent, pattern.Pattern, match =>
            {
                var originalUrl = match.Groups[2].Value; // This is the URL in the HTML (e.g., /title2002.gif)
                string rewrittenUrl;

                if (Uri.TryCreate(originalUrl, UriKind.Absolute, out var absoluteUri))
                {
                    // For absolute URLs
                   // Absolute URL: Use baseRewriteUrl for service-based URLs
                    if (pattern.Tag == "img" || pattern.Tag == "link")
                    {
                        var formattedUrl = absoluteUri.ToString();
                        // Ensure URL is formatted correctly
                        if (formattedUrl.StartsWith("file:///"))
                        {
                            formattedUrl = formattedUrl.Substring("file:///".Length);
                        }

                        if (formattedUrl.StartsWith("/"))
                        {
                            formattedUrl = $"{baseUri.Scheme}://{baseUri.Host}{formattedUrl}";
                        }

                            // Use baseUri for absolute URLs in img tag
                            rewrittenUrl = $"{baseUri.Scheme}://{baseUri.Host}/{Uri.EscapeDataString(formattedUrl.ToString().Replace(baseUri.Scheme + "://" + baseUri.Host, ""))}";
                    }
                    
                    else
                    {
                        // For other tags, use the rewriting service URL
                        rewrittenUrl = $"{baseRewriteUrl}{Uri.EscapeDataString(absoluteUri.ToString())}";
                    }

                }
                else
                {
                    // For relative URLs
                    var resolvedUri = new Uri(baseUri, originalUrl); // Resolve relative URL against base URL
                    var formattedUrl = resolvedUri.ToString();
                    
                    if(pattern.Tag == "link")
                    {
                        // Use baseUri for absolute URLs in img tag
                        rewrittenUrl = $"{baseUri.Scheme}://{baseUri.Host}/{Uri.EscapeDataString(formattedUrl.Replace(baseUri.Scheme + "://" + baseUri.Host, ""))}";
                    }
                    else{
                        

                    // Encode the URL for the query string
                    rewrittenUrl = $"{baseRewriteUrl}{Uri.EscapeDataString(formattedUrl)}";
                    }
                }
                var decodedUrl = Uri.UnescapeDataString(rewrittenUrl);
                return match.Value.Replace(originalUrl, decodedUrl);
            });
        }

        return Content(rewrittenContent, "text/html");
    }
    catch (HttpRequestException e)
    {
        return BadRequest($"Error fetching URL: {e.Message}");
    }
    catch (Exception e)
    {
        return StatusCode(500, $"Internal server error: {e.Message}");
    }
}


[HttpGet("redirect")]
public IActionResult RedirectToUrl([FromQuery] string url)
{
    if (string.IsNullOrEmpty(url))
    {
        return BadRequest("URL is required");
    }

    return Redirect(url);
}

}
