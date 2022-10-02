using System;

namespace WebBrowserExtension.Utils
{
    internal static class UriHelper
    {
        public static Uri MakeUri(string rawUrl)
        {
            if (string.IsNullOrEmpty(rawUrl))
                return new Uri("about:blank");

            if (Uri.IsWellFormedUriString(rawUrl, UriKind.Absolute))
                return new Uri(rawUrl);

            // An invalid URI contains a dot and no spaces, try tacking http:// on the front.
            if (!rawUrl.Contains(" ") && rawUrl.Contains(".")) 
                return new Uri("http://" + rawUrl);
            
            // Otherwise treat it as a web search.
            return new Uri("https://bing.com/search?q=" +
                string.Join("+", Uri.EscapeDataString(rawUrl).Split(new string[] { "%20" }, StringSplitOptions.RemoveEmptyEntries)));
        }
    }
}
