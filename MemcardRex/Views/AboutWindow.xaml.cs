using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MemcardRex.ViewModels;

namespace MemcardRex.Views
{
    public class AboutWindow : FluentWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}