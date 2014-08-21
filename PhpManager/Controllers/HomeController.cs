using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using PhpManager.Models;
using Web.Management.PHP.Config;

namespace PhpManager.Controllers
{
    public class HomeController : Controller
    {
        private PhpSettings _phpSettings = new PhpSettings();
        private string _webConfigPath;
        private string WebConfigPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_webConfigPath))
                {
                    var home = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Machine);
                    _webConfigPath = Path.Combine(home, "site", "wwwroot", "web.config");
                }
                return _webConfigPath;
            }
        }

        public HomeController()
        {

        }

        public ActionResult Index()
        {
            ViewBag.Message = "Making programming easier since 1975";

            ViewBag.PhpVersion = PhpSettings.GetPhpVersion();
            ViewBag.PhpExe = PhpSettings.GetPhpExePath();

            return View(_phpSettings);
        }

        public ActionResult PhpInfo()
        {
            ViewBag.Message = "Your PHP Info";
            return View();
        }

        public ActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Import(HTAccessModel model)
        {
            if (model.UploadedFile != null)
            {
                string htaccess = string.Empty;
                using (var reader = new StreamReader(model.UploadedFile.InputStream))
                {
                    htaccess = reader.ReadToEnd();
                }

                model.HTAccessFile = htaccess;
            }
            else
            {
                ModelState.Clear();
            }
            model.WebConfigFile = ConvertHTAccessToWebConfig(model.HTAccessFile);

            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveWebConfig(HTAccessModel model)
        {
            if (!model.AcceptTerms)
            {
                ViewBag.Message = "Please indicate you understand the file will be overwritten.";
                ViewBag.AlertType = "danger";
                return View("Import", model);
            }

            try
            {
                using (var stream = new StreamWriter(WebConfigPath, false))
                {
                    stream.Write(model.WebConfigFile);
                }
                ViewBag.AlertType = "success";
                ViewBag.Message = "<h3>Save Successful!</h3><p>To ensure that this file persists across deployments, please <a href=\"/vfs/site/wwwroot/web.config\">download</a> the <code>web.config</code> file to replace your local copy.</p>";
            }
            catch (IOException ioe)
            {
                ViewBag.Message = string.Format("Error saving web.config file. Output: {0}", ioe.Message);
                ViewBag.AlertType = "danger";
            }

            ModelState.Clear();
            return View("Import", model);
        }

        public ActionResult Settings()
        {
            return View(_phpSettings);
        }

        public ActionResult UserIni()
        {
            return DisplayFile(_phpSettings.PhpUserSettingsFile);
        }

        public ActionResult PhpIni()
        {
            return DisplayFile(_phpSettings.PhpSystemSettingsFile);
        }

        public ActionResult PhpErrorLog()
        {
            return DisplayFile(_phpSettings.GetSettingValue("error_log"));
        }

        public ActionResult ErrorReporting()
        {
            return View(_phpSettings);
        }

        public JsonResult RevertAllSettings()
        {
            _phpSettings.RevertAllUserIniChanges();
            return new JsonResult();
        }

        public JsonResult SaveSettingValue(string settingName, string settingValue, string settingSection)
        {
            bool success = false;
            string message = string.Empty;

            try
            {
                if (_phpSettings.SaveSetting(settingName, settingValue, settingSection))
                {
                    success = true;
                    message = string.Format("Saved {0} = {1}", settingName, settingValue);
                }
                else
                {
                    message = string.Format("Cannot change {0}", settingName);
                }
            }
            catch (Exception e)
            {
                message = string.Format(e.Message);
            }

            return Json(new { Success = success, Message = message });
        }

        public JsonResult SetMode(string mode)
        {
            PhpSettings.Mode settingMode;
            bool success = Enum.TryParse<PhpSettings.Mode>(mode, true, out settingMode);
            var errMsg = string.Empty;

            if (success)
            {
                try
                {
                    _phpSettings.SetModeSettings(settingMode);
                }
                catch (Exception e)
                {
                    success = false;
                    errMsg = e.Message;
                }
            }
            else
            {
                errMsg = "Invalid mode specified. Only 'Development' and 'Production' are allowed";
            }

            return Json(new { Success = success, ErrorMessage = errMsg });
        }

        private ActionResult DisplayFile(string filePath)
        {
            filePath = filePath.Replace("\"", "");
            ViewBag.FileContents = FileReader.ReadFile(filePath);
            ViewBag.FileName = filePath;
            return View("~/Views/Shared/DisplayFile.cshtml");
        }

        private string ConvertHTAccessToWebConfig(string htaccess)
        {
            var convert = new ConversionManager();
            string exstWebConfig = WebConfigPath;
            string webconfig = string.Empty;

            if (System.IO.File.Exists(exstWebConfig))
            {
                using (var reader = new StreamReader(exstWebConfig))
                {
                    webconfig = reader.ReadToEnd();
                }
            }

            return convert.GenerateOrUpdateWebConfig(webconfig, htaccess);
        }
    }
}
