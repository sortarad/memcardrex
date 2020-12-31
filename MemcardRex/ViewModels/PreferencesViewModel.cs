using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;

namespace MemcardRex.ViewModels
{
    public class PreferencesViewModel : ViewModelBase
    {
        public ProgramSettings Settings { get; set; }

        public bool CommunicationPortsEnabled { get; set; }
    
        public List<string> CommunicationPorts { get; set; }
        public List<string> Fonts { get; set; }

        public PreferencesViewModel(ProgramSettings programSettings)
        {
            Settings = programSettings;

            try
            {
                CommunicationPorts = new List<string>();
                foreach (string port in SerialPort.GetPortNames())
                {
                    CommunicationPorts.Add(port);
                }
                
                CommunicationPortsEnabled = true;
            }
            catch (Exception e)
            {
                CommunicationPortsEnabled = false;
            }

            Fonts = FontManager.Current.GetInstalledFontFamilyNames().ToList();
        }

        public ICommand Cancel => ReactiveCommand.Create<Window>((window) =>
        {
            window.Close(null);
        });
        
        public ICommand Ok => ReactiveCommand.Create<Window>((window) =>
        {
            window.Close(Settings);
        });
        
      
    }
}