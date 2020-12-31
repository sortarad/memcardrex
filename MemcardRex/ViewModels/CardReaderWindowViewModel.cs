using System.Windows.Input;
using Avalonia.Controls;
using DexDriveCommunication;
using MemCARDuinoCommunication;
using PS1CardLinkCommunication;
using ReactiveUI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using DexDriveCommunication;
using MemcardRex.Views;
using MemCARDuinoCommunication;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using PS1CardLinkCommunication;

namespace MemcardRex.ViewModels
{
    public class CardReaderWindowViewModel : ViewModelBase
    {
        //DexDrive Memory Card reading device
        public static  DexDrive dexDevice = new DexDrive();

        //MemCARDuino Memory Card reading device
        public static   MemCARDuino CARDuino = new MemCARDuino();

        //PS1CardLink Memory Card reading device
        public static   PS1CardLink PS1CLnk = new PS1CardLink();

        public static CardReaderWindow window;
        
        //Maximum number of frames for writing (usually 1024 but 16 for quick format)
        public int maxWritingFrames = 0;

        //Complete Memory Card data
        public  byte[] completeMemoryCard = new byte[131072];

        //Reading status flag
        public  bool sucessfullRead = false;

        //Currently active device (0 - DexDrive, 1 - MemCARDuino, 2 - PS1CardLink)
        public int currentDeviceIdentifier = 0;
        
        
        public System.ComponentModel.BackgroundWorker backgroundReader = new BackgroundWorker();
        public System.ComponentModel.BackgroundWorker backgroundWriter = new BackgroundWorker();

        
        
        public string Title { get; set; } 
        public string Info { get; set; }

        
        private int _Maximum;

        public int Maximum
        {
            get { return _Maximum;}
            set
            {
                _Maximum = value;
                this.RaisePropertyChanged();
            }
        }
        private int _Value;

        public int Value
        {
            get { return _Value; }
            set
            {
                _Value = value;
                this.RaisePropertyChanged();
            }
        }

        
        
