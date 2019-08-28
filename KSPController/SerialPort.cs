using Microsoft.Win32;
using Psimax.IO.Ports;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace KSPController
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KSPSerialPort: MonoBehaviour
    {
        public static SerialPort Port;
        public static string PortNumber;
        public static bool DisplayFound = false;
        public static bool ControlReceived = false;

        public static VesselData VData;
        public static ControlPacket CPacket;
        public static HandShakePacket HPacket;

        public static VesselControls VControls = new VesselControls();
        public static VesselControls VControlsOld = new VesselControls();

        private const int MaxPayloadSize = 255;

        enum ReceiveStates : byte
        {
            FIRSTHEADER, // Waiting for the first header
            SECONDHEADER, // Waiting for the second header
            SIZE, // Waiting for the payload size
            PAYLOAD, // Waiting for the rest of the payload
            CS // Waiting for the checksum
        }

        private static ReceiveStates CurrentState = ReceiveStates.FIRSTHEADER;
        private static byte CurrentPacketLegnth;
        private static byte CurrentBytesRead;
        //Guards access to data shared between threads
        private static Mutex SerialMutex = new Mutex();
        //Serial worker uses this buffer to read bytes
        private static byte[] PayloadBuffer = new byte[MaxPayloadSize];
        //Buffer for sharing packets from serial worker to main thread
        private static volatile bool NewPacketFlag = false;
        private static volatile byte[] NewPacketBuffer = new byte[MaxPayloadSize];
        //Semaphore to indicate whether the serial worker should do work
        private static volatile bool doSerialRead = true;
        private static Thread SerialThread;

        private const byte HSPid = 0, VDid = 1, Cid = 101;//hard coded values for packet IDS

        public static void InboundPacketHandler()
        {
            SerialMutex.WaitOne();
            NewPacketFlag = false;
            //Debug.Log($"KSPSerialIO: Packet Flag: {NewPacketBuffer[0]}");

            switch (NewPacketBuffer[0])
            {
                case HSPid:
                    HPacket = (HandShakePacket)ByteArrayToStructure(NewPacketBuffer, HPacket);
                    SerialMutex.ReleaseMutex();
                    HandShakeMessage();
                    if (HPacket.M1 == 3 && HPacket.M2 == 1 && HPacket.M3 == 4)
                    {
                        Debug.Log("KSPSerialIO: Display Found");
                        DisplayFound = true;
                    }
                    else
                    {
                        Debug.Log("KSPSerialIO: Display Not Found");
                        DisplayFound = false;
                    }
                    break;
                case Cid:
                    CPacket = (ControlPacket)ByteArrayToStructure(PayloadBuffer, CPacket);
                    SerialMutex.ReleaseMutex();
                    VesselControls();
                    break;
                default:
                    SerialMutex.ReleaseMutex();
                    Debug.Log("KSPSerialiIO : Packet id unimplemented");
                    break;
            }
        }

        public static void sendPacket(object anything)
        {
            var Payload = StructureToByteArray(anything);
            byte header1 = 0xBE;
            byte header2 = 0xEF;
            var size = (byte)Payload.Length;
            var checksum = size;

            var packet = new byte[size + 4];

            for (int i = 0; i < size; i++)
            {
                checksum ^= Payload[i];
            }

            Payload.CopyTo(packet, 3);
            packet[0] = header1;
            packet[1] = header2;
            packet[2] = size;
            packet[packet.Length - 1] = checksum;

            Port.Write(packet, 0, packet.Length);
        }

        private void Begin()
        {
            Port = new SerialPort(SettingsNStuff.DefaultPort, SettingsNStuff.BaudRate, Parity.None, 8, StopBits.One);
            SerialThread = new Thread(SerialWorker);
            SerialThread.Start();
            while (!SerialThread.IsAlive) ;
        }

        private void Update()
        {
            if (NewPacketFlag)
            {
                InboundPacketHandler();
            }
        }

        private void SerialWorker()
        {
            var buffer = new byte[MaxPayloadSize + 4];
            Action SerialRead = null;
            Debug.Log("KSPSeriaIO:Serial Worker thread started");
            SerialRead = delegate
            {
                try
                {
                    Port.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar)
                    {
                        try
                        {
                            var actualLength = Port.BaseStream.EndRead(ar);
                            var received = new byte[actualLength];
                            Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
                            ReceivedDataEvent(received, actualLength);
                        }
                        catch (IOException ex)
                        {
                            Debug.Log("IOException in Serial Worker :(");
                            Debug.Log(ex.ToString());
                        }
                    }, null);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.Log("KSPSerialIO: Trying to read port that isn't open. Sleeping");
                    Thread.Sleep(500);
                }
            };

            doSerialRead = true;
            while (doSerialRead)
            {
                SerialRead();
            }
            Debug.Log("KSPSerialIO: Serial worker thread shutting down");
        }

        private void ReceivedDataEvent(byte[] ReadBuffer, int BufferLegnth)
        {
            for (int x = 0; x < BufferLegnth; x++)
            {
                switch (CurrentState)
                {
                    case ReceiveStates.FIRSTHEADER:
                        if (ReadBuffer[x] == 0xBE)
                        {
                            CurrentState = ReceiveStates.SECONDHEADER;
                        }
                        break;
                    case ReceiveStates.SECONDHEADER:
                        if (ReadBuffer[x] == 0xEF)
                        {
                            CurrentState = ReceiveStates.SIZE;
                        }
                        else
                        {
                            CurrentState = ReceiveStates.FIRSTHEADER;
                        }
                        break;
                    case ReceiveStates.SIZE:
                        CurrentPacketLegnth = ReadBuffer[x];
                        CurrentBytesRead = 0;
                        CurrentState = ReceiveStates.PAYLOAD;
                        break;
                    case ReceiveStates.PAYLOAD:
                        PayloadBuffer[CurrentBytesRead] = ReadBuffer[x];
                        CurrentBytesRead++;
                        if (CurrentBytesRead == CurrentPacketLegnth)
                        {
                            CurrentState = ReceiveStates.CS;
                        }
                        break;
                    case ReceiveStates.CS:
                        if (CompareChecksum(ReadBuffer[x]))
                        {
                            SerialMutex.WaitOne();
                            Buffer.BlockCopy(PayloadBuffer, 0, NewPacketBuffer, 0, CurrentBytesRead);
                            NewPacketFlag = true;
                            SerialMutex.ReleaseMutex();
                            //Seedy hack:  Handshake happens during scene
                            //load before Update() is ever called
                            if (!DisplayFound)
                            {
                                InboundPacketHandler();
                            }
                        }
                        CurrentState = ReceiveStates.FIRSTHEADER;
                        break;
                }
            }
        }

        private static bool CompareChecksum(byte readCS)
        {
            byte calcCS = CurrentPacketLegnth;
            for (int i = 0; i < CurrentPacketLegnth; i++)
            {
                calcCS ^= PayloadBuffer[i];
            }

            return (calcCS == readCS);
        }

        private static byte[] StructureToByteArray(object obj)
        {
            var len = Marshal.SizeOf(obj);
            var arr = new byte[len];
            var ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        private static object ByteArrayToStructure(byte[] bytearray, object obj)
        {
            var len = Marshal.SizeOf(obj);
            var i = Marshal.AllocHGlobal(len);
            Marshal.Copy(bytearray, 0, i, len);
            obj = Marshal.PtrToStructure(i, obj.GetType());
            Marshal.FreeHGlobal(i);

            return obj;
        }

        private void initializeDataPackets()
        {
            VData = new VesselData()
            {
                id = VDid
            };

            HPacket = new HandShakePacket()
            {
                id = HSPid,
                M1 = 1,
                M2 = 2,
                M3 = 3
            };

            CPacket = new ControlPacket();

            VControls.ControlGroup = new bool[11];
            VControlsOld.ControlGroup = new bool[11];
        }

        void Awake()
        {
            if (DisplayFound)
            {
                Debug.Log("KSPSerialIO: running...");
                Begin();
            }
            else
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Debug.Log($"KSPSerialIO: Version {version}");
                Debug.Log("KSPSerialIO: Getting serial ports ...");
                Debug.Log($"KSPSerialIO: Output packet size: {Marshal.SizeOf(VData).ToString()} {MaxPayloadSize}");
                initializeDataPackets();

                try
                {
                    var SerialCOMSKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\\DEVICEMAP\\SERIALCOMM\\");

                    Begin();

                    string[] PortNames;
                    if (SerialCOMSKey == null)
                    {
                        Debug.Log("KSPSerialIO: Bro, do you even win32 serial port??");
                        PortNames = new string[1];
                        PortNames[0] = SettingsNStuff.DefaultPort;
                    }
                    else
                    {
                        var realports = SerialCOMSKey.GetValueNames();
                        PortNames = new string[realports.Length + 1];
                        realports.CopyTo(PortNames, 1);
                    }

                    Debug.Log($"KSPSerialIO: Found {PortNames.Length} serial ports");

                    for (int j = 0; j < PortNames.Length; j++)
                    {
                        if (j == 0)
                        {
                            PortNumber = SettingsNStuff.DefaultPort;
                            Debug.Log($"KSPSerialIO: trying default port {PortNumber}");
                        }
                        else
                        {
                            PortNumber = (string)SerialCOMSKey.GetValue(PortNames[j]);
                            Debug.Log($"KSPSerialIO: trying port {PortNames[j]} - {PortNumber}");
                        }

                        Port.PortName = PortNumber;

                        if (!Port.IsOpen)
                        {
                            try
                            {
                                Port.Open();
                            }
                            catch (Exception ex)
                            {
                                Debug.Log($"Error opening Serial Port {Port.PortName}:  {ex.Message}");
                            }

                            if (Port.IsOpen && (SettingsNStuff.HandshakeDisable == 0))
                            {
                                Thread.Sleep(SettingsNStuff.HandshakeDelay);

                                sendPacket(HPacket);

                                int k = 0;
                                while (k < 15 && !DisplayFound)
                                {
                                    Thread.Sleep(100);
                                    k++;
                                }

                                Port.Close();
                                if (DisplayFound)
                                {
                                    Debug.Log($"KSPSerialIO: found KSP Display at {Port.PortName}");
                                }
                                else
                                {
                                    Debug.Log("KSPSerialIO: KSP Display not found");
                                }
                            }
                            else if (Port.IsOpen && (SettingsNStuff.HandshakeDisable == 1))
                            {
                                DisplayFound = true;
                                Debug.Log($"KSPSerialIO: Handshake Disabled, using {Port.PortName}");
                            }
                        }
                        else
                        {
                            Debug.Log($"KSPSerialIO: {PortNumber} is already being used");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                }
            }
        }

        private static void HandShakeMessage()
        {
            Debug.Log($"KSPSerialIO: Handshake received - {HPacket.M1.ToString()} {HPacket.M2.ToString()} {HPacket.M3.ToString()}");
        }

        private static void VesselControls()
        {
            //Debug.Log(CPacket.MainControls.ToString());
            VControls.SAS = BitMathByte(CPacket.MainControls, 7);
            VControls.RCS = BitMathByte(CPacket.MainControls, 6);
            VControls.Lights = BitMathByte(CPacket.MainControls, 5);
            VControls.Gear = BitMathByte(CPacket.MainControls, 4);
            VControls.Brakes = BitMathByte(CPacket.MainControls, 3);
            VControls.Precision = BitMathByte(CPacket.MainControls, 2);
            VControls.Abort = BitMathByte(CPacket.MainControls, 1);
            VControls.Stage = BitMathByte(CPacket.MainControls, 0);
            VControls.Pitch = CPacket.Pitch / 1000.0F;
            VControls.Roll = CPacket.Roll / 1000.0F;
            VControls.Yaw = CPacket.Yaw / 1000.0F;
            VControls.TX = CPacket.TX / 1000.0F;
            VControls.TY = CPacket.TY / 1000.0F;
            VControls.WheelSteer = CPacket.WheelSteer / 1000.0F;
            VControls.Throttle = CPacket.Throttle / 1000.0F;
            VControls.WheelThrottle = CPacket.WheelThrottle / 1000.0F;
            VControls.SASMode = CPacket.NavballSASMode & 0x0F;
            VControls.SpeedMode = CPacket.NavballSASMode >> 4;


            for (int j = 1; j <= 10; j++)
            {
                VControls.ControlGroup[j] = BitMathUshort(CPacket.ControlGroup, j);
            }

            ControlReceived = true;
        }


        #region BitMath

        private static bool BitMathByte(byte x, int n)
        {
            return ((x >> n) & 1) == 1;
        }

        private static bool BitMathUshort(ushort x, int n)
        {
            var val = ((x >> n) & 1) == 1;
            return val;
        }

        #endregion

        public static void ControlStatus(int n, bool s)
        {
            if (s)
            {
                VData.ActionsGroups |= (UInt16)(1 << n); // forces nth bit of x to be 1. all other bits left alone
            }
            else
            {
                VData.ActionsGroups &= (UInt16)~(1 << n); // forces nth bit of x to be 0. all ther bits left alone
            }
        }

        public static void PortCleanup()
        {
            if (Port.IsOpen)
            {
                doSerialRead = false;
                Port.Close();
                Port.Dispose();
                Debug.Log("KSPSerialIO: Port closed");
            }
        }
    }
}
