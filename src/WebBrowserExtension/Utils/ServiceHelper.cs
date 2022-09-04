using System;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace WebBrowserExtension.Utils
{
    internal static class ServiceHelper
    {
        private static readonly ILogger log = Log.Logger;

        public static T GetService<T>(this IServiceProvider sp) where T : class
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var componentModel = sp.GetService<SComponentModel, IComponentModel>();
                var result = componentModel.GetService<T>();
                Assumes.Present(result);
                return result;
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{nameof(WebBrowserOptionsPage)}: Could not retrieve an instance of Service {typeof(T)}");
                throw;
            }
        }
    }
}
