using PDF_Combiner.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media.Imaging;

namespace PDF_Combiner.Service
{
    static class ImageService
    {

        private static RotateFlipType RotationToRotateFlipType(Rotation rotation)
        {
            switch (rotation)
            {
                default:
                case Rotation.Rotate0:
                    return RotateFlipType.RotateNoneFlipNone;
                case Rotation.Rotate90:
                    return RotateFlipType.Rotate90FlipNone;
                case Rotation.Rotate180:
                    return RotateFlipType.Rotate180FlipNone;
                case Rotation.Rotate270:
                    return RotateFlipType.Rotate270FlipNone;
            }
        }

        public static Bitmap RotateAndScaleImage(FileStream fileStream, Rotation rotation, bool scaleToFit, bool optimized, float targetDPI)
        {
            MemoryStream imageStream = new MemoryStream();
            long oldPosition = fileStream.Position;
            fileStream.Position = 0;
            fileStream.CopyTo(imageStream);

            Bitmap image = new Bitmap(imageStream);
            image.RotateFlip(RotationToRotateFlipType(rotation));

            fileStream.Position = oldPosition;

            float imageDPIX = image.HorizontalResolution;
            float imageDPIY = image.VerticalResolution;

            float imageWidth = image.Width;
            float imageHeight = image.Height;

            if (targetDPI == 0)
                targetDPI = Math.Max(imageDPIX, imageDPIY);

            imageWidth = imageWidth / imageDPIX * targetDPI;
            imageHeight = imageHeight / imageDPIY * targetDPI;

            float targetWidthAtImageDPI = 8.27f * targetDPI; // imageDPIX;
            float targetHeightAtImageDPI = 11.69f * targetDPI; // imageDPIY;

            int scaledWidth;
            int scaledHeight;
            if (scaleToFit)
            {
                float scaleAtImageDPI = Math.Min(targetWidthAtImageDPI / imageWidth, targetHeightAtImageDPI / imageHeight);
                scaledWidth = (int)(imageWidth * scaleAtImageDPI);
                scaledHeight = (int)(imageHeight * scaleAtImageDPI);
            }
            else
            {
                scaledWidth = (int)imageWidth;
                scaledHeight = (int)imageHeight;
            }
            var bmp = new Bitmap(scaledWidth, scaledHeight);
            bmp.SetResolution(targetDPI, targetDPI);
            //bmp.SetResolution(imageDPIX, imageDPIY);

            //Console.WriteLine($"{fileStream.Name} Width: {imageWidth} Height: {imageHeight} XDPI: {imageDPIX} YDPI: {imageDPIY}");
            //Console.WriteLine($"{fileStream.Name} TARGETWidth: {targetWidthAtImageDPI} TARGETHeight: {targetHeightAtImageDPI}");
            //Console.WriteLine("---------------------------------------------------------");
            //// uncomment for higher quality output

            var graph = Graphics.FromImage(bmp);
            if (optimized)
            {
                graph.InterpolationMode = InterpolationMode.High;
                graph.CompositingQuality = CompositingQuality.HighQuality;
                graph.SmoothingMode = SmoothingMode.AntiAlias;
            }
            graph.DrawImage(image, 0, 0, scaledWidth, scaledHeight);
            return bmp;
        }

        public static void DrawPageOnPDF(PdfPage pdfPage, PageModel page)
        {
            XGraphics graphics = XGraphics.FromPdfPage(pdfPage);
            pdfPage.Size = PdfSharp.PageSize.A4;
            Bitmap bm = RotateAndScaleImage(page.FileStream, page.Rotation, page.ScaleToFit,
                page.OptimizeImage, page.GetSelectedDPI());
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bm.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                XImage image = XImage.FromStream(memoryStream);
                XPoint origin;
                if (page.IsCentered)
                {
                    double x = (pdfPage.Width.Point - image.PointWidth) / 2;
                    double y = (pdfPage.Height.Point - image.PointHeight) / 2;
                    origin = new XPoint(x, y);
                }
                else
                    origin = new XPoint();
                graphics.DrawImage(image, origin);
            }
        }

    }
}
