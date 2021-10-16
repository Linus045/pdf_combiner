using Prism.Services.Dialogs;
using System;

namespace PDF_Combiner.ViewModels
{
    internal class ProgressParameter : DialogParameters
    {
        public ProgressParameter(IProgress<Tuple<string, int>> progressPDFCreation)
        {
            ProgressPDFCreation = progressPDFCreation;
        }

        public IProgress<Tuple<string, int>> ProgressPDFCreation { get; set; }
    }
}