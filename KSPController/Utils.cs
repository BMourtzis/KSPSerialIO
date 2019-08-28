using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace KSPController
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VesselData
    {
        public byte id; //1
        public float AP; //2
        public float PE; //3
        public float SemiMajorAxis; //4
        public float SemiMinorAxis; //5
        public float VVI; //6
        public float e; //7
        public float inc; //8
        public float G; //9
        public int TAp; //10
        public int TPe; //11
        public float TrueAnomaly; //12
        public float Density; //13
        public int period; //14
        public float RAlt; //15
        public float Alt; //16
        public float Vsurf; //17
        public float Lat; //18
        public float Lon; //19
        public float LiquidFuelTot; //20
        public float LiquidFuel; //21
        public float OxidizerTot; //22
        public float Oxidizer; //23
        public float EChargeTot; //24
        public float ECharge; //25
        public float MonoPropTot; //26
        public float MonoProp; //27
        public float IntakeAirTot; //28
        public float IntakeAir; //29
        public float SolidFuelTot; //30
        public float SolidFuel; //31
        public float XenonGasTot; //32
        public float XenonGas; //33
        public float LiquidFuelTotS; //34
        public float LiquidFuelS; //35
        public float OxidizerTotS; //36
        public float OxidizerS; //37
        public UInt32 MissionTime; //38
        public float deltaTime; //39
        public float VOrbit; //40
        public UInt32 MNTime; //41
        public float MNDeltaV; //42
        public float Pitch; //43
        public float Roll; //44
        public float Heading; //45
        public UInt16 ActionsGroups; //46 status bit order SAS, RCS, Gear, Brakes, Abort, Custom01-10
        public byte SOINumber; //47 SOI number (decimal format sun-planet-moon, eg. 130 kerbin, 131 mun)
        public byte MaxOverHeat; //48 Max part overheart (%)
        public float MachNumber; //49
        public float IAS; //50 Indicated Air Speed
        public byte CurrentStage; //51 Current Stage Number
        public byte TotalStage; //52 TotalNumber of stages
        public float TargetDist; //53 distance of targeted veseel (m)
        public float TargetV; //54 target vessel relative veloxity (m/s)
        public byte NavballSASMode; //55 Combined byte of navball target mode and SAS mode
        // First four bits indicate AutoPilot mode:
        // 0 SAS is off  //1 = Regular Stability Assist //2 = Prograde
        // 3 = RetroGrade //4 = Normal //5 = Antinormal //6 = Radial In
        // 7 = Radial Out //8 = Target //9 = Anti-Target //10 = Maneuver node
        // Last 4 bits set navball mode. (0=ignore,1=ORBIT,2=SURFACE,3=TARGET)
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HandShakePacket
    {
        public byte id;
        public byte M1;
        public byte M2;
        public byte M3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ControlPacket
    {
        public byte id;
        public byte MainControls; //SAS RCS Lights Gear Brakes Precision Abort Stage
        public byte Mode; //0 = state, 1 = dock, 2 = map
        public ushort ControlGroup; //control groups 1-10 in 2 bytes
        public byte NavballSASMode; //AutoPilot mode
        public byte AdditionalControlByte;
        public short Pitch;
        public short Roll; //-1000 -> 1000
        public short Yaw;
        public short TX;
        public short TY;
        public short TZ;
        public short WheelSteer;
        public short Throttle;
        public short WheelThrottle;
    }

    public struct VesselControls
    {
        public bool SAS;
        public bool RCS;
        public bool Lights;
        public bool Gear;
        public bool Brakes;
        public bool Precision;
        public bool Abort;
        public bool Stage;
        public int Mode;
        public int SASMode;
        public int SpeedMode;
        public bool[] ControlGroup;
        public float Pitch;
        public float Roll;
        public float Yaw;
        public float TX;
        public float TY;
        public float TZ;
        public float WheelSteer;
        public float Throttle;
        public float WheelThrottle;

        public string ToString()
        {
            string status = "Vessel Control Status:";
            status += $"\n SAS: {SAS}";
            status += $"\n RCS: {RCS}";
            status += $"\n Lights: {Lights}";
            status += $"\n Gear: {Gear}";
            status += $"\n Brakes: {Brakes}";
            status += $"\n Precision: {Precision}";
            status += $"\n Abort: {Abort}";
            status += $"\n Stage: {Stage}";
            status += $"\n Mode: {Mode}";
            status += $"\n SASMode: {SASMode}";
            status += $"\n SpeedMode: {SpeedMode}";
            status += $"\n Pitch: {Pitch}";
            status += $"\n Roll: {Roll}";
            status += $"\n Yaw: {Yaw}";
            status += $"\n TX: {TX}";
            status += $"\n TY: {TY}";
            status += $"\n TZ: {TZ}";
            status += $"\n WheelSteer: {WheelSteer}";
            status += $"\n Throttle: {Throttle}";
            status += $"\n WheelThrottle: {WheelThrottle}";
            return status;
        }
    }

    public struct IOResource
    {
        public float Max;
        public float Current;
    }

    public enum enumAG: int
    {
        SAS = 0,
        RCS = 1,
        Light = 2,
        Gear = 3,
        Brakes = 4,
        Abort = 5,
        Custom01 = 6,
        Custom02 = 7,
        Custom03 = 8,
        Custom04 = 9,
        Custom05 = 10,
        Custom06 = 11,
        Custom07 = 12,
        Custom08 = 13,
        Custom09 = 14,
        Custom10 = 15
    }
}
