using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FexSync.Data;

namespace FexSync
{
    public interface ISaveable
    {
    }

    public static class ApplicationSettingsManager
    {
        [Serializable]
        public class ApplicationSettings : ISaveable
        {
            public string CurrentFexUserRootFolder { get; set; }

            public List<string> InactiveAccountRootFolders { get; set; } = new List<string>();
        }

        private static string GetExpanded(string key)
        {
            var ret = System.Configuration.ConfigurationManager.AppSettings[key];
            ret = Environment.ExpandEnvironmentVariables(ret);
            return ret;
        }

        public static string ApplicationDataFolder
        {
            get
            {
                var ret = GetExpanded("ApplicationDataFolder");
                return ret;
            }
        }

        public static string ApplicationConfigPath
        {
            get
            {
                var ret = Path.Combine(ApplicationDataFolder, "sync.config");
                return ret;
            }
        }

        public static string ApiHost
        {
            get
            {
                var ret = System.Configuration.ConfigurationManager.AppSettings["FEX.NET.ApiHost"];
                return ret;
            }
        }

        public const string DefaultFexSyncFolderName = "FexSyncFolder";

        public static string CurrentFexUserRootFolder
        {
            get
            {
                try
                {
                    ApplicationSettings loadedSettings = new ApplicationSettings();

                    if (File.Exists(ApplicationConfigPath))
                    {
                        loadedSettings = loadedSettings.Load(ApplicationConfigPath);

                        //// Required for first run after reinstall (previous configuration file exists, but AccountDataFolder differ)
                        if (!loadedSettings.InactiveAccountRootFolders.Contains(GetExpanded("DefaultFexUserRootFolder"))
                            &&
                            loadedSettings.CurrentFexUserRootFolder != GetExpanded("DefaultFexUserRootFolder"))
                        {
                            loadedSettings.InactiveAccountRootFolders.Add(loadedSettings.CurrentFexUserRootFolder);
                            loadedSettings.CurrentFexUserRootFolder = GetExpanded("DefaultFexUserRootFolder");
                            loadedSettings.Save(ApplicationConfigPath);
                        }
                    }
                    else
                    {
                        //// Required for first run
                        loadedSettings.CurrentFexUserRootFolder = GetExpanded("DefaultFexUserRootFolder");
                        loadedSettings.Save(ApplicationConfigPath);
                    }

                    return loadedSettings.CurrentFexUserRootFolder;
                }
                catch (Exception)
                {
                    var ret = GetExpanded("DefaultFexUserRootFolder");
                    CurrentFexUserRootFolder = ret;
                    return ret;
                }
            }

            set
            {
                ApplicationSettings loadedSettings = new ApplicationSettings().Load(ApplicationConfigPath);

                if (loadedSettings.CurrentFexUserRootFolder != value)
                {
                    if (!loadedSettings.InactiveAccountRootFolders.Contains(loadedSettings.CurrentFexUserRootFolder))
                    {
                        loadedSettings.InactiveAccountRootFolders.Add(loadedSettings.CurrentFexUserRootFolder);
                    }

                    loadedSettings.CurrentFexUserRootFolder = value;
                    loadedSettings.InactiveAccountRootFolders.Sort();
                    loadedSettings.Save(ApplicationConfigPath);

                    accountSettings = new AccountSettings(value, accountSettings);
                    accountSettings.Save(accountSettings.AccountConfigFile);
                }
                else if (!accountSettings.Exists())
                {
                    accountSettings.Save(accountSettings.AccountConfigFile);
                }
            }
        }

        public static string CurrentFexUserConfigPath
        {
            get
            {
                return new AccountSettings(CurrentFexUserRootFolder).AccountConfigFile;
            }
        }

        public static T Load<T>(this T type, string fileName) where T : ISaveable
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var stream = File.OpenRead(fileName))
            {
                T loadedSettings = (T)serializer.Deserialize(stream);
                return loadedSettings;
            }
        }

        public static void Save<T>(this T accountSettings, string fileName) where T : ISaveable
        {
            var dir = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (var stream = File.Create(fileName))
            {
                serializer.Serialize(stream, accountSettings);
            }
        }

        private static AccountSettings accountSettings;

        public static AccountSettings AccountSettings
        {
            get
            {
                try
                {
                    if (accountSettings == null)
                    {
                        accountSettings = new AccountSettings(CurrentFexUserRootFolder);
                    }

                    return accountSettings;
                }
                catch (Exception)
                {
                    if (!Directory.Exists(CurrentFexUserRootFolder))
                    {
                        Directory.CreateDirectory(CurrentFexUserRootFolder);
                    }

                    if (accountSettings == null)
                    {
                        accountSettings = new AccountSettings(CurrentFexUserRootFolder);
                    }

                    return accountSettings;
                }
            }
        }
    }
}
