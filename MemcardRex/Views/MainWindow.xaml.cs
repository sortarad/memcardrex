using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MemcardRex.ViewModels;

namespace MemcardRex.Views
{
    public class MainWindow : FluentWindow
    {
        
        public NativeMenu PluginsMenu;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Window_OnClosing(object? sender, CancelEventArgs e)
        {
            (DataContext as MainWindowViewModel).ExitApplication();
        }

        private void InputElement_OnPointerLeave(object? sender, PointerEventArgs e)
        {
            (sender as TextBox).IsEnabled = false;
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            ((sender as ItemsPresenter).Panel.LogicalChildren[this.Get<TabControl>("MainTabControl").SelectedIndex]
                .LogicalChildren[0] as TextBox).IsEnabled = false;
        }

        private void PART_ItemsPresenter_OnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            ((sender as ItemsPresenter).Panel.LogicalChildren[this.Get<TabControl>("MainTabControl").SelectedIndex].LogicalChildren[0] as TextBox).IsEnabled = true;
        }

        private void AvaloniaObject_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            PluginsMenu = (sender as NativeMenu);
        }
    }
}