using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BravoLights
{
    public class CsvFile
    {
        public string[] Headings;
        public string[][] Rows;

        public void Load(string filename)
        {
            var lines = File.ReadAllLines(filename);

            Headings = Split(lines[0]);

            Rows = lines.Skip(1).Select(line => Split(line)).ToArray();
        }

        private static string[] Split(string line)
        {
            var regex = new Regex("\"(.*?)\",?\\s*");
            var matches = regex.Matches(line);

            var values = matches.Select(m => m.Groups[1].Value).ToArray();
            return values;
        }
    }
}
