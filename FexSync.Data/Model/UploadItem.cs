using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FexSync.Data
{
    public class UploadItem
    {
        [Obsolete("Only for (De)Serialization purposes", true)]
        protected UploadItem()
        {
        }

        public UploadItem(string path, string token)
        {
            this.Token = token;
            this.Path = path;
        }

        [Key]
        public int UploadItemId { get; set; }

        [NotMapped]
        private string path;

        public string Path
        {
            get
            {
                return this.path;
            }

            set
            {
                this.path = value.Trim(System.IO.Path.DirectorySeparatorChar);
#if DEBUG
                if (System.IO.Path.IsPathRooted(this.path))
                {
                    throw new ApplicationException();
                }
#endif
            }
        }

        public string Token { get; set; }

        public int TriesCount { get; set; } = 0;

        public System.DateTime ItemCreated { get; set; }
    }
}
