using System.Web.Razor.Parser.SyntaxTree;

namespace Meleze.Web.Razor
{
    /// <summary>
    /// MinifyHtmlCodeGenerator dispatch the page tokens to the minifier.
    /// There is one instance per compiled page.
    /// </summary>
    internal sealed class MinifyHtmlCodeGenerator
    {
        private MinifyHtmlMinifier _minifier;
        private bool _previousIsWhiteSpace;
        private bool _previousTokenEndsWithBlockElement;

        public MinifyHtmlCodeGenerator(MinifyHtmlMinifier minifier)
        {
            _minifier = minifier;
            _previousIsWhiteSpace = true;
            _previousTokenEndsWithBlockElement = true;
        }

        public void VisitSpan(Span span)
        {
            // We only minify the static text
            var markupSpan = span as MarkupSpan;
            if (markupSpan == null)
            {
                // When we have a dynamic markup, we can't know if the last char will be whitespace
                // => to make it work in all cases, we won't minifiy whitespace just after code.
                _previousIsWhiteSpace = false;
                _previousTokenEndsWithBlockElement = false;
                return;
            }

            var content = markupSpan.Content;

            content = _minifier.Minify(content, _previousIsWhiteSpace, _previousTokenEndsWithBlockElement, false);

            bool insideScript = false;
            _minifier.AnalyseContent(content, ref _previousIsWhiteSpace, ref _previousTokenEndsWithBlockElement, ref insideScript);

            span.Content = content;
        }
    }
}
