using BacklogMaintainer.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace BacklogMaintainer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow 
    {
        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            _viewModel = new MainWindowViewModel();
            this.DataContext = this._viewModel;

            InitializeComponent();

            var SpaceName = Properties.Settings.Default.SpaceName;
            var APIKey = Properties.Settings.Default.APIKey;

            this.Loaded += async (s, e) => {
                LoginDialogData result = await this.ShowLoginAsync(
                    "Authentication",
                    "Enter your user setting",
                    new LoginDialogSettings {
                        ColorScheme = this.MetroDialogOptions.ColorScheme,
                        InitialUsername = SpaceName,
                        InitialPassword = APIKey,
                        UsernameWatermark = "SpaceName...",
                        PasswordWatermark = "APIKey...",
                        EnablePasswordPreview = true
                    });

                if (result == null)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    this._viewModel.SpaceName = result.Username;
                    this._viewModel.APIKey = result.Password;
                    this._viewModel.IsBusy = true;
                    this._viewModel.Init();
                    await this._viewModel.Update();
                    this._viewModel.IsBusy = false;
                }
            };
            this.Closing += (s, e) => {
                Properties.Settings.Default.SpaceName = this._viewModel.SpaceName;
                Properties.Settings.Default.APIKey = this._viewModel.APIKey;
                // 設定保存
                Properties.Settings.Default.Save();
            };
            this.PreviewKeyDown += (s, e) => {
                ModifierKeys modifierKeys = Keyboard.Modifiers;

                if ((modifierKeys & ModifierKeys.Alt) != ModifierKeys.None)
                {
                    this.ToggleFlyout(0);
                }
            };
        }

        private async void Refresh(object sender, RoutedEventArgs e)
        {
            this._viewModel.IsBusy = true;
            this._viewModel.Refresh();
            await this._viewModel.Update();
            this._viewModel.IsBusy = false;
            this.ToggleFlyout(0);
        }

        private void ToggleFlyout(int index)
        {
            var flyout = this.Flyouts.Items[index] as Flyout;
            if (flyout == null)
            {
                return;
            }
            flyout.IsOpen = !flyout.IsOpen;
        }
    }
}
