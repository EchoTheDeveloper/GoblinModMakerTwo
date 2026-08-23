using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using GMMLauncher.Views;

namespace GMMLauncher.ViewModels
{
    public partial class UpdateWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);
        public ICommand InstallBepInExCommand => new RelayCommand(InstallBepInEx);
        
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (_isScanning == value)
                    return;

                _isScanning = value;
                OnPropertyChanged();
            }
        }

        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText == value)
                    return;

                _statusText = value;
                OnPropertyChanged();
            }
        }

        private string _gmmVersionText = "";
        public string GMMVersionText
        {
            get => _gmmVersionText;
            set
            {
                if (_gmmVersionText == value)
                    return;

                _gmmVersionText = value;
                OnPropertyChanged();
            }
        }

        private bool _showGMMUpdateButton;
        public bool ShowGMMUpdateButton
        {
            get => _showGMMUpdateButton;
            set
            {
                if (_showGMMUpdateButton == value)
                    return;

                _showGMMUpdateButton = value;
                OnPropertyChanged();
            }
        }

        private string _gmmUpdateText = "";
        public string GMMUpdateText
        {
            get => _gmmUpdateText;
            set
            {
                if (_gmmUpdateText == value)
                    return;

                _gmmUpdateText = value;
                OnPropertyChanged();
            }
        }
        
        private string _bepInExVersionText = "";
        public string BepInExVersionText
        {
            get => _bepInExVersionText;
            set
            {
                if (_bepInExVersionText == value)
                    return;

                _bepInExVersionText = value;
                OnPropertyChanged();
            }
        }

        private bool _showBepInExUpdateButton;
        public bool ShowBepInExUpdateButton
        {
            get => _showBepInExUpdateButton;
            set
            {
                if (_showBepInExUpdateButton == value)
                    return;

                _showBepInExUpdateButton = value;
                OnPropertyChanged();
            }
        }

        private string _bepInExUpdateText = "";
        public string BepInExUpdateText
        {
            get => _bepInExUpdateText;
            set
            {
                if (_bepInExUpdateText == value)
                    return;

                _bepInExUpdateText = value;
                OnPropertyChanged();
            }
        }
        
        
        
        UpdateWindow updateWindow;
        public UpdateWindowViewModel(UpdateWindow updateWindow)
        {
            this.updateWindow = updateWindow;
            
            _ = ScanForUpdates();
        }
        
        
        public async Task ScanForUpdates()
        {
            StatusText = "Scanning...";
            IsScanning = true;
            
            // Search GMM
            StatusText = "Checking Goblin Mod Maker for updates...";
            (bool GMMAvailable, string newGMMVersion) = await App.Settings.GMMUpdateAvailable();
            GMMVersionText = App.appVersion;
            GMMUpdateText = $"Update GMM to {newGMMVersion}";
            ShowGMMUpdateButton = GMMAvailable && !newGMMVersion.Equals("");

            // Search for bepinex
            StatusText = "Checking BepInEx for updates...";
            await VerifyBepInExStatus();
            
            // Search for GoblinManager
            StatusText = "Checking Goblin Manager for updates...";
            IsScanning = false;
        }

        private void CloseWindow()
        {
            updateWindow.Close();
        }

        private void InstallBepInEx()
        {
            App.Settings.InstallBepInEx();
        }

        private async Task VerifyBepInExStatus()
        {
            (bool available, string? currentVersion, string newBepInExVersion) = await App.Settings.BepInExUpdateAvailable();
            bool notInstalled = string.IsNullOrEmpty(currentVersion);
            BepInExVersionText =  notInstalled ? "BepInEx is not installed" : "v" + currentVersion;
            BepInExUpdateText = notInstalled ? "Install BepInEx" : $"Update BepInEx to {newBepInExVersion}";
            ShowBepInExUpdateButton = notInstalled || available;
        }

        public async Task UpdateBepInEx()
        {
            await App.Settings.InstallBepInEx(true);
            await VerifyBepInExStatus();
        }
    }
}
