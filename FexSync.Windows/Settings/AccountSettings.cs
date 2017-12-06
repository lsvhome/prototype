/*
using System;
using System.IO;

namespace FexSync
{
    public interface ISaveable
    {
    }

    [Serializable]
    public class AccountSettings : FexSync.Data.AccountSettings, ISaveable
    {
        public AccountSettings() : this(ApplicationSettingsManager.CurrentFexUserRootFolder)
        {
        }

        protected AccountSettings(string accountRootFolder) : base(accountRootFolder)
        {
        }

        public override string TokenForSync { get => this.CurrentAccountCredentialSettings.SyncToken;  }

        public CredentialsSettings CurrentAccountCredentialSettings
        {
            get
            {
                if (!File.Exists(this.AccountConfigFile))
                {
                    return null;
                }

                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(CredentialsSettings));
                using (var stream = File.OpenRead(AccountConfigFile))
                {
                    var loadedSettings = (CredentialsSettings)serializer.Deserialize(stream);
                    return loadedSettings;
                }
            }
        }
    }
}
*/