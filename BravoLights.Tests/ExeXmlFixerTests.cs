using BravoLights.Installation;
using Xunit;

namespace BravoLights.Tests
{
    public class ExeXmlFixerTests
    {
        [Fact]
        public void InsertsMissingOpenSimBaseDocumentWithXmlHeaderPresent()
        {
            var input = @"<?xml version=""1.0"" encoding=""Windows-1252""?>
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>C:\Something\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>";

            var output = ExeXmlFixer.TryFix(input);

            Assert.Equal(@"<?xml version=""1.0"" encoding=""Windows-1252""?>
<SimBase.Document Type=""SimConnect"" version=""1,0"">
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>C:\Something\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>", output);
        }

        [Fact]
        public void InsertsMissingOpenSimBaseDocumentWithMissingXmlHeader()
        {
            var input = @"<Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>C:\Something\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>";

            var output = ExeXmlFixer.TryFix(input);

            Assert.Equal(@"<?xml version=""1.0"" encoding=""Windows-1252""?>
<SimBase.Document Type=""SimConnect"" version=""1,0"">
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>C:\Something\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>", output);
        }

        [Fact]
        public void KeepsOriginalXmlEncoding()
        {
            var input = @"<?xml version=""1.0"" encoding=""utf-8""?>
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>C:\Something\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>";

            var output = ExeXmlFixer.TryFix(input);

            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<SimBase.Document Type=""SimConnect"" version=""1,0"">
  <Launch.Addon>
    <Name>AFCBridge</Name>
    <Disabled>False</Disabled>
    <Path>C:\Something\AFC_Bridge.exe</Path>
  </Launch.Addon>
</SimBase.Document>", output);
        }

        [Fact]
        public void DoesNotTryToFixATotallyNonsensicalExeXmlFile()
        {
            var input = @"<?xml version=""1.0"" encoding=""Windows-1252""?>
This is totally garbage";
            var output = ExeXmlFixer.TryFix(input);

            Assert.Null(output);
        }
    }
}
