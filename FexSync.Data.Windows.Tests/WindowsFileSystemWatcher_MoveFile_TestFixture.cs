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
    public class WindowsFileSystemWatcher_MoveFile_TestFixture
    {
        [TestMethod]
        public void MoveFileWithinObjectFromRootToDeepTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            var fileNameMoved = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetFileName(fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(fileNameMoved));
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.MoveFileWithin(
                        fileName,
                        fileNameMoved,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                File.Move(fileName, fileNameMoved);
                                System.Diagnostics.Trace.WriteLine("write end");
                            });

                    w.Stop();

                    this.MoveFileWithinValidate(fileName, fileNameMoved, w);
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
            var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            var fileNameMoved = Path.Combine(testDir, Path.GetFileName(fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.MoveFileWithin(
                        fileName,
                        fileNameMoved,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                File.Move(fileName, fileNameMoved);
                                System.Diagnostics.Trace.WriteLine("write end");
                            });

                    w.Stop();

                    this.MoveFileWithinValidate(fileName, fileNameMoved, w);
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
            var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            var fileNameMoved = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetFileName(fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(fileNameMoved));
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.MoveFileWithin(
                        fileName,
                        fileNameMoved,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                File.Move(fileName, fileNameMoved);
                                System.Diagnostics.Trace.WriteLine("write end");
                            });

                    w.Stop();

                    this.MoveFileWithinValidate(fileName, fileNameMoved, w);
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
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            var fileNameMoved = Path.Combine(testDir, "..", Path.GetFileName(fileName));
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.MoveFileOutside(
                        fileName,
                        fileNameMoved,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                File.Move(fileName, fileNameMoved);
                                System.Diagnostics.Trace.WriteLine("write end");
                            });

                    w.Stop();

                    this.MoveFileOutsideValidate(fileName, fileNameMoved, w);
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
            var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            var fileNameMoved = Path.Combine(testDir, "..", Path.GetFileName(fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.MoveFileOutside(
                        fileName,
                        fileNameMoved,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                File.Move(fileName, fileNameMoved);
                                System.Diagnostics.Trace.WriteLine("write end");
                            });

                    w.Stop();

                    this.MoveFileOutsideValidate(fileName, fileNameMoved, w);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [TestMethod]
        public void MoveFileRenameInRootFolderTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            var fileNameMoved = Path.Combine(Path.GetDirectoryName(fileName), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.MoveFileWithin(
                        fileName,
                        fileNameMoved,
                        w,
                        () =>
                    {
                        System.Diagnostics.Trace.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Trace.WriteLine("write end");
                    });

                    w.Stop();

                    this.MoveFileWithinValidate(fileName, fileNameMoved, w);
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
            var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            var fileNameMoved = Path.Combine(Path.GetDirectoryName(fileName), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.MoveFileWithin(
                        fileName,
                        fileNameMoved,
                        w, 
                        () =>
                    {
                        System.Diagnostics.Trace.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Trace.WriteLine("write end");
                    });

                    w.Stop();

                    this.MoveFileWithinValidate(fileName, fileNameMoved, w);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        public void MoveFileWithin(string fileName, string fileNameMoved, WindowsFileSystemWatcherTest w, Action fileMoving)
        {
            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            System.Diagnostics.Trace.WriteLine($"Create Test file is {fileName}");

            Assert.IsTrue(File.Exists(fileName));
            Assert.IsFalse(File.Exists(fileNameMoved));
            fileMoving();
            Assert.IsTrue(File.Exists(fileNameMoved));

            Assert.IsFalse(File.Exists(fileName));

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
        }

        public void MoveFileWithinValidate(string fileName, string fileNameMoved, WindowsFileSystemWatcherTest w)
        {
            Assert.AreEqual(0, w.EventFilterPublic.Count);
            Assert.AreEqual(1, w.FiredEvents.Count);
            Assert.IsTrue(w.FiredEvents.Single() is FileMovedEventArgs);
            var e = (FileMovedEventArgs)w.FiredEvents.Single();
            Assert.AreEqual(fileName, e.OldPath);
            Assert.AreEqual(fileNameMoved, e.NewPath);
        }

        public void MoveFileOutsideValidate(string fileName, string fileNameMoved, WindowsFileSystemWatcherTest w)
        {
            Assert.AreEqual(0, w.EventFilterPublic.Count);
            Assert.AreEqual(1, w.FiredEvents.Count);
            Assert.IsTrue(w.FiredEvents.Single() is FileDeletedEventArgs);
            var e = (FileDeletedEventArgs)w.FiredEvents.Single();
            Assert.AreEqual(fileName, e.FullPath);
        }

        public void MoveFileOutside(string fileName, string fileNameMoved, WindowsFileSystemWatcherTest w, Action fileMoving)
        {
            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            System.Diagnostics.Trace.WriteLine($"Create Test file is {fileName}");

            Assert.IsTrue(File.Exists(fileName));
            Assert.IsFalse(File.Exists(fileNameMoved));
            fileMoving();
            Assert.IsTrue(File.Exists(fileNameMoved));

            Assert.IsFalse(File.Exists(fileName));

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
        }

        [TestMethod]
        public void FileMove20FilesTest()
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
                            var mf = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(fileName)),  Path.GetFileName(fileName));
                            Assert.IsTrue(File.Exists(fileName));
                            Assert.IsFalse(File.Exists(mf));
                            File.Move(fileName, mf);
                            Assert.IsFalse(File.Exists(fileName));
                            Assert.IsTrue(File.Exists(mf));
                            System.Diagnostics.Trace.WriteLine($"Moved Test file is {fileName} at {DateTime.Now.ToString("HH:mm:ss:ffff")}");
                        });
                    }

                    for (int i = 0; i < fn.Length; i++)
                    {
                        tt[i].Wait();
                    }

                    System.Diagnostics.Trace.WriteLine($"Wait completed at {DateTime.Now.ToString("HH:mm:ss:ffff")}");

                    w.Stop();

                    Assert.AreEqual(fn.Length, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.All(x => x is FileMovedEventArgs));
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
