using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NpmPackChecker.WUI.MVVM.View;
using NpmPackChecker.WUI.MVVM.ViewModel;
using NpmPackChecker.WUI.Services;
using System;

namespace NpmPackChecker.WUI
{
    public partial class App : Application
    {
        public static Window MainWindow;
        public static IHost Host;

        private UIElement? _shell { get; set; }

#if IS_NONE_PACKAGE
        public static bool IsNonePackage { get => true; }
#else
        public static bool IsNonePackage { get => false; }
#endif

        public static T GetService<T>() where T : class
        {
            if (App.Host.Services.GetService(typeof(T)) is not T service)
                throw new ArgumentException($"{typeof(T)} is not registered in app");

            return service;
        }

        public App()
        {
            InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<ShellView>();
                    services.AddTransient<ShellViewModel>();

                    services.AddTransient<SettingsView>();
                    services.AddTransient<SettingsViewModel>();

                    services.AddTransient<NpmPackDepView>();
                    services.AddTransient<NpmPackDepViewModel>();

                    services.AddSingleton<NavigationHelperService>();
                    services.AddSingleton<InfoBarService>();
                    services.AddSingleton<NpmRegService>();

                    services.AddHttpClient();
                })
                .Build();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();

            _shell = App.GetService<ShellView>();
            MainWindow.Content = _shell ?? new Frame();

            MainWindow.Activate();
        }

    }
}
