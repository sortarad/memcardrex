//PS1CardLink communication class (based on MemCARDuino)
//Shendo 2013

using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace PS1CardLinkCommunication
{
    public class PS1CardLink
    {
        enum PS1CLnkCommands { GETID = 0xA0, GETVER = 0xA1, MCR = 0xA2, MCW = 0xA3 };
        enum PS1CLnkResponses { ERROR = 0xE0, GOOD = 0x47, BADCHECKSUM = 0x4E, BADSECTOR = 0xFF };

        //PS1CLnk communication port
        SerialPort OpenedPort = null;

        //Contains a software version of a detected device
        string SoftwareVersion = "0.0";

        public string StartPS1CardLink(string ComPortName)
        {
            //Define a port to open
            OpenedPort = new SerialPort(ComPortName, 38400, Parity.None, 8, StopBits.One);
            OpenedPort.ReadBufferSize = 256;

            //Buffer for storing read data from the PS1CLnk
            byte[] ReadData = null;

            //Try to open a selected port (in case of an error return a descriptive string)
            try { OpenedPort.Open(); }
            catch (Exception e) { return e.Message; }

            //Check if this is PS1CLnk
            SendDataToPort((byte)PS1CLnkCommands.GETID, 100);
            ReadData = ReadDataFromPort();

            if (ReadData[0] != 'P' || ReadData[1] != 'S' || ReadData[2] != '1' || ReadData[3] != 'C' || ReadData[4] != 'L' || ReadData[5] != 'N' || ReadData[6] != 'K')
            {
                return "PS1CardLink was not detected on '" + ComPortName + "' port.";
            }

            //Get the software version
            SendDataToPort((byte)PS1CLnkCommands.GETVER, 30);
            ReadData = ReadDataFromPort();

            SoftwareVersion = (ReadData[0] >> 4).ToString() + "." + (ReadData[0] & 0xF).ToString();

            //Everything went well, PS1CLnk is ready to be used
            return null;
        }

        //Cleanly stop working with PS1CLnk
        public void StopPS1CardLink()
        {
            if (OpenedPort.IsOpen == true) OpenedPort.Close();
        }

        //Get the software version of PS1CLnk
        public string GetSoftwareVersion()
        {
            return SoftwareVersion;
        }

        //Send PS1CLnk command on the opened COM port with a delay
        private void SendDataToPort(byte Command, int Delay)
        {
            //Clear everything in the input buffer
            OpenedPort.DiscardInBuffer();

            //Send Command Byte
            OpenedPort.Write(new byte[] { Command }, 0, 1);

            //Wait for a required timeframe (for the PS1CLnk response)
            if (Delay > 0) Thread.Sleep(Delay);
        }

        //Catch the response from a PS1CLnk
        private byte[] ReadDataFromPort()
        {
            //Buffer for reading data
            byte[] InputStream = new byte[256];

            //Read data from PS1CLnk
            if (OpenedPort.BytesToRead != 0) OpenedPort.Read(InputStream, 0, 256);

            return InputStream;
        }

        //Read a specified frame of a Memory Card
        public byte[] ReadMemoryCardFrame(ushort FrameNumber)
        {
            int DelayCounter = 0;

            //Buffer for storing read data from PS1CLnk
            byte[] ReadData = null;

            //128 byte frame data from a Memory Card
            byte[] ReturnDataBuffer = new byte[128];

            byte FrameLsb = (byte)(FrameNumber & 0xFF);     //Least significant byte
            byte FrameMsb = (byte)(FrameNumber >> 8);       //Most significant byte
            byte XorData = (byte)(FrameMsb ^ FrameLsb);     //XOR variable for consistency checking

            //Read a frame from the Memory Card
            SendDataToPort((byte)PS1CLnkCommands.MCR, 0);
            SendDataToPort(FrameMsb, 0);
            SendDataToPort(FrameLsb, 0);

            //Wait for the buffer to fill
            while (OpenedPort.BytesToRead < 130 && DelayCounter < 18)
            {
                Thread.Sleep(5);
                DelayCounter++;
            }

            ReadData = ReadDataFromPort();

            //Copy recieved data
            Array.Copy(ReadData, 0, ReturnDataBuffer, 0, 128);

            //Calculate XOR checksum
            for (int i = 0; i < 128; i++)
            {
                XorData ^= ReturnDataBuffer[i];
            }

            //Return null if there is a checksum missmatch
            if (XorData != ReadData[128] || ReadData[129] != (byte)PS1CLnkResponses.GOOD) return null;

            //Return read data
            return ReturnDataBuffer;
        }

        //Write a specified frame to a Memory Card
        public bool WriteMemoryCardFrame(ushort FrameNumber, byte[] FrameData)
        {
            int DelayCounter = 0;

            //Buffer for storing read data from PS1CLnk
            byte[] ReadData = null;

            byte FrameLsb = (byte)(FrameNumber & 0xFF);     //Least significant byte
            byte FrameMsb = (byte)(FrameNumber >> 8);       //Most significant byte
            byte XorData = (byte)(FrameMsb ^ FrameLsb);     //XOR variable for consistency checking

            //Calculate XOR checksum
            for (int i = 0; i < 128; i++)
            {
                XorData ^= FrameData[i];
            }
            
            OpenedPort.DiscardInBuffer();

            //Write a frame to the Memory Card
            SendDataToPort((byte)PS1CLnkCommands.MCW, 0);
            SendDataToPort(FrameMsb, 0);
            SendDataToPort(FrameLsb, 0);
            OpenedPort.Write(FrameData, 0, 128);
            SendDataToPort(XorData, 0);                      //XOR Checksum

            //Wait for the buffer to fill
            while (OpenedPort.BytesToRead < 1 && DelayCounter < 18)
            {
                Thread.Sleep(5);
                DelayCounter++;
            }

            //Fetch PS1CLnk's response to the last command
            ReadData = ReadDataFromPort();

            if (ReadData[0x0] == (byte)PS1CLnkResponses.GOOD) return true;

            //Data was not written sucessfully
            return false;
        }
    }
}
