using PDF_Combiner.ViewModels;
using PDF_Combiner.Windows;
using PDF_Combiner.Windows.View;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Unity;
using System.Windows;

namespace PDF_Combiner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override void Initialize()
        {
            DispatcherUnhandledException += UnhandledException;
            base.Initialize();
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();
            ViewModelLocationProvider.Register(typeof(MainWindow).ToString(), typeof(MainWindowViewModel));
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<ProgressWindow, ProgressWindowViewModel>(ProgressWindowViewModel.DIALOG_NAME);
        }

        private void UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An error occured!\n Message:\n{e.Exception.Message}\n{e.Exception.StackTrace}");
        }

    }
}
