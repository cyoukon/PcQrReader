using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcQrReader
{
    public static class Extensions
    {
        public static System.Windows.Media.Imaging.BitmapImage ToBitmapImage(this System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Position = 0;
                System.Windows.Media.Imaging.BitmapImage result = new System.Windows.Media.Imaging.BitmapImage();
                result.BeginInit();
                result.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }
    }
}
