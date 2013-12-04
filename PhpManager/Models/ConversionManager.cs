namespace PhpManager.Models
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    public class ConversionManager
    {
        private const string _defaultWebConfig = "<?xml version=\"1.0\"?><configuration><system.webServer></system.webServer></configuration>";
        private const string _systemWebServer = "system.webServer";
        private const string _rewrite = "rewrite";

        public ConversionManager() { }

        public string GenerateOrUpdateWebConfig(Stream webconfig, Stream htaccess)
        {
            string config = ReadFullFile(webconfig);
            string apache = ReadFullFile(htaccess);

            return GenerateOrUpdateWebConfig(config, apache);
        }

        public string GenerateOrUpdateWebConfig(string config, string htaccessFileContent)
        {
            bool addWebServerSection = false;
            if (String.IsNullOrWhiteSpace(config))
            {
                // Use a basic web.config
                config = _defaultWebConfig;
            }

            var doc = XDocument.Parse(config);

            // Look for system.webServer element in Web.config, Add one if it's missing.
            var webServerSection = FindSingle(doc, _systemWebServer);
            if (webServerSection == default(XElement))
            {
                webServerSection = new XElement(_systemWebServer);
                addWebServerSection = true;
            }

            // Look for a rewrite section in system.webServer, Remove if found, we'll replace it.
            var rewriteSection = FindSingle(webServerSection, _rewrite);
            if (rewriteSection != default(XElement))
            {
                rewriteSection.Remove();
            }

            // Add the new rewrite section into the system.webServer element.
            webServerSection.Add(XElement.Parse(GenerateRewriteSectionFromHTAccess(htaccessFileContent)));

            // If system.webServer wasn't found, we'll need to add our system.webServer section to the Web.config
            if (addWebServerSection)
            {
                doc.Root.Add(webServerSection);
            }

            return doc.ToString();
        }

        private string GenerateRewriteSectionFromHTAccess(string fileContents)
        {
            string config = string.Empty;

            try
            {
                var iisRewriteClient = Assembly.Load("Microsoft.Web.Management.Rewrite.Client, Version=7.2.2.1, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

                Type translator = iisRewriteClient.GetType("Microsoft.Web.Management.Iis.Rewrite.Translation.Translator");
                MethodInfo translate = translator.GetMethod("Translate");
                var result = translate.Invoke(null, new object[] { fileContents, true, 0 });

                Type rewriteEntry = iisRewriteClient.GetType("Microsoft.Web.Management.Iis.Rewrite.Translation.RewriteEntry");
                MethodInfo writeTo = rewriteEntry.GetMethod("WriteTo");

                using (var xmlString = new StringWriter())
                {
                    using (var xw = XmlWriter.Create(xmlString, new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        ConformanceLevel = ConformanceLevel.Fragment,
                        Encoding = Encoding.UTF8,
                        Indent = true,
                        IndentChars = "\t"
                    }))
                    {
                        writeTo.Invoke(result, new object[] { xw });
                    }

                    config = xmlString.ToString();
                }
            }
            catch (FileLoadException)
            {
                throw new Exception("One of the required types could not be loaded, please ensure the proper version of IIS Rewrite Module is installed.");
            }
            catch (ArgumentException)
            {
                throw new Exception("One of the required types is not available, please ensure the IIS Rewrite Module is installed.");
            }

            return config;
        }

        private string ReadFullFile(Stream file)
        {
            string contents = String.Empty;

            if (file == null)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        contents = sr.ReadToEnd();
                    }
                }
                catch (OutOfMemoryException)
                {
                    throw new Exception("Not enough memory to perform this action.");
                }
                catch (IOException)
                {
                    throw new Exception("There was a problem reading the stream.");
                }
            }

            return contents;
        }

        private static XElement FindSingle(XContainer container, string elementName)
        {
            return container.Descendants().SingleOrDefault(e => e.Name.LocalName == elementName);
        }
    }
}