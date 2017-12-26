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
    public class WindowsFileSystemWatcher_DeleteFile_TestFixture
    {
        [TestMethod]
        public void FileDeleteInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            File.WriteAllText(fileName, string.Empty);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    System.Diagnostics.Trace.WriteLine($"Start Deleteing {fileName} {DateTime.Now.ToString("HH:mm:ss:ffff")}");
                    File.Delete(fileName);
                    System.Diagnostics.Trace.WriteLine($"Finished Deleteing {fileName} {DateTime.Now.ToString("HH:mm:ss:ffff")}");

                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FileDeletedEventArgs);
                    var e = (FileDeletedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(fileName, e.FullPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FileDeleteInSubFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            File.WriteAllText(fileName, string.Empty);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    File.Delete(fileName);

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FileDeletedEventArgs);
                    var e = (FileDeletedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(fileName, e.FullPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FileDelete20FilesTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);

            string[] fn = new string[20];
            Task[] tt = new Task[fn.Length];
            for (int i = 0; i < fn.Length; i++)
            {
                var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                File.WriteAllText(fileName, string.Empty);
                fn[i] = fileName;
            }

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    for (int i = 0; i < fn.Length; i++)
                    {
                        var fileName = fn[i];
                        tt[i] = Task.Run(() =>
                        {
                            Assert.IsTrue(File.Exists(fileName));
                            File.Delete(fileName);
                            Assert.IsFalse(File.Exists(fileName));
                            System.Diagnostics.Trace.WriteLine($"Deleted Test file is {fileName}");
                        });
                    }

                    for (int i = 0; i < fn.Length; i++)
                    {
                        tt[i].Wait();
                    }

                    w.Stop();

                    Assert.AreEqual(fn.Length, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.All(x => x is FileDeletedEventArgs));
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
