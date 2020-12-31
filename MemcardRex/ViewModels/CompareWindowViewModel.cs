using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

namespace MemcardRex.ViewModels
{

    public class SaveOffset
    {
        public string Offset { get; set; }
        public string Save1 { get; set; }
        public string Save2 { get; set; }
    }

    public class CompareWindowViewModel : ViewModelBase
    {
        public ObservableCollection<SaveOffset> Saves { get; set; }

        public string Save1Text { get; set; }
        public string Save2Text { get; set; }
        
        public ICommand Ok => ReactiveCommand.Create<Window>((window) =>
        {
            window.Close();
        });
    }
}