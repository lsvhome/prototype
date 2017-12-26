using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FexSync.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FexSync.Data.Windows.Tests
{
    [TestClass]
    public class WindowsFileSystemWatcher_MoveFolder_TestFixture
    {
        [TestMethod]
        public void MoveFileWithinObjectFromRootToDeepTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName());
            var dstFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetFileName(srcFolder));
            Directory.CreateDirectory(Path.GetDirectoryName(dstFolder));
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Move(srcFolder, dstFolder);

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderMovedEventArgs);
                    var e = (FolderMovedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(srcFolder, e.OldPath);
                    Assert.AreEqual(dstFolder, e.NewPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void MoveFileWithinObjectFromDeepToRootTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            var dstFolder = Path.Combine(testDir, Path.GetFileName(srcFolder));
            Directory.CreateDirectory(Path.GetDirectoryName(srcFolder));
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Move(srcFolder, dstFolder);

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderMovedEventArgs);
                    var e = (FolderMovedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(srcFolder, e.OldPath);
                    Assert.AreEqual(dstFolder, e.NewPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void MoveFileWithinObjectFromDeepToDeepTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            var dstFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetFileName(srcFolder));
            Directory.CreateDirectory(Path.GetDirectoryName(srcFolder));
            Directory.CreateDirectory(Path.GetDirectoryName(dstFolder));
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Move(srcFolder, dstFolder);

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderMovedEventArgs);
                    var e = (FolderMovedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(srcFolder, e.OldPath);
                    Assert.AreEqual(dstFolder, e.NewPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void MoveFileOutsideObjectFromRootTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName());
            var dstFolder = Path.Combine(testDir, "..", Path.GetFileName(srcFolder));
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Move(srcFolder, dstFolder);

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderDeletedEventArgs);
                    var e = (FolderDeletedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(srcFolder, e.FullPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void MoveFileOutsideObjectFromDeepTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            var dstFolder = Path.Combine(testDir, "..", Path.GetFileName(srcFolder));
            Directory.CreateDirectory(Path.GetDirectoryName(srcFolder));
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Move(srcFolder, dstFolder);

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderDeletedEventArgs);
                    var e = (FolderDeletedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(srcFolder, e.FullPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void MoveFolleRenameInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName());
            var dstFolder = Path.Combine(Path.GetDirectoryName(srcFolder), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(srcFolder));
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Move(srcFolder, dstFolder);

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderMovedEventArgs);
                    var e = (FolderMovedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(srcFolder, e.OldPath);
                    Assert.AreEqual(dstFolder, e.NewPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void MoveFileRenameInSubFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            var dstFolder = Path.Combine(Path.GetDirectoryName(srcFolder), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(srcFolder));
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Move(srcFolder, dstFolder);

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderMovedEventArgs);
                    var e = (FolderMovedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(srcFolder, e.OldPath);
                    Assert.AreEqual(dstFolder, e.NewPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FileMove20FoldersTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);

            string[] fn = new string[20];
            Task[] tt = new Task[fn.Length];
            for (int i = 0; i < fn.Length; i++)
            {
                var srcFileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
                Directory.CreateDirectory(srcFileName);
                fn[i] = srcFileName;
            }

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    for (int i = 0; i < fn.Length; i++)
                    {
                        var srcFolder = fn[i];
                        tt[i] = Task.Run(() =>
                        {
                            var dstFolder = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(srcFolder)),  Path.GetFileName(srcFolder));
                            Assert.IsTrue(Directory.Exists(srcFolder));
                            Assert.IsFalse(Directory.Exists(dstFolder));
                            Directory.Move(srcFolder, dstFolder);
                            Assert.IsFalse(Directory.Exists(srcFolder));
                            Assert.IsTrue(Directory.Exists(dstFolder));
                            System.Diagnostics.Trace.WriteLine($"Moved Test folder is {srcFolder}");
                        });
                    }

                    for (int i = 0; i < fn.Length; i++)
                    {
                        tt[i].Wait();
                    }

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                    w.Stop();

                    Assert.AreEqual(fn.Length, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.All(x => x is FolderMovedEventArgs));
                    Assert.AreEqual(0, w.EventFilterPublic.Count);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }
    }
}
