using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using ReactiveUI;

namespace MemcardRex.ViewModels
{
    public class HeaderWindowViewModel : ViewModelBase
    {
        //Name of the host application
        string appName = null;
        
        //Custom save region (If the save uses nonstandard region)
        private ushort customSaveRegion = 0;
        
        public ObservableCollection<string> Regions { get; set;}
        
        private int _RegionSelectedIndex { get; set; }

        public int RegionSelectedIndex
        {
            get { return _RegionSelectedIndex; }
            set
            {
                _RegionSelectedIndex = value;
                this.RaisePropertyChanged();
            }
        }

        public string ProdCodeText { get; set; }
        public string IdentifierText { get; set; }

        public string SaveTitle { get; set; }


        public HeaderWindowViewModel()
        {
            Regions = new ObservableCollection<string>()
            {
                "America",
                "Europe",
                "Japan"
            };
        }


        public void InitializeDialog(string applicationName, string saveTitle, string prodCode, string identifier, ushort region)
        {
            appName = applicationName;
            
            ProdCodeText = prodCode;
            IdentifierText = identifier;
            SaveTitle = saveTitle;
                
            //Check what region is selected
            switch (region)
            {
                default: //Region custom, show hex
                    customSaveRegion = region;
                    Regions.Add("0x" + region.ToString("X4"));
                    RegionSelectedIndex = 3;
                    break;

                case 0x4142: //America
                    RegionSelectedIndex = 0;
                    break;

                case 0x4542: //Europe
                    RegionSelectedIndex = 1;
                    break;

                case 0x4942: //Japan
                    RegionSelectedIndex = 2;
                    break;
            }
        }

        public ICommand Cancel => ReactiveCommand.Create<Window>((window) => { window.Close(null); });

        public ICommand Ok => ReactiveCommand.Create<Window>(async (window) =>
        {
            //Check if values are valid to be submitted
            if (ProdCodeText.Length < 10 && IdentifierText.Length != 0)
            {
                //String is not valid
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        Style = Style.DarkMode,
                        CanResize = true,
                        MaxWidth = 800,
                        ContentTitle = appName,
                        ContentMessage =  "Product code must be exactly 10 characters long.",
                        ButtonDefinitions = new [] {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(window);
        
            }
            else
            {
                //String is valid
                ushort  saveRegion = customSaveRegion;
                //Set the save region
                switch (RegionSelectedIndex)
                {
                    default: //Custom region
                        saveRegion = customSaveRegion;
                        break;

                    case 0: //America
                        saveRegion = 0x4142;
                        break;

                    case 1: //Europe
                        saveRegion = 0x4542;
                        break;

                    case 2: //Japan
                        saveRegion = 0x4942;
                        break;
                }

                window.Close(new { Region = saveRegion, ProdCode = ProdCodeText,Identifier= IdentifierText  });
            }

        });
    }
}