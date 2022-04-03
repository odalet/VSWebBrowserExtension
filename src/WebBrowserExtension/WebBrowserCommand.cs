using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace WebBrowserExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class WebBrowserCommand
    {
        public const int CommandId = 0x0100;
        public const int WebBrowserWindowNavigateId = 0x101;
        public const int WebBrowserWindowToolbarID = 0x1000;

        public static readonly Guid CommandSet = new Guid("48c3fadd-683b-4577-8583-c9817b4e5a50");

        private readonly ILogger log = Log.Logger;
        private readonly AsyncPackage package;
        private readonly DTE dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebBrowserCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private WebBrowserCommand(AsyncPackage asyncPackage, DTE dteInstance, OleMenuCommandService commandService)
        {
            package = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            dte = dteInstance ?? throw new ArgumentNullException(nameof(dteInstance));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static WebBrowserCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dte = (DTE)await package.GetServiceAsync(typeof(DTE));
            Instance = new WebBrowserCommand(package, dte, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _ = package.JoinableTaskFactory.RunAsync(async delegate
            {
                var window = await package.ShowToolWindowAsync(typeof(WebBrowserWindow), 0, true, package.DisposalToken) as WebBrowserWindow;
                if (window?.Frame == null)
                    log.Error($"{nameof(WebBrowserCommand)}: Cannot create tool window");
            });
        }
    }
}
