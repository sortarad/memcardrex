using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MemcardRex.ViewModels;

namespace MemcardRex.Views
{
    public class CardReaderWindow : FluentWindow
    {
        public CardReaderWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TopLevel_OnClosed(object? sender, EventArgs e)
        {
            (this.DataContext as CardReaderWindowViewModel).Close.Execute(null);
        }
    }
}