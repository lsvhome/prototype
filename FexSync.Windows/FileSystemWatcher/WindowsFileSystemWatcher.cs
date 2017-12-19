﻿//// #define FSEVENTS_DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FexSync.Data;

namespace FexSync
{
    public partial class WindowsFileSystemWatcher : FexSync.Data.IFileSystemWatcher
    {
        public static readonly TimeSpan DefaultWaitPeriod = TimeSpan.FromMilliseconds(500);

        public static readonly TimeSpan TaskScheduleDelay = TimeSpan.FromMilliseconds(100);

        private Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();

        protected Dictionary<string, FileSystemWatcher> Watchers => this.watchers;

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            if (this.OnError != null)
            {
                this.OnError(this, e);
            }
            else
            {
                Exception ex = e.GetException();
                ex.Process();
            }
        }

        public void Start(IEnumerable<DirectoryInfo> folders)
        {
            foreach (var eachFolder in folders)
            {
                var watcherForFiles = new FileSystemWatcher
                {
                    Path = eachFolder.FullName,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
                };

                watcherForFiles.Created += this.Watcher_CreatedFile;
                watcherForFiles.Changed += this.Watcher_ChangedFile;
                watcherForFiles.Renamed += this.Watcher_RenamedFile;
                watcherForFiles.Deleted += this.Watcher_DeletedFile;
                watcherForFiles.Disposed += this.Watcher_Disposed;
                watcherForFiles.Error += this.Watcher_Error;

                this.watchers.Add(eachFolder.FullName + "F", watcherForFiles);

                var watcherForFolders = new FileSystemWatcher
                {
                    Path = eachFolder.FullName,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.DirectoryName
                };

                watcherForFolders.Created += this.Watcher_CreatedFolder;
                watcherForFolders.Renamed += this.Watcher_RenamedFolder;
                watcherForFolders.Deleted += this.Watcher_DeletedFolder;
                watcherForFolders.Disposed += this.Watcher_Disposed;
                watcherForFolders.Error += this.Watcher_Error;

                this.watchers.Add(eachFolder.FullName + "D", watcherForFolders);
            }

            this.watchers.Values.ToList().ForEach(watcher => watcher.EnableRaisingEvents = true);
        }

        private void Watcher_Disposed(object sender, EventArgs e)
        {
            this.DebugMessage($"Watcher_Disposed");
        }

        public virtual void Stop()
        {
            this.DebugMessage($"Watcher begin Stop() {DateTime.Now.ToString("HH:mm:ss:ffff")}");

            //// wait all tasks comlpleted
            this.watchers.Values.ToList().ForEach(watcher => watcher.EnableRaisingEvents = false);

            Task executingTask;
            while ((executingTask = this.scheduledTasks.FirstOrDefault(x => !x.IsCompleted)) != null)
            {
                executingTask.Wait();
            }

            this.watchers.Values.ToList().ForEach(watcher => watcher.Dispose());
            this.watchers.Clear();

            this.DebugMessage($"Watcher finished Stop() {DateTime.Now.ToString("HH:mm:ss:ffff")}");
        }

        public void Dispose()
        {
            this.DebugMessage($"Watcher Dispose() {DateTime.Now.ToString("HH:mm:ss:ffff")}");
            this.Stop();
        }

        public event EventHandler<FexSync.Data.FileCreatedEventArgs> OnFileCreated;

        public event EventHandler<FexSync.Data.FileDeletedEventArgs> OnFileDeleted;

        public event EventHandler<FexSync.Data.FileModifiedEventArgs> OnFileModified;

        public event EventHandler<FexSync.Data.FileMovedEventArgs> OnFileMoved;

        public event EventHandler<FexSync.Data.FolderCreatedEventArgs> OnFolderCreated;

        public event EventHandler<FexSync.Data.FolderDeletedEventArgs> OnFolderDeleted;

        public event EventHandler<FexSync.Data.FolderMovedEventArgs> OnFolderMoved;

        public event EventHandler<ErrorEventArgs> OnError;

        private event EventHandler<CancellableFileSystemEventArgs> ShouldSuppressCreated = delegate { };

