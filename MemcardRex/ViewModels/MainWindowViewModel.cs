using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using MemcardRex.Views;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using ReactiveUI;
using Point = Avalonia.Point;
using Style = MessageBox.Avalonia.Enums.Style;

namespace MemcardRex.ViewModels
{

    //Struct holding all program related settings (public because settings dialog has to access it)
    public class ProgramSettings
    {
        public int titleEncoding { get; set; } //Encoding of the save titles (0 - ASCII, 1 - UTF-16)
        public bool showListGrid { get; set; } //List grid settings
        public int iconInterpolationMode { get; set; } //Icon iterpolation mode settings
        public int iconPropertiesSize { get; set; } //Icon size settings in save properties
        public int iconBackgroundColor { get; set; } //Various colors based on PS1 BIOS backgrounds
        public bool backupMemcards { get; set; } //Backup Memory Card settings
        public bool warningMessage { get; set; } //Warning message settings
        public int formatType { get; set; } //Type of formatting for hardware interfaces
        public string listFont { get; set; } //List font
        public string communicationPort { get; set; } //Communication port for Hardware interfaces
    }

    public class MainWindowViewModel : ViewModelBase
    {
        public static MainWindowViewModel Current;


        //Application related strings
        const string appName = "MemcardRex";
        const string appDate = "Unknown";

#if DEBUG
        const string appVersion = "2.0 (Debug)";
#else
        const string appVersion = "2.0";
#endif
        public ICommand OpenAbout => ReactiveCommand.Create(() =>
        {
            var window = new AboutWindow();

            window.DataContext = new AboutWindowViewModel()
            {
                AppName = appName,
                AppVersion = "Version: " + appVersion,
                CompileDate = "Compile date: " + appDate,
                Copyright = "Copyright © Shendo 2014",
                AdditionalInfo =
                    "Beta testers: Gamesoul Master, Xtreme2damax,\nCarmax91.\n\nThanks to: @ruantec, Cobalt, TheCloudOfSmoke,\nRedawgTS, Hard core Rikki, RainMotorsports,\nZieg, Bobbi, OuTman, Kevstah2004, Kubusleonidas, \nFrédéric Brière, Cor'e, Gemini, DeadlySystem.\n\n" +
                    "Special thanks to the following people whose\nMemory Card utilities inspired me to write my own:\nSimon Mallion (PSXMemTool),\nLars Ole Dybdal (PSXGameEdit),\nAldo Vargas (Memory Card Manager),\nNeill Corlett (Dexter),\nPaul Phoneix (ConvertM).",
            };
            window.ShowDialog((App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
        });



        //Location of the application
        string appPath = AppDomain.CurrentDomain.BaseDirectory;

        public ICommand OpenReadme => ReactiveCommand.Create(() =>
        {
            if (File.Exists(appPath + "/Readme.txt")) System.Diagnostics.Process.Start(appPath + "Readme.txt");
            else
            {
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = appName,
                        ContentMessage = "'ReadMe.txt' was not found.",
                        ButtonDefinitions = new[]
                        {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                messageBoxCustomWindow.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
            }
        });


        //All program settings
        ProgramSettings mainSettings = new ProgramSettings();

        public ICommand OpenPreferences => ReactiveCommand.Create(async () =>
        {
            var window = new PreferencesWindow();
            window.DataContext = new PreferencesViewModel(new ProgramSettings()
            {
                titleEncoding = mainSettings.titleEncoding,
                showListGrid = mainSettings.showListGrid,
                iconInterpolationMode = mainSettings.iconInterpolationMode,
                iconPropertiesSize = mainSettings.iconPropertiesSize,
                iconBackgroundColor = mainSettings.iconBackgroundColor,
                backupMemcards = mainSettings.backupMemcards,
                warningMessage = mainSettings.warningMessage,
                formatType = mainSettings.formatType,
                listFont = mainSettings.listFont,
                communicationPort = mainSettings.communicationPort,
            });
            var settings =
                await window.ShowDialog<ProgramSettings>(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            if (settings != null)
            {
                mainSettings = settings;


                //Refresh all active lists
                for (int i = 0; i < PScard.Count; i++)
                    refreshListView(i, PScard[i].SelectedSaveIndex);

            }
        });

        //Supported plugins for the currently selected save
        int[] supportedPlugins = null;

        //Refresh the ListView
        private void refreshListView(int listIndex, int slotNumber)
        {
            //Temporary FontFamily
            FontFamily tempFontFamily = null;
            PScard[listIndex].Saves.Clear();
            PScard[listIndex].Saves = new ObservableCollection<Save>();

            try
            {
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                //Add 15 List items along with icons
                for (int i = 0; i < 15; i++)
                {
                    switch (PScard[listIndex].saveType[i])
                    {
                        default: //Corrupted
                            PScard[listIndex].Saves.Add(new Save {Title = "Corrupted slot"});
                            break;
                        case 0: //Formatted save
                            PScard[listIndex].Saves.Add(new Save {Title = "Free slot"});
                            break;
                        case 1: //Initial save
                        case 4: //Deleted initial save
                            PScard[listIndex].Saves.Add(new Save
                            {
                                Title = PScard[listIndex].saveName[i, mainSettings.titleEncoding],
                                ProductCode = PScard[listIndex].saveProdCode[i],
                                Identifier = PScard[listIndex].saveIdentifier[i],
                                Icon = prepareIcons(listIndex, i), //Skip two linked slot icons
                            });
                            break;

                        case 2: //Middle link
                            PScard[listIndex].Saves.Add(new Save
                            {
                                Title = "Linked slot (middle link)",
                                Icon = new  Avalonia.Media.Imaging.Bitmap(
                                    assets.Open(new Uri("avares://MemcardRex/Assets/Images/" + "linked" + ".bmp")))
                            });
                            break;

                        case 5: //Middle link deleted
                            PScard[listIndex].Saves.Add(new Save
                            {
                                Title = "Linked slot (middle link)",
                                Icon = new  Avalonia.Media.Imaging.Bitmap(
                                    assets.Open(new Uri("avares://MemcardRex/Assets/Images/"  + "linked_disabled.bmp")))
                            });
                            break;

                        case 3: //End link
                            PScard[listIndex].Saves.Add(new Save
                            {
                                Title = "Linked slot (end link)",
                                Icon = new  Avalonia.Media.Imaging.Bitmap(
                                    assets.Open(new Uri("avares://MemcardRex/Assets/Images/" + "linked" + ".bmp")))
                            });
                            break;

                        case 6: //End link deleted
                            PScard[listIndex].Saves.Add(new Save
                            {
                                Title = "Linked slot (end link)",
                                Icon = new Avalonia.Media.Imaging.Bitmap(
                                    assets.Open(new Uri("avares://MemcardRex/Assets/Images/" + "linked_disabled.bmp")))
                            });
                            break;

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;   
            }


            //Select the active item in the list
            PScard[listIndex].SelectedSaveIndex = slotNumber;
                
           

            //Set font for the list
            if (mainSettings.listFont != null)
            {
                //Create FontFamily from font name
                tempFontFamily = new FontFamily(mainSettings.listFont);

                //Check if regular style is supported
                if (tempFontFamily.IsStyleAvailable(FontStyle.Regular))
                {
                    //Use custom font
                    PScard[listIndex].SavesFont = new Font(mainSettings.listFont, 8.25f);
                }
                else
                {
                    //Use default font
                    mainSettings.listFont = FontFamily.GenericSansSerif.Name;
                    PScard[listIndex].SavesFont = new Font(mainSettings.listFont, 8.25f);
                }
            }

            //Set showListGrid option
             PScard[listIndex].GridLines = mainSettings.showListGrid;

            refreshPluginBindings();

            //Enable certain list items
            enableSelectiveEditItems();
           
        }


        private ObservableCollection<MenuItem>  _EditWithPluginToolStripMenuItems  = new ObservableCollection<MenuItem>();

        public ObservableCollection<MenuItem> EditWithPluginToolStripMenuItems
        {
            get => _EditWithPluginToolStripMenuItems;
            set
            {
                _EditWithPluginToolStripMenuItems = value;
                this.RaisePropertyChanged();
            }

        }

        //Refresh plugin menu
        private void refreshPluginBindings()
        {

            if(System.Runtime.InteropServices.RuntimeInformation
                .IsOSPlatform(OSPlatform.Windows))
            {
                EditWithPluginToolStripMenuItems.Clear(); 
            }
            else
            {
                //Clear the menus
                ((App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow as MainWindow).PluginsMenu.Items.Clear();
            }

            EditWithPluginToolStripMenuItemEnabled = false;
            EditWithPluginToolStripMenuItem1Enabled = false;
                
            //Check if there are any cards
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                if (listIndex==-1)
                {
                    listIndex = 0;
                    TabSelectedIndex = 0;
                }
                //Check if any item on the list is selected
                if (PScard[listIndex].SelectedSaveIndex > -1)
                {
                    int slotNumber = PScard[listIndex].SelectedSaveIndex;

                    //Check the save type
                    switch (PScard[listIndex].saveType[slotNumber])
                    {
                        default:
                            break;

                        case 1: //Initial save
                        case 4: //Deleted initial

                            //Get the supported plugins
                            supportedPlugins =
                                pluginSystem.getSupportedPlugins(PScard[listIndex].saveProdCode[slotNumber]);

                            //Check if there are any plugins that support the product code
                            if (supportedPlugins != null)
                            {
                                //Enable plugin menu
                                EditWithPluginToolStripMenuItemEnabled = true;
                                EditWithPluginToolStripMenuItem1Enabled = true;

                                foreach (int currentAssembly in supportedPlugins)
                                {
                                    if (System.Runtime.InteropServices.RuntimeInformation
                                        .IsOSPlatform(OSPlatform.Windows))
                                    {
                                        //Add item to the plugin menu
                                        EditWithPluginToolStripMenuItems.Add(new MenuItem(){    Header = pluginSystem
                                            .assembliesMetadata[currentAssembly].pluginName, Command = ReactiveCommand.Create(
                                            async () =>
                                            {
                                                await  editWithPlugin(currentAssembly);

                                            })});
                                    }
                                    else
                                    {
                                        //Add item to the plugin menu
                                        ((App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow as MainWindow).PluginsMenu.Items.Add(new NativeMenuItem(){    Header = pluginSystem
                                            .assembliesMetadata[currentAssembly].pluginName, Command = ReactiveCommand.Create(
                                            async () =>
                                            {
                                                await  editWithPlugin(currentAssembly);

                                            })});
                                    }

                                
                                }
                            }

                            break;
                    }
                }
            }
        }


        //Plugin system (public because plugin dialog has to access it)
        public rexPluginSystem pluginSystem = new rexPluginSystem();

        public ICommand OpenPlugins => ReactiveCommand.Create(async () =>
        {
            var window = new PluginsWindow();
            window.DataContext = new PluginsWindowViewModel()
            {
                Plugins = new ObservableCollection<pluginMetadata>(pluginSystem.assembliesMetadata)
            };

            window.ShowDialog(
                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

        });

        private bool _IsTopMenuVisible = true;

        public bool IsTopMenuVisible
        {
            get => _IsTopMenuVisible;
            set

            {
                _IsTopMenuVisible = value;
                this.RaisePropertyChanged();
            }
        }

        public ICommand OpenMenu => ReactiveCommand.Create<Button>(var =>
        {
            var.ContextMenu.Open(var);

        });

        public MainWindowViewModel()
        {
            Current = this;



            if (System.Runtime.InteropServices.RuntimeInformation
                .IsOSPlatform(OSPlatform.OSX))
                IsTopMenuVisible = false;

            //Show name of the application on the mainWindow
            //   Title = appName + " " + appVersion;

            //Set default settings
            mainSettings.titleEncoding = 0;
            mainSettings.listFont = FontFamily.GenericSansSerif.Name;
            mainSettings.showListGrid = false;
            mainSettings.iconInterpolationMode = 0;
            mainSettings.iconPropertiesSize = 1;
            mainSettings.backupMemcards = false;
            mainSettings.warningMessage = true;
            mainSettings.communicationPort = "COM1";
            mainSettings.formatType = 0;

            //Load settings from Settings.ini
            loadProgramSettings();

            //Load available plugins
            pluginSystem.fetchPlugins(appPath + "/Plugins");
            
            PScard = new ObservableCollection<ps1card>();
            openCardfunction(null);
        }


        //Currently clicked plugin (0 - clicked flag, 1 - plugin index)
        int[] clickedPlugin = new int[] {0, 0};


        //Open a Memory Card with OpenFileDialog
        private async Task openCardDialog()
        {
            OpenFileDialog openFileDlg = new OpenFileDialog();
            openFileDlg.Title = "Open Memory Card";
            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "All supported",
                Extensions = new List<string>()
                    {"mcr", "gme", "bin", "mcd", "vgs", "mem", "mc", "ddf", "ps", "psm", "mci", "VMP", "VM1",}
            });
            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "ePSXe/PSEmu Pro Memory Card (*.mcr)",
                Extensions = new List<string>() {"mcr"}
            });
            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "DexDrive Memory Card (*.gme)|*.gme",
                Extensions = new List<string>() {"gme"}
            });

            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "pSX/AdriPSX Memory Card (*.bin)|*.bin",
                Extensions = new List<string>() {"bin"}

            });

            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "Bleem! Memory Card (*.mcd)",
                Extensions = new List<string>() {"mcd"}

            });

            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "VGS Memory Card (*.mem, *.vgs)",
                Extensions = new List<string>() {"vgs", "mem"}

            });


            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "PSXGame Edit Memory Card (*.mc)",
                Extensions = new List<string>() {"mc"}

            });


            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "DataDeck Memory Card (*.ddf)",
                Extensions = new List<string>() {"ddf"}

            });


            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "WinPSM Memory Card (*.ps)",
                Extensions = new List<string>() {"ps"}

            });


            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "Smart Link Memory Card (*.psm)",
                Extensions = new List<string>() {"psm"}

            });


            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "MCExplorer (*.mci)",
                Extensions = new List<string>() {"mci"}

            });


            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "PSP virtual Memory Card (*.VMP)",
                Extensions = new List<string>() {"VMP"}

            });

            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "PS3 virtual Memory Card (*.VM1)",
                Extensions = new List<string>() {"VM1"}

            });

            openFileDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "All files (*.*)",
                Extensions = new List<string>() {"*"}
            });

            openFileDlg.AllowMultiple = true;

            var result =
                await openFileDlg.ShowAsync((App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                    .MainWindow);
            if (result != null || result.Length < 1)
            {
                foreach (string fileName in result)
                {
                    await openCardfunction(fileName);
                }
            }

        }



        //Open a Memory Card from the given filename
        private async Task openCardfunction(string fileName)
        {
            //Container for the error message
            string errorMsg = null;

            //Check if the card already exists
            foreach (ps1card checkCard in PScard)
            {
                if (checkCard.cardLocation == fileName && fileName != null)
                {
                    //Card is already opened, display message and exit
                    var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                        .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                        {
                            Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                            ContentTitle = appName,
                            ContentMessage = "'" + Path.GetFileName(fileName) + "' is already opened.",
                            ButtonDefinitions = new[]
                            {
                                new ButtonDefinition {Name = "OK"},
                            },
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        });
                    await messageBoxCustomWindow.ShowDialog(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                    return;
                }
            }

            //Create a new card
            PScard.Add(new ps1card());

            //Try to open card
            errorMsg = PScard[PScard.Count - 1].openMemoryCard(fileName);

            //If card is sucesfully opened proceed further, else destroy it
            if (errorMsg == null)
            {
                //Backup opened card
                backupMemcard(fileName);

                //Make a new tab for the opened card
                //createTabPage();
               
                makeListView(PScard.Count - 1);

                    
                    if (PScard[PScard.Count - 1].cardLocation != null) filterNullCard();

               
                    TabSelectedIndex = PScard.Count - 1;

                 
                    refreshStatusStrip();

                    //Enable "Close", "Close All", "Save" and "Save as" menu items
                    CloseToolStripMenuItemEnabled = true;
                    CloseAllToolStripMenuItemEnabled = true;
                    SaveToolStripMenuItemEnabled = true;
                    SaveButtonEnabled = true;
                    SaveAsToolStripMenuItemEnabled = true;
                    
                refreshListView(PScard.Count - 1,0);
            }
            else
            {
                //Remove the last card created
                PScard.RemoveAt(PScard.Count - 1);

                //Display error message
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = appName,
                        ContentMessage = errorMsg,
                        ButtonDefinitions = new[]
                        {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            }
        }

        //Backup a Memory Card
        private void backupMemcard(string fileName)
        {
            //Check if backuping of memcard is allowed and the filename is valid
            if (mainSettings.backupMemcards && fileName != null)
            {
                FileInfo fInfo = new FileInfo(fileName);

                //Backup only if file is less then 512KB
                if (fInfo.Length < 524288)
                {
                    //Copy the file
                    try
                    {
                        //Check if the backup directory exists and create it if it's missing
                        if (!Directory.Exists(appPath + "/Backup")) Directory.CreateDirectory(appPath + "/Backup");

                        //Copy the file (make a backup of it)
                        File.Copy(fileName, appPath + "/Backup/" + fInfo.Name);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }






        //Save a Memory Card with SaveFileDialog
        private async Task saveCardDialog(int listIndex)
        {
            //Check if there are any cards to save
            if (PScard.Count > 0)
            {
                byte memoryCardType = 0;
                SaveFileDialog saveFileDlg = new SaveFileDialog();
                saveFileDlg.Title = "Save Memory Card";


                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "ePSXe/PSEmu Pro Memory Card (*.mcr)",
                    Extensions = new List<string>() {"mcr"}
                });
                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "DexDrive Memory Card (*.gme)|*.gme",
                    Extensions = new List<string>() {"gme"}
                });

                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "pSX/AdriPSX Memory Card (*.bin)|*.bin",
                    Extensions = new List<string>() {"bin"}

                });

                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "Bleem! Memory Card (*.mcd)",
                    Extensions = new List<string>() {"mcd"}

                });

                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "VGS Memory Card (*.mem, *.vgs)",
                    Extensions = new List<string>() {"vgs", "mem"}

                });


                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "PSXGame Edit Memory Card (*.mc)",
                    Extensions = new List<string>() {"mc"}

                });


                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "DataDeck Memory Card (*.ddf)",
                    Extensions = new List<string>() {"ddf"}

                });


                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "WinPSM Memory Card (*.ps)",
                    Extensions = new List<string>() {"ps"}

                });


                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "Smart Link Memory Card (*.psm)",
                    Extensions = new List<string>() {"psm"}

                });


                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "MCExplorer (*.mci)",
                    Extensions = new List<string>() {"mci"}

                });


                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "PSP virtual Memory Card (*.VMP)",
                    Extensions = new List<string>() {"VMP"}

                });

                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "PS3 virtual Memory Card (*.VM1)",
                    Extensions = new List<string>() {"VM1"}

                });

                saveFileDlg.Filters.Add(new FileDialogFilter()
                {
                    Name = "All files (*.*)",
                    Extensions = new List<string>() {"*"}
                });

                var result =
                    await saveFileDlg.ShowAsync(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                //If user selected a card save to it
                if (!string.IsNullOrWhiteSpace(result))
                {

                    if (result.ToLower().EndsWith(".gme"))
                    {
                        //GME Memory Card
                        memoryCardType = 2;
                    }
                    else if (result.ToLower().EndsWith(".vgs") || result.ToLower().EndsWith(".mem"))
                    {
                        //VGS Memory Card
                        memoryCardType = 3;
                    }
                    else
                    {
                        //Raw Memory Card
                        memoryCardType = 1;
                    }

                    await saveMemoryCard(listIndex, result, memoryCardType);
                }
            }
        }

        //Save a Memory Card to a given filename
        private async Task saveMemoryCard(int listIndex, string fileName, byte memoryCardType)
        {
            if (PScard[listIndex].saveMemoryCard(fileName, memoryCardType))
            {
                refreshListView(listIndex, PScard[listIndex].SelectedSaveIndex);
                refreshStatusStrip();
            }
            else
            {
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = appName,
                        ContentMessage = "Memory Card could not be saved.",
                        ButtonDefinitions = new[]
                        {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            }
        }

        //Save a selected Memory Card
        private async Task saveCardFunction(int listIndex)
        {
            //Check if there are any cards to save
            if (PScard.Count > 0)
            {
                //Check if file can be saved or save dialog must be shown (VMP is read only)
                if (PScard[listIndex].cardLocation == null || PScard[listIndex].cardType == 4)
                   await  saveCardDialog(listIndex);
                else
                  await   saveMemoryCard(listIndex, PScard[listIndex].cardLocation, PScard[listIndex].cardType);
            }
        }


        //Prompt for save
        private async Task savePrompt(int listIndex)
        {
            //Check if the file has been changed
            if (PScard[listIndex].changedFlag)
            {
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = appName,
                        ContentMessage = "Do you want to save changes to '" + PScard[listIndex].cardName + "'?",
                        ButtonDefinitions = new[]
                        {
                            new ButtonDefinition {Name = "Yes"},
                            new ButtonDefinition {Name = "No"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                var result = await messageBoxCustomWindow.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                if (result == "Yes")
                    saveCardFunction(listIndex);
            }
        }

        //Edit header of the selected save
        private async Task editSaveHeader()
        {
            //Check if there are any cards to edit
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;
                ushort saveRegion = PScard[listIndex].saveRegion[slotNumber];
                string saveProdCode = PScard[listIndex].saveProdCode[slotNumber];
                string saveIdentifier = PScard[listIndex].saveIdentifier[slotNumber];
                string saveTitle = PScard[listIndex].saveName[slotNumber, mainSettings.titleEncoding];

                //Check if slot is allowed to be edited
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default: //Not allowed
                        break;

                    case 1:
                    case 4:
                        HeaderWindow headerDlg = new HeaderWindow();
                        var headerDlgVm = new HeaderWindowViewModel();
                        //Load values to dialog
                        headerDlgVm.InitializeDialog(appName, saveTitle, saveProdCode, saveIdentifier, saveRegion);
                        headerDlg.DataContext = headerDlgVm;

                        var data = await headerDlg.ShowDialog<dynamic>(
                            ((App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow));

                        //Update values if OK was pressed
                        if (data != null)
                        {
                            //Insert data to save header of the selected card and slot
                            PScard[listIndex].setHeaderData(slotNumber, data.ProdCode, data.Identifier, data.Region);
                            refreshListView(listIndex, slotNumber);
                        }

                        break;
                }
            }
        }

        public ICommand OpenCard => ReactiveCommand.Create(async () =>
        {
            //Show browse dialog and open a selected Memory Card
            await openCardDialog();
        });
        public ICommand SaveCard => ReactiveCommand.Create(async () =>
        {
            //Save a Memory Card as...
            await saveCardDialog(TabSelectedIndex);
        });

  public ICommand SaveCardAs => ReactiveCommand.Create(async () =>
  {
      //Save a Memory Card as...
     await  saveCardFunction(TabSelectedIndex);
  });


        //Load program settings
        private void loadProgramSettings()
        {
            Point mainWindowLocation = new Point(0, 0);
            xmlSettingsEditor xmlAppSettings = new xmlSettingsEditor();

            //Check if the Settings.xml exists
            if (File.Exists(appPath + "/Settings.xml"))
            {
                //Open XML file for reading, file is auto-closed
                xmlAppSettings.openXmlReader(appPath + "/Settings.xml");

                //Load list font
                mainSettings.listFont = xmlAppSettings.readXmlEntry("ListFont");

                //Load DexDrive COM port
                mainSettings.communicationPort = xmlAppSettings.readXmlEntry("ComPort");

                //Load Title Encoding
                mainSettings.titleEncoding = xmlAppSettings.readXmlEntryInt("TitleEncoding", 0, 1);

                //Load List Grid settings
                mainSettings.showListGrid = xmlAppSettings.readXmlEntryInt("ShowGrid", 0, 1) == 1;


                //Load icon interpolation settings
                mainSettings.iconInterpolationMode = xmlAppSettings.readXmlEntryInt("IconInterpolationMode", 0, 1);

                //Load icon size settings
                mainSettings.iconPropertiesSize = xmlAppSettings.readXmlEntryInt("IconSize", 0, 1);

                //Load icon background color
                mainSettings.iconBackgroundColor = xmlAppSettings.readXmlEntryInt("IconBackgroundColor", 0, 4);

                //Load backup Memory Cards value
                mainSettings.backupMemcards = xmlAppSettings.readXmlEntryInt("BackupMemoryCards", 0, 1) == 1;

                //Load warning message switch
                mainSettings.warningMessage = xmlAppSettings.readXmlEntryInt("WarningMessage", 0, 1) == 1;


                //Load format type
                mainSettings.formatType = xmlAppSettings.readXmlEntryInt("HardwareFormatType", 0, 1);


                //Apply loaded settings
                applySettings();
            }
        } //Apply program settings

        private void applySettings()
        {
            //Refresh all active lists
            for (int i = 0; i < PScard.Count; i++)
                refreshListView(i, PScard[i].SelectedSaveIndex);

        }


        //Save program settings
        private void saveProgramSettings()
        {
            xmlSettingsEditor xmlAppSettings = new xmlSettingsEditor();

            //Open XML file for writing
            xmlAppSettings.openXmlWriter(appPath + "/Settings.xml", appName + " " + appVersion + " settings data");

            //Set list font
            xmlAppSettings.writeXmlEntry("ListFont", mainSettings.listFont);

            //Set DexDrive port
            xmlAppSettings.writeXmlEntry("ComPort", mainSettings.communicationPort);

            //Set title encoding
            xmlAppSettings.writeXmlEntry("TitleEncoding", mainSettings.titleEncoding.ToString());

            //Set List Grid settings
            xmlAppSettings.writeXmlEntry("ShowGrid", mainSettings.showListGrid.ToString());


            //Set icon interpolation settings
            xmlAppSettings.writeXmlEntry("IconInterpolationMode", mainSettings.iconInterpolationMode.ToString());

            //Set icon size options
            xmlAppSettings.writeXmlEntry("IconSize", mainSettings.iconPropertiesSize.ToString());

            //Set icon background color
            xmlAppSettings.writeXmlEntry("IconBackgroundColor", mainSettings.iconBackgroundColor.ToString());

            //Set backup Memory Cards value
            xmlAppSettings.writeXmlEntry("BackupMemoryCards", mainSettings.backupMemcards.ToString());

            //Set warning message switch
            xmlAppSettings.writeXmlEntry("WarningMessage", mainSettings.warningMessage.ToString());


            //Set format type
            xmlAppSettings.writeXmlEntry("HardwareFormatType", mainSettings.formatType.ToString());


            //Cleanly close opened XML file
            xmlAppSettings.closeXmlWriter();
        }




        //Edit save comments
        private async Task editSaveComments()
        {
            //Check if there are any cards to edit comments on
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;
                string saveTitle = PScard[listIndex].saveName[slotNumber, mainSettings.titleEncoding];
                string saveComment = PScard[listIndex].saveComments[slotNumber];

                //Check if comments are allowed to be edited
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default: //Not allowed
                        break;

                    case 1:
                    case 4:
                        var window = new CommentsWindow();
                        window.DataContext = new CommentsWindowViewModel()
                        {
                            Comment = saveComment,
                        };

                        var result = await window.ShowDialog<string>(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        //Update values if OK was pressed
                        if (result != null)
                        {
                            //Insert edited comments back in the card
                            PScard[listIndex].saveComments[slotNumber] = result;
                        }

                        break;
                }
            }
        }

//Create and show information dialog
        private async Task showInformation()
        {
            //Check if there are any cards
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;
                ushort saveRegion = PScard[listIndex].saveRegion[slotNumber];
                int saveSize = PScard[listIndex].saveSize[slotNumber];
                int iconFrames = PScard[listIndex].iconFrames[slotNumber];
                string saveProdCode = PScard[listIndex].saveProdCode[slotNumber];
                string saveIdentifier = PScard[listIndex].saveIdentifier[slotNumber];
                string saveTitle = PScard[listIndex].saveName[slotNumber, mainSettings.titleEncoding];
                Bitmap[] saveIcons = new Bitmap[3];

                //Get all 3 bitmaps for selected save
                for (int i = 0; i < 3; i++)
                    saveIcons[i] = PScard[listIndex].iconData[slotNumber, i];

                //Check if slot is "legal"
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default: //Not allowed
                        break;

                    case 1:
                    case 4:
                        InformationWindow window = new InformationWindow();
                        var vm = new InformationWindowViewModel();
                        //Load values to dialog
                        vm.InitializeDialog(saveTitle, saveProdCode, saveIdentifier,
                            saveRegion, saveSize, iconFrames, mainSettings.iconInterpolationMode,
                            mainSettings.iconPropertiesSize, saveIcons, PScard[listIndex].findSaveLinks(slotNumber),
                            mainSettings.iconBackgroundColor);
                        window.DataContext = vm;
                        await window.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        break;
                }
            }
        }

        //Restore selected save
        private async Task restoreSaveFunction()
        {
            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;

                //Check the save type
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default:
                        break;

                    case 4: //Deleted initial
                        PScard[listIndex].toggleDeleteSave(slotNumber);
                        refreshListView(listIndex, slotNumber);
                        break;

                    case 1: //Initial save

                        var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage = "The selected save is not deleted.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBoxCustomWindow.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        break;

                    case 2:
                    case 3:
                    case 5:
                    case 6:
                        var messageBox = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage =
                                    "The selected slot is linked. Select the initial save slot to proceed.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBox.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        break;
                }
            }
        }











        //Delete selected save
        private async Task deleteSaveFunction()
        {
            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;

                //Check the save type
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default:
                        break;

                    case 1: //Initial save
                        PScard[listIndex].toggleDeleteSave(slotNumber);
                        refreshListView(listIndex, slotNumber);
                        break;

                    case 4: //Deleted initial
                        var messageBox = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage = "The selected save is already deleted.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBox.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        break;

                    case 2:
                    case 3:
                    case 5:
                    case 6:

                        var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage =
                                    "The selected slot is linked. Select the initial save slot to proceed.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBoxCustom.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        break;
                }
            }
        }

        //Format selected save
        private async Task formatSave()
        {
            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;

                //Check the save type
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default: //Slot is either initial, deleted initial or corrupted so it can be safetly formatted
                        var msg = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage =
                                    "Formatted slots cannot be restored.\nDo you want to proceed with this operation?",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "Yes"},
                                    new ButtonDefinition {Name = "No"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        var result = await msg.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                        if (result == "Yes")
                        {
                            PScard[listIndex].formatSave(slotNumber);
                            refreshListView(listIndex, slotNumber);
                        }

                        break;

                    case 2:
                    case 3:
                    case 5:
                    case 6:

                        var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage =
                                    "The selected slot is linked. Select the initial save slot to proceed.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBoxCustom.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);


                        break;
                }
            }
        }

        //Copy save selected save from Memory Card
        private async Task copySaveFunction()
        {
            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;
                string saveName = PScard[listIndex].saveName[slotNumber, 0];

                //Check the save type
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default:
                        break;

                    case 1: //Initial save
                    case 4: //Deleted initial
                        tempBuffer = PScard[listIndex].getSaveBytes(slotNumber);
                        tempBufferName = PScard[listIndex].saveName[slotNumber, 0];

                        //Show temp buffer toolbar info
                        TBufToolButtonEnabled = true;
                        TBufToolButtonImage = PScard[listIndex].iconData[slotNumber, 0];
                        TBufToolButtonText = saveName;

                        //Refresh the current list
                        refreshListView(listIndex, slotNumber);

                        break;

                    case 2:
                    case 3:
                    case 5:
                    case 6:

                        var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage =
                                    "The selected slot is linked. Select the initial save slot to proceed.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBoxCustom.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        break;
                }
            }
        }


        //Export a save
        private async Task exportSaveDialog()
        {
            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;

                //Check the save type
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default:
                        break;

                    case 1: //Initial save
                        byte singleSaveType = 0;

                        //Set output filename
                        string outputFilename = getRegionString(PScard[listIndex].saveRegion[slotNumber]) +
                                                PScard[listIndex].saveProdCode[slotNumber] +
                                                PScard[listIndex].saveIdentifier[slotNumber];

                        //Filter illegal characters from the name
                        foreach (char illegalChar in Path.GetInvalidPathChars())
                        {
                            outputFilename = outputFilename.Replace(illegalChar.ToString(), "");
                        }

                        SaveFileDialog saveFileDlg = new SaveFileDialog();
                        saveFileDlg.Title = "Export save";
                        saveFileDlg.InitialFileName = outputFilename;

                        saveFileDlg.Filters.Add(new FileDialogFilter()
                        {
                            Name = "All supported",
                            Extensions = new List<string>()
                                {"mcs", "psx", "ps1", "mcb", "mcx", "pda", "B???????????*", "psv",}
                        });
                        saveFileDlg.Filters.Add(new FileDialogFilter()
                        {
                            Name = "PSXGameEdit single save (*.mcs)",
                            Extensions = new List<string>() {"mcs"}
                        });
                        saveFileDlg.Filters.Add(new FileDialogFilter()
                        {
                            Name = "XP, AR, GS, Caetla single save (*.psx)",
                            Extensions = new List<string>() {"psx"}
                        });

                        saveFileDlg.Filters.Add(new FileDialogFilter()
                        {
                            Name = "Memory Juggler (*.ps1)",
                            Extensions = new List<string>() {"ps1"}

                        });

                        saveFileDlg.Filters.Add(new FileDialogFilter()
                        {
                            Name = "Smart Link (*.mcb)",
                            Extensions = new List<string>() {"mcb"}

                        });

                        saveFileDlg.Filters.Add(new FileDialogFilter()
                        {
                            Name = "Datel (*.mcx;*.pda)",
                            Extensions = new List<string>() {"mcx", "pda"}

                        });


                        saveFileDlg.Filters.Add(new FileDialogFilter()
                        {
                            Name = "RAW single save",
                            Extensions = new List<string>() {"B???????????*"}

                        });


                        saveFileDlg.Filters.Add(new FileDialogFilter()
                        {
                            Name = "PS3 virtual save (*.psv)",
                            Extensions = new List<string>() {"psv"}

                        });

                        var result = await saveFileDlg.ShowAsync(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                        //If user selected a card save to it
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            if (result.ToLower().EndsWith(".mcs") || result.ToLower().EndsWith(".ps1"))
                            {
                                //MCS single save
                                //PS1 (Memory Juggler)
                                singleSaveType = 2;
                            }
                            else if (result.ToLower().Contains(".B"))
                            {
                                //RAW single save
                                singleSaveType = 3;

                                //Omit the extension if the user left it
                                result = result.Split('.')[0];
                            }
                            else
                            {
                                //Action Replay
                                singleSaveType = 1;
                            }

                            PScard[listIndex].saveSingleSave(result, slotNumber, singleSaveType);
                        }

                        break;
                    case 4: //Deleted initial

                        var msg = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage = "Deleted saves cannot be exported. Restore a save to proceed.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await msg.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);


                        break;

                    case 2:
                    case 3:
                    case 5:
                    case 6:

                        var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage =
                                    "The selected slot is linked. Select the initial save slot to proceed.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBoxCustom.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        break;
                }
            }
        }

        //Import a save
        private async Task importSaveDialog()
        {
            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;
                int requiredSlots = 0;

                //Check if the slot to import the save on is free
                if (PScard[listIndex].saveType[slotNumber] == 0)
                {
                    OpenFileDialog openFileDlg = new OpenFileDialog();
                    openFileDlg.Title = "Import save";

                    openFileDlg.Filters.Add(new FileDialogFilter()
                    {
                        Name = "All supported",
                        Extensions = new List<string>()
                            {"mcs", "psx", "ps1", "mcb", "mcx", "pda", "B???????????*", "psv",}
                    });
                    openFileDlg.Filters.Add(new FileDialogFilter()
                    {
                        Name = "PSXGameEdit single save (*.mcs)",
                        Extensions = new List<string>() {"mcs"}
                    });
                    openFileDlg.Filters.Add(new FileDialogFilter()
                    {
                        Name = "XP, AR, GS, Caetla single save (*.psx)",
                        Extensions = new List<string>() {"psx"}
                    });

                    openFileDlg.Filters.Add(new FileDialogFilter()
                    {
                        Name = "Memory Juggler (*.ps1)",
                        Extensions = new List<string>() {"ps1"}

                    });

                    openFileDlg.Filters.Add(new FileDialogFilter()
                    {
                        Name = "Smart Link (*.mcb)",
                        Extensions = new List<string>() {"mcb"}

                    });

                    openFileDlg.Filters.Add(new FileDialogFilter()
                    {
                        Name = "Datel (*.mcx;*.pda)",
                        Extensions = new List<string>() {"mcx", "pda"}

                    });


                    openFileDlg.Filters.Add(new FileDialogFilter()
                    {
                        Name = "RAW single save",
                        Extensions = new List<string>() {"B???????????*"}

                    });


                    openFileDlg.Filters.Add(new FileDialogFilter()
                    {
                        Name = "PS3 virtual save (*.psv)",
                        Extensions = new List<string>() {"psv"}

                    });



                    openFileDlg.AllowMultiple = false;

                    var result = await openFileDlg.ShowAsync(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                    //If user selected a card save to it
                    if (result != null && result.Length > 0)
                    {
                        if (PScard[listIndex].openSingleSave(result[0], slotNumber, out requiredSlots))
                        {
                            refreshListView(listIndex, slotNumber);
                        }
                        else if (requiredSlots > 0)
                        {
                            var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                                .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                                {
                                    Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                    ContentTitle = appName,
                                    ContentMessage = "To complete this operation " + requiredSlots.ToString() +
                                                     " free slots are required.",
                                    ButtonDefinitions = new[]
                                    {
                                        new ButtonDefinition {Name = "OK"},
                                    },
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                });
                            await messageBoxCustom.ShowDialog(
                                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                                .MainWindow);
                        }
                        else
                        {


                            var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                                .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                                {
                                    Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                    ContentTitle = appName,
                                    ContentMessage = "File could not be opened.",
                                    ButtonDefinitions = new[]
                                    {
                                        new ButtonDefinition {Name = "OK"},
                                    },
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                });
                            await messageBoxCustom.ShowDialog(
                                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                                .MainWindow);
                        }
                    }
                }
                else
                {
                    var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                        .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                        {
                            Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                            ContentTitle = appName,
                            ContentMessage = "The selected slot is not empty.",
                            ButtonDefinitions = new[]
                            {
                                new ButtonDefinition {Name = "OK"},
                            },
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        });
                    await messageBoxCustom.ShowDialog(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                }
            }
        }

        //Get region string from region data
        private string getRegionString(ushort regionUshort)
        {
            byte[] tempRegion = new byte[3];

            //Convert region to byte array
            tempRegion[0] = (byte) (regionUshort & 0xFF);
            tempRegion[1] = (byte) (regionUshort >> 8);

            //Get UTF-16 string
            return Encoding.Default.GetString(tempRegion);
        }




        //Open edit icon dialog
        private async Task editIconFunction()
        {
            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;
                int iconFrames = PScard[listIndex].iconFrames[slotNumber];
                string saveTitle = PScard[listIndex].saveName[slotNumber, mainSettings.titleEncoding];
                byte[] iconBytes = PScard[listIndex].getIconBytes(slotNumber);

                //Check the save type
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default:
                        break;

                    case 1: //Initial save
                    case 4: //Deleted initial
                        IconWindow iconDlg = new IconWindow();
                        IconWindowViewModel iconDlgVm = new IconWindowViewModel();
                        iconDlgVm.InitializeDialog(saveTitle, iconFrames, iconBytes);
                        iconDlg.DataContext = iconDlgVm;
                        var result = await iconDlg.ShowDialog<byte[]>(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        //Update data if OK has been pressed
                        if (result != null)
                        {
                            PScard[listIndex].setIconBytes(slotNumber, result);
                            refreshListView(listIndex, slotNumber);
                        }

                        break;

                    case 2:
                    case 3:
                    case 5:
                    case 6:

                        var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage =
                                    "The selected slot is linked. Select the initial save slot to proceed.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBoxCustom.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                        break;
                }
            }
        }







        //Save work and close the application
        
        



        private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Show the location of the active card in the tool strip
            refreshStatusStrip();

            //Load available plugins for the selected save
            refreshPluginBindings();

            //Enable certain list items
            enableSelectiveEditItems();
        }


      
        
        public async Task ExitApplication()
        {
            //Close every opened card
            await closeAllCards();

            //Save settings
            saveProgramSettings();
        }


        public ICommand Exit => ReactiveCommand.Create(async () =>
        {
            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow.Close();
        });


      


        
        public ICommand CloseAll => ReactiveCommand.Create(async () =>
        {
            await closeAllCards();
        });

        public ICommand CloseCard => ReactiveCommand.Create(async () =>
        {
            await closeCardFunction(TabSelectedIndex);
        });

        public ICommand RestoreSave => ReactiveCommand.Create(async () =>
        {
            await restoreSaveFunction();
        });
        

        private void saveButton_Click(object sender, EventArgs e)
        {
            //Save a Memory Card
            
        }



        private void cardList_DoubleClick(object sender, EventArgs e)
        {
            //Show information about the selected save
            showInformation();
        }

        private void cardList_IndexChanged(object sender, EventArgs e)
        {
            //Load appropriate plugins for the selected save
            refreshPluginBindings();

            //Enable certain list items
            enableSelectiveEditItems();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Save Memory Card
            saveCardFunction(TabSelectedIndex);
        }






     
        
       
        public ICommand DeleteSave => ReactiveCommand.Create(async () =>
        {
            await  deleteSaveFunction();
        }); 
     
       
        public ICommand RemoveSave => ReactiveCommand.Create(async () =>
        {
            await  formatSave();
        }); 

        

        public ICommand CopySave => ReactiveCommand.Create(async () =>
        {
            await copySaveFunction();
        }); 
        

        private async void saveInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Show information about a selected save
            await showInformation();
        }
