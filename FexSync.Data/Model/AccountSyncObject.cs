using System;
using System.ComponentModel.DataAnnotations;

namespace FexSync.Data
{
    public class AccountSyncObject
    {
        public AccountSyncObject()
        {
        }

        [Key]
        public int AccountSyncObjectId { get; set; }

        public Account Account { get; set; }

        public string Token { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }
    }
}