        private void Watcher_CreatedFile(object sender, FileSystemEventArgs e)
        {
            this.DebugMessage($"Watcher_Created {e.ChangeType.ToString()} : {e.FullPath}");

            var createdArgs = new CancellableFileSystemEventArgs(e);

            this.ShouldSuppressCreated(this, createdArgs);

            if (!createdArgs.Suppress)
            {
                EventHandler<CancellableFileSystemEventArgs> fileChangedHandler = null;
                fileChangedHandler = (sender1, args) =>
                {
                    if (args.FileSystemEventArgs.FullPath == e.FullPath)
                    {
                        this.ShouldSuppressChangedFile -= fileChangedHandler;
                        args.Suppress = true;
                        var fileCreatedHandler = this.OnFileCreated;
                        if (fileCreatedHandler != null)
                        {
                            this.ScheduleTask(() =>
                            {
                                while (true)
                                {
                                    try
                                    {
                                        using (File.OpenWrite(e.FullPath))
                                        {
                                        }

                                        fileCreatedHandler(this, new FexSync.Data.FileCreatedEventArgs { FullPath = e.FullPath });
                                        return;
                                    }
                                    catch (Exception)
                                    {
                                        System.Threading.Thread.Sleep(DefaultWaitPeriod);
                                    }
                                }
                            });
                        }
                    }
                };

                this.ShouldSuppressChangedFile += fileChangedHandler;
            }
        }

        private void Watcher_CreatedFolder(object sender, FileSystemEventArgs e)
        {
            this.DebugMessage($"Watcher_CreatedFolder {e.ChangeType.ToString()} : {e.FullPath}");

            var createdArgs = new CancellableFileSystemEventArgs(e);

            this.ShouldSuppressCreated(this, createdArgs);

            if (!createdArgs.Suppress)
            {
                if (this.OnFolderCreated != null)
                {
                    this.ScheduleTask(() =>
                    {
                        this.OnFolderCreated(this, new FexSync.Data.FolderCreatedEventArgs { FullPath = e.FullPath });
                    });
                }
            }
        }

        private event EventHandler<CancellableFileSystemEventArgs> ShouldSuppressChangedFile = delegate { };

        private void Watcher_ChangedFile(object sender, FileSystemEventArgs e)
        {
            this.DebugMessage($"Watcher_Changed {e.ChangeType.ToString()} : {e.FullPath}");

            var changedArgs = new CancellableFileSystemEventArgs(e);
            this.ShouldSuppressChangedFile(this, changedArgs);

            if (!changedArgs.Suppress)
            {
                //// File modified

                EventHandler<CancellableFileSystemEventArgs> nestedChangeCatcher = null;
                nestedChangeCatcher = (sender1, testedChangedArgs) =>
                {
                    if (testedChangedArgs.FileSystemEventArgs.FullPath == e.FullPath)
                    {
                        this.ShouldSuppressChangedFile -= nestedChangeCatcher;
                        testedChangedArgs.Suppress = true;
                    }
                };

                this.ShouldSuppressChangedFile += nestedChangeCatcher;

                if (this.OnFileModified != null)
                {
                    this.ScheduleTask(() =>
                    {
                        while (true)
                        {
                            try
                            {
                                using (File.Open(e.FullPath, FileMode.Open))
                                {
                                }

                                this.ShouldSuppressChangedFile -= nestedChangeCatcher;
                                this.OnFileModified(this, new FexSync.Data.FileModifiedEventArgs { FullPath = e.FullPath });
                                return;
                            }
                            catch (Exception)
                            {
                                System.Threading.Thread.Sleep(DefaultWaitPeriod);
                            }
                        }
                    });
                }
            }
        }

        private void Watcher_RenamedFile(object sender, RenamedEventArgs e)
        {
            this.DebugMessage($"Watcher_Renamed {e.ChangeType.ToString()} : {e.FullPath}");

            if (this.OnFileMoved != null)
            {
                this.ScheduleTask(() => { this.OnFileMoved(this, new FexSync.Data.FileMovedEventArgs { OldPath = e.OldFullPath, NewPath = e.FullPath }); });
            }
        }

        private void Watcher_RenamedFolder(object sender, RenamedEventArgs e)
        {
            this.DebugMessage($"Watcher_Renamed {e.ChangeType.ToString()} : {e.FullPath}");

            if (this.OnFolderMoved != null)
            {
                this.ScheduleTask(() => { this.OnFolderMoved(this, new FexSync.Data.FolderMovedEventArgs { OldPath = e.OldFullPath, NewPath = e.FullPath }); });
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DebugMessage(string msg)
        {
#if FSEVENTS_DEBUG
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss:ffff")}  " + msg);
#endif
        }
    }
}
