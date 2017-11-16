using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FexSync
{
    public class QuickStartImageList
    {
        private string[] imageNames;

        private int index = 0;

        public QuickStartImageList()
        {
            var regex = new System.Text.RegularExpressions.Regex(@"^FexSync\.Resources\.QuickStart\.(?<number>\d\d\d)\.png$");
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            this.imageNames = assembly.GetManifestResourceNames().Where(x => regex.IsMatch(x)).OrderBy(x => regex.Match(x).Groups["number"].Value).ToArray();
            System.Diagnostics.Debug.Assert(this.imageNames.Length > 0, "QuickStart images");
        }

        public BitmapImage GetCurrentImageSource()
        {
            using (Image image = this.GetCurrentImage())
            {
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Bmp);
                    ms.Seek(0, SeekOrigin.Begin);

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();

                    return bitmapImage;
                }
            }
        }

        private Image GetCurrentImage()
        {
            return this.GetResourceImage(this.imageNames[this.index]);
        }

        private Image GetResourceImage(string resName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resName))
            {
                return Image.FromStream(stream);
            }
        }

        public bool HasPrevious
        {
            get
            {
                return this.index > 0;
            }
        }

        public bool HasNext
        {
            get
            {
                return this.index < this.imageNames.Length - 1;
            }
        }

        public void MoveNext()
        {
            if (!this.HasNext)
            {
                throw new ApplicationException();
            }

            this.index++;
        }

        public void MoveBack()
        {
            if (!this.HasPrevious)
            {
                throw new ApplicationException();
            }

            this.index--;
        }

        public void Reset()
        {
            this.index = 0;
        }
    }
}
