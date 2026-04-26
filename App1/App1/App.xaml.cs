using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace App1
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default <see cref="Application"/> class.
    /// </summary>
    public sealed partial class App : CoreIsland.Application
    {
        private CoreIsland.Window? _window;

        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;

            _window = new CoreIsland.Window()
            {
                Content = rootFrame
            };

            rootFrame.Navigate(typeof(MainPage), e.Arguments);

            _window.Show();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails.
        /// </summary>
        /// <param name="sender">The Frame which failed navigation.</param>
        /// <param name="e">Details about the navigation failure.</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