/*
        private void mainTabControl_MouseDown(object sender)
        {
            //Check if the middle mouse button is pressed
            if (e.Button == MouseButtons.Middle)
            {
                Rectangle tabRectangle;

                //Cycle through all available tabs
                for (int i = 0; i < mainTabControl.TabCount; i++)
                {
                    tabRectangle = mainTabControl.GetTabRect(i);

                    //Close the middle clicked tab
                    if (tabRectangle.Contains(e.X, e.Y)) closeCard(i, false);
                }
            }
        }

        private void mainTabControl_DragDrop(object sender, DragEventArgs e)
        {
            string[] droppedFiles = (string[]) e.Data.GetData(DataFormats.FileDrop);

            //Cycle through every dropped file
            foreach (string fileName in droppedFiles)
            {
                openCardfunction(fileName);
            }
        }
*/


        /*
        private void mainTabControl_DragEnter(object sender, DragEventArgs e)
        {
            //Check if dragged data are files
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
        }
*/
        public ICommand CompareWithTemp => ReactiveCommand.Create(async () => { await compareSaveWithTemp(); });


        

        
        
  
        
        private bool _EditWithPluginToolStripMenuItemEnabled ;

        public bool EditWithPluginToolStripMenuItemEnabled 
        {
            get { return _EditWithPluginToolStripMenuItemEnabled; }
            set
            {
                _EditWithPluginToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }
        
        private  bool _EditWithPluginToolStripMenuItem1Enabled;

        public bool EditWithPluginToolStripMenuItem1Enabled
        {
            get { return _EditWithPluginToolStripMenuItem1Enabled; }
            set
            {
                _EditWithPluginToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }
        
        private ObservableCollection<ps1card> _PScard = new ObservableCollection<ps1card>();

        public ObservableCollection<ps1card> PScard
        {
            get { return _PScard; }
            set
            {
                _PScard = value;
                this.RaisePropertyChanged();
            }
        } 
        private int _TabSelectedIndex = -1;

        public int TabSelectedIndex
        {
            get { return _TabSelectedIndex; }
            set
            {
                _TabSelectedIndex = value;
                this.RaisePropertyChanged();
            }
        }
        
       
        
        
        private void exportSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Export a save
            exportSaveDialog();
        }

        private void exportSaveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Export a save
            exportSaveDialog();
        }
        public ICommand ExportSave => ReactiveCommand.Create(async () =>
        {
            await exportSaveDialog();
        });

        
        private void importSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Import a save
            importSaveDialog();
        }

        private void importSaveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Import a save
            importSaveDialog();
        }

        public ICommand ImportSave => ReactiveCommand.Create(async () =>
        {
            await importSaveDialog();
        });
        
        

        private void editIconToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Edit icon of the selected save
            editIconFunction();
        }
        public ICommand EditIcon => ReactiveCommand.Create(async () =>
        {
            await editIconFunction();
        });
        
        
        
        
        private void editSaveCommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Edit save comment of the selected slot
            editSaveComments();
        }

        private void editSaveCommentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Edit save comment of the selected slot
            editSaveComments();
        }

        public ICommand EditComments=> ReactiveCommand.Create(async () =>
        {
            await editSaveComments();
        });
        
        
        private void editSaveHeaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Edit header of the selected save
             editSaveHeader();
        }

        private void editSaveHeaderToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Edit header of the selected save
            editSaveHeader();
        }
        public ICommand EditHeader => ReactiveCommand.Create(async () =>
        {
            await editSaveHeader();
        });

        
        
        public ICommand New => ReactiveCommand.Create(async () =>
        {
            await openCardfunction(null);
        });

        
        public ICommand PasteSave => ReactiveCommand.Create(async () =>
        {
            await pasteSaveFunction();
        });
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public ICommand DexRead => ReactiveCommand.Create(async () =>
        {

            //Read a Memory Card from DexDrive
            byte[] tempByteArray = await CardReaderWindowViewModel.readMemoryCardDexDrive(
                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow, appName,
                mainSettings.communicationPort);

            await cardReaderRead(tempByteArray);

        });

        public ICommand MemCARDuinoRead => ReactiveCommand.Create(async () =>
        {
            //Read a Memory Card from MemCARDuino
            byte[] tempByteArray = await CardReaderWindowViewModel.readMemoryCardCARDuino(
                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow, appName,
                mainSettings.communicationPort);

          await  cardReaderRead(tempByteArray);
        });

        public ICommand PS1CardLinkRead => ReactiveCommand.Create(async () =>
        {

            //Read a Memory Card from PS1CardLink
            byte[] tempByteArray = await CardReaderWindowViewModel.readMemoryCardPS1CLnk(
                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow, appName,
                mainSettings.communicationPort);

          await  cardReaderRead(tempByteArray);

        });

        public ICommand DexFormat => ReactiveCommand.Create(async () => { await formatHardwareCard(0); });
        public ICommand MemCARDuinoFormat => ReactiveCommand.Create(async () => { await formatHardwareCard(1); });

        public ICommand PS1CardLinkFormat => ReactiveCommand.Create(async () => { await formatHardwareCard(2); });

        public ICommand DexWrite => ReactiveCommand.Create(async () =>
        {
            //Write a Memory Card to DexDrive
            int listIndex = TabSelectedIndex;

            //Check if there are any cards to write
            if (PScard.Count > 0)
            {
                //Open a DexDrive communication window
                await CardReaderWindowViewModel.writeMemoryCardDexDrive(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow, appName,
                    mainSettings.communicationPort, PScard[listIndex].saveMemoryCardStream(), 1024);
            }


        });

        public ICommand MemCARDuinoWrite => ReactiveCommand.Create(async () =>
        {

            //Write a Memory Card to MemCARDuino
            int listIndex = TabSelectedIndex;

            //Check if there are any cards to write
            if (PScard.Count > 0)
            {
                //Open a DexDrive communication window
                await CardReaderWindowViewModel.writeMemoryCardCARDuino(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow, appName,
                    mainSettings.communicationPort, PScard[listIndex].saveMemoryCardStream(), 1024);
            }

        });

        public ICommand PS1CardLinkWrite => ReactiveCommand.Create(async () =>
        {

            int listIndex = TabSelectedIndex;

            //Check if there are any cards to write
            if (PScard.Count > 0)
            {
                //Open a DexDrive communication window
                await CardReaderWindowViewModel.writeMemoryCardPS1CLnk(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow, appName,
                    mainSettings.communicationPort, PScard[listIndex].saveMemoryCardStream(), 1024);
            }

        });


        //Format a Memory Card on the hardware interface (0 - DexDrive, 1 - MemCARDuino, 2 - PS1CardLink)
        private async Task formatHardwareCard(int hardDevice)
        {
            //Show warning message

            var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                {
                    Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                    ContentTitle = appName,
                    ContentMessage =
                        "Formatting will delete all saves on the Memory Card.\nDo you want to proceed with this operation?",
                    ButtonDefinitions = new[]
                    {
                        new ButtonDefinition {Name = "Yes"},
                        new ButtonDefinition {Name = "No"},
                    },
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });
            var result =
                await messageBoxCustomWindow.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            if (result == "No")
            {
                return;
            }

            int frameNumber = 1024;
            ps1card blankCard = new ps1card();

            //Check if quick format option is turned on
            if (mainSettings.formatType == 0) frameNumber = 64;

            //Create a new card by giving a null path
            blankCard.openMemoryCard(null);

            //Check what device to use
            switch (hardDevice)
            {
                case 0: //DexDrive
                    await CardReaderWindowViewModel.writeMemoryCardDexDrive(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow,
                        appName, mainSettings.communicationPort, blankCard.saveMemoryCardStream(), frameNumber);
                    break;

                case 1: //MemCARDuino
                    await CardReaderWindowViewModel.writeMemoryCardCARDuino(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow,
                        appName, mainSettings.communicationPort, blankCard.saveMemoryCardStream(), frameNumber);
                    break;

                case 2: //PS1CardLink
                    await CardReaderWindowViewModel.writeMemoryCardPS1CLnk(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow,
                        appName, mainSettings.communicationPort, blankCard.saveMemoryCardStream(), frameNumber);
                    break;
            }
        }

        //Read a Memory Card from the physical device
        private async Task cardReaderRead(byte[] readData)
        {
            //Check if the Memory Card was sucessfully read
            if (readData != null)
            {
                //Create a new card
                PScard.Add(new ps1card());

                //Fill the card with the new data
                PScard[PScard.Count - 1].openMemoryCardStream(readData);

                //Temporary set a bogus file location (to fool filterNullCard function)
                PScard[PScard.Count - 1].cardLocation = "\0";


                //Make a new ListView control
                makeListView(PScard.Count - 1);

                //Delete the initial "Untitled" card
                if (PScard[PScard.Count - 1].cardLocation != null) await filterNullCard();
                //Switch the active tab to the currently opened card
                TabSelectedIndex = PScard.Count - 1;

                //Show the location of the card in the tool strip
                refreshStatusStrip();

                //Enable "Close", "Close All", "Save" and "Save as" menu items
                CloseToolStripMenuItemEnabled = true;
                CloseAllToolStripMenuItemEnabled = true;
                SaveToolStripMenuItemEnabled = true;
                SaveButtonEnabled = true;
                SaveAsToolStripMenuItemEnabled = true;

                //Restore null location since DexDrive Memory Card is not a file present on the Hard Disk
                PScard[PScard.Count - 1].cardLocation = null;
            }
        }


        //Refresh the toolstrip
        private void refreshStatusStrip()
        {
            //Show the location of the active card in the tool strip (if there are any cards)
            if (PScard.Count > 0)
                TooltipText = PScard[TabSelectedIndex].cardLocation;
            else
                TooltipText = null;
        }

        private string _TooltipText;

        public string TooltipText
        {
            get { return _TooltipText; }
            set
            {
                _TooltipText = value;
                this.RaisePropertyChanged();
            }
        }


        //Remove the first "Untitled" card if the user opened a valid card
        private async Task filterNullCard()
        {
            //Check if there are any cards opened
            if (PScard.Count > 0)
            {
                if (PScard.Count == 2 && PScard[0].cardLocation == null && PScard[0].changedFlag == false)
                {
                   await closeCardFunction(0);
                }
            }
        }

        //Cleanly close the selected card
        private async Task closeCardFunction(int listIndex, bool switchToFirst)
        {
            //Check if there are any cards to delete
            if (PScard.Count > 0)
            {
                //Ask for saving before closing
                await savePrompt(listIndex);

                PScard.RemoveAt(listIndex);
                //Select first tab
                if (PScard.Count > 0 && switchToFirst)
                    TabSelectedIndex = 0;

                //Refresh plugin list
                refreshPluginBindings();

                //Enable certain list items
                enableSelectiveEditItems();
            }

            //If this was the last card disable "Close", "Close All", "Save" and "Save as" menu items
            if (PScard.Count <= 0)
            {
                CloseToolStripMenuItemEnabled = false;
                CloseAllToolStripMenuItemEnabled = false;
                SaveToolStripMenuItemEnabled = false;
                SaveButtonEnabled = false;
                SaveAsToolStripMenuItemEnabled = false;
            }
        }

        //Overload for closeCard function
        private async Task closeCardFunction(int listIndex)
        {
            await closeCardFunction(listIndex, true);
        }

        //Close all opened cards
        private async Task closeAllCards()
        {
            //Run trough the loop as long as there are cards opened
            while (PScard.Count > 0)
            {
                TabSelectedIndex = 0;
                await closeCardFunction(0);
            }
        }

        //Make a new ListView control
        private void makeListView(int cardIndex)
        {
            refreshListView(cardIndex, 0);
        }


        //Prepare icons for drawing (add flags and make them transparent if save is deleted)
        private Avalonia.Media.Imaging.Bitmap prepareIcons(int listIndex, int slotNumber)
        {
            Bitmap iconBitmap = new Bitmap(48, 16);
            
            Graphics iconGraphics = Graphics.FromImage(iconBitmap);
            //Check what background color should be set
            switch (mainSettings.iconBackgroundColor)
            {
                case 1: //Black
                    iconGraphics.FillRegion(new SolidBrush(Color.Black), new Region(new Rectangle(0, 0, 16, 16)));
                    break;

                case 2: //Gray
                    iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x30, 0x30, 0x30)),
                        new Region(new Rectangle(0, 0, 16, 16)));
                    break;

                case 3: //Blue
                    iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x44, 0x44, 0x98)),
                        new Region(new Rectangle(0, 0, 16, 16)));
                    break;
            }

            //Draw icon
            iconGraphics.DrawImage(PScard[listIndex].iconData[slotNumber, 0], 0, 0, 16, 16);
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            //Draw flag depending of the region
            switch (PScard[listIndex].saveRegion[slotNumber])
            {
                default: //Formatted save, Corrupted save, Unknown region
                    iconGraphics.DrawImage(
                        new Bitmap(assets.Open(new Uri("avares://MemcardRex/Assets/Images/" + "naflag" + ".bmp"))), 17, 0, 30,
                        16);
                    break;

                case 0x4142: //American region
                    iconGraphics.DrawImage(
                        new Bitmap(assets.Open(new Uri("avares://MemcardRex/Assets/Images/" + "amflag" + ".bmp"))), 17, 0, 30,
                        16);
                    break;

                case 0x4542: //European region
                    iconGraphics.DrawImage(
                        new Bitmap(assets.Open(new Uri("avares://MemcardRex/Assets/Images/" + "euflag" + ".bmp"))), 17, 0, 30,
                        16);
                    break;

                case 0x4942: //Japanese region
                    iconGraphics.DrawImage(
                        new Bitmap(assets.Open(new Uri("avares://MemcardRex/Assets/Images/" + "jpflag"+ ".bmp"))), 17, 0, 30,
                        16);
                    break;
            }

            //Check if save is deleted and color the icon
            if (PScard[listIndex].saveType[slotNumber] == 4)
                iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF)),
                    new Region(new Rectangle(0, 0, 16, 16)));
            
            iconGraphics.Dispose();
            
            
            iconBitmap.Save("temp.bmp", ImageFormat.Bmp);
            
            return new Avalonia.Media.Imaging.Bitmap("temp.bmp");
        }


        //Enable only supported edit operations
        private void enableSelectiveEditItems()
        {
            //Check if there are any cards
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;
                if (listIndex == -1)
                {
                    listIndex = TabSelectedIndex = 0;
                }
                //Check if any item on the list is selected
                if (PScard[listIndex].SelectedSaveIndex > -1)
                {
                    int slotNumber = PScard[listIndex].SelectedSaveIndex;

                    //Check the save type
                    switch (PScard[listIndex].saveType[slotNumber])
                    {
                        default:
                            break;

                        case 0: //Formatted
                            disableEditItems();
                            pasteSaveFromTemporaryBufferToolStripMenuItemEnabled = true;
                            paseToolStripMenuItemEnabled = true;
                            importSaveToolStripMenuItemEnabled = true;
                            importSaveToolStripMenuItem1Enabled = true;
                            importButtonEnabled = true;
                            break;

                        case 1: //Initial
                            enableEditItems();
                            RestoreSaveToolStripMenuItemEnabled = false;
                            restoreSaveToolStripMenuItem1Enabled = false;
                            pasteSaveFromTemporaryBufferToolStripMenuItemEnabled = false;
                            paseToolStripMenuItemEnabled = false;
                            importSaveToolStripMenuItemEnabled = false;
                            importSaveToolStripMenuItem1Enabled = false;
                            importButtonEnabled = false;
                            break;

                        case 2: //Middle link
                        case 3: //End link
                            disableEditItems();
                            break;

                        case 4: //Deleted initial
                            enableEditItems();
                            DeleteSaveToolStripMenuItemEnabled = false;
                            deleteSaveToolStripMenuItem1Enabled = false;
                            pasteSaveFromTemporaryBufferToolStripMenuItemEnabled = false;
                            paseToolStripMenuItemEnabled = false;
                            importSaveToolStripMenuItemEnabled = false;
                            importSaveToolStripMenuItem1Enabled = false;
                            importButtonEnabled = false;
                            exportSaveToolStripMenuItemEnabled = false;
                            exportSaveToolStripMenuItem1Enabled = false;
                            exportButtonEnabled = false;
                            break;

                        case 5: //Deleted middle link
                        case 6: //Deleted end link
                            disableEditItems();
                            break;

                        case 7: //Corrupted
                            disableEditItems();
                            RemoveSaveformatSlotsToolStripMenuItemEnabled = true;
                            removeSaveformatSlotsToolStripMenuItem1Enabled = true;
                            break;
                    }
                }
                else
                {
                    //No save is selected, disable all items
                    disableEditItems();
                }
            }
            else
            {
                //There is no card, disable all items
                disableEditItems();
            }
        }

        //Disable all items related to save editing
        private void
            disableEditItems()
        {
            //Edit menu
            EditSaveHeaderToolStripMenuItemEnabled = false;
            EditSaveCommentToolStripMenuItemEnabled = false;
            compareWithTempBufferToolStripMenuItemEnabled = false;
            EditIconToolStripMenuItemEnabled = false;
            DeleteSaveToolStripMenuItemEnabled = false;
            RestoreSaveToolStripMenuItemEnabled = false;
            RemoveSaveformatSlotsToolStripMenuItemEnabled = false;
            copySaveToTempraryBufferToolStripMenuItemEnabled = false;
            pasteSaveFromTemporaryBufferToolStripMenuItemEnabled = false;
            importSaveToolStripMenuItemEnabled = false;
            exportSaveToolStripMenuItemEnabled = false;

            //Edit toolbar
            editHeaderButtonEnabled = false;
            commentsButtonEnabled = false;
            editIconButtonEnabled = false;
            importButtonEnabled = false;
            exportButtonEnabled = false;

            //Edit popup
            editSaveHeaderToolStripMenuItem1Enabled = false;
            editSaveCommentsToolStripMenuItemEnabled = false;
            compareWithTempBufferToolStripMenuItem1Enabled = false;
            editIconToolStripMenuItem1Enabled = false;
            deleteSaveToolStripMenuItem1Enabled = false;
            restoreSaveToolStripMenuItem1Enabled = false;
            removeSaveformatSlotsToolStripMenuItem1Enabled = false;
            copySaveToTempBufferToolStripMenuItemEnabled = false;
            paseToolStripMenuItemEnabled = false;
            importSaveToolStripMenuItem1Enabled = false;
            exportSaveToolStripMenuItem1Enabled = false;
            saveInformationToolStripMenuItemEnabled = false;
        }

        //Enable all items related to save editing
        private void enableEditItems()
        {
            //Edit menu
            EditSaveHeaderToolStripMenuItemEnabled = true;
            EditSaveCommentToolStripMenuItemEnabled = true;
            EditIconToolStripMenuItemEnabled = true;
            DeleteSaveToolStripMenuItemEnabled = true;
            RestoreSaveToolStripMenuItemEnabled = true;
            RemoveSaveformatSlotsToolStripMenuItemEnabled = true;
            copySaveToTempraryBufferToolStripMenuItemEnabled = true;
            pasteSaveFromTemporaryBufferToolStripMenuItemEnabled = true;
            importSaveToolStripMenuItemEnabled = true;
            exportSaveToolStripMenuItemEnabled = true;

            //Edit toolbar
            editHeaderButtonEnabled = true;
            commentsButtonEnabled = true;
            editIconButtonEnabled = true;
            importButtonEnabled = true;
            exportButtonEnabled = true;

            //Edit popup
            editSaveHeaderToolStripMenuItem1Enabled = true;
            editSaveCommentsToolStripMenuItemEnabled = true;
            editIconToolStripMenuItem1Enabled = true;
            deleteSaveToolStripMenuItem1Enabled = true;
            restoreSaveToolStripMenuItem1Enabled = true;
            removeSaveformatSlotsToolStripMenuItem1Enabled = true;
            copySaveToTempBufferToolStripMenuItemEnabled = true;
            paseToolStripMenuItemEnabled = true;
            importSaveToolStripMenuItem1Enabled = true;
            exportSaveToolStripMenuItem1Enabled = true;
            saveInformationToolStripMenuItemEnabled = true;

            //Temp buffer related
            if (tempBuffer != null)
            {
                compareWithTempBufferToolStripMenuItemEnabled = true;
                compareWithTempBufferToolStripMenuItem1Enabled = true;
            }
            else
            {
                compareWithTempBufferToolStripMenuItemEnabled = false;
                compareWithTempBufferToolStripMenuItem1Enabled = false;
            }
        }

        private bool _exportSaveToolStripMenuItemEnabled;

        public bool exportSaveToolStripMenuItemEnabled
        {
            get { return _exportSaveToolStripMenuItemEnabled; }
            set
            {
                _exportSaveToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _importSaveToolStripMenuItemEnabled;

        public bool importSaveToolStripMenuItemEnabled
        {
            get { return _importSaveToolStripMenuItemEnabled; }
            set
            {
                _importSaveToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _pasteSaveFromTemporaryBufferToolStripMenuItemEnabled;

        public bool pasteSaveFromTemporaryBufferToolStripMenuItemEnabled
        {
            get { return _pasteSaveFromTemporaryBufferToolStripMenuItemEnabled; }
            set
            {
                _pasteSaveFromTemporaryBufferToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _RemoveSaveformatSlotsToolStripMenuItemEnabled;

        public bool RemoveSaveformatSlotsToolStripMenuItemEnabled
        {
            get { return _RemoveSaveformatSlotsToolStripMenuItemEnabled; }
            set
            {
                _RemoveSaveformatSlotsToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _copySaveToTempraryBufferToolStripMenuItemEnabled;

        public bool copySaveToTempraryBufferToolStripMenuItemEnabled
        {
            get { return _copySaveToTempraryBufferToolStripMenuItemEnabled; }
            set
            {
                _copySaveToTempraryBufferToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _RestoreSaveToolStripMenuItemEnabled;

        public bool RestoreSaveToolStripMenuItemEnabled
        {
            get { return _RestoreSaveToolStripMenuItemEnabled; }
            set
            {
                _RestoreSaveToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _DeleteSaveToolStripMenuItemEnabled;

        public bool DeleteSaveToolStripMenuItemEnabled
        {
            get { return _DeleteSaveToolStripMenuItemEnabled; }
            set
            {
                _DeleteSaveToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _EditIconToolStripMenuItemEnabled;

        public bool EditIconToolStripMenuItemEnabled
        {
            get { return _EditIconToolStripMenuItemEnabled; }
            set
            {
                _EditIconToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _EditSaveCommentToolStripMenuItemEnabled;

        public bool EditSaveCommentToolStripMenuItemEnabled
        {
            get { return _EditSaveCommentToolStripMenuItemEnabled; }
            set
            {
                _EditSaveCommentToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _EditSaveHeaderToolStripMenuItemEnabled;

        public bool EditSaveHeaderToolStripMenuItemEnabled
        {
            get { return _EditSaveHeaderToolStripMenuItemEnabled; }
            set
            {
                _EditSaveHeaderToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _editHeaderButtonEnabled;

        public bool editHeaderButtonEnabled
        {
            get { return _editHeaderButtonEnabled; }
            set
            {
                _editHeaderButtonEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _commentsButtonEnabled;

        public bool commentsButtonEnabled
        {
            get { return _commentsButtonEnabled; }
            set
            {
                _commentsButtonEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _editIconButtonEnabled;

        public bool editIconButtonEnabled
        {
            get { return _editIconButtonEnabled; }
            set
            {
                _editIconButtonEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _importButtonEnabled;

        public bool importButtonEnabled
        {
            get { return _importButtonEnabled; }
            set
            {
                _importButtonEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _exportButtonEnabled;

        public bool exportButtonEnabled
        {
            get { return _exportButtonEnabled; }
            set
            {
                _exportButtonEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _compareWithTempBufferToolStripMenuItem1Enabled;

        public bool compareWithTempBufferToolStripMenuItem1Enabled
        {
            get { return _compareWithTempBufferToolStripMenuItem1Enabled; }
            set
            {
                _compareWithTempBufferToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _compareWithTempBufferToolStripMenuItemEnabled;

        public bool compareWithTempBufferToolStripMenuItemEnabled
        {
            get { return _compareWithTempBufferToolStripMenuItemEnabled; }
            set
            {
                _compareWithTempBufferToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _saveInformationToolStripMenuItemEnabled;

        public bool saveInformationToolStripMenuItemEnabled
        {
            get { return _saveInformationToolStripMenuItemEnabled; }
            set
            {
                _saveInformationToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _exportSaveToolStripMenuItem1Enabled;

        public bool exportSaveToolStripMenuItem1Enabled
        {
            get { return _exportSaveToolStripMenuItem1Enabled; }
            set
            {
                _exportSaveToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _importSaveToolStripMenuItem1Enabled;

        public bool importSaveToolStripMenuItem1Enabled
        {
            get { return _importSaveToolStripMenuItem1Enabled; }
            set
            {
                _importSaveToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _paseToolStripMenuItemEnabled;

        public bool paseToolStripMenuItemEnabled
        {
            get { return _paseToolStripMenuItemEnabled; }
            set
            {
                _paseToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _copySaveToTempBufferToolStripMenuItemEnabled;

        public bool copySaveToTempBufferToolStripMenuItemEnabled
        {
            get { return _copySaveToTempBufferToolStripMenuItemEnabled; }
            set
            {
                _copySaveToTempBufferToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _removeSaveformatSlotsToolStripMenuItem1Enabled;

        public bool removeSaveformatSlotsToolStripMenuItem1Enabled
        {
            get { return _removeSaveformatSlotsToolStripMenuItem1Enabled; }
            set
            {
                _removeSaveformatSlotsToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _restoreSaveToolStripMenuItem1Enabled;

        public bool restoreSaveToolStripMenuItem1Enabled
        {
            get { return _restoreSaveToolStripMenuItem1Enabled; }
            set
            {
                _restoreSaveToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _deleteSaveToolStripMenuItem1Enabled;

        public bool deleteSaveToolStripMenuItem1Enabled
        {
            get { return _deleteSaveToolStripMenuItem1Enabled; }
            set
            {
                _deleteSaveToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _editIconToolStripMenuItem1Enabled;

        public bool editIconToolStripMenuItem1Enabled
        {
            get { return _editIconToolStripMenuItem1Enabled; }
            set
            {
                _editIconToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _editSaveCommentsToolStripMenuItemEnabled;

        public bool editSaveCommentsToolStripMenuItemEnabled
        {
            get { return _editSaveCommentsToolStripMenuItemEnabled; }
            set
            {
                _editSaveCommentsToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _editSaveHeaderToolStripMenuItem1Enabled;

        public bool editSaveHeaderToolStripMenuItem1Enabled
        {
            get { return _editSaveHeaderToolStripMenuItem1Enabled; }
            set
            {
                _editSaveHeaderToolStripMenuItem1Enabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _CloseToolStripMenuItemEnabled;

        public bool CloseToolStripMenuItemEnabled
        {
            get { return _CloseToolStripMenuItemEnabled; }
            set
            {
                _CloseToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _CloseAllToolStripMenuItemEnabled;

        public bool CloseAllToolStripMenuItemEnabled
        {
            get { return _CloseAllToolStripMenuItemEnabled; }
            set
            {
                _CloseAllToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _SaveToolStripMenuItemEnabled;

        public bool SaveToolStripMenuItemEnabled
        {
            get { return _SaveToolStripMenuItemEnabled; }
            set
            {
                _SaveToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _SaveButtonEnabled = false;

        public bool SaveButtonEnabled
        {
            get { return _SaveButtonEnabled; }
            set
            {
                _SaveButtonEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _SaveAsToolStripMenuItemEnabled;

        public bool SaveAsToolStripMenuItemEnabled
        {
            get { return _SaveAsToolStripMenuItemEnabled; }
            set
            {
                _SaveAsToolStripMenuItemEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _TBufToolButtonEnabled;

        public bool TBufToolButtonEnabled
        {
            get { return _TBufToolButtonEnabled; }
            set
            {
                _TBufToolButtonEnabled = value;
                this.RaisePropertyChanged();
            }
        }

        private string _TBufToolButtonText = "Temp buffer is empty";

        public string TBufToolButtonText
        {
            get { return _TBufToolButtonText; }
            set
            {
                _TBufToolButtonText = value;
                this.RaisePropertyChanged();
            }
        }

        private Bitmap _TBufToolButtonImage;

        public Bitmap TBufToolButtonImage
        {
            get { return _TBufToolButtonImage; }
            set
            {
                _TBufToolButtonImage = value;
                this.RaisePropertyChanged();
            }
        }


     

        //Temp buffer used to store saves
        byte[] tempBuffer = null;
        string tempBufferName = null;

        //Paste save to Memory Card
        private async Task pasteSaveFunction()
        {
            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex == -1) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;
                int requiredSlots = 0;

                //Check if temp buffer contains anything
                if (tempBuffer != null)
                {
                    //Check if the slot to paste the save on is free
                    if (PScard[listIndex].saveType[slotNumber] == 0)
                    {
                        if (PScard[listIndex].setSaveBytes(slotNumber, tempBuffer, out requiredSlots))
                        {
                            refreshListView(listIndex, slotNumber);
                        }
                        else
                        {
                            var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                                .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                                {
                                    Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                    ContentTitle = appName,
                                    ContentMessage = "To complete this operation " + requiredSlots.ToString() +
                                                     " free slots are required.",
                                    ButtonDefinitions = new[]
                                    {
                                        new ButtonDefinition {Name = "OK"},
                                    },
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                });
                            await messageBoxCustomWindow.ShowDialog(
                                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                                .MainWindow);

                        }
                    }
                    else
                    {
                        var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage = "The selected slot is not empty.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBoxCustomWindow.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                    }
                }
                else
                {
                    var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                        .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                        {
                            Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                            ContentTitle = appName,
                            ContentMessage = "Temp buffer is empty.",
                            ButtonDefinitions = new[]
                            {
                                new ButtonDefinition {Name = "OK"},
                            },
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        });
                    await messageBoxCustomWindow.ShowDialog(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                }
            }


        }


        //Edit a selected save with a selected plugin
        private async Task editWithPlugin(int pluginIndex)
        {
            //Check if there are any cards to edit
            if (PScard.Count > 0)
            {
                //Show backup warning message
                if (mainSettings.warningMessage)
                {
                    var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                        .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                        {
                            Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                            ContentTitle = appName,
                            ContentMessage =
                                "Save editing may potentialy corrupt the save.\nDo you want to proceed with this operation?",
                            ButtonDefinitions = new[]
                            {
                                new ButtonDefinition {Name = "Yes"},
                                new ButtonDefinition {Name = "No"},
                            },
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        });
                    var result = await messageBoxCustomWindow.ShowDialog(
                        (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                    if (result == "No")
                    {
                        return;
                    }
                }

                int listIndex = TabSelectedIndex;
                int slotNumber = PScard[listIndex].SelectedSaveIndex;
                int reqSlots = 0;
                byte[] editedSaveBytes = pluginSystem.editSaveData(supportedPlugins[pluginIndex],
                    PScard[listIndex].getSaveBytes(slotNumber), PScard[listIndex].saveProdCode[slotNumber]);

                if (editedSaveBytes != null)
                {
                    //Delete save so the edited one can be placed in.
                    PScard[listIndex].formatSave(slotNumber);

                    PScard[listIndex].setSaveBytes(slotNumber, editedSaveBytes, out reqSlots);

                    //Refresh the list with new data
                    refreshListView(listIndex, slotNumber);

                    //Set the edited flag of the card
                    PScard[listIndex].changedFlag = true;
                }
            }
        }

        //Compare currently selected save with the temp buffer
        private async Task compareSaveWithTemp()
        {
            //Save data to work with
            byte[] fetchedData = null;
            string fetchedDataTitle = null;

            //Check if temp buffer contains anything
            if (tempBuffer == null)
            {
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = appName,
                        ContentMessage =
                            "Temp buffer is empty. Save can't be compared.",
                        ButtonDefinitions = new[]
                        {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                return;
            }

            //Check if there are any cards available
            if (PScard.Count > 0)
            {
                int listIndex = TabSelectedIndex;

                //Check if a save is selected
                if (PScard[listIndex].SelectedSaveIndex < 0) return;

                int slotNumber = PScard[listIndex].SelectedSaveIndex;

                //Check the save type
                switch (PScard[listIndex].saveType[slotNumber])
                {
                    default:
                        break;

                    case 1: //Initial save
                    case 4: //Deleted initial

                        //Get data to work with
                        fetchedData = PScard[listIndex].getSaveBytes(slotNumber);
                        fetchedDataTitle = PScard[listIndex].saveName[slotNumber, mainSettings.titleEncoding];

                        //Check if selected saves have the same size
                        if (fetchedData.Length != tempBuffer.Length)
                        {
                            var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                                .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                                {
                                    Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                    ContentTitle = appName,
                                    ContentMessage =
                                        "Save file size mismatch. Saves can't be compared.",
                                    ButtonDefinitions = new[]
                                    {
                                        new ButtonDefinition {Name = "OK"},
                                    },
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                });
                            await messageBoxCustomWindow.ShowDialog(
                                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                                .MainWindow);

                            return;
                        }

                        //Show compare window

                        var window = new CompareWindow();
                        var vm = new CompareWindowViewModel()
                        {
                            Save1Text = "Save 1: " + fetchedDataTitle,
                            Save2Text = "Save 2: " + tempBufferName + " (temp buffer)",
                            Saves = new ObservableCollection<SaveOffset>()
                        };

                        //Compare saves
                        for (int i = 0; i < fetchedData.Length; i++)
                        {
                            //Check if the bytes are different
                            if (fetchedData[i] != tempBuffer[i])
                            {
                                vm.Saves.Add(new SaveOffset()
                                {
                                    Offset = "0x" + i.ToString("X4") + " (" + i.ToString() + ")",
                                    Save1 = "0x" + fetchedData[i].ToString("X2") + " (" +
                                            fetchedData[i].ToString() + ")",
                                    Save2 = "0x" + tempBuffer[i].ToString("X2") + " (" + tempBuffer[i].ToString() + ")",

                                });
                            }
                        }

                        //Check if the list contains any items
                        if (vm.Saves.Count < 1)
                        {
                            var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                                .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                                {
                                    Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                    ContentTitle = appName,
                                    ContentMessage =
                                        "Compared saves are identical.",
                                    ButtonDefinitions = new[]
                                    {
                                        new ButtonDefinition {Name = "OK"},
                                    },
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                });
                            await messageBoxCustomWindow.ShowDialog(
                                (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                                .MainWindow);
                            return;

                        }

                        window.DataContext = window;
                        await window.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                        break;

                    case 2:
                    case 3:
                    case 5:
                    case 6:
                        var messageBox = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                            {
                                Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                                ContentTitle = appName,
                                ContentMessage =
                                    "The selected slot is linked. Select the initial save slot to proceed.",
                                ButtonDefinitions = new[]
                                {
                                    new ButtonDefinition {Name = "OK"},
                                },
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            });
                        await messageBox.ShowDialog(
                            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                        break;
                }
            }
        }
    }

}