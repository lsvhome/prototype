using System;
using System.ComponentModel.DataAnnotations;

namespace FexSync.Data
{
    public class RemoteTree
    {
        [Key]
        public int RemoteTreeId { get; set; }

        public DateTime Created { get; set; }
    }
}
