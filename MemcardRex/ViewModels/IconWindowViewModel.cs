using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Remote.Protocol.Input;
using Avalonia.VisualTree;
using Egorozh.ColorPicker.Dialog;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using ReactiveUI;
using Key = Avalonia.Input.Key;

namespace MemcardRex.ViewModels
{
    public class IconWindowViewModel : ViewModelBase
    {
        //Icon data
        public byte[] iconData;
        
        Color[] iconPalette = new Color[16];
        Bitmap[] iconBitmap = new Bitmap[3];
        int[] selectedColor = new int[2];
        int selectedIcon = 0;

        public string SaveTitle { get; set; }
        //Initialize default values


        private bool _FramesEnabled;

        public bool FramesEnabled
        {
            get { return _FramesEnabled; }
            set
            {
                _FramesEnabled = value;
                this.RaisePropertyChanged();
            }
        }
        
        
        private string _XText;

        public string XText
        {
            get { return _XText; }
            set
            {
                _XText = value;
                this.RaisePropertyChanged();
            }
        }

             
        private string _YText;

        public string YText
        {
            get { return _YText; }
            set
            {
                _YText = value;
                this.RaisePropertyChanged();
            }
        }
        

        private int _FramesSelectedIndexd;

        public int FramesSelectedIndex
        {
            get { return _FramesSelectedIndexd; }
            set
            {
                _FramesSelectedIndexd = value;
                this.RaisePropertyChanged();
                
                selectedIcon = FramesSelectedIndex;
                drawIcon();
            }
        }
        
        
        private Avalonia.Media.Color _Color1;

        public Avalonia.Media.Color Color1
        {
            get { return _Color1; }
            set
            {
                _Color1 = value;
                this.RaisePropertyChanged();
            }
        }
        
        private Avalonia.Media.Color _Color2;

        public Avalonia.Media.Color Color2
        {
            get { return _Color2; }
            set
            {
                _Color2 = value;
                this.RaisePropertyChanged();
            }
        }

        private Avalonia.Media.Imaging.Bitmap _PaletteImage;
        public Avalonia.Media.Imaging.Bitmap PaletteImage
        {
            get { return _PaletteImage; }
            set
            {
                _PaletteImage = value;
                this.RaisePropertyChanged();
            }
        }
        
        private Avalonia.Media.Imaging.Bitmap _IconImage;
        public Avalonia.Media.Imaging.Bitmap IconImage
        {
            get { return _IconImage; }
            set
            {
                _IconImage = value;
                this.RaisePropertyChanged();
            }
        }
        public ObservableCollection<string> Frames { get; set; }

        public void InitializeDialog(string dialogTitle, int iconFrames, byte[] iconBytes)
        {
            SaveTitle = dialogTitle;
            iconData = iconBytes;
            Frames = new ObservableCollection<string>();
            switch (iconFrames)
            {
                default: //Assume that there is only one icon frame
                    Frames.Add("1st frame");
                    FramesEnabled = false;
                    break;

                case 2: //Two icons
                    Frames.Add("1st frame");
                    Frames.Add("2nd frame");
                    break;

                case 3: //Three icons
                    Frames.Add("1st frame");
                    Frames.Add("2nd frame");
                    Frames.Add("3rd frame");
                    break;
            }

            //Draw palette and icon
            setUpDisplay();

            //Select first frame
            FramesSelectedIndex = 0;
        }

        //Set everything up for drawing
        private void setUpDisplay()
        {
            //Draw palette to color selector
            drawPalette();

            //Set selected colors to first and second colors in the palete
            setSelectedColor(0, 0);
            setSelectedColor(1, 1);

            //Draw icon on the icon render
            drawIcon();
        }

        //Load palette, copied from ps1class :p
        private void loadPalette()
        {
            int redChannel = 0;
            int greenChannel = 0;
            int blueChannel = 0;
            int colorCounter = 0;

            //Clear existing data
            iconPalette = new Color[16];

            //Reset color counter
            colorCounter = 0;

            //Fetch two bytes at a time
            for (int byteCount = 0; byteCount < 32; byteCount += 2)
            {
                redChannel = (iconData[byteCount] & 0x1F) << 3;
                greenChannel = ((iconData[byteCount + 1] & 0x3) << 6) | ((iconData[byteCount] & 0xE0) >> 2);
                blueChannel = ((iconData[byteCount + 1] & 0x7C) << 1);

                //Get the color value
                iconPalette[colorCounter] = Color.FromArgb(redChannel, greenChannel, blueChannel);
                colorCounter++;
            }
        }

