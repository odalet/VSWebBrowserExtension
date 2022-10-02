using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Serilog;
using WebBrowserExtension.Utils;

namespace WebBrowserExtension
{
    // Commands
    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Commands")]
    partial class WebBrowserWindowControl
    {
        private async Task<T> GetCoreWebView2ValueAsync<T>(Func<CoreWebView2, T> func)
        {
            await webView.EnsureCoreWebView2Async();
            return func(webView.CoreWebView2);
        }

        // BrowseBack
        private /*async*/ void BrowseBackCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = !isNavigating && (webView?.CoreWebView2?.CanGoBack ?? false);

        private void BrowseBackCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            Log.Verbose($"Navigating Backward");
            try
            {
                webView.CoreWebView2.GoBack();
            }
            catch (Exception ex)
            {
                HandleError(nameof(GoToPageCmdExecuted), ex);
            }
        }

        // BrowseForward
        private void BrowseForwardCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = !isNavigating && (webView?.CoreWebView2?.CanGoForward ?? false);

        private void BrowseForwardCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            Log.Verbose($"Navigating Forward");
            try
            {
                webView.CoreWebView2.GoForward();
            }
            catch (Exception ex)
            {
                HandleError(nameof(GoToPageCmdExecuted), ex);
            }
        }

        // Refresh
        private void RefreshCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = !isNavigating && webView?.CoreWebView2 != null;

        private void RefreshCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            Log.Verbose($"Reloading Current Page");
            try
            {
                webView.CoreWebView2.Reload();
            }
            catch (Exception ex)
            {
                HandleError(nameof(GoToPageCmdExecuted), ex);
            }
        }

        // BrowseHome
        private void BrowseHomeCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = !isNavigating && webView?.CoreWebView2 != null;

        private async void BrowseHomeCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            Log.Verbose($"Navigating to Home Page");
            try
            {
                await NavigateToHomeAsync();
            }
            catch (Exception ex)
            {
                HandleError(nameof(GoToPageCmdExecuted), ex);
            }
        }

        // GoToPage
        private void GoToPageCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = !isNavigating && webView?.CoreWebView2 != null;

        private async void GoToPageCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            Log.Verbose($"Navigating to '{e.Parameter ?? "<null>"}'");
            try
            {
                var uri = UriHelper.MakeUri((string)e.Parameter);
                await NavigateToAsync(uri);
            }
            catch (Exception ex)
            {
                HandleError(nameof(GoToPageCmdExecuted), ex);
            }
        }
    }
}
