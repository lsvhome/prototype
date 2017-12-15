//// #define FSEVENTS_DEBUG
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

        private static object lockObj = new object();

        private Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();

        protected Dictionary<string, FileSystemWatcher> Watchers => this.watchers;

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            e.GetException().Process();
        }

        public void Start(IEnumerable<DirectoryInfo> folders)
        {
            foreach (var each in folders)
            {
                var watcher = new FileSystemWatcher
                {
                    Path = each.FullName,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.Size
                };

                watcher.Created += this.Watcher_Created;
                watcher.Changed += this.Watcher_Changed;
                watcher.Renamed += this.Watcher_Renamed;
                watcher.Deleted += this.Watcher_Deleted;
                watcher.Disposed += this.Watcher_Disposed;
                watcher.Error += this.Watcher_Error;

                this.watchers.Add(each.FullName, watcher);
            }

            this.watchers.Values.ToList().ForEach(watcher => watcher.EnableRaisingEvents = true);
        }

        private void Watcher_Disposed(object sender, EventArgs e)
        {
            this.DebugMessage($"Watcher_Disposed");
        }

        public void Stop()
        {
            this.DebugMessage($"Watcher Stop() {DateTime.Now.ToString("HH:mm:ss:ffff")}");
            this.watchers.Values.ToList().ForEach(watcher => watcher.EnableRaisingEvents = false);
            this.watchers.Values.ToList().ForEach(watcher => watcher.Dispose());
            this.watchers.Clear();
        }

        public void Dispose()
        {
            this.DebugMessage($"Watcher Dispose() {DateTime.Now.ToString("HH:mm:ss:ffff")}");
            this.Stop();
        }

        public event EventHandler<FexSync.Data.FileCreatedEventArgs> OnFileCreated;

        public event EventHandler<FexSync.Data.FileOrFolderDeletedEventArgs> OnFileOrFolderDeleted;

        public event EventHandler<FexSync.Data.FileModifiedEventArgs> OnFileModified;

        public event EventHandler<FexSync.Data.FileMovedEventArgs> OnFileMoved;

        public event EventHandler<FexSync.Data.FolderCreatedEventArgs> OnFolderCreated;

        public event EventHandler<FexSync.Data.FolderDeletedEventArgs> OnFolderDeleted;

        public event EventHandler<FexSync.Data.FolderMovedEventArgs> OnFolderMoved;

        private ThreadSafeListWithLock<FileSystemEventFilter> eventFilter = new ThreadSafeListWithLock<FileSystemEventFilter>();

        private void Filter_Add(FileSystemEventFilter f)
        {
            this.eventFilter.Add(f);
            this.DebugMessage($"Filter_Add {DateTime.Now.ToString("HH:mm:ss:ffff")}");
        }

        private bool ShouldSuppress(FileSystemEventArgs e)
        {
            this.DebugMessage($"ShouldSuppress begin {eventFilter.Count} {DateTime.Now.ToString("HH:mm:ss:ffff")}");
            bool removed = false;
            do
            {
                removed = false;
                if (this.eventFilter.Any())
                {
                    var firstFilter = this.eventFilter.First();
                    removed = firstFilter.StopMoment < DateTime.Now;
                    removed |= firstFilter.Completed != null && firstFilter.Completed();
                    if (removed)
                    {
                        this.DebugMessage("Filter removed");
                        this.eventFilter.RemoveAt(0);
                    }
                }
            }
            while (removed);

            foreach (var each in this.eventFilter)
            {
                this.DebugMessage($"each.filterConditionShouldSuppress != null => {each.FilterConditionShouldSuppress != null}");
                this.DebugMessage($"each.filterConditionShouldSuppress(e) => {each.FilterConditionShouldSuppress(e)}");
                this.DebugMessage($"each.dates => {each.StopMoment <= DateTime.Now}    {each.StopMoment.ToString("HH:mm:ss:ffff")} {DateTime.Now.ToString("HH:mm:ss:ffff")}");
                if (each.FilterConditionShouldSuppress != null && each.FilterConditionShouldSuppress(e) && each.StopMoment >= DateTime.Now)
                {
                    this.DebugMessage($"ShouldSuppress true {DateTime.Now.ToString("HH:mm:ss:ffff")}");
                    return true;
                }
            }

            this.DebugMessage($"ShouldSuppress false {DateTime.Now.ToString("HH:mm:ss:ffff")}");
            return false;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            this.DebugMessage($"Watcher_Created {e.ChangeType.ToString()} : {e.FullPath}");

            if (!this.ShouldSuppress(e))
            {
                //// File or directory created
                this.Filter_Add(new FileSystemEventFilter { FilterConditionShouldSuppress = (FileSystemEventArgs e1) => { return e1.FullPath == e.FullPath && e1.ChangeType == WatcherChangeTypes.Changed; }, StopMoment = DateTime.Now.Add(DefaultWaitPeriod) });
                this.Filter_Add(new FileSystemEventFilter { FilterConditionShouldSuppress = (FileSystemEventArgs e1) => { return e1.FullPath == Path.GetDirectoryName(e.FullPath) && e1.ChangeType == WatcherChangeTypes.Changed; }, StopMoment = DateTime.Now.Add(DefaultWaitPeriod) });

                if (File.Exists(e.FullPath))
                {
                    if (this.OnFileCreated != null)
                    {
                        this.OnFileCreated(this, new FexSync.Data.FileCreatedEventArgs { FullPath = e.FullPath });
                    }
                }
                else if (Directory.Exists(e.FullPath))
                {
                    if (this.OnFolderCreated != null)
                    {
                        this.OnFolderCreated(this, new FexSync.Data.FolderCreatedEventArgs { FullPath = e.FullPath });
                    }
                }
                else
                {
                    throw new ApplicationException();
                }
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            this.DebugMessage($"Watcher_Deleted {e.ChangeType.ToString()} : {e.FullPath}");
            string moved = null; //// indicator move operation instead of delete (move = delete + create)
            string deletedRoot = e.FullPath; //// For folder deletion: first deleted event may be file(s) within deleted folder, we want fire only event of deletion of higest folder
            if (!this.ShouldSuppress(e))
            {
                var stopMoment = DateTime.Now.Add(DefaultWaitPeriod);
                this.Filter_Add(new FileSystemEventFilter { FilterConditionShouldSuppress = (FileSystemEventArgs e1) => { return e1.FullPath == e.FullPath && e1.ChangeType == WatcherChangeTypes.Changed; }, StopMoment = stopMoment });
                this.Filter_Add(new FileSystemEventFilter { FilterConditionShouldSuppress = (FileSystemEventArgs e1) => { return e1.FullPath == Path.GetDirectoryName(e.FullPath) && e1.ChangeType == WatcherChangeTypes.Changed; }, StopMoment = stopMoment });

                this.Filter_Add(new FileSystemEventFilter
                {
                    FilterConditionShouldSuppress = (FileSystemEventArgs e1) =>
                    {
                        var ret = e.FullPath.Contains(e1.FullPath) && e1.ChangeType == WatcherChangeTypes.Deleted;
                        if (ret)
                        {
                            if (deletedRoot.Contains(e1.FullPath) && e1.ChangeType == WatcherChangeTypes.Deleted)
                            {
                                deletedRoot = e1.FullPath;
                            }
                        }

                        return ret;
                    },
                    StopMoment = stopMoment
                });

                this.Filter_Add(new FileSystemEventFilter
                {
                    FilterConditionShouldSuppress = (FileSystemEventArgs e1) =>
                    {
                        this.DebugMessage($"suppressedEvent = {e1.Name} {e.Name} |  {e1.FullPath}  {e.FullPath} | {e1.ChangeType}");

                        var ret = Path.GetFileName(e1.Name) == Path.GetFileName(e.Name) && e1.FullPath != e.FullPath && e1.ChangeType == WatcherChangeTypes.Created;
                        if (ret)
                        {
                            moved = e1.FullPath;
                            this.Filter_Add(new FileSystemEventFilter { FilterConditionShouldSuppress = (FileSystemEventArgs e2) => { return e2.FullPath == Path.GetDirectoryName(e1.FullPath) && e2.ChangeType == WatcherChangeTypes.Changed; }, StopMoment = stopMoment });
                        }

                        return ret;
                    },
                    StopMoment = stopMoment
                });

                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(DefaultWaitPeriod);
                    if (!string.IsNullOrWhiteSpace(moved))
                    {
                        if (File.Exists(moved))
                        {
                            if (this.OnFileMoved != null)
                            {
                                this.OnFileMoved(this, new FexSync.Data.FileMovedEventArgs { OldPath = e.FullPath, NewPath = moved });
                            }
                        }
                        else if (Directory.Exists(moved))
                        {
                            if (this.OnFolderMoved != null)
                            {
                                this.OnFolderMoved(this, new FexSync.Data.FolderMovedEventArgs { OldPath = e.FullPath, NewPath = moved });
                            }
                        }
                        else
                        {
                            throw new ApplicationException();
                        }
                    }
                    else
                    {
                        if (deletedRoot != e.FullPath)
                        {
                            if (this.OnFolderDeleted != null)
                            {
                                this.OnFolderDeleted(this, new FexSync.Data.FolderDeletedEventArgs { FullPath = deletedRoot });
                            }
                        }
                        else
                        {
                            if (this.OnFileOrFolderDeleted != null)
                            {
                                this.OnFileOrFolderDeleted(this, new FexSync.Data.FileOrFolderDeletedEventArgs { FullPath = deletedRoot });
                            }
                        }
                    }
                });
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            this.DebugMessage($"Watcher_Changed {e.ChangeType.ToString()} : {e.FullPath}");

            if (!this.ShouldSuppress(e))
            {
                //// File modified
                this.Filter_Add(new FileSystemEventFilter { FilterConditionShouldSuppress = (FileSystemEventArgs e1) => { return e1.FullPath == e.FullPath && e1.ChangeType == WatcherChangeTypes.Changed; }, StopMoment = DateTime.Now.Add(DefaultWaitPeriod) });

                if (File.Exists(e.FullPath))
                {
                    if (this.OnFileModified != null)
                    {
                        this.OnFileModified(this, new FexSync.Data.FileModifiedEventArgs { FullPath = e.FullPath });
                    }
                }
                else
                {
                    throw new ApplicationException();
                }
            }
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            this.DebugMessage($"Watcher_Renamed {e.ChangeType.ToString()} : {e.FullPath}");

            if (!this.ShouldSuppress(e))
            {
                //// File or folder renamed
                this.Filter_Add(new FileSystemEventFilter { FilterConditionShouldSuppress = (FileSystemEventArgs e1) => { return e1.FullPath == Path.GetDirectoryName(e.FullPath) && e1.ChangeType == WatcherChangeTypes.Changed; }, StopMoment = DateTime.Now.Add(DefaultWaitPeriod) });

                if (File.Exists(e.FullPath))
                {
                    if (this.OnFileMoved != null)
                    {
                        this.OnFileMoved(this, new FexSync.Data.FileMovedEventArgs { OldPath = e.OldFullPath, NewPath = e.FullPath });
                    }
                }
                else if (Directory.Exists(e.FullPath))
                {
                    if (this.OnFolderMoved != null)
                    {
                        this.OnFolderMoved(this, new FexSync.Data.FolderMovedEventArgs { OldPath = e.OldFullPath, NewPath = e.FullPath });
                    }
                }
                else
                {
                    throw new ApplicationException();
                }
            }
        }

        private void DebugMessage(string msg)
        {
#if FSEVENTS_DEBUG
            System.Diagnostics.Debug.WriteLine(msg);
#endif
        }
    }
}
