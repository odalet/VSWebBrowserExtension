# Web Browser Extension for VS2022

<!-- https://vsmarketplacebadge.apphb.com/ -->
[![VSMarketplace](https://vsmarketplacebadge.apphb.com/version-short/odalet.webbrowserforvs2022.svg)](https://marketplace.visualstudio.com/items?itemName=odalet.webbrowserforvs2022) [![Installs](https://vsmarketplacebadge.apphb.com/installs-short/odalet.webbrowserforvs2022.svg)](https://marketplace.visualstudio.com/items?itemName=odalet.webbrowserforvs2022) [![Downloads](https://vsmarketplacebadge.apphb.com/downloads-short/odalet.webbrowserforvs2022.svg)](https://marketplace.visualstudio.com/items?itemName=odalet.webbrowserforvs2022)

## About

Starting with Visual Studio 2022, the embedded Web Browser Tool Window was removed. This extension brings it back. You can either download the **vsix** package from the Releases tab here or install it from the Visual Studio Marketplace.

The underlying Web Browser control is [Microsoft Edge WebView2](https://docs.microsoft.com/en-us/microsoft-edge/webview2/).

## History

* [0.3.0](https://github.com/odalet/VSWebBrowserExtension/releases/tag/v0.3.0) - 2022/10/02
  * Upgraded a few dependencies
  * Changed the way the initial page is loaded (no more using `WebView2.Source`)
  * More logging
  * Added _Back_, _Forward_, _Reload_ and _Home_ navigation commands
  * Replaced Text with icons on buttons (thanks to [Syncfusion's Metro Studio](https://www.syncfusion.com/downloads/metrostudio))

* [0.2.0](https://github.com/odalet/VSWebBrowserExtension/releases/tag/v0.2.0) - 2022/09/04
  * Added an Options page allowing to define the default Home Page and the logging level

* [0.1.0](https://github.com/odalet/VSWebBrowserExtension/releases/tag/v0.1.0) - 2022/03/27
  * Initial version
