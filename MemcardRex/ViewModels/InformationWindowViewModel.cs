using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;

namespace MemcardRex.ViewModels
{
    public class InformationWindowViewModel :ViewModelBase
    {
        Bitmap[] iconData;
        int iconIndex = 0;
        int maxCount = 1;
        int iconInterpolationMode = 0;
        int iconSize = 0;
        int iconBackColor = 0;


        public string SaveTitleText { get; set; }
        public string ProductCodeText { get; set; }
        public string IdentifierText  { get; set; }
        public string SizeText { get; set; }
        public string IconFramesText { get; set; }
        public string RegionText { get; set; }
        public string SlotText { get; set; }
        
        
        private Bitmap _Icon { get; set; }
        public Bitmap Icon
        {
            get { return _Icon;}
            set
            {
                _Icon = value;
                this.RaisePropertyChanged();
            }
        }
    

        public void InitializeDialog(string saveTitle, string saveProdCode, string saveIdentifier, ushort saveRegion, int saveSize, int iconFrames, int interpolationMode, int iconPropertiesSize,  Bitmap[] saveIcons, int[] slotNumbers, int backColor)
        {
            string ocupiedSlots = null;

            iconInterpolationMode = interpolationMode;
            iconSize = iconPropertiesSize;
            SaveTitleText = saveTitle;
            ProductCodeText = saveProdCode;
            IdentifierText = saveIdentifier;
            SizeText = saveSize.ToString() + " KB";
            IconFramesText = iconFrames.ToString();
            maxCount = iconFrames;
            iconData = saveIcons;
            iconBackColor = backColor;

            //Show region string
            switch(saveRegion)
            {
                default:        //Region custom, show hex
                    RegionText = "0x" + saveRegion.ToString("X4");
                    break;

                case 0x4142:    //America
                    RegionText = "America";
                    break;

                case 0x4542:    //Europe
                    RegionText = "Europe";
                    break;

                case 0x4942:    //Japan
                    RegionText = "Japan";
                    break;
            }

            //Get ocupied slots
            for (int i = 0; i < slotNumbers.Length; i++)
            {
                ocupiedSlots += (slotNumbers[i] + 1).ToString() + ", ";
            }

            //Show ocupied slots
            SlotText = ocupiedSlots.Remove(ocupiedSlots.Length-2);

            //Draw first icon so there is no delay
            drawIcons(iconIndex);

            //Enable Paint timer in case of multiple frames
            if (iconFrames > 1)
            {
                TimerObject = DispatcherTimer.Run(()=>
                {
                    if (iconIndex < (maxCount-1)) iconIndex++; else iconIndex = 0;
                    drawIcons(iconIndex);
                    return true;
                }, TimeSpan.FromMilliseconds(180));
            }
        }

        private IDisposable TimerObject;

        public void Close()
        {
            //Disable Paint timer
            TimerObject?.Dispose();
        }
        
        //Draw scaled icons
        private void drawIcons(int selectedIndex)
        {
            Bitmap tempBitmap = new Bitmap(48, 48);
            Graphics iconGraphics = Graphics.FromImage(tempBitmap);

            //Set icon interpolation mode
            if(iconInterpolationMode == 0)iconGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            else iconGraphics.InterpolationMode = InterpolationMode.Bilinear;

            iconGraphics.PixelOffsetMode = PixelOffsetMode.Half;

            //Check what background color should be set
            switch (iconBackColor)
            {
                case 1:     //Black
                    iconGraphics.FillRegion(new SolidBrush(Color.Black), new Region(new Rectangle(0, 0, 48, 48)));
                    break;

                case 2:     //Gray
                    iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x30, 0x30, 0x30)), new Region(new Rectangle(0, 0, 48, 48)));
                    break;

                case 3:     //Blue
                    iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x44, 0x44, 0x98)), new Region(new Rectangle(0, 0, 48, 48)));
                    break;
            }

            iconGraphics.DrawImage(iconData[selectedIndex], 0, 0, 32 + (iconSize * 16), 32 + (iconSize * 16));
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Icon = tempBitmap;
                iconGraphics.Dispose();
            });
        }

        
        public ICommand Ok => ReactiveCommand.Create<Window>((window) =>
        {
            window.Close();
        });
    }
}