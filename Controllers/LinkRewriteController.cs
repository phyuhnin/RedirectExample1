using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LinkRewritingExample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LinkRewriteController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public LinkRewriteController(HttpClient httpClient)
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
                // URLの内容を取得
                var response = await _httpClient.GetStringAsync(url);

                // リンクを抽出し、書き換える正規表現パターン
                var pattern = @"<a\s+(?:[^>]*?\s+)?href=([""'])(.*?)\1";
                var rewrittenContent = Regex.Replace(response, pattern, match =>
                {
                    var originalUrl = match.Groups[2].Value;
                    var rewrittenUrl = originalUrl;

                    if (Uri.IsWellFormedUriString(originalUrl, UriKind.Absolute))
                    {
                        rewrittenUrl = $"/home?url={Uri.EscapeDataString(originalUrl)}";
                    }
                    else if (Uri.IsWellFormedUriString(originalUrl, UriKind.Relative))
                    {
                        // 相対URLの場合、元のURLのベースを使用して絶対URLに変換する
                        var baseUri = new Uri(url);
                        var absoluteUri = new Uri(baseUri, originalUrl);
                        rewrittenUrl = $"/home?url={Uri.EscapeDataString(absoluteUri.ToString())}";
                    }

                    return match.Value.Replace(originalUrl, rewrittenUrl);
                });

                return Content(rewrittenContent, "text/html");
            }
            catch (HttpRequestException e)
            {
                return BadRequest($"Error fetching URL: {e.Message}");
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
}
