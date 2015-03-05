using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.WebPages;
using Microsoft.Ajax.Utilities;
using Web.Management.PHP.Config;

namespace PhpManager.Models
{

    public class PhpSettings
    {

        private Dictionary<SettingLocation, string> _settingsFiles = new Dictionary<SettingLocation, string>();
        private Dictionary<SettingLocation, PHPIniFile> _settings = new Dictionary<SettingLocation, PHPIniFile>();

        // These rules are obtained from http://www.php.net/manual/en/configuration.changes.modes.php
        private Dictionary<ConfigLocation, List<SettingLocation>> filesWhereSettingIsReadFrom = new Dictionary<ConfigLocation, List<SettingLocation>>()
        {
            {ConfigLocation.PHP_INI_USER,   new List<SettingLocation>(){SettingLocation.UserIni, SettingLocation.PhpIni}},
            {ConfigLocation.PHP_INI_PERDIR, new List<SettingLocation>(){SettingLocation.UserIni, SettingLocation.PhpIni}},
            {ConfigLocation.PHP_INI_SYSTEM, new List<SettingLocation>(){SettingLocation.PhpIni}},
            {ConfigLocation.PHP_INI_ALL,    new List<SettingLocation>(){SettingLocation.UserIni, SettingLocation.PhpIni}},
        };

        private enum SettingLocation
        {
            UserIni = 1,
            PhpIni
        }

        public string PhpUserSettingsFile
        {
            get
            {
                return _settingsFiles[SettingLocation.UserIni];
            }
        }

        public string PhpSystemSettingsFile
        {
            get
            {
                return _settingsFiles[SettingLocation.PhpIni];
            }
        }

        public string[] PhpSettingsFiles
        {
            get
            {
                return _settingsFiles.Values.ToArray();
            }
        }

        public PhpSettings()
            : this(GetPhpIniFilePath())
        {
        }

        public PhpSettings(string phpIniFile)
        {
            //
            // Load php.ini file
            //
            _settingsFiles[SettingLocation.PhpIni] = phpIniFile;
            if (string.IsNullOrEmpty(phpIniFile))
            {
                return;
            }

            //
            // Load .user.ini file
            //
            _settingsFiles[SettingLocation.UserIni] = GetUserSettingsFilePath();

            _settings[SettingLocation.PhpIni] = LoadPhpFile(_settingsFiles[SettingLocation.PhpIni]);

            var userIniFile = _settingsFiles[SettingLocation.UserIni];
            if (!string.IsNullOrWhiteSpace(userIniFile) && !File.Exists(userIniFile))
            {
                // Generate .user.ini file if one does not already exist
                Directory.CreateDirectory(Path.GetDirectoryName(userIniFile));
                File.Create(userIniFile).Dispose();
            }
            _settings[SettingLocation.UserIni] = LoadPhpFile(userIniFile);

            // Ensure error log is in a writeable location
            var errLog = GetSettingValue("error_log");
            // TODO: Implement
        }

        public static string GetPhpVersion()
        {
            var phpDir = Path.GetDirectoryName(GetPhpIniFilePath());
            if (phpDir == null) return string.Empty;

            var version = FileVersionInfo.GetVersionInfo(Path.Combine(phpDir, "php-cgi.exe"));
            return version.FileVersion;
        }

        public static string GetPhpExePath()
        {
            var phpDir = Path.GetDirectoryName(GetPhpIniFilePath());
            return phpDir == null ? string.Empty : Path.Combine(phpDir, "php.exe");
        }

        public string GetSettingValue(string settingName)
        {
            var setting = GetSetting(settingName);
            return setting.Value;
        }

        public string GetSettingSection(string settingName)
        {
            var setting = GetSetting(settingName);
            return setting.Section;
        }

        private PHPIniSetting GetSetting(string settingName)
        {
            var settingChangable = PlacementRules.GetChangableLocation(settingName);
            foreach (var settingFileLocation in filesWhereSettingIsReadFrom[settingChangable])
            {
                var setting = _settings[settingFileLocation].GetSetting(settingName);
                if (setting == null) continue;
                return setting;
            }

            return null;
        }

        public bool IsSettingUpdatable(string name)
        {
            var loc = PlacementRules.GetChangableLocation(name);
            return  loc != ConfigLocation.PHP_INI_SYSTEM;
        }

        public IEnumerable<PHPIniExtension> GetExtensions()
        {
            // Return each requested extension 
            foreach (var extension in _settings[SettingLocation.PhpIni].Extensions)
            {
                yield return extension;
            }
            
        }

        public IEnumerable<string> GetAllUnusedPhpSettingNames()
        {
            var allSettings = PlacementRules.GetAllPHPSettingNames();
            var definedSettings = GetAllDefinedSettingNames().ToDictionary(v => v, v => true);

            foreach (var setting in allSettings)
            {
                if (!definedSettings.ContainsKey(setting))
                {
                    yield return setting;
                }
            }
        }

        public IEnumerable<string> GetAllDefinedSettingNames()
        {
            var settingsFound = new HashSet<string>();

            // Ensure we return each setting name only once
            foreach (var location in (SettingLocation[])Enum.GetValues(typeof(SettingLocation)))
            {
                foreach (var setting in _settings[location].Settings)
                {
                    if (settingsFound.Contains(setting.Name)) continue;
                    settingsFound.Add(setting.Name);
                    yield return setting.Name;
                }
            }
        }

