using System;
using System.Configuration;
using System.Reflection;
using System.Web.Mvc;

namespace Meleze.Web.Razor
{
    /// <summary>
    /// MinifyHtmlWebRazorHostFactory removes useless whitespace and comments
    /// in an HTML page.
    /// It is executed when Razor generate the code for a page, just before the compilation.
    /// => There is a small performance penalty when the page compiles and a small optimization at execution.
    /// </summary>
    public sealed partial class MinifyHtmlWebRazorHostFactory : MvcWebRazorHostFactory
    {
        private MinifyHtmlMinifier _minifier;

        public MinifyHtmlWebRazorHostFactory()
        {
            // Javascript minification

            Func<string, string> minifyJS = null;
            Func<string, string> minifyCSS = null;
            //minifyJS = delegate(string code)
            //{
            //    var min = new Microsoft.Ajax.Utilities.Minifier();
            //    var scsettings = new Microsoft.Ajax.Utilities.CodeSettings() { LocalRenaming = Microsoft.Ajax.Utilities.LocalRenaming.KeepLocalizationVars };
            //    var minifiedCode = min.MinifyJavaScript(code, scsettings);
            //    return minifiedCode;
            //};
            //minifyCSS = delegate(string code)
            //{
            //    var min = new Microsoft.Ajax.Utilities.Minifier();
            //    var scsettings = new Microsoft.Ajax.Utilities.CssSettings() { CommentMode = Microsoft.Ajax.Utilities.CssComment.Hacks };
            //    var minifiedCode = min.MinifyStyleSheet(code, scsettings);
            //    return minifiedCode;
            //};

            // We initialize the JS and CSS minification by reflexion to remove a DLL dependency
            try
            {
                var ajaxmin = Assembly.Load("ajaxmin");
                if (ajaxmin != null)
                {
                    var minifierType = ajaxmin.GetType("Microsoft.Ajax.Utilities.Minifier");

                    // JS
                    var codeSettingsType = ajaxmin.GetType("Microsoft.Ajax.Utilities.CodeSettings");
                    var localRenamingProperty = codeSettingsType.GetProperty("LocalRenaming");
                    var minifyJavaScriptMethod = minifierType.GetMethod("MinifyJavaScript", new Type[] { typeof(string), codeSettingsType });

                    var min = minifierType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    
                    var scsettings = codeSettingsType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    localRenamingProperty.SetValue(scsettings, 1, null);

                    minifyJS = delegate(string code)
                    {
                        var minifiedCode = (string)minifyJavaScriptMethod.Invoke(min, new object[] { code, scsettings });
                        return minifiedCode;
                    };

                    // CSS
                    var cssSettingsType = ajaxmin.GetType("Microsoft.Ajax.Utilities.CssSettings");
                    var commentModeProperty = cssSettingsType.GetProperty("CommentMode");
                    var minifyStyleSheetMethod = minifierType.GetMethod("MinifyStyleSheet", new Type[] { typeof(string), cssSettingsType });

                    var scsettings2 = cssSettingsType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    commentModeProperty.SetValue(scsettings2, 2, null);

                    minifyCSS = delegate(string code)
                    {
                        var minifiedCode = (string)minifyStyleSheetMethod.Invoke(min, new object[] { code, scsettings2 });
                        return minifiedCode;
                    };
                }
            }
            catch
            {
            }

            var confAggressive = ConfigurationManager.AppSettings["meleze-minifier:Aggressive"];
            var confComments = ConfigurationManager.AppSettings["meleze-minifier:Comments"];
            var confJavascript = ConfigurationManager.AppSettings["meleze-minifier:Javascript"];
            var confCSS = ConfigurationManager.AppSettings["meleze-minifier:CSS"];

            _minifier = new MinifyHtmlMinifier();
            _minifier.MinifyJS = minifyJS;
            _minifier.MinifyCSS = minifyCSS;
            _minifier.Aggressive = confAggressive == null || confAggressive.ToLower() == "true";
            _minifier.Comments = confComments == null || confComments.ToLower() == "true";
            _minifier.Javascript = confJavascript == null || confJavascript.ToLower() == "true";
            _minifier.CSS = confCSS == null || confCSS.ToLower() == "true";
        }
    }
}