using System;
using System.IO;
using System.Linq;
using System.Threading;

using FexSync.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FexSync.Data.Windows.Tests
{
    [TestClass]
    public class WindowsFileSystemWatcher_CreateFolder_TestFixture
    {
        [TestMethod]
        public void CreateFolderInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var newFolder = Path.Combine(testDir, Path.GetRandomFileName());

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.CreateDirectory(newFolder);
                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderCreatedEventArgs);
                    var e = (FolderCreatedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(newFolder, e.FullPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }
    }
}
