using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MiniBrowser.Utils;
using NLog;
using SD = System.Drawing;

namespace MiniBrowser
{
    public partial class EdgeView : UserControl
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private readonly List<CoreWebView2Frame> webViewFrames = new List<CoreWebView2Frame>();
        private CoreWebView2Environment environment;
        private bool isNavigating = false;

        public EdgeView()
        {
            try
            {
                InitializeComponent();
                InitializeAddressBar();
                InitializeAsync();
                AttachControlEventHandlers(webView);

                Loaded += async (s, e) =>
                {
                    // Dirty Hack: this forces a Resize event on the webView. If we do not do that
                    // when the window is hidden then shown again, the web view is wrongly positioned:
                    // it seems it is drawn relatively to the screen and not its parent grid...
                    // By forcing a size change, the web view is correctly drawn relatively to its
                    // parent control.
                    rightFiller.Width = rightFiller.Width == 1.0 ? 0.0 : 1.0;

                    // Only now can we navigate to the home page
                    // We should not use the Source property on the Xaml side (see https://github.com/MicrosoftEdge/WebView2Feedback/issues/1778#issuecomment-934072596)
                    await NavigateTo(Constants.HomePage);
                };
            }
            catch (Exception ex)
            {
                HandleError("Constructor", ex);
            }
        }

        public Action<string> SetTitleAction { get; set; }

        private void InitializeAddressBar()
        {
            try
            {
                addressBar.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    if (addressBar.IsKeyboardFocusWithin) return;

                    // If the textbox is not yet focused, give it the focus and
                    // stop further processing of this click event.
                    _ = addressBar.Focus();
                    e.Handled = true;
                };

                addressBar.GotKeyboardFocus += (s, e) => addressBar.SelectAll();
                addressBar.MouseDoubleClick += (s, e) => addressBar.SelectAll();
            }
            catch (Exception ex)
            {
                HandleError(nameof(InitializeAddressBar), ex);
            }
        }

        private async void InitializeAsync()
        {
            try
            {
                // See https://github.com/MicrosoftEdge/WebView2Feedback/issues/271
                var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiniBrowser");
                environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                await webView.EnsureCoreWebView2Async(environment);
            }
            catch (Exception ex)
            {
                HandleError(nameof(InitializeAsync), ex);
            }
        }

        private void AttachControlEventHandlers(WebView2 control)
        {
            control.NavigationStarting += OnNavigationStarting;
            control.NavigationCompleted += OnNavigationCompleted;
            control.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            log.Trace($"{e.NavigationId} - Navigation Started. Uri: {e.Uri}, User Initiated: {e.IsUserInitiated}, Redirected: {e.IsRedirected}");
            isNavigating = true;
            RequeryCommands();
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            log.Trace($"{e.NavigationId} - Navigation Completed. Status: {e.HttpStatusCode}");
            isNavigating = false;
            RequeryCommands();
        }

        private void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                HandleError($"WebView2 creation failed: {e.InitializationException.Message}", e.InitializationException);
                return;
            }

            webView.CoreWebView2.DocumentTitleChanged += OnWebViewDocumentTitleChanged;
            webView.CoreWebView2.FrameCreated += OnWebViewHandleIFrames;
            SetDefaultDownloadDialogPosition();
        }

        void OnWebViewDocumentTitleChanged(object sender, object e) =>
            SetTitleAction?.Invoke(webView.CoreWebView2.DocumentTitle);

        private void OnWebViewHandleIFrames(object sender, CoreWebView2FrameCreatedEventArgs args)
        {
            webViewFrames.Add(args.Frame);
            args.Frame.Destroyed += (frameDestroyedSender, frameDestroyedArgs) =>
            {
                var frameToRemove = webViewFrames.SingleOrDefault(r => r.IsDestroyed() == 1);
                if (frameToRemove != null)
                    _ = webViewFrames.Remove(frameToRemove);
            };
        }

        private void SetDefaultDownloadDialogPosition()
        {
            try
            {
                const int defaultMarginX = 75, defaultMarginY = 0;
                var cornerAlignment = CoreWebView2DefaultDownloadDialogCornerAlignment.TopLeft;
                var margin = new SD.Point(defaultMarginX, defaultMarginY);
                webView.CoreWebView2.DefaultDownloadDialogCornerAlignment = cornerAlignment;
                webView.CoreWebView2.DefaultDownloadDialogMargin = margin;
            }
            catch (NotImplementedException) { }
        }

        private static void RequeryCommands() => CommandManager.InvalidateRequerySuggested();

        private static void HandleError(string message, Exception exception = null) =>
            log.Error(exception, $"{nameof(EdgeView)} - {message}");

        private void GoToPageCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = webView != null && !isNavigating;

        private async void GoToPageCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            log.Trace($"Go to '{e.Parameter ?? "<null>"}'");
            try
            {
                await NavigateTo((string)e.Parameter);
            }
            catch (Exception ex)
            {
                HandleError($"{nameof(GoToPageCmdExecuted)}:", ex);
            }
        }

        private async Task NavigateTo(string url)
        {
            await webView.EnsureCoreWebView2Async();

            var uri = UriHelper.MakeUri(url);

            // Setting webView.Source will not trigger a navigation if the Source is the same
            // as the previous Source. CoreWebView.Navigate() will always trigger a navigation.
            webView.CoreWebView2.Navigate(uri.ToString());
        }
    }
}
