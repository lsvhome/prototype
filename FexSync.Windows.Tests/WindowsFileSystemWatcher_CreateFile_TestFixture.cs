using System;
using System.IO;
using System.Linq;
using System.Threading;

using FexSync.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FexSync.Windows.Tests
{
    [TestClass]
    public class WindowsFileSystemWatcher_CreateFile_TestFixture
    {
        [TestMethod]
        public void FileCreateSmallFileInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    CreateFile(fileName, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        using (var fStream = File.Create(fileName))
                        {
                            using (var fsWriter = new StreamWriter(fStream))
                            {
                                fsWriter.WriteLine(string.Empty);
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    w.Stop();
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FileCreateLargeFileInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    CreateFile(fileName, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        byte[] buffer = new byte[1024 * 1024];
                        using (var fStream = File.Create(fileName))
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                                fStream.Write(buffer, 0, buffer.Length);
                                System.Diagnostics.Debug.WriteLine("writing...");
                                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                            }
                        }

                        System.Diagnostics.Debug.WriteLine("write end");
                    });

                    w.Stop();
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FileCreateSmallFileInSubFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    CreateFile(fileName, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        using (var fStream = File.Create(fileName))
                        {
                            using (var fsWriter = new StreamWriter(fStream))
                            {
                                fsWriter.WriteLine(string.Empty);
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    w.Stop();
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FileCreateLargeFileInSubFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));



            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    CreateFile(fileName, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        byte[] buffer = new byte[1024 * 1024];
                        using (var fStream = File.Create(fileName))
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                                fStream.Write(buffer, 0, buffer.Length);
                                System.Diagnostics.Debug.WriteLine("writing...");
                                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    w.Stop();
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        public void CreateFile(string fileName, WindowsFileSystemWatcherTest w, Action fileCreating)
        {
            bool error_file_inaccessible = false;
            w.OnFileCreated +=
                (s, y) => {

                    try
                    {


                        error_file_inaccessible = !File.Exists(y.FullPath);

                        using (var x = File.OpenRead(y.FullPath))
                        {
                        }
                    }
                    catch (Exception)
                    {
                        error_file_inaccessible = true;
                        System.Diagnostics.Debug.WriteLine("file exists, but it is inaccessible");
                    }


                };


            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            System.Diagnostics.Debug.WriteLine($"Create Test file is {fileName}");

            Assert.IsFalse(File.Exists(fileName));

            fileCreating();

            Assert.IsTrue(File.Exists(fileName));

            Assert.IsFalse(error_file_inaccessible, "file is inaccessible");

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);



            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            //Assert.AreEqual(0, w.PublicEventsForDebug.Select(x => x.Value.Count).Sum());
            Assert.AreEqual(0, w.EventFilterPublic.Count);

            Assert.AreEqual(1, w.FiredEvents.Count);
            Assert.IsTrue(w.FiredEvents.Single() is FileCreatedEventArgs);
            var e = (FileCreatedEventArgs)w.FiredEvents.Single();
            Assert.AreEqual(fileName, e.FullPath);
        }
    }
}
