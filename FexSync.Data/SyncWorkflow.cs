using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class SyncWorkflow : IDisposable
    {
        public enum SyncWorkflowStatus
        {
            Stopped,

            Starting,

            Started,

            WaitingForAlert,

            Indexing,

            Transferring,

            Idle,

            Stopping
        }

        public class Singleton : Singleton<SyncWorkflow>
        {
        }

        public class SyncWorkflowConfig
        {
            public string ApiHost { get; set; }

            public Autofac.IContainer Container { get; set; }

            public Account Account { get; set; }

            public AccountSyncObject[] SyncObjects { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        protected SyncWorkflowConfig config = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed.")]
        protected static object lockObj = new object();

        private IConnection connection = null;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public SyncWorkflow()
        {
            this.Status = SyncWorkflowStatus.Stopped;
            this.alerts.OnChanged += (object sender, EventArgs e) =>
            {
                this.OnStatusChanged?.Invoke(this, new EventArgs());
            };
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
                try
                {
                    if (this.config != null && this.connection == null)
                    {
                        //// recreate cancellationTokenSource
                        this.cancellationTokenSource.Dispose();
                        this.cancellationTokenSource = new CancellationTokenSource();

                        this.Status = SyncWorkflowStatus.Starting;

                        Uri endPoint = new Uri(this.config.ApiHost);
                        this.connection = this.config.Container.Resolve<IConnectionFactory>()
                            .CreateConnection(endPoint, this.cancellationTokenSource.Token);

                        this.Task_RunSafe(this.SignIn);

                        this.OnStarted?.Invoke(this, new EventArgs());
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine("ApplicationException");
                        System.Diagnostics.Debug.Flush();
                        throw new ApplicationException();
                    }
                }
                catch (Exception ex)
                {
                    ex.Process();
                    throw;
                }
            }
        }

        public void Stop()
        {
            lock (lockObj)
            {
                this.Status = SyncWorkflow.SyncWorkflowStatus.Stopping;

                if (this.connection != null)
                {
                    this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>().Stop();

                    this.cancellationTokenSource.Cancel();

                    Task.WaitAll(this.scheduledTasks.ToArray());

                    if (this.connection.IsSignedIn)
                    {
                        this.connection.SignOut();
                    }

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

        #region Status

        public event EventHandler OnStatusChanged;

        private SyncWorkflowStatus internalStatus = SyncWorkflowStatus.Stopped;

        public SyncWorkflowStatus Status
        {
            get
            {
                lock (lockObj)
                {
                    if (this.cancellationTokenSource.IsCancellationRequested)
                    {
                        System.Diagnostics.Trace.WriteLine("Status = Stopping");
                        return SyncWorkflowStatus.Stopping;
                    }
                    else if (this.alerts.Any())
                    {
                        System.Diagnostics.Trace.WriteLine("Status = WaitingForAlert");
                        return SyncWorkflowStatus.WaitingForAlert;
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"Status = {this.internalStatus}");
                        return this.internalStatus;
                    }
                }
            }

            private set
            {
                this.internalStatus = value;
                this.OnStatusChanged?.Invoke(this, new EventArgs());
                System.Diagnostics.Trace.WriteLine($"Status2 = {value}");
            }
        }

        #endregion Status

        private void Connect_OnCaptchaUserInputRequired(object sender, Net.Fex.Api.CommandCaptchaRequestPossible.CaptchaRequestedEventArgs args)
        {
            using (var waiter = new AutoResetEvent(false))
            {
                var alert = new CaptchaRequiredAlert(args, waiter) { Id = 1 };

                this.alerts.Add(alert);

                this.OnAlert?.Invoke(this, new Alert.AlertEventArgs { Alert = alert });

                WaitHandle.WaitAny(new WaitHandle[] { waiter, this.cancellationTokenSource.Token.WaitHandle });

                this.alerts.Remove(alert);
            }
        }

        protected virtual void SignIn()
        {
            this.connection.OnCaptchaUserInputRequired = this.Connect_OnCaptchaUserInputRequired;
            while (!this.connection.IsSignedIn)
            {
                this.connection.CancellationToken.ThrowIfCancellationRequested();
                try
                {
                    this.connection.SignIn(this.config.Account.Login, this.config.Account.Password, false);
                }
                catch (CaptchaRequiredException ex)
                {
                    ex.Process();
                }
            }

            this.Task_RunSafe(() => { this.Init(this.connection); });
        }

        protected virtual void ReIndex()
        {
            this.Status = SyncWorkflowStatus.Indexing;
            this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>().Stop();

            this.PrepareTransferQueues(this.connection);

            this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>().Start(this.config.SyncObjects.Select(obj => new DirectoryInfo(obj.Path)).ToArray());

            this.Task_RunSafe(() => { this.Transfer(this.connection); });
        }

        private void Init(IConnection conn)
        {
            foreach (var syncObject in this.config.SyncObjects)
            {
                if (!Directory.Exists(syncObject.Path))
                {
                    Directory.CreateDirectory(syncObject.Path);
                }

                if (!conn.Exists(syncObject.Token, null, Constants.TrashBinFolderName))
                {
                    conn.CreateFolder(syncObject.Token, null, Constants.TrashBinFolderName);
                }
            }

            this.Task_RunSafe(this.ReIndex);
        }

        private void PrepareTransferQueues(IConnection conn)
        {
            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            syncDb.LockedRun(() =>
            {
                foreach (var syncObject in this.config.SyncObjects)
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
            });
        }

        private long transferQueue = 0;

        public event EventHandler OnTransferFinished;

        private void Transfer(IConnection conn)
        {
            try
            {
                this.connection.CancellationToken.ThrowIfCancellationRequested();
                this.Status = SyncWorkflowStatus.Transferring;

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

                    foreach (var syncObject in this.config.SyncObjects)
                    {
                        this.connection.CancellationToken.ThrowIfCancellationRequested();

                        using (CommandUploadQueue cmd = new CommandUploadQueue(syncDb, new DirectoryInfo(syncObject.Path), syncObject.Token))
                        {
                            cmd.Execute(conn);
                        }

                        using (CommandDownloadQueue cmd = new CommandDownloadQueue(syncDb, syncObject))
                        {
                            var fileSystemWatcher = this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>();

                            cmd.OnBeforeSave += (sender, args) => { fileSystemWatcher.AddFilterPath(args.FullPath); };

                            cmd.OnAfterSave += (sender, args) => { fileSystemWatcher.RemoveFilterPath(args.FullPath); };

                            cmd.Execute(conn);
                        }
                    }
                });

                this.Status = SyncWorkflowStatus.Idle;

                this.OnTransferFinished?.Invoke(this, new EventArgs());

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