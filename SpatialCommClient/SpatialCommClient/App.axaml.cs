using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SpatialCommClient.ViewModels;
using SpatialCommClient.Views;

namespace SpatialCommClient
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void App_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            //((MainWindowViewModel)DataContext).OnExit();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            //((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime).Exit += App_Exit;

            base.OnFrameworkInitializationCompleted();
        }
    }
}
