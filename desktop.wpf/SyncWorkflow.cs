using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autofac;
using Net.Fex.Api;
using FexSync.Data;

namespace FexSync
{
    public class SyncWorkflow : IDisposable
    {
        public enum SyncWorkflowStatus
        {
            Started,
            Stopped
        }

        private static object lockObj = new object();

        private readonly System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();

        public SyncWorkflow()
        {
            this.worker.WorkerSupportsCancellation = true;
            this.worker.DoWork += this.Worker_DoWork;
        }

        public void Start()
        {
            lock (lockObj)
            {
                if (this.status == SyncWorkflowStatus.Stopped && !this.worker.CancellationPending)
                {
                    this.status = SyncWorkflowStatus.Started;
                    this.worker.RunWorkerAsync();
                }
            }
        }

        public void Stop()
        {
            lock (lockObj)
            {
                if (this.worker.IsBusy)
                {
                    this.worker.CancelAsync();
                }
            }
        }

        public event EventHandler<Connection.ExceptionEventArgs>  OnException;

        public void WaitStoppped(TimeSpan timeout)
        {
            if (this.worker.CancellationPending)
            {
                System.Threading.AutoResetEvent waiter = new System.Threading.AutoResetEvent(false);
                waiter.Reset();
                this.worker.RunWorkerCompleted += (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) => { waiter.Set(); };
                waiter.WaitOne();
            }
        }

        public void SyncOneFile(string token)
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        private SyncWorkflowStatus status = SyncWorkflowStatus.Stopped;
        
        public SyncWorkflowStatus Status
        {
            get
            {
                lock (lockObj)
                {
                    return this.status;
                }
            }
        }

        private void Connect_OnCaptchaUserInputRequired(object sender, Net.Fex.Api.CommandCaptchaRequestPossible.CaptchaRequestedEventArgs e)
        {
            // do nothing. We can't request user input
        }

        private void Worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                string login;
                string password;

                CredentialsManager.Load(out login, out password);

                using (var conn = ((App)App.Current).Container.Resolve<IConnectionFactory>().CreateConnection())
                {
                    conn.OnCaptchaUserInputRequired = this.Connect_OnCaptchaUserInputRequired;
                    var signin = conn.SignIn(login, password, false);
                    try
                    {
                        while (!this.worker.CancellationPending)
                        {
                            this.BuildDownloadList(conn);

                            this.BuildUploadList();



                            System.Threading.Thread.Sleep(10000);
                        }
                    }
                    finally
                    {
                        conn.SignOut();
                        conn.OnCaptchaUserInputRequired = null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.OnException != null)
                {
                    this.OnException(this, new Connection.ExceptionEventArgs(ex));
                }

                System.Diagnostics.Debug.Fail(ex.ToString());
            }
            finally
            {
                lock (lockObj)
                {
                    this.status = SyncWorkflowStatus.Stopped;
                }
            }
        }

        //private readonly List<string> syncList = new List<string>();

        //private static string appFolderName = "FEX.NET";

        //private static string AppFolderFullPath
        //{
        //    get
        //    {
        //        11
        //        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appFolderName);
        //    }
        //}

        private void BuildDownloadList(IConnection conn)
        {
            CommandBuildRemoteTree.CommandBuildRemoteTreeResponse tree = conn.BuildRemoteTree();
            ISyncDataDbContext syncDb = ((App)App.Current).Container.Resolve<ISyncDataDbContext>();

            System.Diagnostics.Debug.Assert(tree.List.All(x => x is CommandBuildRemoteTree.CommandBuildRemoteTreeItemArchive));

            foreach (var each in tree.List.SelectMany(x => x.Childern))
            {
                ProcessRemoteItemForDownload(each, syncDb);
            }
        }

