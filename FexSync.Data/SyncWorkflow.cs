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

namespace FexSync
{
    public partial class SyncWorkflow : IDisposable, IConfigurable
    {
        private static object lockDb = new object();

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

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public SyncWorkflow()
        {
        }

        public void Reconfigure(SyncWorkflowConfig config)
        {
            lock (lockObj)
            {
                this.config = config;
            }
        }

        public void Start()
        {
            lock (lockObj)
            {
                if (this.config != null && this.worker == null)
                {
                    System.Diagnostics.Debug.WriteLine("BackgroundWorker creating");
                    this.worker = new System.ComponentModel.BackgroundWorker();
                    System.Diagnostics.Debug.WriteLine("BackgroundWorker created");
                    this.worker.WorkerSupportsCancellation = true;
                    this.worker.DoWork += this.Worker_DoWork;

                    this.worker.RunWorkerCompleted += (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) =>
                    {
                        Task.Run(() =>
                        {
                            var stoppedWorker = this.worker;
                            lock (lockObj)
                            {
                                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                                System.Diagnostics.Debug.WriteLine("BackgroundWorker destroing");
                                this.worker = null;
                                System.Diagnostics.Debug.WriteLine("BackgroundWorker destroyed");
                            }

                            stoppedWorker.Dispose();

                            System.Diagnostics.Debug.WriteLine("OnStopped begin");
                            if (this.OnStopped != null)
                            {
                                this.OnStopped(this, new EventArgs());
                            }
                        });
                    };

                    this.cancellationTokenSource.Dispose();
                    this.cancellationTokenSource = new CancellationTokenSource();
                    this.worker.RunWorkerAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ApplicationException");
                    System.Diagnostics.Debug.Flush();
                    throw new ApplicationException();
                }
            }
        }

        public void Stop()
        {
            lock (lockObj)
            {
                if (this.worker != null)
                {
                    this.worker.CancelAsync();
                }

                this.cancellationTokenSource.Cancel();
            }
        }

        public event EventHandler<ExceptionEventArgs> OnException;

        public event EventHandler OnIterationFinished;

        public event EventHandler OnStarted;

        public event EventHandler OnStopped;

        public void Dispose()
        {
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
                        System.Diagnostics.Debug.WriteLine("Status = WaitingForAlert");
                        return SyncWorkflowStatus.WaitingForAlert;
                    }

                    if (this.worker == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Status = Stopped");
                        return SyncWorkflowStatus.Stopped;
                    }
                    else
                    {
                        if (this.cancellationTokenSource.IsCancellationRequested)
                        {
                            System.Diagnostics.Debug.WriteLine("Status = Stopping");
                            return SyncWorkflowStatus.Stopping;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Status = Started");
                            return SyncWorkflowStatus.Started;
                        }
                    }

                    throw new ApplicationException();
                }
            }
        }

        public Action<object, CommandCaptchaRequestPossible.CaptchaRequestedEventArgs> OnCaptchaUserInputRequired { get; set; }

        private void Connect_OnCaptchaUserInputRequired(object sender, Net.Fex.Api.CommandCaptchaRequestPossible.CaptchaRequestedEventArgs args)
        {
            using (var waiter = new AutoResetEvent(false))
            {
                var alert = new CaptchaRequiredAlert(args, waiter) { Id = 1 };

                this.alerts.Add(alert);

                waiter.WaitOne();

                this.alerts.Remove(alert);
            }

            if (this.OnCaptchaUserInputRequired != null)
            {
                this.OnCaptchaUserInputRequired(sender, args);
            }
        }

        protected virtual void Worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (this.OnStarted != null)
            {
                this.OnStarted(this, new EventArgs());
            }

            try
            {
                Uri endPoint = new Uri(this.config.ApiHost);
                using (var conn = this.config.Container.Resolve<IConnectionFactory>().CreateConnection(endPoint, this.cancellationTokenSource.Token))
                {
                    try
                    {
                        conn.OnCaptchaUserInputRequired = this.Connect_OnCaptchaUserInputRequired;
                        while (!conn.IsSignedIn)
                        {
                            conn.CancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                conn.SignIn(this.config.AccountSettings.Login, this.config.AccountSettings.Password, false);
                            }
                            catch (CaptchaRequiredException ex)
                            {
                                ex.Process();
                            }
                        }

                        this.Init(conn);

                        this.PrepareTransferQueues(conn);

                        this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>().Start(new[] { new DirectoryInfo(this.config.AccountSettings.AccountDataFolder) });

                        while (true)
                        {
                            conn.CancellationToken.ThrowIfCancellationRequested();

                            this.Transfer(conn);

                            if (this.OnIterationFinished != null)
                            {
                                this.OnIterationFinished(this, new EventArgs());
                            }

                            conn.CancellationToken.ThrowIfCancellationRequested();

                            System.Diagnostics.Debug.WriteLine("FexSync goes to sleep.");
#if DEBUG
                            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
#else
                            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(60));
#endif
                            System.Diagnostics.Debug.WriteLine("FexSync waked up.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        //// do nothing - suppress exception
                    }
                    catch (Exception ex)
                    {
                        ex.Process();
                        throw;
                    }
                    finally
                    {
                        this.config.Container.Resolve<FexSync.Data.IFileSystemWatcher>().Stop();
                        conn.SignOut();
                        conn.OnCaptchaUserInputRequired = null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.OnException != null)
                {
                    this.OnException(this, new ExceptionEventArgs(ex));
                }

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
            lock (lockDb)
            {
                syncDb.EnsureDatabaseExists();
            }
        }

        private void PrepareTransferQueues(IConnection conn)
        {
            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            lock (lockDb)
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
            }
        }

        private void Transfer(IConnection conn)
        {
            ISyncDataDbContext syncDb = this.config.Container.Resolve<ISyncDataDbContext>();
            lock (lockDb)
            {
                using (CommandUploadQueue cmd = new CommandUploadQueue(syncDb, new DirectoryInfo(this.config.AccountSettings.AccountDataFolder), this.config.AccountSettings.TokenForSync))
                {
                    cmd.Execute(conn);
                }

                using (CommandDownloadQueue cmd = new CommandDownloadQueue(syncDb, new DirectoryInfo(this.config.AccountSettings.AccountDataFolder)))
                {
                    cmd.Execute(conn);
                }
            }
        }

        private readonly ThreadSafeListWithLock<Alert> alerts = new ThreadSafeListWithLock<Alert>();

        public IEnumerable<Alert> Alerts
        {
            get
            {
                Task.Run(() => { this.alerts.RemoveAll(x => x.Processed); });
                return this.alerts.Where(item => !item.Processed).ToArray();
            }
        }
    }
}