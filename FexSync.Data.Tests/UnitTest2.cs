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
    public class DataTest2 : Net.Fex.Api.Tests.ConnectionTestFixture
    {
        [TestMethod]
        public void Test1()
        {
            var appRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(appRoot);
            var databaseFullPath = Path.Combine(appRoot, Path.GetRandomFileName());

            try
            {
                using (var db0 = new FexSync.Data.SyncDataDbContext(databaseFullPath))
                {
                    db0.EnsureDatabaseExists();
                    db0.Accounts.Add(new Account { Login = "1", Password = "2" });
                    db0.SaveChanges();
                }

                using (var db1 = new FexSync.Data.SyncDataDbContext(databaseFullPath))
                {
                    var a = db1.Accounts.Single();
                    Assert.AreEqual("1", a.Login);
                    Assert.AreEqual("2", a.Password);
                }
            }
            finally
            {
                Directory.Delete(appRoot, true);
            }
        }
    }
}
