using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

namespace MemcardRex.ViewModels
{
    public class AboutWindowViewModel : ViewModelBase
    {
        public string AppName { get; set; }
        public string AppVersion { get; set; }
        public string CompileDate { get; set; }
        public string Copyright { get; set; }
        public string AdditionalInfo { get; set; }

        public ICommand Close => ReactiveCommand.Create<Window>((window) =>
        {
            window.Close();
        });
    }
}