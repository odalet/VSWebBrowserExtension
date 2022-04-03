using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
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

        public WebBrowserWindowControl()
        {
            InitializeComponent();
            InitializeAddressBar();
            InitializeAsync();
            AttachControlEventHandlers(webView);

            // Dirty Hack: this forces a Resize event on the webView. If we do not do that
            // when the window is hidden then shown again, the web view is wrongly positioned:
            // it seems it is drawn relatively to the screen and not its parent grid...
            // By forcing a size change, the web view is correctly drawn relatively to its
            // parent control.
            Loaded += (s, e) => rightFiller.Width = rightFiller.Width == 1.0 ? 0.0 : 1.0;
        }

        public Action<string> SetTitleAction { get; set; }

        private void InitializeAddressBar()
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

        private async void InitializeAsync()
        {
            // See https://github.com/MicrosoftEdge/WebView2Feedback/issues/271
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VS2022WebBrowserExtension");
            environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            await webView.EnsureCoreWebView2Async(environment);
        }

        private void AttachControlEventHandlers(WebView2 control)
        {
            control.NavigationStarting += OnNavigationStarting;
            control.NavigationCompleted += OnNavigationCompleted;
            control.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            isNavigating = true;
            RequeryCommands();
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
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
            Log.Error(exception, $"{nameof(WebBrowserWindowControl)} - {message}");

        private void GoToPageCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = webView != null && !isNavigating;

        private async void GoToPageCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async();

            var rawUrl = (string)e.Parameter;
            var uri = UriHelper.MakeUri(rawUrl);

            // Setting webView.Source will not trigger a navigation if the Source is the same
            // as the previous Source. CoreWebView.Navigate() will always trigger a navigation.
            webView.CoreWebView2.Navigate(uri.ToString());
        }
    }
}