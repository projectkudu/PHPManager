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
        private readonly string WebConfigPath;

        public HomeController()
        {
            WebConfigPath = Environment.ExpandEnvironmentVariables(@"%HOME%\site\wwwroot\web.config");
        }

        public ActionResult Index()
        {
            ViewBag.Message = "Making programming easier since 1975";

            ViewBag.PhpVersion = PhpSettings.GetPhpVersion();
            ViewBag.PhpExe = PhpSettings.GetPhpExePath();

            return View(_phpSettings);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
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
        public ActionResult Import(HTAccessModel model)
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

            string htaccess = string.Empty;
            using (var reader = new StreamReader(model.UploadedFile.InputStream))
            {
                htaccess = reader.ReadToEnd();
            }

            string output = convert.GenerateOrUpdateWebConfig(webconfig, htaccess);

            return View(new HTAccessModel { HTAccessFile = htaccess, WebConfigFile = output, Path = exstWebConfig });
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveWebConfig(HTAccessModel model)
        {
            if (model.AcceptTerms)
            {
                try
                {
                    using (var stream = System.IO.File.CreateText(WebConfigPath))
                    {
                        stream.Write(model.WebConfigFile);
                    }
                    ViewBag.AlertType = "success";
                    ViewBag.Message = "<h3>Save Successful!</h3><p>To ensure that this file persists across deployments, please <a href=\"/vfs/site/wwwroot/web.config\">download</a> the <code>web.config</code> file to replace your local copy.</p>";
                }
                catch (Exception)
                {
                    ViewBag.Message = "Error saving web.config file.";
                    ViewBag.AlertType = "danger";
                }
            }
            else
            {
                ViewBag.Message = "Please indicate you understand the file will be overwritten.";
                ViewBag.AlertType = "danger";
            }

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
    }
}
