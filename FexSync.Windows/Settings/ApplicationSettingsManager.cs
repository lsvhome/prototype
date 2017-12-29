using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FexSync.Data;

namespace FexSync
{
    public interface ISaveable
    {
    }

    public static class ApplicationSettingsManager
    {
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

        [XmlIgnore]
        public static string AccountCacheDbFile => Path.Combine(ApplicationDataFolder, "sync.db");

        public static string ApiHost
        {
            get
            {
                var ret = System.Configuration.ConfigurationManager.AppSettings["FEX.NET.ApiHost"];
                return ret;
            }
        }

        public const string DefaultFexSyncFolderName = "FexSyncFolder";

        public static string DefaultFexUserRootFolder
        {
            get
            {
                return GetExpanded("DefaultFexUserRootFolder");
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
    }
}
