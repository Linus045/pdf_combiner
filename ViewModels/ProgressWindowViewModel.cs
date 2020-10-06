using Prism.Mvvm;

namespace PDF_Combiner.ViewModels
{
    class ProgressWindowViewModel : BindableBase
    {

        public ProgressWindowViewModel()
        {
            _min = 0;
            _max = 1;
            _current = 0;
            _description = "";
        }

        public void Reset()
        {
            Current = 0;
            Description = "";
        }

        public void Step(string description)
        {
            Current += 1;
            Description = description;
        }

        public bool IsDone => Current >= Max;

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                RaisePropertyChanged();
            }
        }

        private int _min;
        public int Min
        {
            get => _min;
            set
            {
                _min = value;
                RaisePropertyChanged();
            }
        }

        private int _max;
        public int Max
        {
            get => _max;
            set
            {
                _max = value;
                RaisePropertyChanged();
            }
        }

        private int _current;
        public int Current
        {
            get => _current;
            set
            {
                _current = value;
                RaisePropertyChanged();
            }
        }

    }
}
