using System;
using System.Collections.Generic;
using System.Linq;

using FexSync.Data;

namespace FexSync.Data.Windows.Tests
{
    public class WindowsFileSystemWatcherTest : FexSync.WindowsFileSystemWatcher
    {
        public static readonly TimeSpan TestWaitPeriod = FexSync.WindowsFileSystemWatcher.DefaultWaitPeriod.Add(FexSync.WindowsFileSystemWatcher.DefaultWaitPeriod).Add(FexSync.WindowsFileSystemWatcher.DefaultWaitPeriod);

        public WindowsFileSystemWatcherTest()
        {
            this.OnFileCreated += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFileDeleted += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFileModified += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFileMoved += (sender, e) => { this.FiredEvents_Add(e); };

            this.OnFolderCreated += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFolderDeleted += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFolderMoved += (sender, e) => { this.FiredEvents_Add(e); };

            this.OnError += (sender, e) => { e.GetException().Process(); };
        }

        private void FiredEvents_Add(EventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException();
            }

            this.FiredEvents.Add(e);

            if (e is FexSync.Data.FilePathEventArgs x)
            {
                System.Diagnostics.Trace.WriteLine($" = = = Fired event {e.ToString()} {x.FullPath} {DateTime.Now.ToString("HH:mm:ss:ffff")}");
            }
            else if (e is FexSync.Data.FilePathChangedEventArgs y)
            {
                System.Diagnostics.Trace.WriteLine($" = = = Fired event {e.ToString()} {y.OldPath} {y.NewPath} {DateTime.Now.ToString("HH:mm:ss:ffff")}");
            }
            else
            {
                System.Diagnostics.Trace.WriteLine($" = = = Fired event {e.ToString()} {DateTime.Now.ToString("HH:mm:ss:ffff")}");
            }
        }

        public IList<EventArgs> FiredEvents { get; private set; } = new ThreadSafeListWithLock<EventArgs>();

        public IList<FileSystemEventFilter> EventFilterPublic => new List<FileSystemEventFilter>();

        public void SetRaisingEvents(bool val)
        {
            this.Watchers.Values.ToList().ForEach(watcher => watcher.EnableRaisingEvents = val);
        }

        public override void Stop()
        {
            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            base.Stop();
        }
    }
}
