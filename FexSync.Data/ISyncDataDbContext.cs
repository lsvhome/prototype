using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace FexSync.Data
{
    public interface ISyncDataDbContext : Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable
    {
        bool EnsureCreated();

        DbSet<RemoteFile> RemoteFiles { get; set; }

        DbSet<LocalFile> Local { get; set; }

        DbSet<UploadItem> Upload { get; set; }

        DbSet<DownloadItem> Download { get; set; }

        DbSet<RemoveLocalItem> RemoveLocal { get; set; }
    }
}
