using System.Windows;
using System.Windows.Threading;

namespace ModCompendium
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup( StartupEventArgs e )
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private static void App_DispatcherUnhandledException( object sender, DispatcherUnhandledExceptionEventArgs e )
        {
#if DEBUG
            e.Handled = false;
#else
            MessageBox.Show( $"Unhandled exception occured:\n{e.Exception.Message}\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK,
                             MessageBoxImage.Error );

            e.Handled = true;
#endif
        }
    }
}
