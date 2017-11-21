using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolePrivateTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(typeof(FexUploadHttpRequestCreator).AssemblyQualifiedName);

            System.Diagnostics.Debug.Assert(new PrivateContainer().Exec().AllowWriteStreamBuffering);
            FexUploadHttpRequestCreator.CustomInitAfterCreate = (x) => { x.AllowWriteStreamBuffering = false; };
            System.Diagnostics.Debug.Assert(!new PrivateContainer().Exec().AllowWriteStreamBuffering);
            FexUploadHttpRequestCreator.CustomInitAfterCreate = null;
            System.Diagnostics.Debug.Assert(new PrivateContainer().Exec().AllowWriteStreamBuffering);
        }
    }
}
