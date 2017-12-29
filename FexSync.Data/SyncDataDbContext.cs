using System.IO;
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
    }
}
