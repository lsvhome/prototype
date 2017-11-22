using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FexSync
{
    public static class Tests
    {
        public static void Run()
        {
            Test002();
        }

        /*
        private static void Test001()
        {
            using (var store = new FexSync.Data.BloggingContext())
            {
                store.Database.EnsureCreated();
                var blog = new Data.Blog { BlogId = 1, Url = "http://a.a.a" };
                store.Blogs.Add(blog);
                store.Posts.Add(new Data.Post { PostId = 1, Content="aaa", Blog = blog, Title= "title" });
                store.SaveChanges();
            }


            using (var store = new FexSync.Data.BloggingContext())
            {
                if (store.Blogs.Count() == store.Posts.Count())
                {

                }
            }
        }
        */
        private static void Test002()
        {
            //FexSync.Data.Class1.Test001();
        }
    }
}
