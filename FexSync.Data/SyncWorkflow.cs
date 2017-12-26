using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Autofac;
using FexSync.Data;
using Net.Fex.Api;

namespace FexSync.Data
{
    public partial class SyncWorkflow : IDisposable, IConfigurable
    {
        public enum SyncWorkflowStatus
        {
            Stopped,

            Starting,

            Started,

            WaitingForAlert,

            Stopping
        }

        public void Configure(object settings)
        {
            this.Reconfigure((SyncWorkflowConfig)settings);
        }

        public class Singleton : Singleton<SyncWorkflow>
        {
        }

        public class SyncWorkflowConfig
        {
            public string ApiHost { get; set; }

            public Autofac.IContainer Container { get; set; }

            public AccountSettings AccountSettings { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        protected SyncWorkflowConfig config = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        protected static object lockObj = new object();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        protected System.ComponentModel.BackgroundWorker worker;

        private IConnection connection = null;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public SyncWorkflow()
        {
        }

        private void Watcher_OnError(object sender, ErrorEventArgs e)
        {
            this.OnException?.Invoke(this, new ExceptionEventArgs(e.GetException()));
        }

        public void Reconfigure(SyncWorkflowConfig config)
        {
            lock (lockObj)
            {
                this.config = config;

                FexSync.Data.IFileSystemWatcher fileSystemWatcher = this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>();

                // usubscribe
                fileSystemWatcher.OnFileCreated -= this.Watcher_OnFileCreated;
                fileSystemWatcher.OnFileModified -= this.Watcher_OnFileModified;
                fileSystemWatcher.OnFileMoved -= this.Watcher_OnFileMoved;
                fileSystemWatcher.OnFileDeleted -= this.Watcher_OnFileDeleted;

                fileSystemWatcher.OnFolderDeleted -= this.Watcher_OnFolderDeleted;
                fileSystemWatcher.OnFolderMoved -= this.Watcher_OnFolderMoved;

                fileSystemWatcher.OnError -= this.Watcher_OnError;

                // subscribe
                fileSystemWatcher.OnFileCreated += this.Watcher_OnFileCreated;
                fileSystemWatcher.OnFileModified += this.Watcher_OnFileModified;
                fileSystemWatcher.OnFileMoved += this.Watcher_OnFileMoved;
                fileSystemWatcher.OnFileDeleted += this.Watcher_OnFileDeleted;

                fileSystemWatcher.OnFolderDeleted += this.Watcher_OnFolderDeleted;
                fileSystemWatcher.OnFolderMoved += this.Watcher_OnFolderMoved;

                fileSystemWatcher.OnError += this.Watcher_OnError;
            }
        }

        public void Start()
        {
            lock (lockObj)
            {
                if (this.config != null && this.worker == null && this.connection == null)
                {
                    System.Diagnostics.Trace.WriteLine("BackgroundWorker creating");
                    this.worker = new System.ComponentModel.BackgroundWorker();
                    System.Diagnostics.Trace.WriteLine("BackgroundWorker created");
                    this.worker.WorkerSupportsCancellation = true;
                    this.worker.DoWork += this.Worker_DoWork;

                    //// recreate cancellationTokenSource
                    this.cancellationTokenSource.Dispose();
                    this.cancellationTokenSource = new CancellationTokenSource();

                    Uri endPoint = new Uri(this.config.ApiHost);
                    this.connection = this.config.Container.Resolve<IConnectionFactory>().CreateConnection(endPoint, this.cancellationTokenSource.Token);
                    this.worker.RunWorkerAsync();
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine("ApplicationException");
                    System.Diagnostics.Debug.Flush();
                    throw new ApplicationException();
                }
            }
        }

        public void Stop()
        {
            lock (lockObj)
            {
                if (this.connection != null)
                {
                    this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>().Stop();

                    if (this.worker != null)
                    {
                        using (var workerCompleteWaiter = new AutoResetEvent(false))
                        {
                            this.worker.RunWorkerCompleted += (workerSender, args) =>
                            {
                                workerCompleteWaiter.Set();
                            };

                            if (this.worker.IsBusy)
                            {
                                this.cancellationTokenSource.Cancel();
                                this.worker.CancelAsync();
                                workerCompleteWaiter.WaitOne();
                            }
                        }

                        this.worker.Dispose();
                        this.worker = null;
                    }

                    Task.WaitAll(this.scheduledTasks.ToArray());

                    this.connection.SignOut();
                    this.connection.OnCaptchaUserInputRequired = null;

                    this.connection.Dispose();
                    this.connection = null;
                }

                //// Wait for all db opertions finished
                ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
                syncDb.LockedRun(() => { });
                this.OnStopped?.Invoke(this, new EventArgs());
            }
        }

        public event EventHandler<Alert.AlertEventArgs> OnAlert;

        public event EventHandler<ExceptionEventArgs> OnException;

        public event EventHandler OnStarted;

        public event EventHandler OnStopped;

        public void Dispose()
        {
            FexSync.Data.IFileSystemWatcher fileSystemWatcher = this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>();

            fileSystemWatcher.OnFileCreated -= this.Watcher_OnFileCreated;
            fileSystemWatcher.OnFileModified -= this.Watcher_OnFileModified;
            fileSystemWatcher.OnFileMoved -= this.Watcher_OnFileMoved;
            fileSystemWatcher.OnFileDeleted -= this.Watcher_OnFileDeleted;

            fileSystemWatcher.OnFolderDeleted -= this.Watcher_OnFolderDeleted;
            fileSystemWatcher.OnFolderMoved -= this.Watcher_OnFolderMoved;

            fileSystemWatcher.OnError -= this.Watcher_OnError;
            fileSystemWatcher.Dispose();

            this.Stop();
            this.cancellationTokenSource.Dispose();
        }

        public SyncWorkflowStatus Status
        {
            get
            {
                lock (lockObj)
                {
                    if (this.alerts.Any())
                    {
                        System.Diagnostics.Trace.WriteLine("Status = WaitingForAlert");
                        return SyncWorkflowStatus.WaitingForAlert;
                    }

                    if (this.worker == null)
                    {
                        System.Diagnostics.Trace.WriteLine("Status = Stopped");
                        return SyncWorkflowStatus.Stopped;
                    }
                    else
                    {
                        if (this.cancellationTokenSource.IsCancellationRequested)
                        {
                            System.Diagnostics.Trace.WriteLine("Status = Stopping");
                            return SyncWorkflowStatus.Stopping;
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine("Status = Started");
                            return SyncWorkflowStatus.Started;
                        }
                    }

                    throw new ApplicationException();
                }
            }
        }

        private void Connect_OnCaptchaUserInputRequired(object sender, Net.Fex.Api.CommandCaptchaRequestPossible.CaptchaRequestedEventArgs args)
        {
            using (var waiter = new AutoResetEvent(false))
            {
                var alert = new CaptchaRequiredAlert(args, waiter) { Id = 1 };

                this.alerts.Add(alert);

                this.OnAlert?.Invoke(this, new Alert.AlertEventArgs { Alert = alert });

                waiter.WaitOne();

                this.alerts.Remove(alert);
            }
        }

        protected virtual void Worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            this.OnStarted?.Invoke(this, new EventArgs());

            try
            {
                Uri endPoint = new Uri(this.config.ApiHost);

                this.connection.OnCaptchaUserInputRequired = this.Connect_OnCaptchaUserInputRequired;
                while (!this.connection.IsSignedIn)
                {
                    this.connection.CancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        this.connection.SignIn(this.config.AccountSettings.Login, this.config.AccountSettings.Password, false);
                    }
                    catch (CaptchaRequiredException ex)
                    {
                        ex.Process();
                    }
                }

                this.Init(this.connection);

                this.PrepareTransferQueues(this.connection);

                this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>().Start(new[] { new DirectoryInfo(this.config.AccountSettings.AccountDataFolder) });

                this.connection.CancellationToken.ThrowIfCancellationRequested();

                this.Transfer(this.connection);

                this.connection.CancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                //// do nothing - suppress exception
            }
            catch (Exception ex)
            {
                ex.Process();

                this.OnException?.Invoke(this, new ExceptionEventArgs(ex));

                System.Diagnostics.Debug.Fail(ex.ToString());
            }
        }

