using System.Web.Mvc.Razor;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;

namespace Meleze.Web.Razor
{
    internal sealed class MinifyHtmlVBCodeGenerator : MvcVBRazorCodeGenerator
    {
        public MinifyHtmlVBCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
        }

        public override void VisitSpan(Span span)
        {
            // We only minify the static text
            var markupSpan = span as MarkupSpan;
            if (markupSpan == null)
            {
                base.VisitSpan(span);
                return;
            }

            var content = markupSpan.Content;

            content = MinifyHtmlCodeGenerator.Minify(content);

            span.Content = content;
            base.VisitSpan(span);
        }
    }
}
