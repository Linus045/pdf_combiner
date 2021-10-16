using Microsoft.Win32;
using PDF_Combiner.Models;
using PDF_Combiner.Windows;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
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
    public class MainWindowViewModel : BindableBase
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

        public bool IsRunning
        {
            get => _running;
            set
            {
                SetProperty(ref _running, value);
            }
        }

        private int imageHeight;
        public int ImageHeight
        {
            get => imageHeight;
            set => SetProperty(ref imageHeight, value);
        }

        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            SaveToPDFCommand = new DelegateCommand(async () => await SaveToPDF(), () => !IsRunning && Pages.Count > 0).ObservesProperty(() => IsRunning);
            AddImageCommand = new DelegateCommand(AddImage, () => !IsRunning).ObservesProperty(() => IsRunning);
            ScanImageCommand = new DelegateCommand(ScanImage, () => false);
            MoveLeftCommand = new DelegateCommand(MoveLeft, CanMoveLeft).ObservesProperty(() => IsRunning);
            MoveRightCommand = new DelegateCommand(MoveRight, CanMoveRight).ObservesProperty(() => IsRunning);
            DeleteImageCommand = new DelegateCommand(DeleteImage, () => !IsRunning && PageIsSelected).ObservesProperty(() => IsRunning);
            RotateImageCommand = new DelegateCommand(RotateImage, () => !IsRunning && PageIsSelected).ObservesProperty(() => IsRunning);
            Pages = new ObservableCollection<Page>();
            IsRunning = false;
            Optimization = Optimization.None;
            ImageHeight = 400;
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

        private readonly IDialogService _dialogService;
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
            if (IsRunning)
                return false;
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
            if (IsRunning)
                return false;
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

        private async Task SaveToPDF()
        {
            //TODO: Make Async
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF-File|*.pdf"
            };

            if ((bool)saveFileDialog.ShowDialog())
            {
                string fileName = saveFileDialog.FileName;
                IsRunning = true;

                IProgress<Tuple<string, int>> progressPDFCreation = new Progress<Tuple<string, int>>();

                _dialogService.Show(ProgressWindowViewModel.DIALOG_NAME, new ProgressParameter(progressPDFCreation), (result) =>
                {
                    IsRunning = false;
                    progressPDFCreation.Report(new Tuple<string, int>("", 100));
                    MessageBox.Show($@"Datei wurde erfolgreich unter ""{fileName}"" abgespeichert und wird jetzt geöffnet.");
                    Process.Start(fileName);
                });

                await Task.Run(() => CreatePDF(fileName, Optimization, progressPDFCreation));
            }
        }

        private void CreatePDF(string fileName, Optimization optimization, IProgress<Tuple<string, int>> progressPDFCreation)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            using (PdfDocument pdf = new PdfDocument(fileName))
            {
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

                progressPDFCreation.Report(new Tuple<string, int>("", 0));
                for (int i = 0; i < Pages.Count; i++)
                {
                    Page page = Pages[i];
                    progressPDFCreation.Report(new Tuple<string, int>(page.ImageName, 100 / Pages.Count * i));
                    //if (!_progressWindowViewModel.IsDone)
                    //    _progressWindowViewModel.Step($"Converting {page.ImageName}...");
                    PdfPage pdfPage = pdf.AddPage();
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
                progressPDFCreation.Report(new Tuple<string, int>("", 100));
                pdf.Close();
            }
        }

        private Bitmap RotateAndScaleImage(FileStream fileStream, Rotation rotation, bool scaleToFit, bool optimized, float targetDPI)
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