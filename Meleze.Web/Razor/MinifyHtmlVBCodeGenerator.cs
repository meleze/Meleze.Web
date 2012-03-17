using System.Web.Mvc.Razor;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;

namespace Meleze.Web.Razor
{
    internal sealed class MinifyHtmlVBCodeGenerator : MvcVBRazorCodeGenerator
    {
        private MinifyHtmlCodeGenerator _generator;

        public MinifyHtmlVBCodeGenerator(MinifyHtmlCodeGenerator generator, string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
            _generator = generator;
        }

        public override void VisitSpan(Span span)
        {
            _generator.VisitSpan(span);
            base.VisitSpan(span);
        }
    }
}
