using System;
using System.Collections.Generic;
using System.Linq;

namespace FexSync.Windows.Tests
{
    public class WindowsFileSystemWatcherTest : FexSync.WindowsFileSystemWatcher
    {
        public static readonly TimeSpan TestWaitPeriod = FexSync.WindowsFileSystemWatcher.DefaultWaitPeriod.Add(FexSync.WindowsFileSystemWatcher.DefaultWaitPeriod).Add(FexSync.WindowsFileSystemWatcher.DefaultWaitPeriod);

        public WindowsFileSystemWatcherTest()
        {
            this.OnFileCreated += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFileOrFolderDeleted += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFileModified += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFileMoved += (sender, e) => { this.FiredEvents_Add(e); };

            this.OnFolderCreated += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFolderDeleted += (sender, e) => { this.FiredEvents_Add(e); };
            this.OnFolderMoved += (sender, e) => { this.FiredEvents_Add(e); };
        }

        private void FiredEvents_Add(EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($" = = = Fired event {e.ToString()}");
            this.FiredEvents.Add(e);
        }

        public List<EventArgs> FiredEvents = new List<EventArgs>();

        public void SetRaisingEvents(bool val)
        {
            this.Watchers.Values.ToList().ForEach(watcher => watcher.EnableRaisingEvents = val);
        }
    }
}
