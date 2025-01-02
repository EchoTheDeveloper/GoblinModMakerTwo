using System;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Threading;

namespace GMMLauncher.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const string DocumentationURL = "https://github.com/EchoTheDeveloper/Goblin-Mod-Maker/blob/main/DOCUMENTATION.md";

        public ICommand OpenDocumentationCommand { get; }
        public ICommand QuitAppCommand { get; }

        public MainWindowViewModel()
        {// im acc so confused rn
            OpenDocumentationCommand = new RelayCommand(OpenDocumentation);
            QuitAppCommand = new RelayCommand(QuitApp);
        }

        // The error is somewhere here: I think its due to the async file running on the background thread
        
        private void OpenDocumentation()
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DocumentationURL,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening documentation: {ex.Message}");
                }
            });
        }

        private void QuitApp()
        {
            Dispatcher.UIThread.Post(() => 
            {
                Environment.Exit(0);
            });
        }
    }
    
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
