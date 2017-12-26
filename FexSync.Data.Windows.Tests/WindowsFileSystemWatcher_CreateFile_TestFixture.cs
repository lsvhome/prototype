using System;
using System.IO;
using System.Linq;
using System.Threading;

using FexSync.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FexSync.Data.Windows.Tests
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

                    this.CreateFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                using (var fStream = File.Create(fileName))
                                {
                                    using (var fsWriter = new StreamWriter(fStream))
                                    {
                                        fsWriter.WriteLine(string.Empty);
                                    }
                                }

                                System.Diagnostics.Trace.WriteLine("write end");
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

                    this.CreateFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                byte[] buffer = new byte[1024 * 1024];
                                using (var fStream = File.Create(fileName))
                                {
                                    for (int i = 0; i < 10; i++)
                                    {
                                        System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                                        fStream.Write(buffer, 0, buffer.Length);
                                        System.Diagnostics.Trace.WriteLine("writing...");
                                        System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                                    }
                                }

                                System.Diagnostics.Trace.WriteLine("write end");
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

                    this.CreateFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                using (var fStream = File.Create(fileName))
                                {
                                    using (var fsWriter = new StreamWriter(fStream))
                                    {
                                        fsWriter.WriteLine(string.Empty);
                                    }
                                }

                                System.Diagnostics.Trace.WriteLine("write end");
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

                    this.CreateFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                byte[] buffer = new byte[1024 * 1024];
                                using (var fStream = File.Create(fileName))
                                {
                                    for (int i = 0; i < 10; i++)
                                    {
                                        System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                                        fStream.Write(buffer, 0, buffer.Length);
                                        System.Diagnostics.Trace.WriteLine("writing...");
                                        System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                                    }
                                }

                                System.Diagnostics.Trace.WriteLine("write end");
                            });

                    w.Stop();
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        public void CopyFileInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var dstFileName = Path.Combine(testDir, Path.GetRandomFileName());
            File.WriteAllText(srcFileName, string.Empty);

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    System.Diagnostics.Trace.WriteLine("write begin");
                    File.Copy(srcFileName, dstFileName);
                    System.Diagnostics.Trace.WriteLine("write end");

                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(1, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.Single() is FileCreatedEventArgs);
                    var e = (FileCreatedEventArgs)w.FiredEvents.Single();
                    Assert.AreEqual(dstFileName, e.FullPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void FileCreateSmallFileInRootFolderAndModifyTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    System.Diagnostics.Trace.WriteLine("write begin");
                    using (var fileStream = File.Create(fileName))
                    {
                        using (var writer = new StreamWriter(fileStream))
                        {
                            writer.WriteLine(string.Empty);
                        }
                    }

                    System.Diagnostics.Trace.WriteLine("write end");

                    System.Threading.Thread.Sleep(200);

                    System.Diagnostics.Trace.WriteLine("modify begin");
                    File.AppendAllText(fileName, "aa");
                    System.Diagnostics.Trace.WriteLine("modify end");

                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(2, w.FiredEvents.Count);

                    Assert.IsTrue(w.FiredEvents.First() is FileCreatedEventArgs);
                    var e = (FileCreatedEventArgs)w.FiredEvents.First();
                    Assert.AreEqual(fileName, e.FullPath);

                    Assert.IsTrue(w.FiredEvents.Skip(1).First() is FileModifiedEventArgs);
                    var e1 = (FileModifiedEventArgs)w.FiredEvents.Skip(1).First();
                    Assert.AreEqual(fileName, e1.FullPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void CopyFileAndModifyInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var srcFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var dstFileName = Path.Combine(testDir, Path.GetRandomFileName());
            File.WriteAllText(srcFileName, string.Empty);

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    System.Diagnostics.Trace.WriteLine("write begin");
                    File.Copy(srcFileName, dstFileName);
                    System.Diagnostics.Trace.WriteLine("write end");

                    System.Threading.Thread.Sleep(200);

                    System.Diagnostics.Trace.WriteLine("modify begin");
                    File.AppendAllText(dstFileName, "aa");
                    System.Diagnostics.Trace.WriteLine("modify end");

                    w.Stop();

                    Assert.AreEqual(0, w.EventFilterPublic.Count);

                    Assert.AreEqual(2, w.FiredEvents.Count);

                    Assert.IsTrue(w.FiredEvents.First() is FileCreatedEventArgs);
                    var e = (FileCreatedEventArgs)w.FiredEvents.First();
                    Assert.AreEqual(dstFileName, e.FullPath);

                    Assert.IsTrue(w.FiredEvents.Skip(1).First() is FileModifiedEventArgs);
                    var e1 = (FileModifiedEventArgs)w.FiredEvents.Skip(1).First();
                    Assert.AreEqual(dstFileName, e1.FullPath);
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
            w.OnFileCreated += (s, y) =>
            {
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
                    System.Diagnostics.Trace.WriteLine("file exists, but it is inaccessible");
                }
            };

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            System.Diagnostics.Trace.WriteLine($"Create Test file is {fileName}");

            Assert.IsFalse(File.Exists(fileName));

            fileCreating();

            Assert.IsTrue(File.Exists(fileName));

            Assert.IsFalse(error_file_inaccessible, "file is inaccessible");

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            Assert.AreEqual(0, w.EventFilterPublic.Count);

            Assert.AreEqual(1, w.FiredEvents.Count);
            Assert.IsTrue(w.FiredEvents.Single() is FileCreatedEventArgs);
            var e = (FileCreatedEventArgs)w.FiredEvents.Single();
            Assert.AreEqual(fileName, e.FullPath);
        }
    }
}
