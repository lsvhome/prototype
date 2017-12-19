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

                    MoveFileWithin(fileName, fileNameMoved, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    w.Stop();

                    MoveFileWithinValidate(fileName, fileNameMoved, w);
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

                    MoveFileWithin(fileName, fileNameMoved, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    w.Stop();

                    MoveFileWithinValidate(fileName, fileNameMoved, w);
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

                    MoveFileWithin(fileName, fileNameMoved, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    w.Stop();

                    MoveFileWithinValidate(fileName, fileNameMoved, w);
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

                    MoveFileOutside(fileName, fileNameMoved, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    //Assert.AreEqual(0, w.EventFilterPublic.Count);

                    //Assert.AreEqual(1, w.FiredEvents.Count);
                    //Assert.IsTrue(w.FiredEvents.Single() is FileDeletedEventArgs);
                    //var e = (FileDeletedEventArgs)w.FiredEvents.Single();
                    //Assert.AreEqual(fileName, e.FullPath);

                    w.Stop();

                    MoveFileOutsideValidate(fileName, fileNameMoved, w);
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

                    MoveFileOutside(fileName, fileNameMoved, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    //Assert.AreEqual(0, w.EventFilterPublic.Count);

                    //Assert.AreEqual(1, w.FiredEvents.Count);
                    //Assert.IsTrue(w.FiredEvents.Single() is FileDeletedEventArgs);
                    //var e = (FileDeletedEventArgs)w.FiredEvents.Single();
                    //Assert.AreEqual(fileName, e.FullPath);

                    w.Stop();

                    MoveFileOutsideValidate(fileName, fileNameMoved, w);
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

                    MoveFileWithin(fileName, fileNameMoved, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    w.Stop();

                    MoveFileWithinValidate(fileName, fileNameMoved, w);
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

                    MoveFileWithin(fileName, fileNameMoved, w, () =>
                    {
                        System.Diagnostics.Debug.WriteLine("write begin");
                        File.Move(fileName, fileNameMoved);
                        System.Diagnostics.Debug.WriteLine("write end");
                    }
                    );

                    w.Stop();

                    MoveFileWithinValidate(fileName, fileNameMoved, w);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }



        /*



        [TestMethod]
        public void FileMoveTest2()
        {
            TestRunTemplate(MoveFile2);
        }

        public void MoveFile2(string testDir, WindowsFileSystemWatcherTest w)
        {
            w.SetRaisingEvents(false);
            var createdFileInRootFolder = Path.Combine(testDir, Path.GetRandomFileName());

            var movedFileInSubFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetFileName(createdFileInRootFolder));

            //// Preparations

            Directory.CreateDirectory(Path.GetDirectoryName(movedFileInSubFolder));
            File.WriteAllText(createdFileInRootFolder, this.GetType().Name);

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            {
                System.Diagnostics.Debug.WriteLine($"Create Test file is {createdFileInRootFolder}");
                //// Create
                w.FiredEvents.Clear();
                w.SetRaisingEvents(true);


                Assert.IsTrue(File.Exists(createdFileInRootFolder));
                Assert.IsFalse(File.Exists(movedFileInSubFolder));
                File.Move(createdFileInRootFolder, movedFileInSubFolder);
                Assert.IsFalse(File.Exists(createdFileInRootFolder));
                Assert.IsTrue(File.Exists(movedFileInSubFolder));

                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod.Add(TimeSpan.FromSeconds(8)));

                Assert.AreEqual(0, w.EventFilterPublic.Count);
                Assert.AreEqual(1, w.FiredEvents.Count);
                //Assert.IsTrue(w.FiredEvents.Single() is FileMovedEventArgs);
                var e = (FileMovedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFileInRootFolder, e.OldPath);
                Assert.AreEqual(movedFileInSubFolder, e.NewPath);

            }

            {
                System.Diagnostics.Debug.WriteLine($"Create Test file is {movedFileInSubFolder}");
                //// Create
                w.FiredEvents.Clear();
                w.SetRaisingEvents(true);

                Assert.IsFalse(File.Exists(createdFileInRootFolder));
                Assert.IsTrue(File.Exists(movedFileInSubFolder));
                File.Move(movedFileInSubFolder, createdFileInRootFolder);
                Assert.IsTrue(File.Exists(createdFileInRootFolder));
                Assert.IsFalse(File.Exists(movedFileInSubFolder));


                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);


                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsTrue(w.FiredEvents.Single() is FileMovedEventArgs);
                var e = (FileMovedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFileInRootFolder, e.NewPath);
                Assert.AreEqual(movedFileInSubFolder, e.OldPath);
            }
        }

        private void TestRunTemplate(Action<string, WindowsFileSystemWatcherTest> testAction)
        {
            var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testDir);

            try
            {
                using (var w = new WindowsFileSystemWatcherTest())
                {
                    w.Start(new[] { new DirectoryInfo(testDir) });

                    testAction(testDir, w);

                    w.Stop();
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }
        */
        public void MoveFileWithin(string fileName, string fileNameMoved, WindowsFileSystemWatcherTest w, Action fileMoving)
        {
            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            System.Diagnostics.Debug.WriteLine($"Create Test file is {fileName}");

            Assert.IsTrue(File.Exists(fileName));
            Assert.IsFalse(File.Exists(fileNameMoved));
            fileMoving();
            Assert.IsTrue(File.Exists(fileNameMoved));

            Assert.IsFalse(File.Exists(fileName));


            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);



            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            //Assert.AreEqual(0, w.PublicEventsForDebug.Select(x => x.Value.Count).Sum());



            //Assert.AreEqual(0, w.EventFilterPublic.Count);

            //Assert.AreEqual(1, w.FiredEvents.Count);
            //Assert.IsTrue(w.FiredEvents.Single() is FileMovedEventArgs);
            //var e = (FileMovedEventArgs)w.FiredEvents.Single();
            //Assert.AreEqual(fileName, e.OldPath);
            //Assert.AreEqual(fileNameMoved, e.NewPath);
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
            //bool error_file_inaccessible = false;
            //w.OnFileMoved +=
            //    (s, y) => {

            //        try
            //        {


            //            error_file_inaccessible = !File.Exists(y.NewPath);

            //            using (var x = File.OpenRead(y.NewPath))
            //            {
            //            }
            //        }
            //        catch (Exception)
            //        {
            //            error_file_inaccessible = true;
            //            System.Diagnostics.Debug.WriteLine("file exists, but it is inaccessible");
            //        }


            //    };


            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            System.Diagnostics.Debug.WriteLine($"Create Test file is {fileName}");

            Assert.IsTrue(File.Exists(fileName));
            Assert.IsFalse(File.Exists(fileNameMoved));
            fileMoving();
            Assert.IsTrue(File.Exists(fileNameMoved));

            Assert.IsFalse(File.Exists(fileName));

            //Assert.IsFalse(error_file_inaccessible, "file is inaccessible");

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);



            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            //Assert.AreEqual(0, w.PublicEventsForDebug.Select(x => x.Value.Count).Sum());



            //Assert.AreEqual(0, w.EventFilterPublic.Count);

            //Assert.AreEqual(1, w.FiredEvents.Count);
            //Assert.IsTrue(w.FiredEvents.Single() is FileDeletedEventArgs);
            //var e = (FileDeletedEventArgs)w.FiredEvents.Single();
            //Assert.AreEqual(fileName, e.FullPath);
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
                            {
                                //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);


                                var mf = Path.Combine( Path.GetDirectoryName(Path.GetDirectoryName(fileName)),  Path.GetFileName(fileName));
                                Assert.IsTrue(File.Exists(fileName));
                                Assert.IsFalse(File.Exists(mf));
                                File.Move(fileName, mf);
                                Assert.IsFalse(File.Exists(fileName));
                                Assert.IsTrue(File.Exists(mf));
                                System.Diagnostics.Debug.WriteLine($"Moved Test file is {fileName} at {DateTime.Now.ToString("HH:mm:ss:ffff")}");
                            }

                        });

                    }

                    for (int i = 0; i < fn.Length; i++)
                        tt[i].Wait();

                    System.Diagnostics.Debug.WriteLine($"Wait completed at {DateTime.Now.ToString("HH:mm:ss:ffff")}");

                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                    w.Stop();

                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
                    //System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                    Assert.AreEqual(fn.Length, w.FiredEvents.Count);
                    Assert.IsTrue(w.FiredEvents.All(x => x is FileMovedEventArgs));
                    Assert.AreEqual(0, w.EventFilterPublic.Count);
                    //var e = (FileModifiedEventArgs)w.FiredEvents.Single();
                    //Assert.AreEqual(fileName, e.FullPath);
                }
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }


    }
}
