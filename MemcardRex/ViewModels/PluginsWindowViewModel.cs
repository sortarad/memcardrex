using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

namespace MemcardRex.ViewModels
{
    public class PluginsWindowViewModel : ViewModelBase
    {
        public ObservableCollection<pluginMetadata> Plugins { get; set; }

        public int PluginIndex { get; set; }
        
        public ICommand Ok => ReactiveCommand.Create<Window>((window) =>
        {
            window.Close();
        });
        
        public ICommand Config => ReactiveCommand.Create<Window>((window) =>
        {
            if (PluginIndex!=-1)
            {
                MainWindowViewModel.Current.pluginSystem.showAboutDialog(PluginIndex);
            }
        });
        
        
        public ICommand About => ReactiveCommand.Create<Window>((window) =>
        {
            if (PluginIndex != -1)
            {
                MainWindowViewModel.Current.pluginSystem.showAboutDialog(PluginIndex);
            }
        });
    }
}