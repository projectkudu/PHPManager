using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace PhpManager.Models
{
    public class FileReader
    {
        public static IEnumerable<string> ReadFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                yield return fileName + " does not exist or is not accessible";
                yield break;
            }

            using (StreamReader file = new StreamReader(fileName))
            {
                var line = file.ReadLine();
                while (line != null)
                {
                    yield return line + "\n";
                    line = file.ReadLine();
                }
            }
        }
    }
}