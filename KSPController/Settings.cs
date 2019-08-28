using KSP.IO;
using UnityEngine;

namespace KSPController
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class SettingsNStuff: MonoBehaviour
    {
        public static PluginConfiguration cfg = PluginConfiguration.CreateForType<SettingsNStuff>();
        public static string DefaultPort = "COM4";
        public static double refreshrate = 100;
        public static int HandshakeDelay = 100;
        public static int HandshakeDisable = 0;
        public static int BaudRate = 115200;
        // Throttle and axis controls have the following settings:
        // 0: The internal value (supplied by KSP) is always used.
        // 1: The external value (read from serial packet) is always used.
        // 2: If the internal value is not zero use it, otherwise use the external value.
        // 3: If the external value is not zero use it, otherwise use the internal value.
        public static int PitchEnable = 1;
        public static int RollEnable = 1;
        public static int YawEnable = 1;
        public static int TXEnable = 1;
        public static int TYEnable = 1;
        public static int TZEnable = 1;
        public static int WheelSteerEnable = 1;
        public static int ThrottleEnable = 1;
        public static int WheelThrottleEnable = 1;
        public static double SASTol = 1.0;

        void Awake()
        {
            print("KSPSerialIO: Loading settings...");

            cfg.load();
            var port = cfg.GetValue<string>("DefaultPort");
            
            if(port == null)
            {
                CreateConfig();
            }
            else
            {
                DefaultPort = port;
                print($"KSPSerialIO: DefaultPort = {DefaultPort}");

                refreshrate = cfg.GetValue<int>("refreshrate");
                print($"KSPSerialIO: Refreshrate = {refreshrate.ToString()}");

                BaudRate = cfg.GetValue<int>("BaudRate");
                print($"KSPSerialIO: BaudRate = {BaudRate.ToString()}");

                HandshakeDelay = cfg.GetValue<int>("HandshakeDelay");
                print($"KSPSerialIO: Handshake Delay = {HandshakeDelay.ToString()}");

                HandshakeDisable = cfg.GetValue<int>("HandshakeDisable");
                print($"KSPSerialIO: Handshake Disable = {HandshakeDisable.ToString()}");

                PitchEnable = cfg.GetValue<int>("PitchEnable");
                print($"KSPSerialIO: Pitch Enable = {PitchEnable.ToString()}");

                RollEnable = cfg.GetValue<int>("RollEnable");
                print($"KSPSerialIO: Roll Enable = {RollEnable.ToString()}");

                YawEnable = cfg.GetValue<int>("YawEnable");
                print($"KSPSerialIO: Yaw Enable = {YawEnable.ToString()}");

                TXEnable = cfg.GetValue<int>("TXEnable");
                print($"KSPSerialIO: Translate X Enable = {TXEnable.ToString()}");

                TYEnable = cfg.GetValue<int>("TYEnable");
                print($"KSPSerialIO: Translate Y Enable = {TYEnable.ToString()}");

                TZEnable = cfg.GetValue<int>("TZEnable");
                print($"KSPSerialIO: Translate Z Enable = {TZEnable.ToString()}");

                WheelSteerEnable = cfg.GetValue<int>("WheelSteerEnable");
                print($"KSPSerialIO: Wheel Steer Enable = {WheelSteerEnable.ToString()}");

                ThrottleEnable = cfg.GetValue<int>("ThrottleEnable");
                print($"KSPSerialIO: Throttle Enable = {ThrottleEnable.ToString()}");

                WheelThrottleEnable = cfg.GetValue<int>("WheelThrottleEnable");
                print($"KSPSerialIO: Wheel Throttle Enable = {WheelThrottleEnable.ToString()}");

                SASTol = cfg.GetValue<int>("SASTol");
                print($"KSPSerialIO: SAS Tol = {SASTol.ToString()}");
            }
        }

        private static void CreateConfig()
        {
            cfg["DefaultPort"] = DefaultPort;
            cfg["refreshrate"] = refreshrate;
            cfg["BaudRate"] = BaudRate;
            cfg["HandshakeDelay"] = HandshakeDelay;
            cfg["HandshakeDisable"] = HandshakeDisable;
            cfg["PitchEnable"] = PitchEnable;
            cfg["RollEnable"] = RollEnable;
            cfg["YawEnable"] = YawEnable;
            cfg["TXEnable"] = TXEnable;
            cfg["TYEnable"] = TYEnable;
            cfg["TZEnable"] = TZEnable;
            cfg["WheelSteerEnable"] = WheelSteerEnable;
            cfg["ThrottleEnable"] = ThrottleEnable;
            cfg["WheelThrottleEnable"] = WheelThrottleEnable;
            cfg["SASTol"] = SASTol;

            cfg.save();
        }
    }
}
