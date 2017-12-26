/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Fex.Api.Tests
{
    public partial class FakeConnectionFactory : FexSync.Data.IConnectionFactory
    {
        public IConnection CreateConnection(Uri endPoint)
        {
            return new FakeConnection();
        }

        public IConnection CreateConnection(Uri endPoint, CancellationToken cancellationToken)
        {
            return new FakeConnection();
        }
    }


    public partial class FakeConnection : IConnection
    {
        IDictionary<string, object> x = new Dictionary<string, object>();
        Queue<object> cc = new Queue<object>();
        public FakeConnection(IDictionary<string, object> actions = null)
        {
        }

        public CommandSignIn.User UserSignedIn => (CommandSignIn.User)this.cc.Dequeue();

        public bool IsSignedIn => (bool)this.cc.Dequeue();

        public string AuthCookie { get; set; }

        public Uri Endpoint => new Uri("http://test.com");

        public IHttpClient Client => (IHttpClient)this.cc.Dequeue();

        public CancellationToken CancellationToken => (CancellationToken)this.cc.Dequeue();

        public Action<object, CommandCaptchaRequestPossible.CaptchaRequestedEventArgs> OnCaptchaUserInputRequired { get; set; }

        public event EventHandler<ExceptionEventArgs> OnException;

        public CommandAccess.CommandAccessResponse Access(string token)
        {
            return (CommandAccess.CommandAccessResponse)this.cc.Dequeue();
        }

        public Task<CommandAccess.CommandAccessResponse> AccessAsync(string token)
        {
            return Task.FromResult<CommandAccess.CommandAccessResponse>(this.Access(token));
        }

        public CommandArchive.CommandArchiveResponse Archive(int offset, int limit)
        {
            return (CommandArchive.CommandArchiveResponse)this.cc.Dequeue();
        }

        public Task<CommandArchive.CommandArchiveResponse> ArchiveAsync(int offset, int limit)
        {
            (aaa)this.cc.Dequeue();
        }

        public string CaptchaGetUserInput(CommandCaptchaToken.CommandCaptchaTokenResult r)
        {
            (aaa)this.cc.Dequeue();
        }

        public int CreateFolder(string token, int? folderId, string folderName)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<int> CreateFolderAsync(string token, int? folderId, string folderName)
        {
            (aaa)this.cc.Dequeue();
        }

        public CommandCreateObject.CommandCreateObjectResponse CreateObject()
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<CommandCreateObject.CommandCreateObjectResponse> CreateObjectAsync()
        {
            (aaa)this.cc.Dequeue();
        }

        public void DeleteFile(string token, int uploadId)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task DeleteFileAsync(string token, int uploadId)
        {
            (aaa)this.cc.Dequeue();
        }

        public void Dispose()
        {
            (aaa)this.cc.Dequeue();
        }

        public bool Exists(string token, int? parentFolderUploadId, string name)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<bool> ExistsAsync(string token, int? parentFolderUploadId, string name)
        {
            (aaa)this.cc.Dequeue();
        }

        public void Get(string token, int uploadId, string filePath)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task GetAsync(string token, int uploadId, string filePath)
        {
            (aaa)this.cc.Dequeue();
        }

        public CommandObjectView.CommandObjectViewResponseObject[] GetChildren(string token, int? folderUploadId)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<CommandObjectView.CommandObjectViewResponseObject[]> GetChildrenAsync(string token, int? folderUploadId)
        {
            (aaa)this.cc.Dequeue();
        }

        public CommandIndex.CommandIndexResponse Index()
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<CommandIndex.CommandIndexResponse> IndexAsync()
        {
            (aaa)this.cc.Dequeue();
        }

        public void Load(string token, int uploadId, string filePath)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task LoadAsync(string token, int uploadId, string filePath)
        {
            (aaa)this.cc.Dequeue();
        }

        public bool LoginCheck(string login)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<bool> LoginCheckAsync(string login)
        {
            (aaa)this.cc.Dequeue();
        }

        public void Move(string token, int dstFolderId, params int[] moveUploadIds)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task MoveAsync(string token, int dstFolderId, params int[] moveUploadIds)
        {
            (aaa)this.cc.Dequeue();
        }

        public CommandObjectFolderList.CommandObjectFolderListResponse ObjectFolderList(string token, int[] folderUploadIds)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<CommandObjectFolderList.CommandObjectFolderListResponse> ObjectFolderListAsync(string token, int[] folderUploadIds)
        {
            (aaa)this.cc.Dequeue();
        }

        public CommandObjectFolderView.CommandObjectFolderViewResponse ObjectFolderView(string token, int folderUploadId)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<CommandObjectFolderView.CommandObjectFolderViewResponse> ObjectFolderViewAsync(string token, int folderUploadId)
        {
            (aaa)this.cc.Dequeue();
        }

        public void ObjectUpdate(string token, string post)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task ObjectUpdateAsync(string token, string post)
        {
            (aaa)this.cc.Dequeue();
        }

        public CommandObjectView.CommandObjectViewResponse ObjectView(string token)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<CommandObjectView.CommandObjectViewResponse> ObjectViewAsync(string token)
        {
            (aaa)this.cc.Dequeue();
        }

        public CommandSignIn.User SignIn(string login, string password, bool stay_signed)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<CommandSignIn.User> SignInAsync(string login, string password, bool stay_signed)
        {
            (aaa)this.cc.Dequeue();
        }

        public void SignOut()
        {
            (aaa)this.cc.Dequeue();
        }

        public Task SignOutAsync()
        {
            (aaa)this.cc.Dequeue();
        }

        public CommandUpload.CommandUploadResponse Upload(string token, int? folderId, string filePath, TimeSpan? timeout = null)
        {
            (aaa)this.cc.Dequeue();
        }

        public Task<CommandUpload.CommandUploadResponse> UploadAsync(string token, int? folderId, string filePath, TimeSpan? timeout = null)
        {
            (aaa)this.cc.Dequeue();
        }
    }
}
*/