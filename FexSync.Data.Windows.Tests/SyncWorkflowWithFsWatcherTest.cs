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
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine(root);
            System.Diagnostics.Trace.WriteLine(root);

            var localFileName = Path.Combine(root, "Data", TokenValid, Path.GetRandomFileName(), Path.ChangeExtension("12345678_" + Path.GetRandomFileName(), ".txt"));
            var movedFileName = Path.Combine(root, "Data", TokenValid, Path.GetRandomFileName(), Path.GetFileName(localFileName));

            try
            {
                var config = new SyncWorkflow.SyncWorkflowConfig();
                config.AccountSettings = new AccountSettings(root);
                config.AccountSettings.Login = ConnectionTestFixture.LoginValid;
                config.AccountSettings.Password = ConnectionTestFixture.PasswordValid;
                config.ApiHost = this.UriValid.ToString();
                config.AccountSettings.TokenForSync = ConnectionTestFixture.TokenValid;

                var builder = new ContainerBuilder();
                builder.RegisterInstance<IConnectionFactory>(new ConnectionFactory());
                var syncDb = new FexSync.Data.SyncDataDbContext(config.AccountSettings.AccountCacheDbFile);
                builder.RegisterInstance<FexSync.Data.ISyncDataDbContext>(syncDb);
                builder.RegisterInstance<FexSync.Data.IFileSystemWatcher>(new WindowsFileSystemWatcher());

                var relativePath = localFileName.Replace(config.AccountSettings.AccountDataFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar);
                var relativeMovedPath = movedFileName.Replace(config.AccountSettings.AccountDataFolder, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                using (var conn = new Net.Fex.Api.Connection(new Net.Fex.Api.HttpClientWrapper(), this.UriValid, this.UserAgent))
                {
                    conn.OnCaptchaUserInputRequired = this.ProcessCaptchaUserInputRequired;

                    CommandSignIn.User user = conn.SignIn(LoginValid, PasswordValid, false);
                    using (var cmd = new CommandClearObject(config.AccountSettings.TokenForSync))
                    {
                        cmd.Execute(conn);
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

                            Assert.IsFalse(File.Exists(config.AccountSettings.AccountCacheDbFile));

                            using (var transferWaiter1 = new AutoResetEvent(false))
                            {
                                EventHandler onFinished = (x, y) => { transferWaiter1.Set(); };

                                syncWorkflow.OnTransferFinished += onFinished;
                                syncWorkflow.Start();

                                transferWaiter1.WaitOne();
                                syncWorkflow.OnTransferFinished -= onFinished;
                            }

                            syncDb.LockedRun(() =>
                            {
                                Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(0, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                            });

                            using (var transferWaiter1 = new AutoResetEvent(false))
                            {
                                EventHandler onFinished = (x, y) => { transferWaiter1.Set(); };

                                syncWorkflow.OnTransferFinished += onFinished;
                                if (!Directory.Exists(Path.GetDirectoryName(localFileName)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(localFileName));
                                }

                                File.WriteAllText(localFileName, "TestString");

                                transferWaiter1.WaitOne();
                                syncWorkflow.OnTransferFinished -= onFinished;
                            }

                            var sha1_01 = new FileInfo(localFileName).Sha1();

                            syncDb.LockedRun(() =>
                            {
                                Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                var xx = syncDb.LocalFiles.Single(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName));
                                var yy = syncDb.RemoteFiles.Single(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName));

                                Assert.AreEqual(sha1_01, xx.Sha1);
                                Assert.AreEqual(sha1_01, yy.Sha1);
                            });

                            using (var transferWaiter1 = new AutoResetEvent(false))
                            {
                                EventHandler onFinished = (x, y) => { transferWaiter1.Set(); };

                                syncWorkflow.OnTransferFinished += onFinished;
                                File.WriteAllText(localFileName, "tESTsTRING");

                                transferWaiter1.WaitOne();
                                syncWorkflow.OnTransferFinished -= onFinished;
                            }

                            var sha1_02 = new FileInfo(localFileName).Sha1();

                            Assert.AreNotEqual(sha1_01, sha1_02);

                            syncDb.LockedRun(() =>
                            {
                                Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Path == relativePath), relativePath);

                                var xx = syncDb.LocalFiles.Single(x => x.Path == relativePath);
                                var yy = syncDb.RemoteFiles.Single(x => x.Path == relativePath);

                                Assert.AreEqual(sha1_02, xx.Sha1);
                                Assert.AreEqual(sha1_02, yy.Sha1);
                            });

                            using (var transferWaiter1 = new AutoResetEvent(false))
                            {
                                EventHandler onFinished = (x, y) => { transferWaiter1.Set(); };

                                syncWorkflow.OnTransferFinished += onFinished;
                                if (!Directory.Exists(Path.GetDirectoryName(movedFileName)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(movedFileName));
                                }

                                File.Move(localFileName, movedFileName);

                                transferWaiter1.WaitOne();
                                syncWorkflow.OnTransferFinished -= onFinished;
                            }

                            Assert.IsFalse(File.Exists(localFileName));
                            Assert.IsTrue(File.Exists(movedFileName));

                            syncDb.LockedRun(() =>
                            {
                                Assert.AreEqual(0, syncDb.Downloads.Count(x => Path.GetFileName(x.FilePathLocalRelative) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(0, syncDb.Uploads.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(1, syncDb.LocalFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));
                                Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => Path.GetFileName(x.Path) == Path.GetFileName(localFileName)));

                                Assert.AreEqual(1, syncDb.RemoteFiles.Count(x => x.Path == relativeMovedPath), relativeMovedPath);

                                var xx = syncDb.LocalFiles.Single(x => x.Path == relativeMovedPath);
                                var yy = syncDb.RemoteFiles.Single(x => x.Path == relativeMovedPath);

                                Assert.AreEqual(sha1_02, xx.Sha1);
                                Assert.AreEqual(sha1_02, yy.Sha1);
                            });

                            Assert.IsFalse(File.Exists(localFileName));
                            Assert.IsTrue(File.Exists(movedFileName));

                            File.Delete(movedFileName);
                            System.Threading.Thread.Sleep(2000);

                            syncWorkflow.Stop();

                            syncDb.LockedRun(() =>
                            {
                                Assert.AreEqual(0, syncDb.Downloads.Count(x => x.FilePathLocalRelative == relativePath));
                                Assert.AreEqual(0, syncDb.Uploads.Count(x => x.Path == relativePath));

                                Assert.AreEqual(0, syncDb.LocalFiles.Count(x => x.Path == relativeMovedPath));
                                Assert.AreEqual(0, syncDb.RemoteFiles.Count(x => x.Path == relativeMovedPath));
                            });
                        }
                    }

                    var db = config.Container.Resolve<FexSync.Data.ISyncDataDbContext>();
                    Assert.IsNotNull(db);
                    db.LockedRun(() => { });
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
