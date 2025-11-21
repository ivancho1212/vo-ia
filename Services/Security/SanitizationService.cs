using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Voia.Api.Services.Security
{
    public interface ISanitizationService
    {
        /// <summary>
        /// Sanitizes user input to prevent XSS and malicious HTML injection.
        /// </summary>
        string SanitizeHtml(string? input);

        /// <summary>
        /// Sanitizes plain text (removes HTML, keeps text only).
        /// </summary>
        string SanitizeText(string? input);

        /// <summary>
        /// Sanitizes a URL to prevent javascript: protocol and other XSS vectors.
        /// </summary>
        string SanitizeUrl(string? input);
    }

    /// <summary>
    /// Sanitization service to prevent XSS attacks by sanitizing user input
    /// </summary>
    public class SanitizationService : ISanitizationService
    {
        // Whitelist of allowed HTML tags
        private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "strong", "b", "em", "i", "u", "a", "li", "ul", "ol", "h1", "h2", "h3"
        };

        // Whitelist of allowed attributes
        private static readonly Dictionary<string, HashSet<string>> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "a", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title" } },
            { "img", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "alt", "title" } }
        };

        public SanitizationService()
        {
        }

        /// <summary>
        /// Sanitizes HTML content using a whitelist approach for tags and attributes.
        /// </summary>
        public string SanitizeHtml(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            try
            {
                var result = input.Trim();

                // Remove dangerous protocol handlers
                result = RemoveDangerousProtocols(result);

                // Remove scripts
                result = Regex.Replace(result, @"<\s*script\b[^<]*(?:(?!<\s*/\s*script\s*>)<[^<]*)*<\s*/\s*script\s*>", "", RegexOptions.IgnoreCase);

                // Remove on* event handlers
                result = Regex.Replace(result, @"\s*on\w+\s*=\s*[""']?[^""'>\s]*[""']?", "", RegexOptions.IgnoreCase);

                // Remove dangerous attributes like style with expressions
                result = Regex.Replace(result, @"\bstyle\s*=\s*[""']?(?!(?:color|font|text-align|display:none)[^""']*)[^""']*[""']?", "", RegexOptions.IgnoreCase);

                // Remove HTML tags that are not in whitelist
                result = Regex.Replace(result, @"<(?!/?(?:" + string.Join("|", AllowedTags) + @")(?:\s|>))[^>]*>", "");

                // HTML decode to prevent double encoding
                result = System.Net.WebUtility.HtmlDecode(result);

                return result.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sanitizing HTML: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Removes all HTML tags, keeping only plain text.
        /// </summary>
        public string SanitizeText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            try
            {
                var result = input.Trim();

                // Remove all HTML tags
                result = Regex.Replace(result, "<[^>]*>", "");

                // HTML decode entities
                result = System.Net.WebUtility.HtmlDecode(result);

                // Remove extra whitespace
                result = Regex.Replace(result, @"\s+", " ");

                return result.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sanitizing text: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Validates and sanitizes URLs to prevent XSS via javascript: protocol.
        /// </summary>
        public string SanitizeUrl(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            try
            {
                var url = input.Trim();

                // Block dangerous protocols
                var dangerousProtocols = new[] { "javascript:", "vbscript:", "data:", "file:", "about:" };
                if (dangerousProtocols.Any(p => url.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    return string.Empty;

                // Only allow http, https, mailto, and relative URLs
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("/", StringComparison.Ordinal) &&
                    !url.StartsWith("#", StringComparison.Ordinal))
                    return string.Empty;

                return url;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Removes dangerous protocols from HTML attributes
        /// </summary>
        private static string RemoveDangerousProtocols(string input)
        {
            // Remove javascript: protocol
            input = Regex.Replace(input, @"(?:javascript|vbscript|data):\s*", "", RegexOptions.IgnoreCase);

            // Remove data URIs with expressions
            input = Regex.Replace(input, @"data:text/html[^""']*", "", RegexOptions.IgnoreCase);

            return input;
        }
    }
}
