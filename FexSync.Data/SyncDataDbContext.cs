
using Microsoft.EntityFrameworkCore;

namespace FexSync.Data
{
    public class SyncDataDbContext : DbContext, ISyncDataDbContext
    {
        private string dbFullPath = "syncdata.db";

        public SyncDataDbContext():base()
        {
        }

        public SyncDataDbContext(string dbFullPath) : base()
        {
            this.dbFullPath = dbFullPath;
        }

        public DbSet<RemoteFile> RemoteFiles { get; set; }

        public DbSet<LocalFile> Local { get; set; }

        public DbSet<UploadItem> Upload { get; set; }

        public DbSet<DownloadItem> Download { get; set; }

        public DbSet<RemoveLocalItem> RemoveLocal { get; set; }

        public DbSet<RemoveRemoteItem> RemoveRemote { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={dbFullPath}");
        }

        public bool EnsureCreated()
        {
            return this.Database.EnsureCreated();
        }
    }
}
