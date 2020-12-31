using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MemcardRex.ViewModels;

namespace MemcardRex.Views
{
    public class CommentsWindow : FluentWindow
    {
        public CommentsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TopLevel_OnClosed(object? sender, EventArgs e)
        {
            //(this.DataContext as CommentsWindowViewModel).c();
        }
    }
}