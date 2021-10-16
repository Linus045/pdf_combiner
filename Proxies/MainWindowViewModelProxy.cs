using PDF_Combiner.ViewModels;
using System.Windows;

namespace PDF_Combiner.Proxies
{
    public class MainWindowViewModelProxy : BindingProxy<MainWindowViewModel>
    {
        protected override Freezable CreateInstanceCore()
        {
            return new MainWindowViewModelProxy();
        }
    }
}
