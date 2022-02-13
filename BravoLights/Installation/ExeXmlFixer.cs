using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BravoLights.Installation
{
    internal static class ExeXmlFixer
    {
        /// <summary>
        /// Attempts to fix broken exe.xml file contents.
        /// </summary>
        /// <param name="exeXml">exe.xml contents, already known to be broken.</param>
        /// <returns>A fixed copy of the exe.xml contents if we can DEFINITELY
        /// fix it with zero risk, or <code>null</code> if it's not auto-fixable.
        /// </returns>
        public static string TryFix(string exeXml)
        {
            if (!exeXml.Contains("<SimBase.Document>"))
            {
                // This is a well-known corruption. Something has trashed the opening <SimBase.Document>
                // line. We just need to insert one back in, which is straightforward.

                if (!exeXml.StartsWith("<?xml"))
                {
                    // Document doesn't even start with an XML heading? Seems a bit weird, but let's try fixing that.
                    exeXml = @"<?xml version=""1.0"" encoding=""Windows-1252""?>" + Environment.NewLine + exeXml;
                }

                var endOfXmlHeader = exeXml.IndexOf("?>");
                if (endOfXmlHeader > 15 && endOfXmlHeader < 60)
                {
                    // That looks a reasonable place for the <?xml> header to end.
                    exeXml = exeXml.Substring(0, endOfXmlHeader+2) + Environment.NewLine + @"<SimBase.Document Type=""SimConnect"" version=""1,0"">" + exeXml.Substring(endOfXmlHeader+2);
                }

                try
                {
                    var doc = XDocument.Parse(exeXml);
                    // Hooray. With those changes we can read the XML file.

                    var windows1252EncodingRegex = new Regex("windows-1252", RegexOptions.IgnoreCase);
                    var originalEncoding = (doc.Declaration != null && doc.Declaration.Encoding != null) ? doc.Declaration.Encoding : "Windows-1252";

                    var sw = new StringWriterWithEncoding(originalEncoding);
                    doc.Save(sw);

                    // The .NET CP-1252 Encoding calls itself 'windows-1252' but MSFS usually uses 'Windows-1252'.
                    // Let's restore the original encoding name;                    
                    if (windows1252EncodingRegex.IsMatch(originalEncoding))
                    {
                        return sw.ToString().Replace("windows-1252", originalEncoding);
                    }
                    else
                    {
                        return sw.ToString();
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        static ExeXmlFixer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }

    class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding mEncoding;

        public StringWriterWithEncoding(string encoding)
        {
            try
            {
                mEncoding = Encoding.GetEncoding(encoding);
            }
            catch (Exception)
            {
                mEncoding = Encoding.GetEncoding("windows-1252");
            }
        }

        public override Encoding Encoding => mEncoding;
    }
}
