using System;
using System.Configuration;
using System.Reflection;
using System.Web;
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
            Func<string, string> minifyJS = null;
            Func<string, string> minifyCSS = null;

            // We initialize the JS and CSS minification by reflexion to remove a DLL dependency
            try
            {
                if ((minifyJS == null) || (minifyCSS == null))
                {
                    // Minification using the SWO transforms
                    var swo = Assembly.Load("System.Web.Optimization");
                    if (swo != null)
                    {
                        var bundleCollectionType = swo.GetType("System.Web.Optimization.BundleCollection");
                        var bundleContextType = swo.GetType("System.Web.Optimization.BundleContext");
                        var bundleContextConstructor = bundleContextType.GetConstructor(new Type[] { typeof(HttpContextBase), bundleCollectionType, typeof(string) });
                        var bundleReponseType = swo.GetType("System.Web.Optimization.BundleResponse");
                        var bundleReponseConstructor = bundleReponseType.GetConstructor(Type.EmptyTypes);
                        var bundleReponseContentProperty = bundleReponseType.GetProperty("Content");
                        var bundleTransformType = swo.GetType("System.Web.Optimization.IBundleTransform");
                        var bundleTransformProcessMethod = bundleTransformType.GetMethod("Process");

                        var httpContext = new EmptyHttpContext();
                        var bundleCollection = bundleCollectionType.GetConstructor(Type.EmptyTypes).Invoke(null);
                        var bundleContext = bundleContextConstructor.Invoke(new object[] { httpContext, bundleCollection, "" });

                        if (minifyCSS == null)
                        {
                            var cssMinifyType = swo.GetType("System.Web.Optimization.CssMinify");
                            var cssMinify = cssMinifyType.GetConstructor(Type.EmptyTypes).Invoke(null);

                            minifyCSS = delegate(string code)
                            {
                                var bundleReponse = bundleReponseConstructor.Invoke(null);
                                bundleReponseContentProperty.SetValue(bundleReponse, code, null);

                                bundleTransformProcessMethod.Invoke(cssMinify, new object[] { bundleContext, bundleReponse });

                                var minifiedCode = (string)bundleReponseContentProperty.GetValue(bundleReponse, null);
                                return minifiedCode;
                            };
                        }
                        if (minifyJS == null)
                        {
                            var jsMinifyType = swo.GetType("System.Web.Optimization.JsMinify");
                            var jsMinify = jsMinifyType.GetConstructor(Type.EmptyTypes).Invoke(null);

                            minifyJS = delegate(string code)
                            {
                                var bundleReponse = bundleReponseConstructor.Invoke(null);
                                bundleReponseContentProperty.SetValue(bundleReponse, code, null);

                                bundleTransformProcessMethod.Invoke(jsMinify, new object[] { bundleContext, bundleReponse });

                                var minifiedCode = (string)bundleReponseContentProperty.GetValue(bundleReponse, null);
                                return minifiedCode;
                            };
                        }
                    }
                }

                if ((minifyJS == null) || (minifyCSS == null))
                {
                    // Minification using Microsoft AjaxMin
                    var ajaxmin = Assembly.Load("ajaxmin");
                    if (ajaxmin != null)
                    {
                        var scriptCruncherType = ajaxmin.GetType("Microsoft.Ajax.Utilities.ScriptCruncher");
                        var sc = scriptCruncherType.GetConstructor(Type.EmptyTypes).Invoke(null);

                        if (minifyJS == null)
                        {
                            // JS
                            var codeSettingsType = ajaxmin.GetType("Microsoft.Ajax.Utilities.CodeSettings");
                            var localRenamingProperty = codeSettingsType.GetProperty("LocalRenaming");
                            var crunchMethod = scriptCruncherType.GetMethod("Crunch", new Type[] { typeof(string), codeSettingsType });

                            var scsettings = codeSettingsType.GetConstructor(Type.EmptyTypes).Invoke(null);
                            localRenamingProperty.SetValue(scsettings, 1, null);

                            minifyJS = delegate(string code)
                            {
                                var minifiedCode = (string)crunchMethod.Invoke(sc, new object[] { code, scsettings });
                                return minifiedCode;
                            };
                        }

                        if (minifyCSS == null)
                        {
                            // CSS
                            var cssSettingsType = ajaxmin.GetType("Microsoft.Ajax.Utilities.CssSettings");
                            var commentModeProperty = cssSettingsType.GetProperty("CommentMode");
                            var minifyStyleSheetMethod = scriptCruncherType.GetMethod("MinifyStyleSheet", new Type[] { typeof(string), cssSettingsType });

                            var scsettings2 = cssSettingsType.GetConstructor(Type.EmptyTypes).Invoke(null);
                            commentModeProperty.SetValue(scsettings2, 2, null);

                            minifyCSS = delegate(string code)
                            {
                                var minifiedCode = (string)minifyStyleSheetMethod.Invoke(sc, new object[] { code, scsettings2 });
                                return minifiedCode;
                            };
                        }
                    }
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

        private sealed class EmptyHttpContext : HttpContextBase
        {
        }
    }
}