using Prism.Mvvm;

namespace PDF_Combiner.Models
{
    public class DPIOption : BindableBase
    {
        private int dPI;
        private string caption;

        public DPIOption(int dpi, string caption)
        {
            DPI = dpi;
            Caption = caption;
        }

        public int DPI
        {
            get => dPI;
            private set => SetProperty(ref dPI, value);
        }

        public string Caption
        {
            get => $"{caption} ({DPI}dpi)";
            private set => SetProperty(ref caption, value);
        }
    }
}