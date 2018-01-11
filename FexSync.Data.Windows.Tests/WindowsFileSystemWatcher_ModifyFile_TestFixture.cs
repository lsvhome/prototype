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
    public class WindowsFileSystemWatcher_ModifyFile_TestFixture
    {
        [TestMethod]
        public void ModifyRootFile001Test()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.ModifyFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                using (var f = File.OpenWrite(fileName))
                                {
                                    byte[] buffer = fileName.AsEnumerable().Select(x => (byte)x).ToArray();
                                    f.Write(buffer, 0, buffer.Length);
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
        public void ModifyRootFile002Test()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.ModifyFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                File.AppendAllText(fileName, "-");
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
        public void ModifyRootFile003Test()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.ModifyFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                byte[] buffer = fileName.AsEnumerable().Select(x => (byte)x).ToArray();
                                using (var f = File.OpenWrite(fileName))
                                {
                                    for (int i = 0; i < 10; i++)
                                    {
                                        System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                                        f.Write(buffer, 0, buffer.Length);
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
        public void ModifyRootFileViaReCreateTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.ModifyFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                using (var f = new FileStream(fileName, FileMode.Create))
                                {
                                    byte[] buffer = fileName.AsEnumerable().Select(x => (byte)x).ToArray();
                                    f.Write(buffer, 0, buffer.Length);
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
        public void ModifyRootLargeFileViaReCreateTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName());
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.ModifyFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                using (var f = new FileStream(fileName, FileMode.Create))
                                {
                                    byte[] buffer = fileName.AsEnumerable().Select(x => (byte)x).ToArray();

                                    for (int i = 0; i < 10; i++)
                                    {
                                        System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                                        f.Write(buffer, 0, buffer.Length);
                                        System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                                        System.Diagnostics.Trace.WriteLine("writing...");
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
        public void ModifySubFolderFileViaReCreateTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            var fileName = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            File.WriteAllText(fileName, "#");
            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    this.ModifyFile(
                        fileName,
                        w,
                        () =>
                            {
                                System.Diagnostics.Trace.WriteLine("write begin");
                                using (var f = new FileStream(fileName, FileMode.Create))
                                {
                                    byte[] buffer = fileName.AsEnumerable().Select(x => (byte)x).ToArray();
                                    f.Write(buffer, 0, buffer.Length);
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
        public void Modify20FilesTest()
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);
            string[] fn = new string[20];
            Task[] tt = new Task[fn.Length];
            for (int i = 0; i < fn.Length; i++)
            {
                fn[i] = Path.Combine(testDir, Path.GetRandomFileName());
                File.WriteAllText(fn[i], "#");
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
                            bool error_file_inaccessible = false;
                            w.OnFileMoved += (s, y) =>
                            {
                                try
                                {
                                    error_file_inaccessible = !File.Exists(y.NewPath);

                                    using (var x = File.OpenRead(y.NewPath))
                                    {
                                    }
                                }
                                catch (Exception)
                                {
                                    error_file_inaccessible = true;
                                    System.Diagnostics.Trace.WriteLine("file exists, but it is inaccessible");
                                }
                            };

                            Assert.IsTrue(File.Exists(fileName));
                            File.AppendAllText(fileName, "-");
                            Assert.IsTrue(File.Exists(fileName));
                            System.Diagnostics.Trace.WriteLine($"Created Test file is {fileName}");

                            Assert.IsFalse(error_file_inaccessible, "file is inaccessible");
                        });
                    }

                    for (int i = 0; i < fn.Length; i++)
                    {
                        tt[i].Wait();
                    }

                    w.Stop();

                    Assert.AreEqual(fn.Length, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.All(x => x is FileModifiedEventArgs));
                    Assert.AreEqual(0, w.EventFilterPublic.Count);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        public void ModifyFile(string fileName, WindowsFileSystemWatcherTest w, Action fileMoving)
        {
            bool error_file_inaccessible = false;
            w.OnFileMoved += (s, y) =>
            {
                    try
                    {
                        error_file_inaccessible = !File.Exists(y.NewPath);

                        using (var x = File.OpenRead(y.NewPath))
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

            Assert.IsTrue(File.Exists(fileName));
            fileMoving();
            Assert.IsTrue(File.Exists(fileName));

            Assert.IsFalse(error_file_inaccessible, "file is inaccessible");

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            Assert.AreEqual(0, w.EventFilterPublic.Count);

            Assert.AreEqual(1, w.FiredEvents.Count);
            Assert.IsTrue(w.FiredEvents.Single() is FileModifiedEventArgs);
            var e = (FileModifiedEventArgs)w.FiredEvents.Single();
            Assert.AreEqual(fileName, e.FullPath);
        }
    }
}
