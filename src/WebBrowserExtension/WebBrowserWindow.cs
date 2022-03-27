using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace WebBrowserExtension
{
    [Guid("8ab2cef3-7c52-4e4a-8d07-1dd7f9f90a1c")]
    public class WebBrowserWindow : ToolWindowPane
    {
        private readonly WebBrowserWindowControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebBrowserWindow"/> class.
        /// </summary>
        public WebBrowserWindow() : base(null)
        {
            Caption = "Web Browser";
            control = new WebBrowserWindowControl { SetTitleAction = x => Caption = x };
            Content = control;
        }
    }
}
