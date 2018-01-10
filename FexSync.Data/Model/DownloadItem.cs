using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FexSync.Data
{
    public class DownloadItem : RemoteFileBase
    {
        [Obsolete("Only for (De)Serialization purposes", true)]
        protected DownloadItem()
        {
        }

        public DownloadItem(string relativePath, int uploadId, AccountSyncObject syncObject) : base(uploadId, syncObject)
        {
            this.FilePathLocalRelative = relativePath;
        }

        [Key]
        public int DownloadItemId { get; set; }

        public string FilePathLocalRelative { get; set; }

        public int TriesCount { get; set; } = 0;

        public System.DateTime ItemCreated { get; set; }
    }
}
