using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace FexSync.Data
{
    [Serializable]
    public class AccountSettings
    {
        protected AccountSettings()
        {
        }

        public AccountSettings(string accountRootFolder) : this()
        {
            this.AccountRootFolder = accountRootFolder;
        }

        public AccountSettings(string accountRootFolder, AccountSettings template) : this(accountRootFolder)
        {
            this.UserName = template.UserName;
            this.UserPassword = template.UserPassword;
            this.UserToken = template.UserToken;
        }

        public const string ConfigName = "sync.config";

        public const string DbName = "sync.db";

        public const string DataFolderName = "Data";

        public const string TrashBinFolderName = "Trash";

        [XmlIgnore]
        public string AccountConfigFile => Path.Combine(this.AccountRootFolder, ConfigName);

        [XmlIgnore]
        public string AccountCacheDbFile => Path.Combine(this.AccountRootFolder, DbName);

        [XmlIgnore]
        public string AccountDataFolder => Path.Combine(this.AccountRootFolder, DataFolderName);

        [XmlIgnore]
        public string AccountRootFolder { get; private set; }

        public byte[] UserToken { get; set; }

        public byte[] UserName { get; set; }

        public byte[] UserPassword { get; set; }

        [XmlIgnore]
        public string Login
        {
            get
            {
                var login = Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(this.UserName, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
                return login;
            }

            set
            {
                this.UserName = System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(value), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
        }

        [XmlIgnore]
        public string Password
        {
            get
            {
                var password = Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(this.UserPassword, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
                return password;
            }

            set
            {
                this.UserPassword = System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(value), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
        }

        [XmlIgnore]
        public string TokenForSync
        {
            get
            {
                var token = Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(this.UserToken, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
                return token;
            }

            set
            {
                this.UserToken = System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(value), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
        }

        public bool Exists()
        {
            try
            {
                var ret = File.Exists(this.AccountConfigFile) && !string.IsNullOrWhiteSpace(this.Login);
                System.Diagnostics.Debug.WriteLine($"AccountSettings {this.AccountConfigFile} exists = {ret}   Login = {this.Login}");
                return ret;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Clear()
        {
            if (File.Exists(this.AccountConfigFile))
            {
                File.Delete(this.AccountConfigFile);
            }
        }

        public void Save(string fileName)
        {
            var parentDirectory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            using (var stream = File.OpenWrite(fileName))
            {
                this.CreateSerializer().Serialize(stream, this);
            }
        }

        public AccountSettings Load(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                var loadedSettings = (AccountSettings)this.CreateSerializer().Deserialize(stream);
                return loadedSettings;
            }
        }

        private System.Xml.Serialization.XmlSerializer CreateSerializer()
        {
            return new System.Xml.Serialization.XmlSerializer(typeof(AccountSettings));
        }
    }
}