        //Read a Memory Card from DexDrive (return null if there was an error)
        public static async Task<byte[]>  readMemoryCardDexDrive(Window hostWindow, string applicationName, string comPort)
        {
            //Initialize DexDrive
            string errorString = dexDevice.StartDexDrive(comPort);

            //Check if there were any errors
            if (errorString != null)
            {
                //Display an error message and cleanly close DexDrive communication
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = applicationName,
                        ContentMessage = errorString,
                        ButtonDefinitions = new [] {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(hostWindow);
                dexDevice.StopDexDrive();
                return null;
            }


            var vm = new CardReaderWindowViewModel()
            {
                //Set scale for progress bar
                Maximum = 1024,

                //Set current device to DexDrive
                currentDeviceIdentifier = 0,

                //Set window title and information
                Title = "DexDrive communication",
                Info = "Reading data from DexDrive (ver. " + dexDevice.GetFirmwareVersion() +  ")..."
            };
            
            //Start reading data
            vm.backgroundReader = new BackgroundWorker();
            vm.backgroundReader.ProgressChanged += vm.backgroundReader_ProgressChanged;
            vm.backgroundReader.DoWork += vm.backgroundReader_DoWork;
            vm.backgroundReader.RunWorkerCompleted += vm.backgroundReader_RunWorkerCompleted;
            
            vm.backgroundWriter = new BackgroundWorker();
            vm.backgroundWriter.ProgressChanged += vm.backgroundWriter_ProgressChanged;
            vm.backgroundWriter.DoWork += vm.backgroundWriter_DoWork;
            vm.backgroundWriter.RunWorkerCompleted += vm.backgroundWriter_RunWorkerCompleted;
            
            vm.backgroundReader.RunWorkerAsync();
            
            window = new CardReaderWindow();
            window.DataContext = vm;
            await window.ShowDialog(hostWindow);
            
            //Stop working with DexDrive
            dexDevice.StopDexDrive();

            //Check the final status (return data if all is ok, otherwise return null)
            if (vm.sucessfullRead == true) return vm.completeMemoryCard;
            else return null;
        }

        //Read a Memory Card from MemCARDuino
        public static   async Task<byte[]> readMemoryCardCARDuino(Window hostWindow, string applicationName, string comPort)
        {
            //Initialize MemCARDuino
            string errorString = CARDuino.StartMemCARDuino(comPort);

            //Check if there were any errors
            if (errorString != null)
            {
                //Display an error message and cleanly close MemCARDuino communication
               
                //Display an error message and cleanly close DexDrive communication
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = applicationName,
                        ContentMessage = errorString,
                        ButtonDefinitions = new [] {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(hostWindow);
                CARDuino.StopMemCARDuino();
                return null;
            }

            var vm = new CardReaderWindowViewModel()
            {
                //Set scale for progress bar
                Maximum = 1024,

                //Set current device to MemCARDuino
                currentDeviceIdentifier = 1,

                //Set window title and information
                Title = "MemCARDuino communication",
                Info = "Reading data from MemCARDuino (ver. " + CARDuino.GetFirmwareVersion() + ")...",

                
            };
            //Start reading data
            vm.backgroundReader = new BackgroundWorker();
            vm.backgroundReader.ProgressChanged += vm.backgroundReader_ProgressChanged;
            vm.backgroundReader.DoWork += vm.backgroundReader_DoWork;
            vm.backgroundReader.RunWorkerCompleted += vm.backgroundReader_RunWorkerCompleted;
            
            vm.backgroundWriter = new BackgroundWorker();
            vm.backgroundWriter.ProgressChanged += vm.backgroundWriter_ProgressChanged;
            vm.backgroundWriter.DoWork += vm.backgroundWriter_DoWork;
            vm.backgroundWriter.RunWorkerCompleted += vm.backgroundWriter_RunWorkerCompleted;
            
            vm.backgroundReader.RunWorkerAsync();
            
            window = new CardReaderWindow();
            window.DataContext = vm;
            await window.ShowDialog(hostWindow);

            //Stop working with MemCARDuino
            CARDuino.StopMemCARDuino();

            //Check the final status (return data if all is ok, otherwise return null)
            if (vm.sucessfullRead == true) return vm.completeMemoryCard;
            else return null;
        }

        //Read a Memory Card from PS1CardLink
        public static   async Task<byte[]>  readMemoryCardPS1CLnk(Window hostWindow, string applicationName, string comPort)
        {
            //Initialize PS1CardLink
            string errorString = PS1CLnk.StartPS1CardLink(comPort);

            //Check if there were any errors
            if (errorString != null)
            {
                //Display an error message and cleanly close PS1CardLink communication
               
                //Display an error message and cleanly close DexDrive communication
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = applicationName,
                        ContentMessage = errorString,
                        ButtonDefinitions = new [] {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(hostWindow);
                PS1CLnk.StopPS1CardLink();
                return null;
            }

            var vm = new CardReaderWindowViewModel()
            {
                //Set scale for progress bar
                Maximum = 1024,

                //Set current device to PS1CardLink
                currentDeviceIdentifier = 2,

                //Set window title and information
                Title = "PS1CardLink communication",
                Info = "Reading data from PS1CardLink (ver. " + PS1CLnk.GetSoftwareVersion() + ")...",
            };
            //Start reading data
            vm.backgroundReader = new BackgroundWorker();
            vm.backgroundReader.ProgressChanged += vm.backgroundReader_ProgressChanged;
            vm.backgroundReader.DoWork += vm.backgroundReader_DoWork;
            vm.backgroundReader.RunWorkerCompleted += vm.backgroundReader_RunWorkerCompleted;
            
            vm.backgroundWriter = new BackgroundWorker();
            vm.backgroundWriter.ProgressChanged += vm.backgroundWriter_ProgressChanged;
            vm.backgroundWriter.DoWork += vm.backgroundWriter_DoWork;
            vm.backgroundWriter.RunWorkerCompleted += vm.backgroundWriter_RunWorkerCompleted;
            
            vm.backgroundReader.RunWorkerAsync();

             window = new CardReaderWindow();
            window.DataContext = vm;
            await window.ShowDialog(hostWindow);

            //Stop working with PS1CardLink
            PS1CLnk.StopPS1CardLink();

            //Check the final status (return data if all is ok, otherwise return null)
            if (vm.sucessfullRead == true) return vm.completeMemoryCard;
            else return null;
        }

        //Write a Memory Card to DexDrive
        public static  async Task writeMemoryCardDexDrive(Window hostWindow, string applicationName, string comPort, byte[] memoryCardData, int frameNumber)
        {
            //Initialize DexDrive
            string errorString = dexDevice.StartDexDrive(comPort);

            //Check if there were any errors
            if (errorString != null)
            {
                //Display an error message and cleanly close DexDrive communication
                
                //Display an error message and cleanly close DexDrive communication
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = applicationName,
                        ContentMessage = errorString,
                        ButtonDefinitions = new [] {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(hostWindow);
                dexDevice.StopDexDrive();
                return;
            }

            var vm = new CardReaderWindowViewModel()
            {
                //Set maximum number of frames to write
                maxWritingFrames = frameNumber,

                //Set scale for progress bar
                Maximum = frameNumber,

                //Set current device to DexDrive
                currentDeviceIdentifier = 0,

                //Set window title and information
                Title = "DexDrive communication",
                Info = "Writing data to DexDrive (ver. " + dexDevice.GetFirmwareVersion() + ")...",

                //Set reference to the Memory Card data
                completeMemoryCard = memoryCardData,
            };
            //Start reading data
            vm.backgroundReader = new BackgroundWorker();
            vm.backgroundReader.ProgressChanged += vm.backgroundReader_ProgressChanged;
            vm.backgroundReader.DoWork += vm.backgroundReader_DoWork;
            vm.backgroundReader.RunWorkerCompleted += vm.backgroundReader_RunWorkerCompleted;
            
            vm.backgroundWriter = new BackgroundWorker();
            vm.backgroundWriter.ProgressChanged += vm.backgroundWriter_ProgressChanged;
            vm.backgroundWriter.DoWork += vm.backgroundWriter_DoWork;
            vm.backgroundWriter.RunWorkerCompleted += vm.backgroundWriter_RunWorkerCompleted;
            
            vm.backgroundReader.RunWorkerAsync();
             window = new CardReaderWindow();
            window.DataContext = vm;
            await window.ShowDialog(hostWindow);

            //Stop working with DexDrive
            dexDevice.StopDexDrive();
        }
        
        //Write a Memory Card to MemCARDuino
        public static  async Task writeMemoryCardCARDuino(Window hostWindow, string applicationName, string comPort, byte[] memoryCardData, int frameNumber)
        {
            //Initialize MemCARDuino
            string errorString = CARDuino.StartMemCARDuino(comPort);

            //Check if there were any errors
            if (errorString != null)
            {
                //Display an error message and cleanly close MemCARDuino communication
                
                //Display an error message and cleanly close DexDrive communication
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = applicationName,
                        ContentMessage = errorString,
                        ButtonDefinitions = new [] {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(hostWindow);
                CARDuino.StopMemCARDuino();
                return;
            }

            var vm = new CardReaderWindowViewModel()
            {
                //Set maximum number of frames to write
                maxWritingFrames = frameNumber,

                //Set scale for progress bar
                Maximum = frameNumber,

                //Set current device to MemCARDuino
                currentDeviceIdentifier = 1,

                //Set window title and information
                Title = "MemCARDuino communication",
                Info = "Writing data to MemCARDuino (ver. " + CARDuino.GetFirmwareVersion() + ")...",

                //Set reference to the Memory Card data
                completeMemoryCard = memoryCardData,
            };
            //Start reading data
            vm.backgroundReader = new BackgroundWorker();
            vm.backgroundReader.ProgressChanged += vm.backgroundReader_ProgressChanged;
            vm.backgroundReader.DoWork += vm.backgroundReader_DoWork;
            vm.backgroundReader.RunWorkerCompleted += vm.backgroundReader_RunWorkerCompleted;
            
            vm.backgroundWriter = new BackgroundWorker();
            vm.backgroundWriter.ProgressChanged += vm.backgroundWriter_ProgressChanged;
            vm.backgroundWriter.DoWork += vm.backgroundWriter_DoWork;
            vm.backgroundWriter.RunWorkerCompleted += vm.backgroundWriter_RunWorkerCompleted;
            
            vm.backgroundReader.RunWorkerAsync();

           
             window = new CardReaderWindow();
            window.DataContext = vm;
            await window.ShowDialog(hostWindow);

            //Stop working with MemCARDuino
            CARDuino.StopMemCARDuino();
        }

        //Write a Memory Card to PS1CardLink
        public static  async Task writeMemoryCardPS1CLnk(Window hostWindow, string applicationName, string comPort, byte[] memoryCardData, int frameNumber)
        {
            //Initialize PS1CardLink
            string errorString = PS1CLnk.StartPS1CardLink(comPort);

            //Check if there were any errors
            if (errorString != null)
            {
                //Display an error message and cleanly close PS1CardLink communication
                
                //Display an error message and cleanly close DexDrive communication
                var messageBoxCustomWindow = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = applicationName,
                        ContentMessage = errorString,
                        ButtonDefinitions = new [] {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustomWindow.ShowDialog(hostWindow);
                PS1CLnk.StopPS1CardLink();
                return;
            }

            var vm = new CardReaderWindowViewModel()
            {
                //Set maximum number of frames to write
                maxWritingFrames = frameNumber,

                //Set scale for progress bar
                Maximum = frameNumber,

                //Set current device to PS1CardLink
                currentDeviceIdentifier = 2,

                //Set window title and information
                Title = "PS1CardLink communication",
                Info = "Writing data to PS1CardLink (ver. " + PS1CLnk.GetSoftwareVersion() + ")...",

                //Set reference to the Memory Card data
                completeMemoryCard = memoryCardData,
            };
            
            //Start reading data
            vm.backgroundReader = new BackgroundWorker();
            vm.backgroundReader.ProgressChanged += vm.backgroundReader_ProgressChanged;
            vm.backgroundReader.DoWork += vm.backgroundReader_DoWork;
            vm.backgroundReader.RunWorkerCompleted += vm.backgroundReader_RunWorkerCompleted;
            
            vm.backgroundWriter = new BackgroundWorker();
            vm.backgroundWriter.ProgressChanged += vm.backgroundWriter_ProgressChanged;
            vm.backgroundWriter.DoWork += vm.backgroundWriter_DoWork;
            vm.backgroundWriter.RunWorkerCompleted += vm.backgroundWriter_RunWorkerCompleted;
            
            vm.backgroundReader.RunWorkerAsync();

            
             window = new CardReaderWindow();
            window.DataContext = vm;
            await window.ShowDialog(hostWindow);

            //Stop working with PS1CardLink
            PS1CLnk.StopPS1CardLink();
        }

  

        private void backgroundReader_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] tempDataBuffer = null;
            ushort i = 0;

            //Read all 1024 frames of the Memory Card
            while(i < 1024)
            {
                //Check if the "Abort" button has been pressed
                if (backgroundReader.CancellationPending == true) return;

                //Get 128 byte frame data from DexDrive
                if(currentDeviceIdentifier == 0) tempDataBuffer = dexDevice.ReadMemoryCardFrame(i);

                //Get 128 byte frame data from MemCARDuino
                if (currentDeviceIdentifier == 1) tempDataBuffer = CARDuino.ReadMemoryCardFrame(i);

                //Get 128 byte frame data from PS1CardLink
                if (currentDeviceIdentifier == 2) tempDataBuffer = PS1CLnk.ReadMemoryCardFrame(i);

                //Check if there was a checksum mismatch
                if (tempDataBuffer != null)
                {
                    Array.Copy(tempDataBuffer, 0, completeMemoryCard, i * 128, 128);
                    backgroundReader.ReportProgress(i);
                    i++;
                }
            }

            //All data has been read, report success
            sucessfullRead = true;
        }

        private void backgroundReader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Report the read progress to the progress bar
           Value = e.ProgressPercentage;
        }

        private void backgroundReader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Reading was completed or canceled, close the form
            window?.Close();
        }

        private void backgroundWriter_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] tempDataBuffer = new byte[128];
            ushort i = 0;
            bool lastStatus = false;

            //Write all required frames of the Memory Card
            while (i < maxWritingFrames)
            {
                //Check if the "Abort" button has been pressed
                if (backgroundWriter.CancellationPending == true) return;

                //Get 128 byte frame data
                Array.Copy(completeMemoryCard, i * 128, tempDataBuffer, 0, 128);

                //Reset write status
                lastStatus = false;

                //Write data to DexDrive
                if (currentDeviceIdentifier == 0) lastStatus = dexDevice.WriteMemoryCardFrame(i, tempDataBuffer);

                //Write data to MemCARDuino
                if (currentDeviceIdentifier == 1) lastStatus = CARDuino.WriteMemoryCardFrame(i, tempDataBuffer);

                //Write data to PS1CardLink
                if (currentDeviceIdentifier == 2) lastStatus = PS1CLnk.WriteMemoryCardFrame(i, tempDataBuffer);

                //Check if there was a frame or checksum mismatch
                if (lastStatus == true)
                {
                    backgroundWriter.ReportProgress(i);
                    i++;
                }
            }

            //All data has been written, report success
            sucessfullRead = true;
        }

        private void backgroundWriter_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Report the write progress to the progress bar
            Value = e.ProgressPercentage;
        }

        private void backgroundWriter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Writing was completed or canceled, close the form
            window?.Close();
        }

        public ICommand Close => ReactiveCommand.Create<Window>((window) =>
        {
            //Cancel reading job
            if (backgroundReader.IsBusy)backgroundReader.CancelAsync();

            //Cancel writing job
            if(backgroundWriter.IsBusy)backgroundWriter.CancelAsync();
        });
    }
}