        private void Init(IConnection conn)
        {
            string tokenFolder = Path.Combine(this.config.AccountSettings.AccountDataFolder, this.config.AccountSettings.TokenForSync);
            if (!Directory.Exists(tokenFolder))
            {
                Directory.CreateDirectory(tokenFolder);
            }

            if (!conn.Exists(this.config.AccountSettings.TokenForSync, null, AccountSettings.TrashBinFolderName))
            {
                conn.CreateFolder(this.config.AccountSettings.TokenForSync, null, AccountSettings.TrashBinFolderName);
            }

            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            syncDb.LockedRun(() => { syncDb.EnsureDatabaseExists(); });
        }

        private void PrepareTransferQueues(IConnection conn)
        {
            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            syncDb.LockedRun(() =>
            {
                using (CommandSaveLocalTree cmd = new CommandSaveLocalTree(syncDb, new DirectoryInfo(this.config.AccountSettings.AccountDataFolder)))
                {
                    cmd.Execute(conn);
                }

                int treeId;
                using (CommandSaveRemoteTree cmd = new CommandSaveRemoteTree(syncDb, this.config.AccountSettings.TokenForSync))
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
            });
        }

        private long transferQueue = 0;

        public event EventHandler OnTransferFinished;

        private void Transfer(IConnection conn)
        {
            if (Interlocked.Read(ref this.transferQueue) > 0)
            {
                return;
            }

            Interlocked.Increment(ref this.transferQueue);

            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            //// Transfer should be locked!!!
            syncDb.LockedRun(() =>
            {
                Interlocked.Decrement(ref this.transferQueue);

                using (CommandUploadQueue cmd = new CommandUploadQueue(syncDb, new DirectoryInfo(this.config.AccountSettings.AccountDataFolder), this.config.AccountSettings.TokenForSync))
                {
                    cmd.Execute(conn);
                }

                using (CommandDownloadQueue cmd = new CommandDownloadQueue(syncDb, new DirectoryInfo(this.config.AccountSettings.AccountDataFolder)))
                {
                    var fileSystemWatcher = this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>();

                    cmd.OnBeforeSave += (sender, args) =>
                    {
                        fileSystemWatcher.AddFilterPath(args.FullPath);
                    };

                    cmd.OnAfterSave += (sender, args) =>
                    {
                        fileSystemWatcher.RemoveFilterPath(args.FullPath);
                    };

                    cmd.Execute(conn);
                }
            });

            this.OnTransferFinished?.Invoke(this, new EventArgs());
        }

        private readonly ThreadSafeListWithLock<Alert> alerts = new ThreadSafeListWithLock<Alert>();

        public IEnumerable<Alert> Alerts
        {
            get
            {
                this.Task_RunSafe(() => { this.alerts.RemoveAll(x => x.Processed); });
                return this.alerts.Where(item => !item.Processed).ToArray();
            }
        }
    }
}