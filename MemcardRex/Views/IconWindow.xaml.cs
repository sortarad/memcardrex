using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MemcardRex.ViewModels;

namespace MemcardRex.Views
{
    public class IconWindow : FluentWindow
    {
        public IconWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InputElement_OnPointerLeave(object? sender, PointerEventArgs e)
        {
            (this.DataContext as IconWindowViewModel).PointerLeave();
        }

        private void PalettePonterDown(object? sender, PointerPressedEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                (this.DataContext as IconWindowViewModel).PaletteDoubleClick(sender as IVisual,e);
            }
            else
            {
                (this.DataContext as IconWindowViewModel).PalettePonterDown(sender as IVisual,e);
            }
        }

        private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            (this.DataContext as IconWindowViewModel).IconMouseMove(sender as IVisual,e);
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            (this.DataContext as IconWindowViewModel).IconMouseDown(sender as IVisual,e);
        }

        private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
        {
            (this.DataContext as IconWindowViewModel).IconKeyDown(e);
            e.Handled = true;
        }

        private void InputElement_OnKeyUp(object? sender, KeyEventArgs e)
        { 
            (this.DataContext as IconWindowViewModel).IconKeyUp(e);
            e.Handled = true;
        }
    }
}