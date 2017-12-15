using System;
using System.IO;
using System.Linq;
using System.Threading;

using FexSync.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FexSync.Windows.Tests
{

    [TestClass]
    public class WindowsFileSystemWatcherTestFixture
    {
        [TestMethod]
        public void FileCreateTest()
        {
            TestRunTemplate(CreateFile);
        }

        [TestMethod]
        public void FileMoveTest()
        {
            TestRunTemplate(MoveFile);
        }

        [TestMethod]
        public void FileCRUDTest()
        {
            TestRunTemplate(CRUD);
        }

        public void CreateFile(string testDir, WindowsFileSystemWatcherTest w)
        {
            w.SetRaisingEvents(false);
            var createdFileInRootFolder = Path.Combine(testDir, Path.GetRandomFileName());

            var createdFileInSubFolder = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetRandomFileName());

            //// Preparations

            Directory.CreateDirectory(Path.GetDirectoryName(createdFileInSubFolder));
            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);
            {
                System.Diagnostics.Debug.WriteLine($"Create Test file is {createdFileInRootFolder}");
                //// Create
                w.FiredEvents.Clear();
                w.SetRaisingEvents(true);

                Assert.IsFalse(File.Exists(createdFileInRootFolder));

                File.WriteAllText(createdFileInRootFolder, this.GetType().Name);
                //var p = System.Diagnostics.Process.Start(@"C:\Windows\System32\fsutil.exe", $"file createnew \"{createdFile}\" 1024");
                //p.WaitForExit((int)WindowsFileSystemWatcherTest.TestWaitPeriod.TotalMilliseconds);
                Assert.IsTrue(File.Exists(createdFileInRootFolder));

                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);


                //Assert.AreEqual(0, w.PublicEventsForDebug.Select(x => x.Value.Count).Sum());
                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsTrue(w.FiredEvents.Single() is FileCreatedEventArgs);
                var e = (FileCreatedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFileInRootFolder, e.FullPath);
            }

            {
                System.Diagnostics.Debug.WriteLine($"Create Test file is {createdFileInSubFolder}");
                //// Create
                w.FiredEvents.Clear();
                w.SetRaisingEvents(true);

                Assert.IsFalse(File.Exists(createdFileInSubFolder));

                File.WriteAllText(createdFileInSubFolder, this.GetType().Name);
                //var p = System.Diagnostics.Process.Start(@"C:\Windows\System32\fsutil.exe", $"file createnew \"{createdFile}\" 1024");
                //p.WaitForExit((int)WindowsFileSystemWatcherTest.TestWaitPeriod.TotalMilliseconds);
                Assert.IsTrue(File.Exists(createdFileInSubFolder));

                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);


                //Assert.AreEqual(0, w.PublicEventsForDebug.Select(x => x.Value.Count).Sum());
                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsTrue(w.FiredEvents.Single() is FileCreatedEventArgs);
                var e = (FileCreatedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFileInSubFolder, e.FullPath);
            }

        }

        public void MoveFile(string testDir, WindowsFileSystemWatcherTest w)
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

                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

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

        public void CRUD(string testDir, WindowsFileSystemWatcherTest w)
        {
            w.SetRaisingEvents(false);
            var createdFile = Path.Combine(testDir, Path.GetRandomFileName());
            var movedFile = Path.Combine(Path.GetDirectoryName(createdFile), Path.GetRandomFileName());

            var movedFile2 = Path.Combine(testDir, Path.GetRandomFileName(), Path.GetRandomFileName(), Path.GetFileName(movedFile));
            var movedFileOutside = Path.Combine(Path.GetTempPath(), Path.GetFileName(movedFile));

            //// Preparations

            Directory.CreateDirectory(Path.GetDirectoryName(movedFile2));

            {
                System.Diagnostics.Debug.WriteLine($"Create Test file is {createdFile}");
                //// Create
                w.FiredEvents.Clear();
                w.SetRaisingEvents(true);

                Assert.IsFalse(File.Exists(createdFile));

                File.WriteAllText(createdFile, this.GetType().Name);
                //var p = System.Diagnostics.Process.Start(@"C:\Windows\System32\fsutil.exe", $"file createnew \"{createdFile}\" 1024");
                //p.WaitForExit((int)WindowsFileSystemWatcherTest.TestWaitPeriod.TotalMilliseconds);
                Assert.IsTrue(File.Exists(createdFile));

                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsNotNull(w.FiredEvents.Single() is FileCreatedEventArgs);
                var e = (FileCreatedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFile, e.FullPath);
            }

            {
                //// Modify
                System.Diagnostics.Debug.WriteLine($"Modify Test file is {createdFile}");
                w.FiredEvents.Clear();
                using (var f = File.OpenWrite(createdFile))
                {
                    byte[] buffer = createdFile.AsEnumerable().Select(x=> (byte)x).ToArray();
                    f.Write(buffer, 0, buffer.Length);
                }

                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsNotNull(w.FiredEvents.Single() is FileModifiedEventArgs);
                var e = (FileModifiedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFile, e.FullPath);
            }

            {
                //// Rename (within)
                System.Diagnostics.Debug.WriteLine($"Rename Test file is {createdFile} -> {movedFile}");
                w.FiredEvents.Clear();
                File.Move(createdFile, movedFile);
                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsNotNull(w.FiredEvents.Single() is FileMovedEventArgs);
                var e = (FileMovedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFile, e.OldPath);
                Assert.AreEqual(movedFile, e.NewPath);
            }

            System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

            {
                //// Move (within)
                System.Diagnostics.Debug.WriteLine($"Move (within) Test file is {movedFile} -> {movedFile2}");
                w.FiredEvents.Clear();
                File.Move(movedFile, movedFile2);
                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsNotNull(w.FiredEvents.Single() is FileMovedEventArgs);
                var e = (FileMovedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(movedFile, e.OldPath);
                Assert.AreEqual(movedFile2, e.NewPath);
            }




            {
                //// Delete
                System.Diagnostics.Debug.WriteLine("============================================================");
                System.Diagnostics.Debug.WriteLine($"Delete Test file is {movedFile2}");
                w.FiredEvents.Clear();
                File.Delete(movedFile2);
                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsNotNull(w.FiredEvents.Single() is FileOrFolderDeletedEventArgs);
                var e2 = (FileOrFolderDeletedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(movedFile2, e2.FullPath);
            }


            {
                //// Create
                w.FiredEvents.Clear();
                File.WriteAllText(createdFile, this.GetType().Name);
                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsNotNull(w.FiredEvents.Single() is FileCreatedEventArgs);
                var e = (FileCreatedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFile, e.FullPath);
            }

            {
                //// Move Outside
                w.FiredEvents.Clear();
                File.Move(createdFile, movedFileOutside);
                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                Assert.AreEqual(1, w.FiredEvents.Count);
                Assert.IsNotNull(w.FiredEvents.Single() is FileOrFolderDeletedEventArgs);
                var e2 = (FileOrFolderDeletedEventArgs)w.FiredEvents.Single();
                Assert.AreEqual(createdFile, e2.FullPath);
            }

            {
                //// Delete Outside
                w.FiredEvents.Clear();
                File.Delete(movedFileOutside);
                System.Threading.Thread.Sleep(WindowsFileSystemWatcherTest.TestWaitPeriod);

                Assert.AreEqual(0, w.FiredEvents.Count);
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
    }
}
