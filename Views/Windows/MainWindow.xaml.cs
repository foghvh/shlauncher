using Wpf.Ui.Appearance;

namespace shlauncher.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            IThemeService themeService)
        {
            ViewModel = viewModel;
            DataContext = this;

            themeService.SetTheme(ApplicationTheme.Dark);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);
            navigationService.SetNavigationControl(RootNavigation);
        }

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
            RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            // This method is part of the interface.
            // WPF UI's default NavigationWindow implementation might use this
            // internally if needed, or it's for extensibility.
            // For now, a basic implementation or even throwing NotImplementedException
            // if you don't directly use it. However, to satisfy the interface:
            // RootNavigation.SetServiceProvider(serviceProvider); // This line might not be needed if SetPageProviderService is used
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            System.Windows.Application.Current.Shutdown();
        }
    }
}