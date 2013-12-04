namespace PhpManager.Models
{
    using System.ComponentModel;
    using System.Web;

    public class HTAccessModel
    {
        public HTAccessModel()
        {
            WebConfigFile = string.Empty;
            HTAccessFile = string.Empty;
            Path = string.Empty;
            AcceptTerms = false;
        }
        [DisplayName("web.config")]
        public string WebConfigFile { get; set; }
        [DisplayName(".htaccess")]
        public string HTAccessFile { get; set; }
        public string Path { get; set; }
        [DisplayName("I understand that any existing <rewrite> settings were removed and saving this file may cause adverse effects to my application.")]
        public bool AcceptTerms { get; set; }

        [DisplayName("Configuration file:")]
        public HttpPostedFileBase UploadedFile { get; set; }
    }
}