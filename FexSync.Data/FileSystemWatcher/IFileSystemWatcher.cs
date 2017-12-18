using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FexSync.Data
{
    public interface IFileSystemWatcher : IDisposable
    {
        void Start(IEnumerable<DirectoryInfo> folders);

        void Stop();

        event EventHandler<FexSync.Data.FileCreatedEventArgs> OnFileCreated;

        event EventHandler<FexSync.Data.FileOrFolderDeletedEventArgs> OnFileOrFolderDeleted;

        event EventHandler<FexSync.Data.FileModifiedEventArgs> OnFileModified;

        event EventHandler<FexSync.Data.FileMovedEventArgs> OnFileMoved;

        event EventHandler<FexSync.Data.FolderCreatedEventArgs> OnFolderCreated;

        event EventHandler<FexSync.Data.FolderDeletedEventArgs> OnFolderDeleted;

        event EventHandler<FexSync.Data.FolderMovedEventArgs> OnFolderMoved;
    }
}
