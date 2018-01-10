using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace FexSync.Data
{
    public interface ISyncDataDbContext : Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable, IDisposable
    {
        void LockedRun(System.Action action);

        DbSet<Account> Accounts { get; set; }

        DbSet<AccountSyncObject> AccountSyncObjects { get; set; }

        DbSet<RemoteTree> RemoteTrees { get; set; }

        DbSet<RemoteFile> RemoteFiles { get; set; }

        DbSet<LocalFile> LocalFiles { get; set; }

        DbSet<UploadItem> Uploads { get; set; }

        DbSet<DownloadItem> Downloads { get; set; }

        DbSet<RemoteFileModified> RemoteModifications { get; set; }

        DbSet<LocalFileModified> LocalModifications { get; set; }

        DbSet<ConflictItem> Conflicts { get; set; }

        int SaveChanges();

        void AcceptAllChangesWithoutSaving();

        bool EnsureDatabaseExists();

        void RemoveAccountRecursive(Account account);

        void RemoveAccountSyncObjectRecursive(AccountSyncObject syncObject);
    }
}
