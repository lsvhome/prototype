using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace FexSync.Data
{
    public class SyncDataDbContext : DbContext, ISyncDataDbContext
    {
        private readonly object lockObj = new object();

        public void LockedRun(System.Action action)
        {
            lock (this.lockObj)
            {
                action();
            }
        }

        private string databaseFullPath = "syncdata.db";

        public SyncDataDbContext() : base()
        {
        }

        public SyncDataDbContext(string databaseFullPath) : this()
        {
            this.databaseFullPath = databaseFullPath;
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<AccountSyncObject> AccountSyncObjects { get; set; }

        public DbSet<RemoteTree> RemoteTrees { get; set; }

        public DbSet<RemoteFile> RemoteFiles { get; set; }

        public DbSet<LocalFile> LocalFiles { get; set; }

        public DbSet<UploadItem> Uploads { get; set; }

        public DbSet<DownloadItem> Downloads { get; set; }

        public DbSet<ConflictItem> Conflicts { get; set; }

        public DbSet<RemoteFileModified> RemoteModifications { get; set; }

        public DbSet<LocalFileModified> LocalModifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            System.Diagnostics.Debug.Assert(Directory.Exists(Path.GetDirectoryName(this.databaseFullPath)), $"Database {this.databaseFullPath} does not exists.");
            var connectionString = $"Data Source={this.databaseFullPath}";
            System.Diagnostics.Trace.WriteLine(connectionString);
            optionsBuilder.UseSqlite(connectionString);
        }

        public bool EnsureDatabaseExists()
        {
            return this.Database.EnsureCreated();
        }

        public void AcceptAllChangesWithoutSaving()
        {
            this.ChangeTracker.AcceptAllChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            Account.RegisterType(modelBuilder);
        }

        public void RemoveAccountRecursive(Account account)
        {
            var objects = this.AccountSyncObjects.Where(x => x.Account == account).ToList();
            var localFilesModified = this.LocalModifications.Where(x => objects.Any(z => z.Path.StartsWith(x.Path))).ToList();
            var remoteFilesModified = this.RemoteModifications.Where(x => objects.Any(z => z.Path.StartsWith(x.Path))).ToList();
            var localFiles = this.LocalFiles.Where(x => objects.Any(z => z.Token == x.Token)).ToList();
            var trees = this.RemoteTrees.Where(x => objects.Any(z => z.AccountSyncObjectId == x.SyncObject.AccountSyncObjectId)).ToList();
            var remoteFiles = this.RemoteFiles.Where(x => trees.Any(z => z.RemoteTreeId == x.RemoteTreeId)).ToList();
            var downloads = this.Downloads.Where(x => objects.Any(z => z.AccountSyncObjectId == x.SyncObject.AccountSyncObjectId)).ToList();
            var uploads = this.Uploads.Where(x => objects.Any(z => z.Token == x.Token)).ToList();

            ///////////////////

            this.Uploads.RemoveRange(uploads);
            this.Downloads.RemoveRange(downloads);
            this.RemoteFiles.RemoveRange(remoteFiles);
            this.RemoteTrees.RemoveRange(trees);
            this.LocalFiles.RemoveRange(localFiles);
            this.LocalModifications.RemoveRange(localFilesModified);
            this.RemoteModifications.RemoveRange(remoteFilesModified);
            this.AccountSyncObjects.RemoveRange(objects);
            this.Accounts.Remove(account);
        }

        public void RemoveAccountSyncObjectRecursive(AccountSyncObject syncObject)
        {
            var localFilesModified = this.LocalModifications.Where(x => syncObject.Path.StartsWith(x.Path)).ToList();
            var remoteFilesModified = this.RemoteModifications.Where(x => syncObject.Path.StartsWith(x.Path)).ToList();
            var localFiles = this.LocalFiles.Where(x => syncObject.Token == x.Token).ToList();
            var trees = this.RemoteTrees.Where(x => syncObject.AccountSyncObjectId == x.SyncObject.AccountSyncObjectId).ToList();
            var remoteFiles = this.RemoteFiles.Where(x => trees.Any(z => z.RemoteTreeId == x.RemoteTreeId)).ToList();
            var downloads = this.Downloads.Where(x => syncObject.AccountSyncObjectId == x.SyncObject.AccountSyncObjectId).ToList();
            var uploads = this.Uploads.Where(x => syncObject.Token == x.Token).ToList();

            ///////////////////

            this.Uploads.RemoveRange(uploads);
            this.Downloads.RemoveRange(downloads);
            this.RemoteFiles.RemoveRange(remoteFiles);
            this.RemoteTrees.RemoveRange(trees);
            this.LocalFiles.RemoveRange(localFiles);
            this.LocalModifications.RemoveRange(localFilesModified);
            this.RemoteModifications.RemoveRange(remoteFilesModified);
            this.AccountSyncObjects.Remove(syncObject);
        }
    }
}
