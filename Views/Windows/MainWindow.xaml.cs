using Wpf.Ui.Appearance;
using Wpf.Ui.Animations;
using Wpf.Ui.Controls;

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
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            System.Windows.Application.Current.Shutdown();
        }

        private void RootNavigation_OnNavigated(NavigationView sender, NavigatedEventArgs args)
        {
            TransitionAnimationProvider.ApplyTransition(args.Page, Transition.FadeInWithSlide, 200);
        }
    }
}