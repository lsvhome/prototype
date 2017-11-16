using System;

namespace FexSync
{
    [Serializable]
    public class CredentialsSettings
    {
        public byte[] Login { get; set; }

        public byte[] Password { get; set; }
    }
}