        //Load icons, copied from the ps1class :p
        private void loadIcons()
        {
            int byteCount = 0;

            //Clear existing data
            iconBitmap = new Bitmap[3];

            //Each save has 3 icons (some are data but those will not be shown)
            for (int iconNumber = 0; iconNumber < 3; iconNumber++)
            {
                iconBitmap[iconNumber] = new Bitmap(16, 16);
                byteCount = 32 + (128 * iconNumber);

                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x += 2)
                    {
                        iconBitmap[iconNumber].SetPixel(x, y, iconPalette[iconData[byteCount] & 0xF]);
                        iconBitmap[iconNumber].SetPixel(x + 1, y, iconPalette[iconData[byteCount] >> 4]);
                        byteCount++;
                    }
                }
            }
        }

        //Draw selected icon to render
        private void drawIcon()
        {
            Bitmap drawBitmap = new Bitmap(177, 177);
            Graphics drawGraphics = Graphics.FromImage(drawBitmap);
            Pen blackPen = new Pen(Color.Black);

            //Load icon data
            loadIcons();

            drawGraphics.PixelOffsetMode = PixelOffsetMode.Half;
            drawGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;

            //Draw selected icon to drawBitmap
            drawGraphics.DrawImage(iconBitmap[selectedIcon], 0, 0, 177, 177);

            //Set offset mode to default so grid can be drawn
            drawGraphics.PixelOffsetMode = PixelOffsetMode.Default;

            //Draw grid
            for (int y = 0; y < 17; y++)
                drawGraphics.DrawLine(blackPen, 0, (y * 11), 177, (y * 11));

            for (int x = 0; x < 17; x++)
                drawGraphics.DrawLine(blackPen, (x * 11), 0, (x * 11), 177);

            drawGraphics.Dispose();

            drawBitmap.Save("temp.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

            IconImage = new Avalonia.Media.Imaging.Bitmap("temp.bmp") ;
        }

        //Draw palette image to render
        private void drawPalette()
        {
            Bitmap paletteBitmap = new Bitmap(8, 2);
            Bitmap drawBitmap = new Bitmap(121, 31);
            Graphics drawGraphics = Graphics.FromImage(drawBitmap);
            Pen blackPen = new Pen(Color.Black);
            int colorCounter = 0;

            //Load pallete data
            loadPalette();

            drawGraphics.PixelOffsetMode = PixelOffsetMode.Half;
            drawGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;

            //Plot pixels onto bitmap
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    paletteBitmap.SetPixel(x, y, iconPalette[colorCounter]);
                    colorCounter++;
                }
            }

            //Draw palette to drawBitmap
            drawGraphics.DrawImage(paletteBitmap, 0, 0, 120, 30);

            //Set offset mode to default so grid can be drawn
            drawGraphics.PixelOffsetMode = PixelOffsetMode.Default;

            //Draw grid
            for (int y = 0; y < 3; y++)
                drawGraphics.DrawLine(blackPen, 0, (y * 15), 121, (y * 15));

            for (int x = 0; x < 9; x++)
                drawGraphics.DrawLine(blackPen, (x * 15), 0, (x * 15), 31);

            drawGraphics.Dispose();


            drawBitmap.Save("temp.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

            PaletteImage = new Avalonia.Media.Imaging.Bitmap("temp.bmp");
        }

        //Set selected color
        private void setSelectedColor(int selColor, int selectedColorIndex)
        {
            if (selectedColorIndex == 0)
            {
                selectedColor[0] = selColor;
                
                Color1 = new Avalonia.Media.Color(iconPalette[selectedColor[0]].A, iconPalette[selectedColor[0]].R,
                    iconPalette[selectedColor[0]].G, iconPalette[selectedColor[0]].B);
            }
            else
            {
                selectedColor[1] = selColor;
                Color2 = new Avalonia.Media.Color(iconPalette[selectedColor[1]].A, iconPalette[selectedColor[1]].R,
                    iconPalette[selectedColor[1]].G, iconPalette[selectedColor[1]].B);
            }
        }

        //Place pixel on the selected icon
        private void putPixel(int X, int Y, int selectedColorIndex)
        {
            //Calculate destination byte to draw pixel to
            int destinationByte = (X + (Y * 16)) / 2;

            //Check what nibble to draw pixel to
            if ((X + (Y * 16)) % 2 == 0)
            {
                iconData[32 + destinationByte + (selectedIcon * 128)] &= 0xF0;
                iconData[32 + destinationByte + (selectedIcon * 128)] |= (byte) selectedColor[selectedColorIndex];
            }
            else
            {
                iconData[32 + destinationByte + (selectedIcon * 128)] &= 0x0F;
                iconData[32 + destinationByte + (selectedIcon * 128)] |=
                    (byte) (selectedColor[selectedColorIndex] << 4);
            }

            //Redraw icon
            drawIcon();
        }

        //Import currently selected icon
        private async Task importIcon()
        {
            Bitmap OpenedBitmap = null;

            int redChannel = 0;
            int greenChannel = 0;
            int blueChannel = 0;

            Color tempColor = new Color();
            List<Color> foundColors = new List<Color>();

            byte tempIndex = 0;
            byte[,] returnData = new byte[16, 16];

            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Title = "Open icon";
            
            
            openDlg.Filters.Add(new FileDialogFilter(){
                Name = "All supported",
                Extensions = new List<string>(){"bmp","gif","jpg","jpeg","png", }
            });
            openDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "Bitmap (*.bmp)",
                Extensions = new List<string>(){"bmp" }
            });
            openDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "GIF (*.gif)",
                Extensions = new List<string>(){"gif" }
            });
        
            openDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "JPEG (*.jpeg;*.jpg)",
                Extensions = new List<string>(){"jpg","jpeg" }
                
            });
            
            openDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "PNG (*.png)",
                Extensions = new List<string>(){"png" }
                
            });
            openDlg.AllowMultiple = false;
            
            var result = await openDlg.ShowAsync((App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
            //Check if the user pressed OK
            if (result == null || result.Length < 1) return;


            try
            {
                OpenedBitmap = new Bitmap(result[0]);
            }
            catch (Exception e)
            {
                var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = "Error",
                        ContentMessage = e.Message,
                        ButtonDefinitions = new[]
                        {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustom.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
                OpenedBitmap.Dispose();
                return;
            }

            //Check if the image is 16x16 pixels
            if (OpenedBitmap.Width != 16 || OpenedBitmap.Height != 16)
            {
                var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = "Warning",
                        ContentMessage = "Selected image is not a 16x16 pixel image.",
                        ButtonDefinitions = new[]
                        {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustom.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                OpenedBitmap.Dispose();
                return;
            }

            //Create a palette from the given image
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    //Check if the given color exists and add it if it doesn't
                    if (!foundColors.Contains(OpenedBitmap.GetPixel(x, y)))
                        foundColors.Add(OpenedBitmap.GetPixel(x, y));
                }
            }

            //Check if the palette has more than 16 colors
            if (foundColors.Count > 16)
            {
                var messageBoxCustom = MessageBox.Avalonia.MessageBoxManager
                    .GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        Style = Style.DarkMode,
CanResize = true,
 MaxWidth = 800,
                        ContentTitle = "Warning",
                        ContentMessage = "Selected image contains more then 16 colors.",
                        ButtonDefinitions = new[]
                        {
                            new ButtonDefinition {Name = "OK"},
                        },
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });
                await messageBoxCustom.ShowDialog(
                    (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

                OpenedBitmap.Dispose();
                return;
            }

            //Check if some colors should be added to make a 16 color palette
            for (int i = foundColors.Count; i < 16; i++)
            {
                foundColors.Add(Color.Black);
            }

            //Copy palette from the opened Bitmap
            for (int i = 0; i < 16; i++)
            {
                //Get RGB channels from the Bitmap palette
                redChannel = (foundColors[i].R >> 3);
                greenChannel = (foundColors[i].G >> 3);
                blueChannel = (foundColors[i].B >> 3);

                //Set color to iconData (convert 24 bit color to 15 bit)
                iconData[i * 2] = (byte) (redChannel | ((greenChannel & 0x07) << 5));
                iconData[(i * 2) + 1] = (byte) ((blueChannel << 2) | ((greenChannel & 0x18) >> 3));
            }

            //Copy image data from opened bitmap
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    //Reset index variable (not necessary, but to be safe anyway)
                    tempIndex = 0;

                    //Get the ARGB color of the current pixel
                    tempColor = OpenedBitmap.GetPixel(x, y);

                    //Cycle through palette to see the current index
                    //This way is a bit resource heavy but since image is always 16x16 it's not an issue
                    //There is no "good" alternative to do it with indexed bitmaps, only the unsafe one
                    for (byte pIndex = 0; pIndex < 16; pIndex++)
                    {
                        if (foundColors[pIndex] == tempColor)
                        {
                            tempIndex = pIndex;
                            break;
                        }
                    }

                    returnData[x, y] = tempIndex;
                }
            }

            setDataGrid(returnData);

            OpenedBitmap.Dispose();

            //Appy the imported icon
            setUpDisplay();
        }

        //Export currently selected icon
        private  async Task exportIcon()
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Title = "Save icon";
            saveDlg.Filters.Add(new FileDialogFilter(){
                Name = "All supported",
                Extensions = new List<string>(){"bmp","gif","jpg","jpeg","png", }
            });
            saveDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "Bitmap (*.bmp)",
                Extensions = new List<string>(){"bmp" }
            });
            saveDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "GIF (*.gif)",
                Extensions = new List<string>(){"gif" }
            });
        
            saveDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "JPEG (*.jpeg;*.jpg)",
                Extensions = new List<string>(){"jpg","jpeg" }
                
            });
            
            saveDlg.Filters.Add(new FileDialogFilter()
            {
                Name = "PNG (*.png)",
                Extensions = new List<string>(){"png" }
                
            });
            
            var result = await saveDlg.ShowAsync( (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            if (!string.IsNullOrWhiteSpace(result))
            {
                ImageFormat imgFormat;

                if (result.ToLower().EndsWith("bmp"))
                {
                    imgFormat = ImageFormat.Bmp;
                }
                else if (result.ToLower().EndsWith("gif"))
                {
                    imgFormat = ImageFormat.Gif;
                }
                else if (result.ToLower().EndsWith("jpg") ||(result.ToLower().EndsWith("jpeg")) )
                {
                    imgFormat = ImageFormat.Gif;
                }
                else if  (result.ToLower().EndsWith("png"))
                {
                    imgFormat = ImageFormat.Png;
                }
                else
                {
                    imgFormat = ImageFormat.Bmp;
                }
              
                //Save the image in the selected format
                iconBitmap[selectedIcon].Save(result, imgFormat);
            }
        }

        //Flip the icon horizontally
        private void horizontalFlip()
        {
            byte[,] tempIconData = getDataGrid();
            byte[,] processedData = new byte[16, 16];

            //Process the data
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    processedData[x, y] = tempIconData[15 - x, y];
                }
            }

            //Update icon data
            setDataGrid(processedData);

            //Redraw icon
            drawIcon();
        }

        //Flip the icon vertically
        private void verticalFlip()
        {
            byte[,] tempIconData = getDataGrid();
            byte[,] processedData = new byte[16, 16];

            //Process the data
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    processedData[x, y] = tempIconData[x, 15 - y];
                }
            }

            //Update icon data
            setDataGrid(processedData);

            //Redraw icon
            drawIcon();
        }

        //Rotate the icon to the left
        private void leftRotate()
        {
            byte[,] tempIconData = getDataGrid();
            byte[,] processedData = new byte[16, 16];

            //Process the data
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    processedData[x, y] = tempIconData[y, x];
                }
            }

            //Update icon data
            setDataGrid(processedData);

            //Fix icon and update it
            verticalFlip();
        }

        //Rotate the icon to the right
        private void rightRotate()
        {
            byte[,] tempIconData = getDataGrid();
            byte[,] processedData = new byte[16, 16];

            //Process the data
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    processedData[x, y] = tempIconData[y, x];
                }
            }

            //Update icon data
            setDataGrid(processedData);

            //Fix icon and update it
            horizontalFlip();
        }

        //Get icon data as 16x16 byte grid
        private byte[,] getDataGrid()
        {
            byte[,] returnData = new byte[16, 16];
            int byteCount = 32 + (selectedIcon * 128);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x += 2)
                {
                    returnData[x, y] = (byte) (iconData[byteCount] & 0x0F);
                    returnData[x + 1, y] = (byte) ((iconData[byteCount] & 0xF0) >> 4);
                    byteCount++;
                }
            }

            return returnData;
        }

        //Set icon data from 16x16 byte grid
        private void setDataGrid(byte[,] gridData)
        {
            int byteCount = 32 + (selectedIcon * 128);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x += 2)
                {
                    iconData[byteCount] = (byte) (gridData[x, y] | (gridData[x + 1, y] << 4));
                    byteCount++;
                }
            }
        }




     


       

        
        
        public ICommand Import => ReactiveCommand.Create( async () =>
        {
           await  importIcon();
        });
        
        public ICommand Export => ReactiveCommand.Create( async () =>
        {
            await   exportIcon();
        });

     
        public ICommand HFlip => ReactiveCommand.Create( async () =>
        {
            horizontalFlip();
        });
        public ICommand VFlip => ReactiveCommand.Create( async () =>
        {
            verticalFlip();
        });
        
        public ICommand RotateLeft => ReactiveCommand.Create( async () =>
        {
            leftRotate();
        });
        public ICommand RotateRight => ReactiveCommand.Create( async () =>
        {
            rightRotate();
        });
        
      
        public ICommand Cancel => ReactiveCommand.Create<Window>((window) => { window.Close(null); });

        public ICommand Ok => ReactiveCommand.Create<Window>( (window) =>
        {
                window.Close(iconData);
        });

        public void PointerLeave()
        {
            XText = "X:";
            YText = "Y:"; 
        }

        public void PalettePonterDown(IVisual sender,PointerPressedEventArgs e)
        {
            int Xpos = (int)e.GetPosition(sender).X / 15;
            int Ypos = (int)e.GetPosition(sender).Y / 15;

            if (Xpos > 7) Xpos = 7;
            if (Ypos > 1) Ypos = 1;

            if (e.KeyModifiers == KeyModifiers.Control)
                setSelectedColor(Xpos + (Ypos * 8), 1); //Right color selector
            else
                setSelectedColor(Xpos + (Ypos * 8), 0); //Left color selector
        }
        
        
        //User has selected a pixel to draw to
        public void IconMouseMove(IVisual sender, PointerEventArgs e)
        {
            int XposOriginal = (int)e.GetPosition(sender).X / 11;
            int YposOriginal = (int)e.GetPosition(sender).Y / 11;
            int Xpos = (int)e.GetPosition(sender).X / 11;
            int Ypos = (int)e.GetPosition(sender).Y / 11;

            if (Xpos > 15) Xpos = 15;
            if (Ypos > 15) Ypos = 15;
            if (Xpos < 0) Xpos = 0;
            if (Ypos < 0) Ypos = 0;

            XText = "X: " + Xpos.ToString();
            YText = "Y: " + Ypos.ToString();
        }
        
        //User has selected a pixel to draw to
        public void IconMouseDown(IVisual sender, PointerPressedEventArgs e)
        {
            int XposOriginal = (int)e.GetPosition(sender).X / 11;
            int YposOriginal = (int)e.GetPosition(sender).Y / 11;
            int Xpos = (int)e.GetPosition(sender).X / 11;
            int Ypos = (int)e.GetPosition(sender).Y / 11;

            if (Xpos > 15) Xpos = 15;
            if (Ypos > 15) Ypos = 15;
            if (Xpos < 0) Xpos = 0;
            if (Ypos < 0) Ypos = 0;

            XText = "X: " + Xpos.ToString();
            YText = "Y: " + Ypos.ToString();

            //Draw pixels if arrow is in range and left mouse button is pressed
            if (XposOriginal >= 0 && XposOriginal <= 15 && YposOriginal >= 0
                && YposOriginal <= 15)
            {
                //Color with first selected color
                if (e.KeyModifiers  == KeyModifiers.None)
                    putPixel(Xpos, Ypos, 0);

                //Color with second selected colro
                if (e.KeyModifiers  == KeyModifiers.Control)
                    putPixel(Xpos, Ypos, 1);
            }
        }
        
        //Change selected color
        public async void PaletteDoubleClick(object sender, RoutedEventArgs e)
        {
            int selectedColorIndex = 1;

            if (!isControlDown)
                selectedColorIndex = 0;

            ColorPickerDialog colorDlg = new ColorPickerDialog();
            colorDlg.Color = new Avalonia.Media.Color(iconPalette[selectedColor[0]].A, iconPalette[selectedColor[0]].R,
                    iconPalette[selectedColor[0]].G, iconPalette[selectedColor[0]].B);
            await colorDlg.ShowDialog ((App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            //Apply selected palette color
            if (colorDlg.Color !=  new Avalonia.Media.Color(iconPalette[selectedColor[0]].A, iconPalette[selectedColor[0]].R,
                iconPalette[selectedColor[0]].G, iconPalette[selectedColor[0]].B))
            {
                //Get each color channel
                int redChannel = (colorDlg.Color.R >> 3);
                int greenChannel = (colorDlg.Color.G >> 3);
                int blueChannel = (colorDlg.Color.B >> 3);

                //Set color to iconData (convert 24 bit color to 15 bit)
                iconData[selectedColor[selectedColorIndex] * 2] = (byte) (redChannel | ((greenChannel & 0x07) << 5));
                iconData[(selectedColor[selectedColorIndex] * 2) + 1] =
                    (byte) ((blueChannel << 2) | ((greenChannel & 0x18) >> 3));

                //Draw palette to color selector
                drawPalette();

                //Update selected colors
                setSelectedColor(selectedColor[0], 0);
                setSelectedColor(selectedColor[1], 1);

                //Draw icon on the icon render
                drawIcon();
            }
        }

        private bool isControlDown = false;
        public void IconKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                isControlDown = true;
            }
        }

        public void IconKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                isControlDown = false;
            }
        }
    }
}