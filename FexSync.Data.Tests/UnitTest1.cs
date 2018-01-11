using System;
using System.Collections.Generic;
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
            var testRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var appRoot = Path.Combine(testRoot, Path.GetRandomFileName());
            Directory.CreateDirectory(appRoot);
            var databaseFullPath = Path.Combine(appRoot, Path.GetRandomFileName());

            var dataRoot01 = Path.Combine(testRoot, Path.GetRandomFileName());
            var dataRoot02 = Path.Combine(testRoot, Path.GetRandomFileName());
            var dataRoot03 = Path.Combine(testRoot, Path.GetRandomFileName());
            System.Diagnostics.Trace.WriteLine(testRoot);

            var config = new SyncWorkflow.SyncWorkflowConfig
            {
                Account = new Account
                {
                    Login = ConnectionTestFixture.LoginValid,
                    Password = ConnectionTestFixture.PasswordValid
                },
                ApiHost = this.UriValid.ToString()
            };

            config.SyncObjects = new[]
            {
                new AccountSyncObject
                {
                    Path = dataRoot01,
                    Token = ConnectionTestFixture.TokenValid01,
                    Account = config.Account
                },
                new AccountSyncObject
                {
                    Path = dataRoot02,
                    Token = ConnectionTestFixture.TokenValid02,
                    Account = config.Account
                },
                new AccountSyncObject
                {
                    Path = dataRoot03,
                    Token = ConnectionTestFixture.TokenValid03,
                    Account = config.Account
                }
            };

            Dictionary<string, string[]> fileNames = config.SyncObjects
                .ToDictionary(
                    item => item.Token,
                    item => new[]
                    {
                        Path.Combine(item.Path, Path.ChangeExtension(Path.GetRandomFileName(), ".txt_1111")),
                        Path.Combine(item.Path, Path.ChangeExtension(Path.GetRandomFileName(), ".txt"))
                    });

            try
            {
                var builder = new ContainerBuilder();
                builder.RegisterInstance<IConnectionFactory>(new Data.ConnectionFactory());
                var syncDb = new FexSync.Data.SyncDataDbContext(databaseFullPath);
                Assert.IsFalse(File.Exists(databaseFullPath));
                syncDb.EnsureDatabaseExists();
                Assert.IsTrue(File.Exists(databaseFullPath));

                syncDb.Accounts.Add(config.Account);
                syncDb.AccountSyncObjects.AddRange(config.SyncObjects);
                syncDb.SaveChanges();

                builder.RegisterInstance<FexSync.Data.ISyncDataDbContext>(syncDb);
                builder.RegisterInstance<FexSync.Data.IFileSystemWatcher>(new FakeFileSystemWatcher());

                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                {
                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                    CommandSignIn.User user = await conn.SignInAsync(LoginValid, PasswordValid, false);
                    foreach (var syncObject in config.SyncObjects)
                    {
                        using (var cmd = new CommandClearObject(syncObject.Token))
                        {
                            cmd.Execute(conn);
                        }
                    }

                    await conn.SignOutAsync();
                }

                using (config.Container = builder.Build())
                {
                    using (AutoResetEvent waiter = new AutoResetEvent(false))
                    {
                        using (var syncWorkflow = new ExtendedSyncWorkflow(config))
                        {
                            syncWorkflow.OnAlert += this.SyncWorkflow_OnAlert;

                            Exception firstException = null;
                            syncWorkflow.OnException += (object sender, ExceptionEventArgs e) =>
                            {
                                if (firstException == null)
                                {
                                    firstException = e.Exception;
                                }
                            };

                            if (firstException != null)
                            {
                                throw firstException;
                            }

                            syncWorkflow.StartForOneIterationAndStop(TimeSpan.FromSeconds(30));

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var remoteFileNameSource = fileNames[syncObject.Token][1];

                                Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(remoteFileNameSource)));
                                Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(0, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                            }

                            using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                            {
                                conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                                CommandSignIn.User user = await conn.SignInAsync(LoginValid, PasswordValid, false);

                                foreach (var syncObject in config.SyncObjects)
                                {
                                    var localFileName = fileNames[syncObject.Token][0];
                                    using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, syncObject))
                                    {
                                        cmd.Execute(conn);
                                        Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                    }
                                }

                                foreach (var syncObject in config.SyncObjects)
                                {
                                    var localFileName = fileNames[syncObject.Token][0];
                                    var remoteFileNameSource = fileNames[syncObject.Token][1];

                                    File.WriteAllText(remoteFileNameSource, new Random().Next().ToString());
                                    conn.Upload(syncObject.Token, null, remoteFileNameSource);
                                    File.Delete(remoteFileNameSource);

                                    File.WriteAllText(localFileName, new Random().Next().ToString());
                                }

                                await conn.SignOutAsync();
                            }

                            syncWorkflow.StartForOneIterationAndStop(TimeSpan.FromSeconds(30));

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var remoteFileNameSource = fileNames[syncObject.Token][1];

                                Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(remoteFileNameSource)));
                                Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                            }

                            {
                                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                                {
                                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                                    CommandSignIn.User user = await conn.SignInAsync(LoginValid, PasswordValid, false);

                                    Action rescan = () =>
                                    {
                                        foreach (var syncObject in config.SyncObjects)
                                        {
                                            using (CommandSaveLocalTree cmd = new CommandSaveLocalTree(syncDb, syncObject))
                                            {
                                                cmd.Execute(conn);
                                            }

                                            int treeId;
                                            using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, syncObject))
                                            {
                                                cmd.Execute(conn);
                                                treeId = cmd.Result.Value;
                                            }

                                            using (CommandPrepareDownload cmd = new CommandPrepareDownload(syncDb, treeId))
                                            {
                                                cmd.Execute(conn);
                                            }

                                            using (CommandPrepareUpload cmd = new CommandPrepareUpload(syncDb, treeId, syncObject))
                                            {
                                                cmd.Execute(conn);
                                            }
                                        }
                                    };

                                    rescan();

                                    Assert.AreEqual(0, syncDb.Uploads.Count());
                                    foreach (var syncObject in config.SyncObjects)
                                    {
                                        var localFileName = fileNames[syncObject.Token][0];
                                        var remoteFileNameSource = fileNames[syncObject.Token][1];

                                        Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(remoteFileNameSource)));
                                        Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));

                                        Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                        Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                        Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                        Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                        Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                        Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(remoteFileNameSource)));
                                    }

                                    /*
                                        ////----------------------- Conflicts

                                        //// Existing file modified remotely and locally

                                     При изменении содержимого файла:
                                     При изменении таймстампа локального файла в большую сторону он должен попасть в Upload
                                     При изменении таймстампа локального файла в меньшую сторону он должен попасть в Download
                                     При таймстампа локального файла в меньшую сторону он должен попасть в Download
                                     
                                     */

                                    foreach (var syncObject in config.SyncObjects)
                                    {
                                        var localFileName = fileNames[syncObject.Token][0];
                                        var remoteFileNameSource = fileNames[syncObject.Token][1];

                                        File.SetLastWriteTime(localFileName, File.GetLastWriteTime(localFileName).AddSeconds(11));

                                        File.WriteAllText(localFileName, localFileName);
                                        System.Threading.Thread.Sleep(2000);

                                        //// make second copy of file at server (will be two files with same file names)
                                        conn.Upload(syncObject.Token, null, localFileName);

                                        System.Threading.Thread.Sleep(2000);
                                        //// modify local file
                                        File.WriteAllText(localFileName, localFileName + localFileName);
                                    }

                                    rescan();

                                    foreach (var syncObject in config.SyncObjects)
                                    {
                                        var localFileName = fileNames[syncObject.Token][0];
                                        var remoteFileNameSource = fileNames[syncObject.Token][1];

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
                                    }

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
                if (Directory.Exists(appRoot))
                {
                    Directory.Delete(appRoot, true);
                }
            }
        }

        private void SyncWorkflow_OnAlert(object sender, Alert.AlertEventArgs e)
        {
            if (e.Alert is CaptchaRequiredAlert a)
            {
                this.ProcessCaptchaUserInputRequired(null, a.CaptchaRequestedEventArgs);
                e.Alert.MarkProcessed();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void CommandSaveLocalTreeTest()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var appRoot = Path.Combine(testRoot, Path.GetRandomFileName());
            Directory.CreateDirectory(appRoot);
            var databaseFullPath = Path.Combine(appRoot, Path.GetRandomFileName());

            var dataRoot01 = Path.Combine(testRoot, Path.GetRandomFileName());
            var dataRoot02 = Path.Combine(testRoot, Path.GetRandomFileName());
            var dataRoot03 = Path.Combine(testRoot, Path.GetRandomFileName());
            System.Diagnostics.Trace.WriteLine(testRoot);

            var config = new SyncWorkflow.SyncWorkflowConfig
            {
                Account = new Account
                {
                    Login = ConnectionTestFixture.LoginValid,
                    Password = ConnectionTestFixture.PasswordValid
                },
                ApiHost = this.UriValid.ToString()
            };

            config.SyncObjects = new[]
            {
                new AccountSyncObject
                {
                    Path = dataRoot01,
                    Token = ConnectionTestFixture.TokenValid01,
                    Account = config.Account
                },
                new AccountSyncObject
                {
                    Path = dataRoot02,
                    Token = ConnectionTestFixture.TokenValid02,
                    Account = config.Account
                },
                new AccountSyncObject
                {
                    Path = dataRoot03,
                    Token = ConnectionTestFixture.TokenValid03,
                    Account = config.Account
                }
            };

            Dictionary<string, string> fileNames = config.SyncObjects
                .ToDictionary(
                    item => item.Token,
                    item => Path.Combine(item.Path, Path.ChangeExtension(Path.GetRandomFileName(), ".txt")));

            foreach (var syncObject in config.SyncObjects)
            {
                Directory.CreateDirectory(syncObject.Path);
            }

            try
            {
                var syncDb = new FexSync.Data.SyncDataDbContext(databaseFullPath);
                syncDb.EnsureDatabaseExists();

                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                {
                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;
                    Assert.IsFalse(conn.IsSignedIn);

                    foreach (var syncObject in config.SyncObjects)
                    {
                        using (CommandSaveLocalTree cmd =
                            new CommandSaveLocalTree(syncDb, syncObject))
                        {
                            cmd.Execute(conn);
                        }
                    }

                    Assert.IsFalse(conn.IsSignedIn);
                    Assert.AreEqual(0, syncDb.LocalFiles.Count());

                    foreach (var syncObject in config.SyncObjects)
                    {
                        var fn = fileNames[syncObject.Token];
                        Directory.CreateDirectory(Path.GetDirectoryName(fn));
                        File.WriteAllText(fn, "bla-bla");

                        using (CommandSaveLocalTree cmd = new CommandSaveLocalTree(syncDb, syncObject))
                        {
                            cmd.Execute(conn);
                        }
                    }

                    Assert.AreEqual(config.SyncObjects.Length, syncDb.LocalFiles.Count());

                    foreach (var syncObject in config.SyncObjects)
                    {
                        var fn = fileNames[syncObject.Token];
                        File.Delete(fn);

                        using (CommandSaveLocalTree cmd = new CommandSaveLocalTree(syncDb, syncObject))
                        {
                            cmd.Execute(conn);
                        }
                    }

                    Assert.AreEqual(0, syncDb.LocalFiles.Count());

                    foreach (var syncObject in config.SyncObjects)
                    {
                    }
                }
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        [TestMethod]
        public void RemoteCRUDTest()
        {
            /*
            var appRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(appRoot);
            var databaseFullPath = Path.Combine(appRoot, Path.GetRandomFileName());

            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            System.Diagnostics.Trace.WriteLine(root);
            */
            var testRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var appRoot = Path.Combine(testRoot, Path.GetRandomFileName());
            Directory.CreateDirectory(appRoot);
            var databaseFullPath = Path.Combine(appRoot, Path.GetRandomFileName());

            var dataRoot01 = Path.Combine(testRoot, Path.GetRandomFileName());
            var dataRoot02 = Path.Combine(testRoot, Path.GetRandomFileName());
            var dataRoot03 = Path.Combine(testRoot, Path.GetRandomFileName());
            System.Diagnostics.Trace.WriteLine(testRoot);

            var config = new SyncWorkflow.SyncWorkflowConfig
            {
                Account = new Account
                {
                    Login = ConnectionTestFixture.LoginValid,
                    Password = ConnectionTestFixture.PasswordValid
                },
                ApiHost = this.UriValid.ToString()
            };

            config.SyncObjects = new[]
            {
                new AccountSyncObject
                {
                    Path = dataRoot01,
                    Token = ConnectionTestFixture.TokenValid01,
                    Account = config.Account
                },
                new AccountSyncObject
                {
                    Path = dataRoot02,
                    Token = ConnectionTestFixture.TokenValid02,
                    Account = config.Account
                },
                new AccountSyncObject
                {
                    Path = dataRoot03,
                    Token = ConnectionTestFixture.TokenValid03,
                    Account = config.Account
                }
            };

            Dictionary<string, string[]> fileNames = config.SyncObjects
                .ToDictionary(
                    item => item.Token, // key 
                    item => new[]
                    {
                        Path.Combine(item.Path, Path.ChangeExtension(Path.GetRandomFileName(), ".txt")),
                        Path.Combine(item.Path, "SubFolder1", "SubFolder2", "SubFolder3", Path.ChangeExtension(Path.GetRandomFileName(), ".txt"))
                    });

            Dictionary<string, CommandUpload.CommandUploadResponse[]> responses = config.SyncObjects
                .ToDictionary(
                    item => item.Token,
                    item => new CommandUpload.CommandUploadResponse[2]);

            foreach (var syncObject in config.SyncObjects)
            {
                Directory.CreateDirectory(syncObject.Path);
            }

            try
            {
                var syncDb = new FexSync.Data.SyncDataDbContext(databaseFullPath);
                syncDb.EnsureDatabaseExists();

                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                {
                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                    Assert.IsFalse(conn.IsSignedIn);
                    conn.SignIn(config.Account.Login, config.Account.Password, false);
                    Assert.IsTrue(conn.IsSignedIn);

                    try
                    {
                        foreach (var syncObject in config.SyncObjects)
                        {
                            using (var cmd = new CommandClearObject(syncObject.Token))
                            {
                                cmd.Execute(conn);
                            }
                        }

                        foreach (var syncObject in config.SyncObjects)
                        {
                            Assert.AreEqual(0, syncDb.RemoteFiles.Count());

                            using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, syncObject))
                            {
                                cmd.Execute(conn);
                            }
                        }

                        Assert.AreEqual(0, syncDb.RemoteFiles.Count());

                        foreach (var syncObject in config.SyncObjects)
                        {
                            var fn = fileNames[syncObject.Token][0];

                            var fn2 = fileNames[syncObject.Token][1];

                            Directory.CreateDirectory(Path.GetDirectoryName(fn));
                            File.WriteAllText(fn, "bla-bla");

                            responses[syncObject.Token][0] = conn.Upload(syncObject.Token, null, fn);

                            Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn)));

                            using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, syncObject))
                            {
                                cmd.Execute(conn);
                            }

                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn)));
                        }

                        Assert.AreEqual(config.SyncObjects.Length, syncDb.RemoteFiles.Count());

                        foreach (var syncObject in config.SyncObjects)
                        {
                            var fn = fileNames[syncObject.Token][0];

                            var fn2 = fileNames[syncObject.Token][1];

                            conn.DeleteFile(syncObject.Token, responses[syncObject.Token][0].UploadId);

                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn)));

                            using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, syncObject))
                            {
                                cmd.Execute(conn);
                            }

                            Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn)));
                        }

                        Assert.AreEqual(0, syncDb.RemoteFiles.Count());

                        foreach (var syncObject in config.SyncObjects)
                        {
                            var fn = fileNames[syncObject.Token][0];

                            var fn2 = fileNames[syncObject.Token][1];

                            Directory.CreateDirectory(Path.GetDirectoryName(fn2));
                            File.WriteAllText(fn2, "bla-bla2");

                            int? folderId = null;
                            using (var xxx = new CommandEnsureFolderExists(syncDb, syncObject, Path.GetDirectoryName(fn2)))
                            {
                                xxx.Execute(conn);
                                folderId = xxx.Result;
                            }

                            responses[syncObject.Token][1] = conn.Upload(syncObject.Token, folderId, fn2);

                            Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn2)));

                            using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, syncObject))
                            {
                                cmd.Execute(conn);
                            }

                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.SyncObject.Token == syncObject.Token && x.Name == Path.GetFileName(fn2)));
                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.SyncObject.Token == syncObject.Token && x.Name == Path.GetFileName(Path.GetDirectoryName(fn2))));
                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.SyncObject.Token == syncObject.Token && x.Name == Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(fn2)))));
                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.SyncObject.Token == syncObject.Token && x.Name == Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(fn2))))));

                            Assert.AreEqual(4, syncDb.RemoteFiles.Count(x => x.SyncObject.Token == syncObject.Token));
                        }

                        Assert.AreEqual(config.SyncObjects.Length * 4, syncDb.RemoteFiles.Count());

                        foreach (var syncObject in config.SyncObjects)
                        {
                            var fn2 = fileNames[syncObject.Token][1];

                            conn.DeleteFile(syncObject.Token, responses[syncObject.Token][1].UploadId);

                            Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn2)));

                            using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, syncObject))
                            {
                                cmd.Execute(conn);
                            }

                            Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => x.Name == Path.GetFileName(fn2)));
                        }

                        //// Folders still exist
                        Assert.AreEqual(config.SyncObjects.Length * 3, syncDb.RemoteFiles.Count());
                    }
                    finally
                    {
                        Assert.IsTrue(conn.IsSignedIn);

                        foreach (var syncObject in config.SyncObjects)
                        {
                            using (var cmd = new CommandClearObject(syncObject.Token))
                            {
                                cmd.Execute(conn);
                            }
                        }

                        conn.SignOut();
                        Assert.IsFalse(conn.IsSignedIn);
                    }
                }
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
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
