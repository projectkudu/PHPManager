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

        public ActionResult Index()
        {
            ViewBag.Message = "Making programming easier since 1975";

            ViewBag.PhpVersion = PhpSettings.GetPhpVersion();
            ViewBag.PhpExe = PhpSettings.GetPhpExePath();

            return View(_phpSettings);
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

        private ActionResult DisplayFile(string filePath)
        {
            filePath = filePath.Replace("\"", "");
            ViewBag.FileContents = FileReader.ReadFile(filePath);
            ViewBag.FileName = filePath;
            return View("~/Views/Shared/DisplayFile.cshtml");
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
    }
}
