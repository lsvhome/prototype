using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Fex.Api;
using Net.Fex.Api.Tests;

namespace FexSync.Data.Tests
{
    [TestClass]
    public class DataTest1 : Net.Fex.Api.Tests.ConnectionTestFixture
    {
        [TestMethod]
        public async Task BuildRemoteTreeAsyncTest()
        {
            using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
            {
                conn.OnCaptchaUserInputRequired += (sender, captchaRequestArgs) =>
                {
                    var answer = conn.RequestCaptchaAnswerAsync(captchaRequestArgs.CaptchaToken).Result;
                    captchaRequestArgs.CaptchaText = answer.UserInput;
                };

                Assert.IsFalse(conn.IsSignedIn);

                var user = await conn.SignInAsync(LoginValid, PasswordValid, false);
                Assert.IsNotNull(user);
                Assert.AreEqual(ConnectionTestFixture.LoginValid, user.Login);

                Assert.IsTrue(conn.IsSignedIn);

                CommandBuildRemoteTree.CommandBuildRemoteTreeResponse files;
                using (var cmd = new CommandBuildRemoteTree())
                {
                    cmd.Execute(conn);
                    files = cmd.Result;
                }

                Console.WriteLine("----------------------------");

                PrintItemPath(files.List, string.Empty);

                Console.WriteLine("----------------------------");

                await conn.SignOutAsync();

                Assert.IsFalse(conn.IsSignedIn);
            }
        }

        private static void PrintItemPath(CommandBuildRemoteTree.CommandBuildRemoteTreeItem[] files, string path)
        {
            foreach (var each in files)
            {
                if (each is CommandBuildRemoteTree.CommandBuildRemoteTreeItemObject o)
                {
                    Console.WriteLine(path + o.Object.Name);

                    PrintItemPath(each.Childern.ToArray(), path + o.Object.Name + "/");
                }
                else if (each is CommandBuildRemoteTree.CommandBuildRemoteTreeItemArchive a)
                {
                    Console.WriteLine(path + a.ArchiveObject.Preview);
                    PrintItemPath(each.Childern.ToArray(), path + a.ArchiveObject.Preview + "/");
                }
            }
        }

        [TestMethod]
        public async Task SyncWorkflowTest()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine(root);
            System.Diagnostics.Debug.WriteLine(root);
            var localFileName = Path.Combine(root, "Data", TokenValid, Path.ChangeExtension(Path.GetRandomFileName(), ".txt"));
            var remoteFileNameSource = Path.Combine(root, "Data", TokenValid, Path.ChangeExtension(Path.GetRandomFileName(), ".txt"));