        private void Download(IConnection conn)
        {
            ISyncDataDbContext syncDb = ((App)App.Current).Container.Resolve<ISyncDataDbContext>();
            var maxTriesCount = syncDb.Download.Max(x=> (int?)x.TriesCount) ?? 1;
            while (syncDb.Download.Any(x => x.TriesCount < maxTriesCount))
            {
                var di = syncDb.Download.Where(x=>x.TriesCount < maxTriesCount).OrderBy(x => x.TriesCount).ThenByDescending(x => x.ItemCreated).First();
                try
                {
                    conn.Get(di.Token, di.UploadId);
                }
                catch (Exception)
                {
                    //syncDb.DownloadFailed.Add(di);
                    //syncDb.Download.Remove(di);
                }
                finally
                {
                }
            }

        }


        private void BuildUploadList()
        {
            ISyncDataDbContext syncDb = ((App)App.Current).Container.Resolve<ISyncDataDbContext>();

            var localFiles = this.GetLocalFiles();


            foreach (var each in localFiles.Keys)
            {
                ProcessLocalFileForUpload(each, localFiles[each], syncDb);
            }
        }

        private void ProcessLocalFileForUpload(string path, FileInfo item, ISyncDataDbContext syncDb)
        {

            var localFile = syncDb.Local.SingleOrDefault(x => string.Equals(x.Path, item.FullName, StringComparison.InvariantCultureIgnoreCase));

            if (localFile == null || !IsItemsEqual(item, localFile))
            {
                if (!syncDb.Upload.Any(x => x.Path == item.FullName))
                {
                    var uploadItem = new UploadItem
                    {
                        //// UploadItemId - ???
                        Path = item.FullName
                    };

                    syncDb.Upload.Add(uploadItem);
                }
            }
        }

        private void ProcessRemoteItemForDownload(CommandBuildRemoteTree.CommandBuildRemoteTreeItemObject item, ISyncDataDbContext syncDb)
        {
            var remoteFile = syncDb.RemoteFiles.SingleOrDefault(x => x.Token == item.Token && x.UploadId == item.UploadId);

            if (remoteFile == null || !IsItemsEqual(item, remoteFile))
            {
                if (!syncDb.Download.Any(x => x.Token == item.Token && x.UploadId == item.UploadId))
                {
                    var downloadItem = new DownloadItem
                    {
                        //// DownloadItemId - ???
                        Token = item.Token,
                        UploadId = item.UploadId,
                        ItemCreated = DateTime.Now
                    };
                    syncDb.Download.Add(downloadItem);

                }
            }

            foreach (var each in item.Childern)
            {
                ProcessRemoteItemForDownload(each, syncDb);
            }
        }

        private bool IsItemsEqual(FileInfo item, LocalFile localFile)
        {
            if (item == null)
            {
                return false;
            }

            if (localFile == null)
            {
                return false;
            }

            if (item.FullName != localFile.Path)
            {
                return false;
            }

            if (item.Length != localFile.Length)
            {
                return false;
            }

            return true;
        }


        private bool IsItemsEqual(CommandBuildRemoteTree.CommandBuildRemoteTreeItemObject item, RemoteFile remoteFile)
        {
            if (item == null)
            {
                return false;
            }

            if (remoteFile == null)
            {
                return false;
            }

            if (item.Object.Name != remoteFile.Name)
            {
                return false;
            }

            if (item.Token != remoteFile.Token)
            {
                return false;
            }

            if (item.UploadId != remoteFile.UploadId)
            {
                return false;
            }

            if (item.Object.Size != remoteFile.Size)
            {
                return false;
            }

            if (item.Object.Sha1 != remoteFile.Sha1)
            {
                return false;
            }

            return true;
        }

        public Dictionary<string, FileInfo> GetLocalFiles()
        {
            var t = System.IO.Directory.GetFiles(System.Configuration.ConfigurationManager.AppSettings["DataFolder"], "*", SearchOption.AllDirectories);

            Dictionary<string, FileInfo> d = new Dictionary<string, FileInfo>();

            foreach (var each in t)
            {
                FileInfo fi = new FileInfo(each);
                d.Add(each, fi);
            }

            return d;
        }
    }

}