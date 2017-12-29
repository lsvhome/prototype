using System;
using System.ComponentModel.DataAnnotations;

namespace FexSync.Data
{
    public class RemoteTree
    {
        public RemoteTree()
        {
        }

        [Key]
        public int RemoteTreeId { get; set; }

        public AccountSyncObject SyncObject { get; set; }

        public DateTime Created { get; set; }
    }
}
