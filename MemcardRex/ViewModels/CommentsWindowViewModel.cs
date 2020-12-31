using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace MemcardRex.ViewModels
{
    public class CommentsWindowViewModel : ViewModelViewHost
    {
        public string Comment { get; set; }

        public ICommand Cancel => ReactiveCommand.Create<Window>((window) =>
        {
            window.Close(null);
        });
        public ICommand Ok => ReactiveCommand.Create<Window>((window) =>
        {
            window.Close(Comment);
        });
    }
}