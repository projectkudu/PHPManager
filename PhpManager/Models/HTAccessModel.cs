namespace PhpManager.Models
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Web;

    public class HTAccessModel
    {
        public HTAccessModel(string webConfigFile, string htAccessFile, string path, bool acceptTerms)
        {
            WebConfigFile = webConfigFile;
            HTAccessFile = htAccessFile;
            Path = path;
            AcceptTerms = acceptTerms;
        }
        public HTAccessModel() : this(string.Empty, string.Empty, string.Empty, false) { }
        public HTAccessModel(HTAccessModel self) : this(self.WebConfigFile, self.HTAccessFile, self.Path, self.AcceptTerms) { }

        [DisplayName("web.config")]
        [DataType(DataType.MultilineText)]
        public string WebConfigFile { get; set; }
        [DisplayName(".htaccess")]
        [DataType(DataType.MultilineText)]
        public string HTAccessFile { get; set; }
        public string Path { get; set; }
        [DisplayName("I understand that any existing <rewrite> settings were removed and saving this file may cause adverse effects to my application.")]
        public bool AcceptTerms { get; set; }

        [DisplayName("Configuration file")]
        [DataType(DataType.Upload)]
        public HttpPostedFileBase UploadedFile { get; set; }
    }
}