using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Prism.Mvvm;

namespace PDF_Combiner.Models
{
    class Page : BindableBase, IDisposable
    {
        public string ImagePath
        {
            get => _imagePath; private set
            {
                _imagePath = value;
                _imageName = Path.GetFileName(ImagePath);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ImageName));
            }
        }

        public string ImageName => _imageName;

        public ImageSource ImageSource { get; private set; }

        public bool ImageValid { get; private set; }
        public Rotation Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                LoadImage();
                RaisePropertyChanged();
            }
        }

        public FileStream FileStream { get => _fileStream; }
        public bool IsCentered
        {
            get => _isCentered;
            set
            {
                _isCentered = value;
                RaisePropertyChanged();
            }
        }

        private bool _scaleToFit;
        public bool ScaleToFit
        {
            get => _scaleToFit;
            set
            {
                _scaleToFit = value;
                RaisePropertyChanged();
            }
        }

        private bool _optimizeImage;
        public bool OptimizeImage
        {
            get => _optimizeImage;
            set
            {
                _optimizeImage = value;
                RaisePropertyChanged();
            }
        }


        private FileStream _fileStream;
        private string _imagePath;
        private string _imageName;
        private Rotation _rotation;
        private bool _isCentered;

        public Page(string imagePath)
        {
            ImageValid = true;
            _rotation = Rotation.Rotate0;
            _isCentered = true;
            _scaleToFit = true;
            _optimizeImage = true;
            ImagePath = imagePath;
            LoadImage();
        }

        BitmapImage GetBitmapImage(Rotation rotation)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = _fileStream;
            bitmapImage.Rotation = rotation;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        private void LoadImage()
        {
            try
            {
                _fileStream = File.OpenRead(ImagePath);
                ImageSource = GetBitmapImage(Rotation);
                RaisePropertyChanged(nameof(ImageSource));
                ImageValid = true;
            }
            catch
            {
                ImageValid = false;
            }
        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }
    }
}