        public IEnumerable<PHPIniSetting> GetAllDefinedSettings()
        {
            var settingNames = GetAllDefinedSettingNames();
            return settingNames.Select(settingName => GetSetting(settingName)).Where(s => s != null);
        }

        private static PHPIniFile LoadPhpFile(string phpFilePath)
        {
            if (phpFilePath == null) throw new InvalidOperationException();
            var phpFile = new PHPIniFile(phpFilePath);
            return phpFile;
        }

        private static string GetPhpIniFilePath()
        {
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            var paths = pathVar.Split(';');
            foreach (var path in paths)
            {
                var phpPath = Path.Combine(path, "php.ini");
                if (File.Exists(phpPath))
                {
                    return phpPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Gives the location of the .user.ini file, even if one doesn't exist yet
        /// </summary>
        private static string GetUserSettingsFilePath()
        {
            var rootPath = Environment.GetEnvironmentVariable("HOME"); // For use on Azure Websites
            if (rootPath == null)
            {
                rootPath = System.IO.Path.GetTempPath(); // For testing purposes
            };
            var userSettingsFile = Path.Combine(rootPath, @"site\wwwroot\.user.ini");
            return userSettingsFile;
        }

        public bool SaveSetting(string settingName, string settingValue, string settingSection)
        {
            var setting = GetSetting(settingName);
            if (setting == null)
            {
                // The setting doesn't currently exist. Add a new setting
                setting = new PHPIniSetting()
                {
                    Name = settingName,
                    Section = settingSection,
                };
            }
            else
            {
                if (String.Compare(setting.Value, settingValue) == 0) return true; // No update needed
            }

            setting.Value = settingValue;

            var settingChangableType = PlacementRules.GetChangableLocation(settingName);
            if (!filesWhereSettingIsReadFrom[settingChangableType].Contains(SettingLocation.UserIni))
            {
                //throw new InvalidOperationException("Can't set this");
                return false;
            }

            // Save this setting to .user.ini file 
            var phpFile = _settings[SettingLocation.UserIni];
            phpFile.AddOrUpdateSettings(new List<PHPIniSetting>() { setting });
            phpFile.Save();

            return true;
        }

        public void RevertAllUserIniChanges()
        {
            var file = _settingsFiles[SettingLocation.UserIni];
            File.Delete(file);
            File.Create(file).Dispose();
        }

        #region Set Mode

        public enum Mode
        {
            Development,
            Production,
            None
        }

        private static readonly Dictionary<Mode, List<PHPIniSetting>> SettingsForMode = new Dictionary<Mode, List<PHPIniSetting>>()
        {
            {
                Mode.Development, 
                new List<PHPIniSetting>()
                {
                    new PHPIniSetting() { Name = "error_reporting", Value = "E_ALL", Section = "PHP"},
                    new PHPIniSetting() { Name = "display_errors", Value = "On", Section = "PHP" },
                    new PHPIniSetting() { Name = "display_startup_errors", Value = "On", Section = "PHP" },
                    new PHPIniSetting() { Name = "track_errors", Value = "On", Section = "PHP" },
                    //new PHPIniSetting() { Name = "mysqlnd.collect_memory_statistics", Value = "On", Section = "mysqlnd"},
                    new PHPIniSetting() { Name = "session.bug_compat_42", Value = "On", Section = "Session"},
                    new PHPIniSetting() { Name = "session.bug_compat_warn", Value = "On", Section = "Session" },
                }
            },
            {
                Mode.Production, 
                new List<PHPIniSetting>()
                {
                    new PHPIniSetting() { Name = "error_reporting", Value = "E_ALL & ~E_DEPRECATED & ~E_STRICT", Section = "PHP" },
                    new PHPIniSetting() { Name = "display_errors", Value = "Off", Section = "PHP" },
                    new PHPIniSetting() { Name = "display_startup_errors", Value = "Off", Section = "PHP" },
                    new PHPIniSetting() { Name = "track_errors", Value = "Off", Section = "PHP" },
                    //new PHPIniSetting() { Name = "mysqlnd.collect_memory_statistics", Value = "Off", Section = "mysqlnd" },
                    new PHPIniSetting() { Name = "session.bug_compat_42", Value = "Off", Section = "Session" },
                    new PHPIniSetting() { Name = "session.bug_compat_warn", Value = "Off", Section = "Session" },
                }
            }
        };

        public void SetModeSettings(Mode mode)
        {
            List<PHPIniSetting> newSettings;
            if (!SettingsForMode.TryGetValue(mode, out newSettings))
            {
                throw new Exception(string.Format("{0} is an invalid mode", mode));
            }

            foreach (var setting in newSettings)
            {
                SaveSetting(setting.Name, setting.Value, setting.Section);
            }
        }

        public Mode GetCurrentMode()
        {
            Mode currentMode = Mode.None;

            foreach (Mode mode in SettingsForMode.Keys)
            {
                bool matchFound = true;

                var modeSettings = SettingsForMode[mode];
                foreach (var modeSetting in modeSettings)
                {
                    var savedSetting = GetSetting(modeSetting.Name);
                    if (!savedSetting.Value.Equals(modeSetting.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        // We're not in this mode. Try the next one
                        matchFound = false;
                        break;
                    }
                }

                if (matchFound)
                {
                    // We have matched all the settings in this mode. This must be the mode we're in
                    currentMode = mode;
                    break;
                }
            }

            return currentMode;
        }

        #endregion
    }
}