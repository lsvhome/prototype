using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FexSync.Data
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        protected byte[] LoginEncrypted { get; set; }

        protected byte[] PasswordEncrypted { get; set; }

        [NotMapped]
        public string Login
        {
            get
            {
                var login = Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(this.LoginEncrypted, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
                return login;
            }

            set
            {
                this.LoginEncrypted = System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(value), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
        }

        [NotMapped]
        public string Password
        {
            get
            {
                var password = Encoding.Default.GetString(System.Security.Cryptography.ProtectedData.Unprotect(this.PasswordEncrypted, new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser));
                return password;
            }

            set
            {
                this.PasswordEncrypted = System.Security.Cryptography.ProtectedData.Protect(Encoding.Default.GetBytes(value), new byte[0], System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
        }

        public static void RegisterType(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().Property(x => x.LoginEncrypted);
            modelBuilder.Entity<Account>().Property(x => x.PasswordEncrypted);
        }
    }
}
