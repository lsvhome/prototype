using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FexSync.Data
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        protected byte[] LoginEncrypted { get; set; }

        protected byte[] PasswordEncrypted { get; set; }

        private static byte[] GetRandomData(int bits)
        {
            var buffer = Encoding.UTF8.GetBytes(typeof(Account).FullName.PadRight(bits / 8, '0')).Take(bits / 8).ToArray();
            return buffer;
            //var result = new byte[bits / 8];
            //RandomNumberGenerator.Create().GetBytes(result);
            //return result;
        }

        private static byte[] Encrypt(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);

            var iv = GetRandomData(128);
            var keyAes = GetRandomData(256);


            //byte[] result;
            using (var aes = Aes.Create())
            {
                aes.Key = keyAes;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var resultStream = new MemoryStream())
                {
                    using (var aesStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Write))
                    using (var plainStream = new MemoryStream(buffer))
                    {
                        plainStream.CopyTo(aesStream);
                    }

                    return resultStream.ToArray();
                }
            }
        }


        private static string Decrypt(byte[] data)
        {
            //var buffer = Encoding.UTF8.GetBytes(text);

            var iv = GetRandomData(128);
            var keyAes = GetRandomData(256);


            //byte[] result;
            using (var aes = Aes.Create())
            {
                aes.Key = keyAes;
                aes.IV = iv;

                using (var encryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var resultStream = new MemoryStream(data))
                {
                    using (var aesStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Read))
                    using (var plainStream = new MemoryStream())
                    {
                        aesStream.CopyTo(plainStream);
                        //plainStream.CopyTo(aesStream);
                        return Encoding.UTF8.GetString(plainStream.ToArray());
                        //return ;
                    }

                    
                }
            }
        }
        [NotMapped]
        public string Login
        {
            get
            {
                var login = Decrypt(this.LoginEncrypted);
                    //Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(this.LoginEncrypted, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
                return login;
            }

            set
            {
                //this.LoginEncrypted = System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(value), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);

                //var text = "Hello World";
                this.LoginEncrypted = Encrypt(value);

            }
        }

        [NotMapped]
        public string Password
        {
            get
            {
                var password = Decrypt(this.PasswordEncrypted);
                    //Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(this.PasswordEncrypted, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
                return password;
            }

            set
            {
                this.PasswordEncrypted = Encrypt(value);
                //System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(value), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
        }

//#if !__MACOS__

        public static void RegisterType(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().Property(x => x.LoginEncrypted);
            modelBuilder.Entity<Account>().Property(x => x.PasswordEncrypted);
        }

//#endif
    }
}
