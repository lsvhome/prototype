using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FexSync
{
    public class SyncDataManager
    {
        private static string ConfigFullPath
        {
            get
            {
                return Path.Combine(System.Configuration.ConfigurationManager.AppSettings["DataFolder"], "sync.cache");
            }
        }

        private static System.Xml.Serialization.XmlSerializer CreateSerializer()
        {
            return new System.Xml.Serialization.XmlSerializer(typeof(CredentialsSettings));
        }

        public static void Save(string login, string password)
        {
            var loginEncrypted = System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(login), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);

            var passwordEncrypted = System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(password), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);

            using (var stream = File.OpenWrite(ConfigFullPath))
            {
                CreateSerializer().Serialize(stream, new CredentialsSettings { Login = loginEncrypted, Password = passwordEncrypted });
            }
        }

        public static void Load(out string login, out string password)
        {
            using (var stream = File.OpenRead(ConfigFullPath))
            {
                var loadedSettings = (CredentialsSettings)CreateSerializer().Deserialize(stream);
                login = Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(loadedSettings.Login, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
                password = Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(loadedSettings.Password, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
            }
        }

        public static bool Exists()
        {
            return File.Exists(ConfigFullPath);
        }

        public static void Clear()
        {
            if (File.Exists(ConfigFullPath))
            {
                File.Delete(ConfigFullPath);
            }
        }
    }
}
