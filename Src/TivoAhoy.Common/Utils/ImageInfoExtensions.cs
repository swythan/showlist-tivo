using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.Utils
{
    public static class ImageInfoExtensions
    {
        public static ImageInfo GetBestImageForHeight(this IEnumerable<ImageInfo> imageList, int heightCutoff)
        {
            if (imageList == null ||
                !imageList.Any())
            {
                return null;
            }

            var imagesWithHeight = imageList.Where(x => x.Height != null).ToList();
            var largeImages = imagesWithHeight.Where(x => x.Height >= heightCutoff).OrderBy(x => x.Height).ToList();
            var smallImages = imagesWithHeight.Where(x => x.Height < heightCutoff).OrderByDescending(x => x.Height).ToList();

            return largeImages.Concat(smallImages).Concat(imageList.Except(imagesWithHeight)).FirstOrDefault();
        }

        public static async Task<ImageBrush> GetResizedImageBrushAsync(this ImageInfo bestImage, int requiredHeight)
        {
            ImageBrush brush = null;

            if (bestImage != null)
            {
                var bi = new BitmapImage();
                if (await bi.SetUriSourceAsync(bestImage.ImageUrl))
                {
                    var wb = new WriteableBitmap(bi);
                    bi.UriSource = null;

                    var bigImage = new BitmapImage();
                    using (var tempStream = new MemoryStream())
                    {
                        var aspectRatio = wb.PixelHeight / (double)wb.PixelWidth;

                        wb.SaveJpeg(tempStream, (int)(requiredHeight / aspectRatio), requiredHeight, 0, 95);

                        tempStream.Seek(0, SeekOrigin.Begin);
                        if (await bigImage.SetSourceAsync(tempStream))
                        {
                            brush = new ImageBrush();
                            brush.ImageSource = bigImage;
                            brush.Stretch = Stretch.Uniform;
                            brush.Opacity = 0.4;
                        }
                    }
                }
            }

            return brush;
        }

    }
}
