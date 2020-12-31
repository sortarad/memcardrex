using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MemcardRex.ViewModels;

namespace MemcardRex.Views
{
    public class PluginsWindow : FluentWindow
    {
        public PluginsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}