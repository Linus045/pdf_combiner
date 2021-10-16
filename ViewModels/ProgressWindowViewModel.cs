using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;

namespace PDF_Combiner.ViewModels
{
    class ProgressWindowViewModel : BindableBase, IDialogAware
    {
        public const string DIALOG_NAME = "ProgressWindow";

        private Progress<Tuple<string, int>> _progress;
        private bool IsDone = false;

        public ProgressWindowViewModel()
        {
            _description = "";
        }

        public string Title => "Loading";

        public void Reset()
        {
            Description = "";
        }

        public void CloseDialog()
        {
            RaiseRequestClose(null);
        }

        public bool CanCloseDialog()
        {
            return IsDone;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters is ProgressParameter progressParameter)
            {
                _progress = progressParameter.ProgressPDFCreation as Progress<Tuple<string, int>>;
                _progress.ProgressChanged += ProgressChanged;
            }
        }

        private void ProgressChanged(object sender, Tuple<string, int> currentProgress)
        {
            Description = currentProgress.Item1;
            Current = currentProgress.Item2;
            if (Current >= 100)
            {
                IsDone = true;
                CloseDialog();
            }
        }

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

        public event Action<IDialogResult> RequestClose;

        public virtual void RaiseRequestClose(IDialogResult dialogResult) => RequestClose?.Invoke(dialogResult);

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
