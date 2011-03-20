using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace Meleze.Web.Sprites
{
    public sealed class SpriteGenerator
    {
        public int HorizontalSpace { get; set; }

        public string RuleFormat { get; set; }

        public SpriteGenerator()
        {
            //            RuleFormat = ".sprite-{0}(@y:{2}px){{background:url('sprite.png') {1}px @y no-repeat;width:{3}px;height:{4}px;display:block;text-indent:-9000px;}}\n";
            RuleFormat = ".sprite-{0}{{background:url('sprite.png') {1}px {2}px no-repeat;width:{3}px;height:{4}px;display:block;text-indent:-9000px;}}\n";
        }

        public void Generate(IEnumerable<Tuple<string, Stream>> sources, out byte[] sprite, out string css)
        {
            sprite = null;
            css = null;

            var sourceTokens = new List<Tuple<string, Image>>();
            var cssRules = new StringBuilder();
            try
            {
                sourceTokens.AddRange(sources.Select(s => new Tuple<string, Image>(s.Item1, Image.FromStream(s.Item2))));

                var totalWidth = sourceTokens.Sum(i => i.Item2.Width);
                var maxHeight = sourceTokens.Max(i => i.Item2.Height);

                var targetWidth = totalWidth + (sourceTokens.Count - 1) * HorizontalSpace;
                var targetHeight = maxHeight;

                using (var targetImg = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb))
                {
                    // Screen resolution
                    targetImg.SetResolution(72, 72);
                    targetImg.MakeTransparent();

                    using (var graphics = Graphics.FromImage(targetImg))
                    {
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        // Add each image
                        var x = 0;
                        foreach (var sourceToken in sourceTokens)
                        {
                            var sourceName = sourceToken.Item1;
                            var sourceImg = sourceToken.Item2;

                            // All the images are centered vertically
                            var y = (targetHeight - sourceImg.Height) / 2;
                            graphics.DrawImage(sourceImg, new Rectangle(x, y, sourceImg.Width, sourceImg.Height), new Rectangle(0, 0, sourceImg.Width, sourceImg.Height), GraphicsUnit.Pixel);

                            WriteCSSRule(sourceName, x, y, sourceImg.Width, sourceImg.Height, cssRules);
                            //  WriteCSSCenteredRule(sourceName, x, y, sourceImg.Width, sourceImg.Height, cssRules);

                            x += sourceImg.Width + HorizontalSpace;
                        }
                    }

                    // Convert the image as PNG
                    var targetStream = new MemoryStream();
                    targetImg.Save(targetStream, ImageFormat.Png);
                    sprite = targetStream.ToArray();
                    css = cssRules.ToString();
                }
            }
            finally
            {
                foreach (var sourceToken in sourceTokens)
                {
                    try
                    {
                        sourceToken.Item2.Dispose();
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void WriteCSSRule(string fileName, int x, int y, int width, int height, StringBuilder cssRules)
        {
            cssRules.AppendFormat(RuleFormat, GetCSSSelectorName(fileName), x, y, width, height);
        }

        private string GetCSSSelectorName(string fileName)
        {
            var ruleName = fileName.Replace(' ', '-');
            return ruleName;
        }
    }
}
