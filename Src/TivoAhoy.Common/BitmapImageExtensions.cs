using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TivoAhoy.Common
{
    public static class BitmapImageExtensions
    {
        public static Task<bool> SetUriSourceAsync(this BitmapImage image, Uri sourceUri)
        {
            var tcs = new TaskCompletionSource<bool>();

            image.CreateOptions = BitmapCreateOptions.None;

            image.ImageOpened += (o, e) =>
            {
                tcs.SetResult(true);
            };

            image.ImageFailed += (o, e) =>
            {
                tcs.SetResult(false);
            };

            image.UriSource = sourceUri;

            return tcs.Task;
        }

        public static Task<bool> SetSourceAsync(this BitmapImage image, Stream source)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (image.PixelWidth != 0)
            {
                tcs.SetException(new InvalidOperationException("Image already loaded"));
            }
            else
            {
                image.CreateOptions = BitmapCreateOptions.None;

                image.ImageOpened += (o, e) =>
                {
                    tcs.SetResult(true);
                };

                image.ImageFailed += (o, e) =>
                {
                    tcs.SetResult(false);
                };

                image.SetSource(source);

                if (image.PixelWidth != 0)
                {
                    tcs.SetResult(true);
                }
            }

            return tcs.Task;
        }
    }

}