            try
            {
                var config = new SyncWorkflow.SyncWorkflowConfig();
                config.AccountSettings = new AccountSettings(root);
                config.AccountSettings.Login = ConnectionTestFixture.LoginValid;
                config.AccountSettings.Password = ConnectionTestFixture.PasswordValid;
                config.ApiHost = this.UriValid.ToString();
                config.AccountSettings.TokenForSync = ConnectionTestFixture.TokenValid;

                var builder = new ContainerBuilder();
                builder.RegisterInstance<IConnectionFactory>(new Data.ConnectionFactory());
                var syncDb = new FexSync.Data.SyncDataDbContext(config.AccountSettings.AccountCacheDbFile);
                builder.RegisterInstance<FexSync.Data.ISyncDataDbContext>(syncDb);
                builder.RegisterInstance<FexSync.Data.IFileSystemWatcher>(new FakseFileSystemWatcher());

                using (config.Container = builder.Build())
                {
                    using (AutoResetEvent waiter = new AutoResetEvent(false))
                    {
                        using (var syncWorkflow = new ExtendedSyncWorkflow(config))
                        {
                            syncWorkflow.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                            syncWorkflow.OnIterationFinished += (object sender, EventArgs e) =>
                            {
                                syncWorkflow.Stop();
                            };
                            syncWorkflow.OnException += (object sender, ExceptionEventArgs e) =>
                            {
                                throw e.Exception;
                            };

                            Assert.IsFalse(File.Exists(config.AccountSettings.AccountCacheDbFile));

                            syncWorkflow.Start();
                            syncWorkflow.WaitForOneIterationAndStoppped(TimeSpan.FromSeconds(30));

                            Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(remoteFileNameSource)));
                            Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                            Assert.AreEqual(0, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                            Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                            using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                            {
                                conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                                CommandSignIn.User user = await conn.SignInAsync(LoginValid, PasswordValid, false);

                                using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, config.AccountSettings.TokenForSync))
                                {
                                    cmd.Execute(conn);
                                    var treeId = cmd.Result.Value;
                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                }

                                File.WriteAllText(remoteFileNameSource, new Random().Next().ToString());
                                conn.Upload(ConnectionTestFixture.TokenValid, null, remoteFileNameSource);
                                File.Delete(remoteFileNameSource);

                                File.WriteAllText(localFileName, new Random().Next().ToString());

                                await conn.SignOutAsync();
                            }

                            syncWorkflow.Start();
                            syncWorkflow.WaitForOneIterationAndStoppped(TimeSpan.FromSeconds(30));

                            Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(remoteFileNameSource)));
                            Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));

                            Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                            Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                            Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                            Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));

                            {
                                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                                {
                                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                                    CommandSignIn.User user = await conn.SignInAsync(LoginValid, PasswordValid, false);

                                    Action rescan = () =>
                                    {
                                        using (CommandSaveLocalTree cmd = new CommandSaveLocalTree(syncDb, new DirectoryInfo(config.AccountSettings.AccountDataFolder)))
                                        {
                                            cmd.Execute(conn);
                                        }

                                        int treeId;
                                        using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, config.AccountSettings.TokenForSync))
                                        {
                                            cmd.Execute(conn);
                                            treeId = cmd.Result.Value;
                                        }

                                        using (CommandPrepareDownload cmd = new CommandPrepareDownload(syncDb, treeId))
                                        {
                                            cmd.Execute(conn);
                                        }

                                        using (CommandPrepareUpload cmd = new CommandPrepareUpload(syncDb, treeId))
                                        {
                                            cmd.Execute(conn);
                                        }
                                    };

                                    rescan();

                                    //// +++++

                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(remoteFileNameSource)));
                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                    Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));

                                    /*
                                        ////----------------------- Conflicts

                                        //// Existing file modified remotely and locally

                                     При изменении содержимого файла:
                                     При изменении таймстампа локального файла в большую сторону он должен попасть в Upload
                                     При изменении таймстампа локального файла в меньшую сторону он должен попасть в Download
                                     При таймстампа локального файла в меньшую сторону он должен попасть в Download
                                     
                                     */

                                    File.SetLastWriteTime(localFileName, File.GetLastWriteTime(localFileName).AddSeconds(11));

                                    File.WriteAllText(localFileName, localFileName);
                                    System.Threading.Thread.Sleep(2000);

                                    conn.Upload(ConnectionTestFixture.TokenValid, null, localFileName);

                                    System.Threading.Thread.Sleep(2000);
                                    File.WriteAllText(localFileName, localFileName + localFileName);

                                    rescan();

                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(remoteFileNameSource)));
                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                    Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                    var xx = syncDb.RemoteFiles.Where(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)).ToArray();
                                    Assert.AreEqual(1, xx.Count());
                                    Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));

                                    Assert.AreEqual(1, syncDb.Conflicts.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    await conn.SignOutAsync();
                                }
                            }
                        }
                    }

                    var db = config.Container.Resolve<FexSync.Data.ISyncDataDbContext>();
                    Assert.IsNotNull(db);
                }
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [TestMethod]
        public void CommandSaveLocalTreeTest()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine(root);
            System.Diagnostics.Debug.WriteLine(root);

            var config = new SyncWorkflow.SyncWorkflowConfig();
            config.AccountSettings = new AccountSettings(root);
            config.AccountSettings.Login = ConnectionTestFixture.LoginValid;
            config.AccountSettings.Password = ConnectionTestFixture.PasswordValid;
            config.ApiHost = this.UriValid.ToString();
            config.AccountSettings.TokenForSync = ConnectionTestFixture.TokenValid;

            Directory.CreateDirectory(config.AccountSettings.AccountDataFolder);
            try
            {
                var fn = Path.Combine(config.AccountSettings.AccountDataFolder, TokenValid, Path.ChangeExtension(Path.GetRandomFileName(), ".txt"));

                var syncDb = new FexSync.Data.SyncDataDbContext(config.AccountSettings.AccountCacheDbFile);
                syncDb.EnsureDatabaseExists();

                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                {
                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                    Assert.IsFalse(conn.IsSignedIn);

                    using (CommandSaveLocalTree cmd = new CommandSaveLocalTree(syncDb, new DirectoryInfo(config.AccountSettings.AccountDataFolder)))
                    {
                        cmd.Execute(conn);
                    }

                    Assert.IsFalse(conn.IsSignedIn);
                    Assert.AreEqual(0, syncDb.LocalFiles.Count());

                    Directory.CreateDirectory(Path.GetDirectoryName(fn));
                    File.WriteAllText(fn, "bla-bla");

                    using (CommandSaveLocalTree cmd = new CommandSaveLocalTree(syncDb, new DirectoryInfo(config.AccountSettings.AccountDataFolder)))
                    {
                        cmd.Execute(conn);
                    }

                    Assert.AreEqual(1, syncDb.LocalFiles.Count());

                    File.Delete(fn);

                    using (CommandSaveLocalTree cmd = new CommandSaveLocalTree(syncDb, new DirectoryInfo(config.AccountSettings.AccountDataFolder)))
                    {
                        cmd.Execute(conn);
                    }

                    Assert.AreEqual(0, syncDb.LocalFiles.Count());
                }
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [TestMethod]
        public void RemoteCRUDTest()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine(root);
            System.Diagnostics.Debug.WriteLine(root);

            var config = new SyncWorkflow.SyncWorkflowConfig();
            config.AccountSettings = new AccountSettings(root);
            config.AccountSettings.Login = ConnectionTestFixture.LoginValid;
            config.AccountSettings.Password = ConnectionTestFixture.PasswordValid;
            config.ApiHost = this.UriValid.ToString();
            config.AccountSettings.TokenForSync = "invalid";

            Directory.CreateDirectory(config.AccountSettings.AccountDataFolder);
            try
            {
                var fn = Path.Combine(config.AccountSettings.AccountDataFolder, TokenValid, Path.ChangeExtension(Path.GetRandomFileName(), ".txt"));

                var fn2 = Path.Combine(config.AccountSettings.AccountDataFolder, TokenValid, "SubFolder1", "SubFolder2", "SubFolder3", Path.ChangeExtension(Path.GetRandomFileName(), ".txt"));

                var syncDb = new FexSync.Data.SyncDataDbContext(config.AccountSettings.AccountCacheDbFile);
                syncDb.EnsureDatabaseExists();

                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                {
                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                    Assert.IsFalse(conn.IsSignedIn);
                    conn.SignIn(config.AccountSettings.Login, config.AccountSettings.Password, false);
                    Assert.IsTrue(conn.IsSignedIn);

                    try
                    {
                        Assert.AreEqual(0, syncDb.RemoteFiles.Count());

                        using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, "invalid"))
                        {
                            cmd.Execute(conn);
                        }

                        Assert.AreEqual(0, syncDb.RemoteFiles.Count());

                        Directory.CreateDirectory(Path.GetDirectoryName(fn));
                        File.WriteAllText(fn, "bla-bla");

                        config.AccountSettings.TokenForSync = ConnectionTestFixture.TokenValid;

                        var upload = conn.Upload(ConnectionTestFixture.TokenValid, null, fn);

                        using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, config.AccountSettings.TokenForSync))
                        {
                            cmd.Execute(conn);
                        }

                        Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn)));

                        conn.DeleteFile(config.AccountSettings.TokenForSync, upload.UploadId);

                        using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, config.AccountSettings.TokenForSync))
                        {
                            cmd.Execute(conn);
                        }

                        Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn)));

                        Directory.CreateDirectory(Path.GetDirectoryName(fn2));
                        File.WriteAllText(fn2, "bla-bla2");

                        int? folderId;
                        using (var xxx = new CommandEnsureFolderExists(syncDb, new DirectoryInfo(config.AccountSettings.AccountDataFolder), TokenValid, Path.GetDirectoryName(fn2)))
                        {
                            xxx.Execute(conn);
                            folderId = xxx.Result;
                        }

                        var upload2 = conn.Upload(ConnectionTestFixture.TokenValid, folderId, fn2);

                        using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, config.AccountSettings.TokenForSync))
                        {
                            cmd.Execute(conn);
                        }

                        Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn2)));

                        conn.DeleteFile(config.AccountSettings.TokenForSync, upload2.UploadId);

                        using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, config.AccountSettings.TokenForSync))
                        {
                            cmd.Execute(conn);
                        }

                        Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn2)));
                    }
                    finally
                    {
                        Assert.IsTrue(conn.IsSignedIn);
                        conn.SignOut();
                        Assert.IsFalse(conn.IsSignedIn);
                    }
                }
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [TestMethod]
        public async Task UpdateServerFileTest()
        {
            using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
            {
                conn.OnCaptchaUserInputRequired += (sender, captchaRequestArgs) =>
                {
                    var answer = conn.RequestCaptchaAnswerAsync(captchaRequestArgs.CaptchaToken).Result;
                    captchaRequestArgs.CaptchaText = answer.UserInput;
                };

                Assert.IsFalse(conn.IsSignedIn);

                var user = await conn.SignInAsync(LoginValid, PasswordValid, false);

                var fn = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".txt"));
                File.WriteAllText(fn, "test001");

                var fff = conn.Upload("004694149924", null, fn); // txt

                File.WriteAllText(fn, "test002");

                var fff2 = conn.Upload("004694149924", null, fn); // txt
                conn.DeleteFile("004694149924", fff.UploadId);

                var x = conn.ObjectView("004694149924");

                Assert.AreEqual(1, x.UploadList.Where(x1 => x1.Name == Path.GetFileName(fn)).Count());

                await conn.SignOutAsync();

                Assert.IsFalse(conn.IsSignedIn);

                Assert.IsTrue(File.Exists(fn));
                File.Delete(fn);
            }
        }

        public void ProcessCaptchaUserInputRequired(object sender, CommandCaptchaRequestPossible.CaptchaRequestedEventArgs captchaRequestArgs)
        {
            var answer = Net.Fex.Api.Tests.Extenders.RequestCaptchaAnswer(null, captchaRequestArgs.CaptchaToken);
            captchaRequestArgs.CaptchaText = answer.UserInput;
        }
    }
}
