using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PDF_Combiner.ViewModels;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Prism.Mvvm;

namespace PDF_Combiner.Models
{
    public class PageModel : BindableBase, IDisposable
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

        private DPIOption _targetDPI;
        public DPIOption TargetDPI
        {
            get => _targetDPI;
            set => SetProperty(ref _targetDPI, value);
        }

        public ObservableCollection<DPIOption> DPIOptions { get; }

        public string DPIText
        {
            get => dPIText;
            set => SetProperty(ref dPIText, value);
        }

        public float GetSelectedDPI()
        {
            if (int.TryParse(DPIText, out int dpi))
            {
                if (dpi < 0)
                    dpi = 1;
            }
            return dpi;
        }

        private FileStream _fileStream;
        private string _imagePath;
        private string _imageName;
        private Rotation _rotation;
        private bool _isCentered;
        private string dPIText;

        public PageModel(string imagePath)
        {
            ImageValid = true;
            _rotation = Rotation.Rotate0;
            _isCentered = true;
            _scaleToFit = true;
            _optimizeImage = true;
            _targetDPI = null;
            ImagePath = imagePath;
            LoadImage();

            DPIOptions = new ObservableCollection<DPIOption>
            {
                new DPIOption(300, "Print resolution"),
                new DPIOption(75, "Normal resolution")
            };

            int imagedPI = (int)Math.Max((ImageSource as BitmapSource).DpiX, (ImageSource as BitmapSource).DpiY);
            DPIOptions.Add(new DPIOption(imagedPI, "Original image DPI"));
            TargetDPI = DPIOptions.Last();
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
