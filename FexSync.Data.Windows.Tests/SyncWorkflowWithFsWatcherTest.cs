using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Autofac;
using FexSync.Data;
using FexSync.Data.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Fex.Api;
using Net.Fex.Api.Tests;

namespace FexSync.Data.Windows.Tests
{
    [TestClass]
    public partial class SyncWorkflowWithFsWatcherTest : Net.Fex.Api.Tests.ConnectionTestFixture
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            AppDomain.CurrentDomain.UnhandledException += (source, exceptionObjectParam) =>
            {
                if (exceptionObjectParam.ExceptionObject is Exception exception)
                {
                    exception.Process();
                }
                else
                {
                    System.Diagnostics.Trace.Fail("Exception is empty");
                }
            };
        }

        [TestMethod]
        public void SyncWorkflowWithFsWatcherTest001()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var appRoot = Path.Combine(testRoot, Path.GetRandomFileName());
            Directory.CreateDirectory(appRoot);
            var databaseFullPath = Path.Combine(appRoot, Path.GetRandomFileName());

            var dataRoot01 = Path.Combine(testRoot, Path.GetRandomFileName());
            var dataRoot02 = Path.Combine(testRoot, Path.GetRandomFileName());
            var dataRoot03 = Path.Combine(testRoot, Path.GetRandomFileName());
            System.Diagnostics.Trace.WriteLine(testRoot);

            var config = new SyncWorkflow.SyncWorkflowConfig();
            config.Account = new Account();
            config.Account.Login = ConnectionTestFixture.LoginValid;
            config.Account.Password = ConnectionTestFixture.PasswordValid;
            config.ApiHost = this.UriValid.ToString();

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
                        Path.Combine(item.Path, Path.GetRandomFileName(), Path.ChangeExtension("12345678_" + item.Token, ".txt")), ////localFileName
                        Path.Combine(item.Path, Path.GetRandomFileName(), Path.ChangeExtension("12345678_" + item.Token, ".txt")), ////movedFileName
                        null,
                        null
                    });

            try
            {
                var builder = new ContainerBuilder();
                builder.RegisterInstance<IConnectionFactory>(new ConnectionFactory());
                var syncDb = new FexSync.Data.SyncDataDbContext(databaseFullPath);
                Assert.IsFalse(File.Exists(databaseFullPath));
                syncDb.EnsureDatabaseExists();
                Assert.IsTrue(File.Exists(databaseFullPath));

                syncDb.Accounts.Add(config.Account);
                syncDb.AccountSyncObjects.AddRange(config.SyncObjects);
                syncDb.SaveChanges();

                builder.RegisterInstance<FexSync.Data.ISyncDataDbContext>(syncDb);
                builder.RegisterInstance<FexSync.Data.IFileSystemWatcher>(new WindowsFileSystemWatcher());

                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                {
                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                    CommandSignIn.User user = conn.SignIn(LoginValid, PasswordValid, false);

                    foreach (var syncObject in config.SyncObjects)
                    {
                        using (var cmd = new CommandClearObject(syncObject.Token))
                        {
                            cmd.Execute(conn);
                        }
                    }

                    conn.SignOut();
                }

                using (config.Container = builder.Build())
                {
                    using (AutoResetEvent waiter = new AutoResetEvent(false))
                    {
                        using (var syncWorkflow = new FexSync.Data.Tests.ExtendedSyncWorkflow(config))
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

                            using (var transferWaiter1 = new AutoResetEvent(false))
                            {
                                EventHandler onFinished = (x, y) => { transferWaiter1.Set(); };

                                syncWorkflow.OnTransferFinished += onFinished;
                                syncWorkflow.Start();

                                transferWaiter1.WaitOne();
                                syncWorkflow.OnTransferFinished -= onFinished;
                            }

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var movedFileName = fileNames[syncObject.Token][1];

                                var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                                syncDb.LockedRun(() =>
                                {
                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(0, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                });
                            }

                            using (var transferWaiter1 = new AutoResetEvent(false))
                            {
                                EventHandler onFinished = (x, y) => { transferWaiter1.Set(); };

                                syncWorkflow.OnTransferFinished += onFinished;

                                foreach (var syncObject in config.SyncObjects)
                                {
                                    var localFileName = fileNames[syncObject.Token][0];
                                    var movedFileName = fileNames[syncObject.Token][1];

                                    var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                    var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                                    if (!Directory.Exists(Path.GetDirectoryName(localFileName)))
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(localFileName));
                                    }

                                    File.WriteAllText(localFileName, "TestString");
                                    fileNames[syncObject.Token][2] = new FileInfo(localFileName).Sha1(); // sha1_01 
                                }

                                transferWaiter1.WaitOne();
                                syncWorkflow.OnTransferFinished -= onFinished;
                            }

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var movedFileName = fileNames[syncObject.Token][1];

                                var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                                syncDb.LockedRun(() =>
                                {
                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    var xx = syncDb.LocalFiles.Single(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName));
                                    var yy = syncDb.RemoteFiles.Single(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName));

                                    Assert.AreEqual(fileNames[syncObject.Token][2], xx.Sha1);
                                    Assert.AreEqual(fileNames[syncObject.Token][2], yy.Sha1);
                                });
                            }

                            using (var transferWaiter1 = new AutoResetEvent(false))
                            {
                                EventHandler onFinished = (x, y) => { transferWaiter1.Set(); };

                                syncWorkflow.OnTransferFinished += onFinished;
                                foreach (var syncObject in config.SyncObjects)
                                {
                                    var localFileName = fileNames[syncObject.Token][0];
                                    var movedFileName = fileNames[syncObject.Token][1];

                                    File.WriteAllText(localFileName, "tESTsTRING");
                                    fileNames[syncObject.Token][3] = new FileInfo(localFileName).Sha1(); // sha1_02

                                    Assert.AreNotEqual(fileNames[syncObject.Token][2], fileNames[syncObject.Token][3]);
                                }

                                transferWaiter1.WaitOne();
                                syncWorkflow.OnTransferFinished -= onFinished;
                            }

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var movedFileName = fileNames[syncObject.Token][1];

                                var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                                syncDb.LockedRun(() =>
                                {
                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Path == relativePath), relativePath);

                                    var xx = syncDb.LocalFiles.Single(x => x.Path == relativePath);
                                    var yy = syncDb.RemoteFiles.Single(x => x.Path == relativePath);

                                    Assert.AreEqual(fileNames[syncObject.Token][3], xx.Sha1);
                                    Assert.AreEqual(fileNames[syncObject.Token][3], yy.Sha1);
                                });
                            }

                            using (var transferWaiter1 = new AutoResetEvent(false))
                            {
                                EventHandler onFinished = (x, y) => { transferWaiter1.Set(); };

                                syncWorkflow.OnTransferFinished += onFinished;
                                foreach (var syncObject in config.SyncObjects)
                                {
                                    var localFileName = fileNames[syncObject.Token][0];
                                    var movedFileName = fileNames[syncObject.Token][1];

                                    var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                    var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                                    if (!Directory.Exists(Path.GetDirectoryName(movedFileName)))
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(movedFileName));
                                    }

                                    File.Move(localFileName, movedFileName);
                                }

                                transferWaiter1.WaitOne();
                                syncWorkflow.OnTransferFinished -= onFinished;
                            }

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var movedFileName = fileNames[syncObject.Token][1];

                                var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                Assert.IsFalse(File.Exists(localFileName));
                                Assert.IsTrue(File.Exists(movedFileName));
                            }

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var movedFileName = fileNames[syncObject.Token][1];

                                var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                                syncDb.LockedRun(() =>
                                {
                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                    Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                    Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Path == relativeMovedPath), relativeMovedPath);

                                    var xx = syncDb.LocalFiles.Single(x => x.Path == relativeMovedPath);
                                    var yy = syncDb.RemoteFiles.Single(x => x.Path == relativeMovedPath);

                                    Assert.AreEqual(fileNames[syncObject.Token][3], xx.Sha1);
                                    Assert.AreEqual(fileNames[syncObject.Token][3], yy.Sha1);
                                });
                            }

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var movedFileName = fileNames[syncObject.Token][1];

                                var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                                Assert.IsFalse(File.Exists(localFileName));
                                Assert.IsTrue(File.Exists(movedFileName));

                                File.Delete(movedFileName);
                                System.Threading.Thread.Sleep(2000);
                            }

                            syncWorkflow.Stop();

                            foreach (var syncObject in config.SyncObjects)
                            {
                                var localFileName = fileNames[syncObject.Token][0];
                                var movedFileName = fileNames[syncObject.Token][1];

                                var relativePath = localFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                                var relativeMovedPath = movedFileName.Replace(syncObject.Path, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                                syncDb.LockedRun(() =>
                                {
                                    Assert.AreEqual(0, syncDb.Downloads.Count(x => x.FilePathLocalRelative == relativePath));
                                    Assert.AreEqual(0, syncDb.Uploads.Count(x => x.Path == relativePath));

                                    Assert.AreEqual(0, syncDb.LocalFiles.Count(x => x.Path == relativeMovedPath));
                                    Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => x.Path == relativeMovedPath));
                                });
                            }
                        }
                    }

                    var db = config.Container.Resolve<FexSync.Data.ISyncDataDbContext>();
                    Assert.IsNotNull(db);
                    db.LockedRun(() => { });
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

        public void ProcessCaptchaUserInputRequired(object sender, CommandCaptchaRequestPossible.CaptchaRequestedEventArgs captchaRequestArgs)
        {
            var answer = Net.Fex.Api.Tests.Extenders.RequestCaptchaAnswer(null, captchaRequestArgs.CaptchaToken);
            captchaRequestArgs.CaptchaText = answer.UserInput;
        }
    }
}
