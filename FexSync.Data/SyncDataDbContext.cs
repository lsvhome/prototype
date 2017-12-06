using System.IO;
using Microsoft.EntityFrameworkCore;

namespace FexSync.Data
{
    public class SyncDataDbContext : DbContext, ISyncDataDbContext
    {
        private string databaseFullPath = "syncdata.db";

        public SyncDataDbContext() : base()
        {
        }

        public SyncDataDbContext(string databaseFullPath) : this()
        {
            this.databaseFullPath = databaseFullPath;
        }

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
            System.Diagnostics.Debug.WriteLine(connectionString);
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
    }
}
