namespace FexSync.Data
{
    public class DownloadItem : RemoteFileBase
    {
        public int DownloadItemId { get; set; }

        public int TriesCount { get; set; } = 0;

        public System.DateTime ItemCreated { get; set; }
    }
}
