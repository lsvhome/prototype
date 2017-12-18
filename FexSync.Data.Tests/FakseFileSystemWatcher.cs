using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FexSync.Data.Tests
{
    public class FakseFileSystemWatcher : FexSync.Data.IFileSystemWatcher
    {
        public void Dispose()
        {
        }

        public void Start(IEnumerable<DirectoryInfo> folders)
        {
        }

        public void Stop()
        {
        }

#pragma warning disable CS0067
        public event EventHandler<FileCreatedEventArgs> OnFileCreated;

        public event EventHandler<FileOrFolderDeletedEventArgs> OnFileOrFolderDeleted;

        public event EventHandler<FileModifiedEventArgs> OnFileModified;

        public event EventHandler<FileMovedEventArgs> OnFileMoved;

        public event EventHandler<FolderCreatedEventArgs> OnFolderCreated;

        public event EventHandler<FolderDeletedEventArgs> OnFolderDeleted;

        public event EventHandler<FolderMovedEventArgs> OnFolderMoved;
#pragma warning restore CS0067
    }
}
