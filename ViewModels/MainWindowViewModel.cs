using Microsoft.Win32;
using PDF_Combiner.Models;
using PDF_Combiner.Windows;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PDF_Combiner.ViewModels
{

    class MainWindowViewModel : BindableBase
    {
        public ICommand SaveToPDFCommand { get; }
        public ICommand AddImageCommand { get; }
        public ICommand MoveLeftCommand { get; }
        public ICommand MoveRightCommand { get; }
        public ICommand RotateImageCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand ScanImageCommand { get; }
        public ObservableCollection<Page> Pages { get; }
        public bool PageIsSelected { get => _selectedPage != null; }

        private Optimization _optimization;
        public Optimization Optimization
        {
            get => _optimization;
            set
            {
                _optimization = value;
                RaisePropertyChanged();
            }
        }

        private Page _selectedPage;
        public Page SelectedPage
        {
            get => _selectedPage;
            set
            {
                _selectedPage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PageIsSelected));
                ((DelegateCommand)MoveLeftCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)MoveRightCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)RotateImageCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)DeleteImageCommand).RaiseCanExecuteChanged();
            }
        }

        private bool Running
        {
            get => _running;
            set
            {
                _running = value;
                RaisePropertyChanged();
            }
        }

        public MainWindowViewModel()
        {
            SaveToPDFCommand = new DelegateCommand(SaveToPDF, () => Pages.Count > 0);
            AddImageCommand = new DelegateCommand(AddImage);
            ScanImageCommand = new DelegateCommand(ScanImage);
            MoveLeftCommand = new DelegateCommand(MoveLeft, CanMoveLeft);
            MoveRightCommand = new DelegateCommand(MoveRight, CanMoveRight);
            DeleteImageCommand = new DelegateCommand(DeleteImage, () => PageIsSelected);
            RotateImageCommand = new DelegateCommand(RotateImage, () => PageIsSelected);
            Pages = new ObservableCollection<Page>();
            //TODO: Utilize Dialog Service from Prism
            _progressWindow = new ProgressWindow
            {
                Topmost = true
            };
            _progressWindowViewModel = new ProgressWindowViewModel();
            _progressWindow.DataContext = _progressWindowViewModel;
            Running = false;
            Optimization = Optimization.None;
        }

        private void ScanImage()
        {
            MessageBox.Show("Not yet implemented!");
        }

        private void DeleteImage()
        {
            if (MessageBox.Show("Are you sure you want to remove this page?", "Remove Page", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Pages.Remove(SelectedPage);
                SelectedPage = null;
            }
        }

        //TODO: Utilize Dialog Service from Prism
        private readonly ProgressWindowViewModel _progressWindowViewModel;
        private readonly ProgressWindow _progressWindow;
        private bool _running;

        private void RotateImage()
        {
            if (SelectedPage != null)
                switch (SelectedPage.Rotation)
                {
                    case Rotation.Rotate0:
                        SelectedPage.Rotation = Rotation.Rotate90;
                        break;
                    case Rotation.Rotate90:
                        SelectedPage.Rotation = Rotation.Rotate180;
                        break;
                    case Rotation.Rotate180:
                        SelectedPage.Rotation = Rotation.Rotate270;
                        break;
                    case Rotation.Rotate270:
                        SelectedPage.Rotation = Rotation.Rotate0;
                        break;
                }
        }


        private bool CanMoveRight()
        {
            if (!PageIsSelected)
                return false;
            int selectedPageIdx = Pages.IndexOf(SelectedPage);
            return selectedPageIdx < Pages.Count - 1;
        }

        private void MoveRight()
        {
            if (PageIsSelected)
            {
                int selectedPageIdx = Pages.IndexOf(SelectedPage);
                if (selectedPageIdx < Pages.Count)
                    Pages.Move(selectedPageIdx, selectedPageIdx + 1);
                SelectedPage = SelectedPage;
            }
        }

        private bool CanMoveLeft()
        {
            if (!PageIsSelected)
                return false;
            int selectedPageIdx = Pages.IndexOf(SelectedPage);
            return selectedPageIdx > 0;
        }

        private void MoveLeft()
        {
            if (PageIsSelected)
            {
                int selectedPageIdx = Pages.IndexOf(SelectedPage);
                if (selectedPageIdx > 0)
                    Pages.Move(selectedPageIdx, selectedPageIdx - 1);
                SelectedPage = SelectedPage;
            }
        }

        private void AddImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select image file to add",
                Filter = @"All Image Files|*.BMP;*.bmp;*.JPG;*.JPEG*.jpg;*.jpeg;*.PNG;*.png;|PNG|*.PNG;*.png|JPEG|*.JPG;*.JPEG*.jpg;*.jpeg|Bitmap(.BMP,.bmp)|*.BMP;*.bmp"
            };
            if ((bool)openFileDialog.ShowDialog())
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    Page page = new Page(filename);
                    if (page.ImageValid)
                        Pages.Add(page);
                }
            }
            ((DelegateCommand)SaveToPDFCommand).RaiseCanExecuteChanged();
        }

        private void SaveToPDF()
        {
            //TODO: Make Async
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF-File|*.pdf"
            };
            if ((bool)saveFileDialog.ShowDialog())
            {
                string fileName = saveFileDialog.FileName;
                _progressWindowViewModel.Reset();
                _progressWindowViewModel.Min = 0;
                _progressWindowViewModel.Max = Pages.Count;
                Running = true;
                Task taskPDF = new Task(() => CreatePDF(fileName, Optimization));
                taskPDF.ContinueWith((oldTask) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        {
                            Running = false;
                            _progressWindow.Hide();
                            if (oldTask.IsFaulted)
                                MessageBox.Show($"Error while creating PDF: {oldTask.Exception.Message} \n {oldTask.Exception.StackTrace}");
                            else
                            {
                                MessageBox.Show($@"Datei wurde erfolgreich unter ""{fileName}"" abgespeichert und wird jetzt geöffnet.");
                                Process.Start(fileName);
                            }
                        });
                });
                taskPDF.Start();
                _progressWindow.ShowDialog();
            }
        }

        private void CreatePDF(string fileName, Optimization optimization)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
            PdfDocument pdf = new PdfDocument(fileName);
            switch (optimization)
            {
                case Optimization.OptimizeSpeed:
                    pdf.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Never;
                    pdf.Options.FlateEncodeMode = PdfFlateEncodeMode.BestSpeed;
                    pdf.Options.NoCompression = true;
                    pdf.Options.CompressContentStreams = false;
                    break;
                case Optimization.OptimizeSize:
                    pdf.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
                    pdf.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Automatic;
                    pdf.Options.NoCompression = false;
                    pdf.Options.CompressContentStreams = true;
                    break;
                default:
                case Optimization.None:
                    pdf.Options.CompressContentStreams = false;
                    pdf.Options.FlateEncodeMode = PdfFlateEncodeMode.Default;
                    break;
            }

            foreach (Page page in Pages)
            {
                if (!_progressWindowViewModel.IsDone)
                    _progressWindowViewModel.Step($"Converting {page.ImageName}...");
                PdfPage pdfPage = pdf.AddPage();
                XGraphics graphics = XGraphics.FromPdfPage(pdfPage);
                pdfPage.Size = PdfSharp.PageSize.A4;
                Bitmap bm = RotateAndScaleImage(page.FileStream, page.Rotation, page.ScaleToFit, page.OptimizeImage);
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
            pdf.Close();
        }

        private Bitmap RotateAndScaleImage(FileStream fileStream, Rotation rotation, bool scaleToFit, bool optimized)
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

            float targetWidthAtImageDPI = 8.27f * imageDPIX;
            float targetHeightAtImageDPI = 11.69f * imageDPIY;

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
            bmp.SetResolution(imageDPIX, imageDPIY);

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

        private RotateFlipType RotationToRotateFlipType(Rotation rotation)
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
    }
}