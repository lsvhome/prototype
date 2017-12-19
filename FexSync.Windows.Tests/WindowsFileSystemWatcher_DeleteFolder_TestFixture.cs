using System;
using System.IO;
using System.Linq;
using System.Threading;

using FexSync.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace FexSync.Windows.Tests
{
    [TestClass]
    public class WindowsFileSystemWatcher_DeleteFolder_TestFixture
    {
        [TestMethod]
        public void FolderDeleteInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName());
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Delete(srcFolder);

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
        public void FolderDeleteInSubFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(srcFolder));
            Directory.CreateDirectory(srcFolder);
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    Directory.Delete(srcFolder);

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
        public void FolderDelete20FilesTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);

            string[] fn = new string[20];
            Task[] tt = new Task[fn.Length];
            for (int i = 0; i < fn.Length; i++)
            {
                var folderName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
                Directory.CreateDirectory(folderName);
                fn[i] = folderName;
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
                            {
                                //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);


                                Assert.IsTrue(Directory.Exists(srcFolder));
                                Directory.Delete(srcFolder);
                                Assert.IsFalse(Directory.Exists(srcFolder));
                                System.Diagnostics.Debug.WriteLine($"Deleted Test folder is {srcFolder}");
                            }

                        });

                    }

                    for (int i = 0; i < fn.Length; i++)
                        tt[i].Wait();

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                    Assert.AreEqual(fn.Length, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.All(x => x is FolderDeletedEventArgs));
                    Assert.AreEqual(0, w.EventFilterPublic.Count);
                    //var e = (FileModifiedEventArgs)w.FiredEvents.Single();
                    //Assert.AreEqual(fileName, e.FullPath);
                    w.Stop();
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FolderDeleteRecursive10Test()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);

            int k = 10;
            string[] fn = new string[k];
            Task[] tt = new Task[fn.Length];

            for (int i = 0; i < fn.Length; i++)
            {
                var folderName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
                Directory.CreateDirectory(folderName);
                fn[i] = folderName;
                
                for (int i1 = 0; i1 < k; i1++)
                {
                    var srcFolder2 = Path.Combine(folderName, Path.GetRandomFileName(), Path.GetRandomFileName());
                    Directory.CreateDirectory(srcFolder2);
                    for (int j = 0; j < k; j++)
                    {
                        File.WriteAllText(Path.Combine(srcFolder2, Path.GetRandomFileName()), string.Empty);
                    }
                }

            }

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });
                    Exception lastException = null;
                    w.OnError += (errorSender, errorArgs) =>
                    {
                        lastException = errorArgs.GetException();
                    };

                    for (int i = 0; i < fn.Length; i++)
                    {

                        var srcFolder = fn[i];
                        tt[i] = Task.Run(() =>
                        {
                            Assert.IsTrue(Directory.Exists(srcFolder));
                            Directory.Delete(srcFolder, true);
                            Assert.IsFalse(Directory.Exists(srcFolder));
                            System.Diagnostics.Debug.WriteLine($"Deleted Test folder is {srcFolder}");

                        });

                    }

                    for (int i = 0; i < fn.Length; i++)
                        tt[i].Wait();

                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                    w.Stop();
                    if (lastException != null)
                    {
                        throw lastException;
                    }

                    Assert.AreEqual(fn.Length, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.All(x => x is FolderDeletedEventArgs));
                    Assert.AreEqual(0, w.EventFilterPublic.Count);


                }
            }
            catch (System.IO.InternalBufferOverflowException ex)
            {
                //// Suppress
                ex.Process();

                throw new AssertInconclusiveException();
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FolderDeleteRecursiveInSubFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName());
            Directory.CreateDirectory(srcFolder);
            int k = 10;
            for (int i = 0; i < k; i++)
            {
                var srcFolder2 = Path.Combine(srcFolder, Path.GetRandomFileName(), Path.GetRandomFileName());
                //var srcFolder2 = Path.Combine(srcFolder, $"d{i}");
                Directory.CreateDirectory(srcFolder2);
                for (int j = 0; j < k; j++)
                {
                    File.WriteAllText(Path.Combine(srcFolder2, Path.GetRandomFileName()), string.Empty);
                    //File.WriteAllText(Path.Combine(srcFolder2, $"d{i}-f{j}"), string.Empty);
                }
            }

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    System.Diagnostics.Debug.WriteLine ($"Start Deleteing {srcFolder} {DateTime.Now.ToString("HH:mm:ss:ffff")}");
                    Directory.Delete(srcFolder, true);
                    System.Diagnostics.Debug.WriteLine($"Finished Deleteing {srcFolder} {DateTime.Now.ToString("HH:mm:ss:ffff")}");

                    for (int i = 0; i < 10; i++)
                    {
                        System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    }

                    w.Stop();

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FolderDeletedEventArgs);
                    var e = (FolderDeletedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(srcFolder, e.FullPath);

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
