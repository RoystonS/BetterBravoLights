using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BravoLights.Installation
{
    static class FileUtils
    {

        public static void CopyDirectory(string source, string destination)
        {
            CopyDirectory(new DirectoryInfo(source), new DirectoryInfo(destination));
        }

        public static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!Directory.Exists(destination.FullName))
            {
                Directory.CreateDirectory(destination.FullName);
            }

            foreach (var fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(destination.FullName, fi.Name), true);
            }

            foreach (var sourceChild in source.GetDirectories())
            {
                var destinationChild = destination.CreateSubdirectory(sourceChild.Name);
                CopyDirectory(sourceChild, destinationChild);
            }
        }

        public static void RemoveDirectoryRecursively(string name)
        {
            if (Directory.Exists(name))
            {
                Directory.Delete(name, true);
            }
        }
    }
}
