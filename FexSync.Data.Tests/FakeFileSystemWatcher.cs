using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FexSync.Data.Tests
{
    public class FakeFileSystemWatcher : FexSync.Data.IFileSystemWatcher
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

        public void AddFilterPath(string path)
        {
        }

        public void RemoveFilterPath(string path)
        {
        }

#pragma warning disable CS0067
        public event EventHandler<FileCreatedEventArgs> OnFileCreated;

        public event EventHandler<FileDeletedEventArgs> OnFileDeleted;

        public event EventHandler<FileModifiedEventArgs> OnFileModified;

        public event EventHandler<FileMovedEventArgs> OnFileMoved;

        public event EventHandler<FolderCreatedEventArgs> OnFolderCreated;

        public event EventHandler<FolderDeletedEventArgs> OnFolderDeleted;

        public event EventHandler<FolderMovedEventArgs> OnFolderMoved;

        public event EventHandler<ErrorEventArgs> OnError;
#pragma warning restore CS0067
    }
}
