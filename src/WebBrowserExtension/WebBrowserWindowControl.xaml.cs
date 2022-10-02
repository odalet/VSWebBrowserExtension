using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using WebBrowserExtension.Settings;
using WebBrowserExtension.Utils;
using SD = System.Drawing;

namespace WebBrowserExtension
{
    // Inspiration:
    // * https://github.com/MicrosoftEdge/WebView2Samples/blob/master/GettingStartedGuides/WPF_GettingStarted/MainWindow.xaml.cs
    // * https://github.com/MicrosoftEdge/WebView2Samples/blob/master/SampleApps/WebView2WpfBrowser/README.md
    public partial class WebBrowserWindowControl : UserControl
    {
        private readonly ILogger log = Log.Logger;
        private readonly List<CoreWebView2Frame> webViewFrames = new List<CoreWebView2Frame>();
        private CoreWebView2Environment environment;
        private bool isNavigating = false;
        private bool isFirstTimeLoad = true;

        public WebBrowserWindowControl()
        {
            try
            {
                InitializeComponent();
                InitializeAddressBar();
                InitializeAsync();
                AttachControlEventHandlers(webView);

                Loaded += WebBrowserWindowControl_Loaded;
                Unloaded += WebBrowserWindowControl_Unloaded;
            }
            catch (Exception ex)
            {
                HandleError("Constructor", ex);
            }
        }

        ////public Uri HomePageUri { get; set; }
        public IServiceProvider Services { get; set; }
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
                var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VS2022WebBrowserExtension");
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
            Log.Verbose($"{e.NavigationId} - Navigation Started. Uri: {e.Uri}, User Initiated: {e.IsUserInitiated}, Redirected: {e.IsRedirected}");
            isNavigating = true;
            RequeryCommands();
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            var status = e.HttpStatusCode.ToString();
            if (e.WebErrorStatus != CoreWebView2WebErrorStatus.Unknown)
                status += $" ({e.WebErrorStatus})";

            Log.Verbose($"{e.NavigationId} - Navigation Completed. Status: {status}");
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

        private void OnWebViewDocumentTitleChanged(object sender, object e) =>
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
            catch (NotImplementedException ex)
            {
                Log.Verbose(ex, $"In {nameof(SetDefaultDownloadDialogPosition)}, encountered {nameof(NotImplementedException)}: {ex.Message}");
            }
        }

        private static void RequeryCommands() => CommandManager.InvalidateRequerySuggested();

        private static void HandleError(string message, Exception exception = null) =>
            Log.Error(exception, $"{nameof(WebBrowserWindowControl)} - {message}");

        private void WebBrowserWindowControl_Unloaded(object sender, RoutedEventArgs e) =>
            Log.Verbose("Unloaded Event Handler");

        private async void WebBrowserWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Verbose("Loaded Event Handler");
            try
            {
                // Dirty Hack: this forces a Resize event on the webView. If we do not do that
                // when the window is hidden then shown again, the web view is wrongly positioned:
                // it seems it is drawn relatively to the screen and not its parent grid...
                // By forcing a size change, the web view is correctly drawn relatively to its
                // parent control.
                rightFiller.Width = rightFiller.Width == 1.0 ? 0.0 : 1.0;

                if (isFirstTimeLoad)
                {
                    Log.Verbose($"First time Load: navigate to Home Page");
                    isFirstTimeLoad = false;
                    await NavigateToHomeAsync();
                }
            }
            catch (Exception ex)
            {
                HandleError("Inside Loaded", ex);
            }
        }

        private async Task NavigateToAsync(Uri uri)
        {
            await webView.EnsureCoreWebView2Async();

            // Setting webView.Source will not trigger a navigation if the Source is the same
            // as the previous Source. CoreWebView.Navigate() will always trigger a navigation.
            webView.CoreWebView2.Navigate(uri.ToString());

            Log.Verbose($"Initiated Navigation to '{uri}'");
        }

        private async Task NavigateToHomeAsync()
        {
            try
            {
                var settings = GetService<IWebBrowserSettings>();
                var homepage = settings.GetHomePageUri();
                Log.Verbose($"Home Page Uri is '{homepage}'");
                await NavigateToAsync(homepage);
            }
            catch (Exception ex)
            {
                HandleError("Failed to navigate to Home Uri", ex);
            }
        }

        private T GetService<T>() where T : class => Services.GetService<T>();
    }
}