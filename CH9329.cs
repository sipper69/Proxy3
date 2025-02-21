using System;
using System.Collections.Generic;
using System.IO.Ports;                  // NuGet ->  System.IO.Ports 
using System.Linq;
using System.Threading.Tasks;

namespace Proxy3
{
    public class CH9329
    {
        // This class is a modified version of the class provided by SmallCodeNote under MIT License
        // https://github.com/SmallCodeNote/CH9329-109KeyClass

        public string PortName;
        public int BaudRate;
        public int MStatus = 0;
        private SerialPort serialPort;
        public bool Error = false;
        public enum MouseButtonCode : byte
        {
            LEFT = 0x01,
            RIGHT = 0x02,
            MIDDLE = 0x04,
        }
        public byte CHIP_VERSION = 0;
        public byte CHIP_STATUS = 0;
        public bool NUM_LOCK = false;
        public bool CAPS_LOCK = false;
        public bool SCROLL_LOCK = false;
        public CH9329(string PortName, int BaudRate)
        {
            Error = false;
            this.PortName = PortName;
            this.BaudRate = BaudRate;
            serialPort = new SerialPort(PortName, BaudRate);
            try { serialPort.Open(); } catch { Error = true; }
        }
        public void closeSerial()
        {
            serialPort.Close();
            serialPort.Dispose();
        }
        private string sendPacket(byte[] data)
        {
            try { serialPort.Write(data, 0, data.Length); } catch { }
            return "";
        }
        private byte[] createPacketArray(List<int> arrList, bool addCheckSum)
        {
            List<byte> bytePacketList = arrList.ConvertAll(b => (byte)b);
            if (addCheckSum) bytePacketList.Add((byte)(arrList.Sum() & 0xff));
            return bytePacketList.ToArray();
        }
        public async Task getInfo()
        {
            serialPort.DiscardOutBuffer();
            serialPort.DiscardInBuffer();

            try
            {
                List<int> getInfoPacket = new List<int> { 0x57, 0xAB, 0x00, 0x01, 0x00 };
                byte[] InfoPacket = createPacketArray(getInfoPacket, true);
                sendPacket(InfoPacket);
                await Task.Delay(100);
            }
            catch { }

            List<byte> receivedData = new List<byte>();
            byte[] extractedData = new byte[8];

            if (serialPort.BytesToRead == 14)
            {
                try
                {
                    while (serialPort.BytesToRead > 0)
                    {
                        int byteRead = serialPort.ReadByte();
                        if (byteRead != -1) { receivedData.Add((byte)byteRead); }
                    }
                    Array.Copy(receivedData.ToArray(), 5, extractedData, 0, 8);
                }
                catch { }
            }

            serialPort.DiscardOutBuffer();
            serialPort.DiscardInBuffer();

            CHIP_VERSION = (byte)extractedData[0];
            CHIP_STATUS = (byte)extractedData[1];
            byte flagByte = (byte)extractedData[2];
            NUM_LOCK = (flagByte & 0b00000001) > 0;
            CAPS_LOCK = (flagByte & 0b00000010) > 0;
            SCROLL_LOCK = (flagByte & 0b00000100) > 0;
        }
        public void keyDown(byte Decoration, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0)
        {
            List<int> keyDownPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x02, 0x08, Decoration, 0x00, k1, k2, k3, k4, k5, k6 };
            byte[] keyDownPacket = createPacketArray(keyDownPacketListInt, true);
            sendPacket(keyDownPacket);
        }
        public void keyUp(byte Decoration)
        {
            List<int> keyUpPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x02, 0x08, Decoration, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] keyUpPacket = createPacketArray(keyUpPacketListInt, true);
            sendPacket(keyUpPacket);
        }
        public async void StringTypeQWERTY(string s, bool International)
        {
            string cs = s;
            if (s.Length > 1000) { cs = s.Substring(0, 1000); }
            foreach (char c in cs)
            {
                if ((KeyMap.ASCII2QWERTY[(int)c, 1] != 0) && (((int)c < 127) || International))
                {
                    var Decoration = KeyMap.ASCII2QWERTY[(int)c, 0];
                    keyDown(Decoration, KeyMap.ASCII2QWERTY[(int)c, 1]);
                    await Task.Delay(2);
                    keyUp(0);
                    await Task.Delay(2);
                }
                if (International && (KeyMap.ASCII2QWERTY[(int)c, 3] != 0))
                {
                    var Decoration = KeyMap.ASCII2QWERTY[(int)c, 2];
                    keyDown(Decoration, KeyMap.ASCII2QWERTY[(int)c, 3]);
                    await Task.Delay(2);
                    keyUp(0);
                    await Task.Delay(2);
                }
            }
        }
        public async void StringTypeAZERTY(string s)
        {
            string cs = s;
            if (s.Length > 1000) { cs = s.Substring(0, 1000); }
            foreach (char c in cs)
            {
                if (KeyMap.ASCII2AZERTY[(int)c, 1] != 0)
                {
                    var Decoration = KeyMap.ASCII2AZERTY[(int)c, 0];
                    keyDown(Decoration, KeyMap.ASCII2AZERTY[(int)c, 1]);
                    await Task.Delay(2);
                    keyUp(0);
                    await Task.Delay(2);
                }
                if (KeyMap.ASCII2AZERTY[(int)c, 3] != 0)
                {
                    var Decoration = KeyMap.ASCII2AZERTY[(int)c, 2];
                    keyDown(Decoration, KeyMap.ASCII2AZERTY[(int)c, 3]);
                    await Task.Delay(2);
                    keyUp(0);
                    await Task.Delay(2);
                }
            }
        }
        public async void StringTypeQWERTZ(string s)
        {
            string cs = s;
            if (s.Length > 1000) { cs = s.Substring(0, 1000); }
            foreach (char c in cs)
            {
                if (KeyMap.ASCII2QWERTZ[(int)c, 1] != 0)
                {
                    var Decoration = KeyMap.ASCII2QWERTZ[(int)c, 0];
                    keyDown(Decoration, KeyMap.ASCII2QWERTZ[(int)c, 1]);
                    await Task.Delay(2);
                    keyUp(0);
                    await Task.Delay(2); ;
                }
                if (KeyMap.ASCII2QWERTZ[(int)c, 3] != 0)
                {
                    var Decoration = KeyMap.ASCII2QWERTZ[(int)c, 2];
                    keyDown(Decoration, KeyMap.ASCII2QWERTZ[(int)c, 3]);
                    await Task.Delay(2);
                    keyUp(0);
                    await Task.Delay(2);
                }
            }
        }
        public async void SyngoHotkey()
        {
            //Hotkey for the Syngo RM system to unlock Kiosk Mode -> "TAB" + "DEL" + "+"
            keyDown(0, 0x2B, 0x4C, 0x57);
            await Task.Delay(2);
            keyUp(0);
            await Task.Delay(2);
        }
        public void mouseMoveRel(int x, int y)
        {
            if (x > 127) { x = 127; }; if (x < -128) { x = -128; }; if (x < 0) { x = 0x100 + x; };
            if (y > 127) { y = 127; }; if (y < -128) { y = -128; }; if (y < 0) { y = 0x100 + y; };

            // ========================
            // mouseMoveRelPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01, 0x00}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseMoveRelPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00 };
            mouseMoveRelPacketListInt[6] = MStatus;
            mouseMoveRelPacketListInt.Add((byte)x);
            mouseMoveRelPacketListInt.Add(0);
            mouseMoveRelPacketListInt.Add(0x00);
            byte[] mouseMoveRelPacket = createPacketArray(mouseMoveRelPacketListInt, true);
            sendPacket(mouseMoveRelPacket);

            mouseMoveRelPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00 };
            mouseMoveRelPacketListInt[6] = MStatus;
            mouseMoveRelPacketListInt.Add(0);
            mouseMoveRelPacketListInt.Add((byte)y);
            mouseMoveRelPacketListInt.Add(0x00);
            mouseMoveRelPacket = createPacketArray(mouseMoveRelPacketListInt, true);
            sendPacket(mouseMoveRelPacket);
        }
        public void mouseMoveAbs(int xPos, int yPos, int xSize, int ySize)
        {
            int xAbs = (int)(4096 * xPos / xSize);
            int yAbs = (int)(4096 * yPos / ySize);

            // ========================
            // mouseMoveAbsPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x04} + LEN{0x07} + DATA{0x02, 0x00, [x],[x],[y],[y], 0x00}
            // CMD = 0x04 : USB mouse absolute mode
            // ========================

            List<int> mouseMoveAbsPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x04, 0x07, 0x02, 0x00 };
            mouseMoveAbsPacketListInt[6] = MStatus;
            mouseMoveAbsPacketListInt.Add((byte)(xAbs & 0xff));
            mouseMoveAbsPacketListInt.Add((byte)(xAbs >> 8));
            mouseMoveAbsPacketListInt.Add((byte)(yAbs & 0xff));
            mouseMoveAbsPacketListInt.Add((byte)(yAbs >> 8));
            mouseMoveAbsPacketListInt.Add(0x00);

            byte[] mouseMoveAbsPacket = createPacketArray(mouseMoveAbsPacketListInt, true);
            sendPacket(mouseMoveAbsPacket);
        }
        public void mouseButtonDown(MouseButtonCode buttonCode)
        {
            // ========================
            // mouseClickPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================

            List<int> mouseButtonDownPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00 };
            mouseButtonDownPacketListInt[6] = (int)buttonCode;
            MStatus = (int)buttonCode;

            byte[] mouseButtonDownPacket = createPacketArray(mouseButtonDownPacketListInt, true);
            sendPacket(mouseButtonDownPacket);
        }
        public void mouseButtonUpAll()
        {
            byte[] mouseButtonUpPacket = { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00, 0x0D };
            MStatus = 0;
            sendPacket(mouseButtonUpPacket);
        }
        public string mouseScroll(int scrollCount)
        {
            // ========================
            // mouseScrollPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================

            List<int> mouseScrollPacketListInt = new List<int> { 0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00 };
            mouseScrollPacketListInt[9] = scrollCount;

            byte[] mouseScrollPacket = createPacketArray(mouseScrollPacketListInt, true);
            return sendPacket(mouseScrollPacket);
        }
    }
}
