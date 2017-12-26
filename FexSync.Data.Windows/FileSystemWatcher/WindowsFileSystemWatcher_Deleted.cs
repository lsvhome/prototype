//// 
#define FSEVENTS_DEBUG
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FexSync.Data;

namespace FexSync
{
    public partial class WindowsFileSystemWatcher
    {
        private DateTime lastDeletedEvent = DateTime.Now.AddDays(-1);

        private ThreadSafeListWithLock<Task> scheduledTasks = new ThreadSafeListWithLock<Task>();

        private ThreadSafeListWithLock<string> deletedItems = new ThreadSafeListWithLock<string>();

        private object deletedLock = new object();

        private void Watcher_DeletedFile(object sender, FileSystemEventArgs e)
        {
            Action<string, string> fireMoved = (pathFrom, pathTo) =>
            {
                var movedEventHandler = this.OnFileMoved;
                if (movedEventHandler != null)
                {
                    movedEventHandler(this, new FexSync.Data.FileMovedEventArgs { OldPath = pathFrom, NewPath = pathTo });
                }
            };

            Action<string> fireDeleted = (fullPath) =>
            {
                if (!filterPath.Contains(fullPath))
                {
                    var deletedEventHandler = this.OnFileDeleted;
                    if (deletedEventHandler != null)
                    {
                        deletedEventHandler(this, new FexSync.Data.FileDeletedEventArgs { FullPath = fullPath });
                    }
                }
            };

            this.Watcher_DeletedCommon(sender, e, fireMoved, fireDeleted);
        }

        private void Watcher_DeletedFolder(object sender, FileSystemEventArgs e)
        {
            Action<string, string> fireMoved = (movedFromPath, movedToPath) => 
            {
                var movedEventHandler = this.OnFolderMoved;
                if (movedEventHandler != null)
                {
                    movedEventHandler(this, new FexSync.Data.FolderMovedEventArgs { OldPath = movedFromPath, NewPath = movedToPath });
                }
            };

            Action<string> fireDeleted = (deletedFilePath) => 
            {
                if (!filterPath.Contains(deletedFilePath))
                {
                    var deletedEventHandler = this.OnFolderDeleted;
                    if (deletedEventHandler != null)
                    {
                        deletedEventHandler(this, new FexSync.Data.FolderDeletedEventArgs { FullPath = deletedFilePath });
                    }
                }
            };

            this.Watcher_DeletedCommon(sender, e, fireMoved, fireDeleted);
        }

        private event EventHandler<CancellableFileSystemEventArgs> ShouldSuppressDeleted = delegate { };

        private void Watcher_DeletedCommon(object sender, FileSystemEventArgs e, Action<string, string> fireMoved, Action<string> fireDeleted)
        {
            this.DebugMessage($"Watcher_Deleted {e.ChangeType.ToString()} : {e.FullPath} :");

            if (this.filterPath.Contains(e.FullPath))
            {
                return;
            }

            string movedToPath = null; //// indicator move operation instead of delete (move = delete + create)

            EventHandler<CancellableFileSystemEventArgs> createdHandler = null;
            createdHandler = (sender1, createdArgs) =>
            {
                if (Path.GetFileName(createdArgs.FileSystemEventArgs.FullPath) == Path.GetFileName(e.FullPath)
                    && createdArgs.FileSystemEventArgs.FullPath != e.FullPath
                    && createdArgs.FileSystemEventArgs.ChangeType == WatcherChangeTypes.Created)
                {
                    this.ShouldSuppressCreated -= createdHandler;
                    createdArgs.Suppress = true;
                    movedToPath = createdArgs.FileSystemEventArgs.FullPath;
                    ScheduleDeferredTask(() => { fireMoved(e.FullPath, createdArgs.FileSystemEventArgs.FullPath); });
                }
            };

            this.ShouldSuppressCreated += createdHandler;

            Action deferredDeletedAction = () =>
            {
                if (string.IsNullOrWhiteSpace(movedToPath))
                {
                    this.ShouldSuppressCreated -= createdHandler;

                    deletedItems.Add(e.FullPath);
                    lock (deletedLock)
                    {
                        lastDeletedEvent = DateTime.Now;
                    }

                    Action waitParentDeleteAction = () =>
                    {
                        DateTime continueToAnalizeMoment;

                        //// Ensure all deleted events catched and deletion finished
                        do
                        {
                            System.Threading.Thread.Sleep(DefaultWaitPeriod);
                            lock (deletedLock)
                            {
                                continueToAnalizeMoment = lastDeletedEvent.Add(DefaultWaitPeriod);
                            }
                        }
                        while (continueToAnalizeMoment >= DateTime.Now);

                        if (IsParentPathDeleted(e.FullPath))
                        {
                            return;
                        }
                        else
                        {
                            deletedItems.RemoveAll(x => x.Contains(e.FullPath));

                            fireDeleted(e.FullPath);
                        }
                    };

                    ScheduleDeferredTask(DefaultWaitPeriod.Add(DefaultWaitPeriod), waitParentDeleteAction);
                }
            };

            this.ScheduleTask(deferredDeletedAction);
        }

        private bool IsParentPathDeleted(string pathDeleted)
        {
            try
            {
                var clonedItems = this.deletedItems.Clone();

                return
                    clonedItems.Count == 0 // parent event cleared all subpaths
                    || !clonedItems.Contains(pathDeleted)   // parent event cleared all subpaths
                    || !(clonedItems.Where(x => pathDeleted.Contains(x)).OrderBy(d => d).FirstOrDefault() == pathDeleted); // deletedItems does not contain parent for pathDeleted
            }
            catch (Exception ex)
            {
                ex.Process();
                throw;
            }
        }

        private void RunTaskImmidiately(Action action)
        {
            using (AutoResetEvent waiter = new AutoResetEvent(false))
            {
                Task task = Task.Run(() => 
                {
                    try
                    {
                        waiter.Set();
                        action();
                    }
                    catch (Exception ex)
                    {
                        ex.Process();
                    }
                });

                this.scheduledTasks.Add(task);
                this.scheduledTasks.RemoveAll(item => item.IsCompleted);
                waiter.WaitOne();

                //// Ensure action is running
                System.Threading.Thread.Sleep(1);
            }
        }

        private void ScheduleTask(Action action)
        {
            this.ScheduleDeferredTask(TaskScheduleDelay, action);
        }

        private void ScheduleDeferredTask(TimeSpan delay, Action action)
        {
            this.scheduledTasks.RemoveAll(item => item.IsCompleted);

            Action safeAction = () =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    ex.Process();
                }
            };

            this.scheduledTasks.Add(Task.Delay(delay).ContinueWith(task => { safeAction(); }));
        }

        private void ScheduleDeferredTask(Action action)
        {
            this.ScheduleDeferredTask(DefaultWaitPeriod, action);
        }
    }
}
