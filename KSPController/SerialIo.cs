using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace KSPController
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KSPSerialIO: MonoBehaviour
    {
        public double refreshrate = 1.0f;
        public static Vessel ActiveVessel = new Vessel();

        private double lastUpdate = 0.0f;
        private double deltaT = 1.0f;
        private double missionTime = 0;
        private double missionTimeOld = 0;
        private double theTime = 0;

        IOResource TempR = new IOResource();

        private static bool wasSASOn = false;

        private ScreenMessageStyle KSPIOScreenStyle = ScreenMessageStyle.UPPER_RIGHT;

        void Awake()
        {
            ScreenMessages.PostScreenMessage("IO Awake", 10f, KSPIOScreenStyle);
            refreshrate = SettingsNStuff.refreshrate;
        }

        void Start()
        {
            if(KSPSerialPort.DisplayFound)
            {
                if(!KSPSerialPort.Port.IsOpen)
                {
                    ScreenMessages.PostScreenMessage($"Starting serial port {KSPSerialPort.Port.PortName}", 10f, KSPIOScreenStyle);

                    try
                    {
                        KSPSerialPort.Port.Open();
                        Thread.Sleep(SettingsNStuff.HandshakeDelay);
                    }
                    catch(Exception ex)
                    {
                        ScreenMessages.PostScreenMessage($"Error opening serail port {KSPSerialPort.Port.PortName}", 10f, KSPIOScreenStyle);
                        ScreenMessages.PostScreenMessage(ex.Message, 10f, KSPIOScreenStyle);
                    }
                }
                else
                {
                    ScreenMessages.PostScreenMessage($"using serial port {KSPSerialPort.Port.PortName}", 10f, KSPIOScreenStyle);

                    if (SettingsNStuff.HandshakeDisable == 1)
                    {
                        ScreenMessages.PostScreenMessage("Handshake disabled");
                    }
                }

                Thread.Sleep(200);

                ActiveVessel.OnPostAutopilotUpdate -= AxisInput;
                ActiveVessel = FlightGlobals.ActiveVessel;
                ActiveVessel.OnPostAutopilotUpdate += AxisInput;

                //sync inputs at start
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.RCS, KSPSerialPort.VControls.RCS);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, KSPSerialPort.VControls.SAS);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Light, KSPSerialPort.VControls.Lights);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Gear, KSPSerialPort.VControls.Gear);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, KSPSerialPort.VControls.Brakes);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, KSPSerialPort.VControls.Abort);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Stage, KSPSerialPort.VControls.Stage);

                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, KSPSerialPort.VControls.ControlGroup[1]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, KSPSerialPort.VControls.ControlGroup[2]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, KSPSerialPort.VControls.ControlGroup[3]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, KSPSerialPort.VControls.ControlGroup[4]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, KSPSerialPort.VControls.ControlGroup[5]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, KSPSerialPort.VControls.ControlGroup[6]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, KSPSerialPort.VControls.ControlGroup[7]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, KSPSerialPort.VControls.ControlGroup[8]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, KSPSerialPort.VControls.ControlGroup[9]);
                ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, KSPSerialPort.VControls.ControlGroup[10]);
            }
            else
            {
                ScreenMessages.PostScreenMessage("No display found", 10f, KSPIOScreenStyle);
            }
        }

        void Update()
        {
            if(FlightGlobals.ActiveVessel != null && KSPSerialPort.Port.IsOpen)
            {
                if(ActiveVessel != null && ActiveVessel.id != FlightGlobals.ActiveVessel.id)
                {
                    ActiveVessel.OnPostAutopilotUpdate -= AxisInput;
                    ActiveVessel = FlightGlobals.ActiveVessel;
                    ActiveVessel.OnPostAutopilotUpdate += AxisInput;

                    ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.RCS, KSPSerialPort.VControls.RCS);
                    ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, KSPSerialPort.VControls.SAS);
                }
                else
                {
                    ActiveVessel = FlightGlobals.ActiveVessel;
                }

                #region outputs
                theTime = Time.unscaledTime;
                if ((theTime - lastUpdate) > refreshrate)
                {
                    //Debug.Log("KSPSerialIO: 2");

                    lastUpdate = theTime;

                    List<Part> ActiveEngines = new List<Part>();
                    ActiveEngines = GetListOfActivatedEngines(ActiveVessel);

                    KSPSerialPort.VData.AP = (float)ActiveVessel.orbit.ApA;
                    KSPSerialPort.VData.PE = (float)ActiveVessel.orbit.PeA;
                    KSPSerialPort.VData.SemiMajorAxis = (float)ActiveVessel.orbit.semiMajorAxis;
                    KSPSerialPort.VData.SemiMinorAxis = (float)ActiveVessel.orbit.semiMinorAxis;
                    KSPSerialPort.VData.e = (float)ActiveVessel.orbit.eccentricity;
                    KSPSerialPort.VData.inc = (float)ActiveVessel.orbit.inclination;
                    KSPSerialPort.VData.VVI = (float)ActiveVessel.verticalSpeed;
                    KSPSerialPort.VData.G = (float)ActiveVessel.geeForce;
                    KSPSerialPort.VData.TAp = (int)Math.Round(ActiveVessel.orbit.timeToAp);
                    KSPSerialPort.VData.TPe = (int)Math.Round(ActiveVessel.orbit.timeToPe);
                    KSPSerialPort.VData.Density = (float)ActiveVessel.atmDensity;
                    KSPSerialPort.VData.TrueAnomaly = (float)ActiveVessel.orbit.trueAnomaly;
                    KSPSerialPort.VData.period = (int)Math.Round(ActiveVessel.orbit.period);

                    //Debug.Log("KSPSerialIO: 3");
                    double ASL = ActiveVessel.mainBody.GetAltitude(ActiveVessel.CoM);
                    double AGL = (ASL - ActiveVessel.terrainAltitude);

                    if (AGL < ASL)
                        KSPSerialPort.VData.RAlt = (float)AGL;
                    else
                        KSPSerialPort.VData.RAlt = (float)ASL;

                    KSPSerialPort.VData.Alt = (float)ASL;
                    KSPSerialPort.VData.Vsurf = (float)ActiveVessel.srfSpeed;
                    KSPSerialPort.VData.Lat = (float)ActiveVessel.latitude;
                    KSPSerialPort.VData.Lon = (float)ActiveVessel.longitude;

                    TempR = GetResourceTotal(ActiveVessel, "LiquidFuel");
                    KSPSerialPort.VData.LiquidFuelTot = TempR.Max;
                    KSPSerialPort.VData.LiquidFuel = TempR.Current;

                    KSPSerialPort.VData.LiquidFuelTotS = (float)ProspectForResourceMax("LiquidFuel", ActiveEngines);
                    KSPSerialPort.VData.LiquidFuelS = (float)ProspectForResource("LiquidFuel", ActiveEngines);

                    TempR = GetResourceTotal(ActiveVessel, "Oxidizer");
                    KSPSerialPort.VData.OxidizerTot = TempR.Max;
                    KSPSerialPort.VData.Oxidizer = TempR.Current;

                    KSPSerialPort.VData.OxidizerTotS = (float)ProspectForResourceMax("Oxidizer", ActiveEngines);
                    KSPSerialPort.VData.OxidizerS = (float)ProspectForResource("Oxidizer", ActiveEngines);

                    TempR = GetResourceTotal(ActiveVessel, "ElectricCharge");
                    KSPSerialPort.VData.EChargeTot = TempR.Max;
                    KSPSerialPort.VData.ECharge = TempR.Current;
                    TempR = GetResourceTotal(ActiveVessel, "MonoPropellant");
                    KSPSerialPort.VData.MonoPropTot = TempR.Max;
                    KSPSerialPort.VData.MonoProp = TempR.Current;
                    TempR = GetResourceTotal(ActiveVessel, "IntakeAir");
                    KSPSerialPort.VData.IntakeAirTot = TempR.Max;
                    KSPSerialPort.VData.IntakeAir = TempR.Current;
                    TempR = GetResourceTotal(ActiveVessel, "SolidFuel");
                    KSPSerialPort.VData.SolidFuelTot = TempR.Max;
                    KSPSerialPort.VData.SolidFuel = TempR.Current;
                    TempR = GetResourceTotal(ActiveVessel, "XenonGas");
                    KSPSerialPort.VData.XenonGasTot = TempR.Max;
                    KSPSerialPort.VData.XenonGas = TempR.Current;

                    missionTime = ActiveVessel.missionTime;
                    deltaT = missionTime - missionTimeOld;
                    missionTimeOld = missionTime;

                    KSPSerialPort.VData.MissionTime = (UInt32)Math.Round(missionTime);
                    KSPSerialPort.VData.deltaTime = (float)deltaT;

                    KSPSerialPort.VData.VOrbit = (float)ActiveVessel.orbit.GetVel().magnitude;

                    //Debug.Log("KSPSerialIO: 4");

                    KSPSerialPort.VData.MNTime = 0;
                    KSPSerialPort.VData.MNDeltaV = 0;

                    if (ActiveVessel.patchedConicSolver != null)
                    {
                        if (ActiveVessel.patchedConicSolver.maneuverNodes != null)
                        {
                            if (ActiveVessel.patchedConicSolver.maneuverNodes.Count > 0)
                            {
                                KSPSerialPort.VData.MNTime = (UInt32)Math.Round(ActiveVessel.patchedConicSolver.maneuverNodes[0].UT - Planetarium.GetUniversalTime());
                                //KSPSerialPort.VData.MNDeltaV = (float)ActiveVessel.patchedConicSolver.maneuverNodes[0].DeltaV.magnitude;
                                KSPSerialPort.VData.MNDeltaV = (float)ActiveVessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(ActiveVessel.patchedConicSolver.maneuverNodes[0].patch).magnitude; //Added JS
                            }
                        }
                    }

                    //Debug.Log("KSPSerialIO: 5");

                    Quaternion attitude = updateHeadingPitchRollField(ActiveVessel);

                    KSPSerialPort.VData.Roll = (float)((attitude.eulerAngles.z > 180) ? (attitude.eulerAngles.z - 360.0) : attitude.eulerAngles.z);
                    KSPSerialPort.VData.Pitch = (float)((attitude.eulerAngles.x > 180) ? (360.0 - attitude.eulerAngles.x) : -attitude.eulerAngles.x);
                    KSPSerialPort.VData.Heading = (float)attitude.eulerAngles.y;

                    KSPSerialPort.ControlStatus((int)enumAG.SAS, ActiveVessel.ActionGroups[KSPActionGroup.SAS]);
                    KSPSerialPort.ControlStatus((int)enumAG.RCS, ActiveVessel.ActionGroups[KSPActionGroup.RCS]);
                    KSPSerialPort.ControlStatus((int)enumAG.Light, ActiveVessel.ActionGroups[KSPActionGroup.Light]);
                    KSPSerialPort.ControlStatus((int)enumAG.Gear, ActiveVessel.ActionGroups[KSPActionGroup.Gear]);
                    KSPSerialPort.ControlStatus((int)enumAG.Brakes, ActiveVessel.ActionGroups[KSPActionGroup.Brakes]);
                    KSPSerialPort.ControlStatus((int)enumAG.Abort, ActiveVessel.ActionGroups[KSPActionGroup.Abort]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom01, ActiveVessel.ActionGroups[KSPActionGroup.Custom01]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom02, ActiveVessel.ActionGroups[KSPActionGroup.Custom02]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom03, ActiveVessel.ActionGroups[KSPActionGroup.Custom03]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom04, ActiveVessel.ActionGroups[KSPActionGroup.Custom04]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom05, ActiveVessel.ActionGroups[KSPActionGroup.Custom05]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom06, ActiveVessel.ActionGroups[KSPActionGroup.Custom06]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom07, ActiveVessel.ActionGroups[KSPActionGroup.Custom07]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom08, ActiveVessel.ActionGroups[KSPActionGroup.Custom08]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom09, ActiveVessel.ActionGroups[KSPActionGroup.Custom09]);
                    KSPSerialPort.ControlStatus((int)enumAG.Custom10, ActiveVessel.ActionGroups[KSPActionGroup.Custom10]);

                    if (ActiveVessel.orbit.referenceBody != null)
                    {
                        KSPSerialPort.VData.SOINumber = GetSOINumber(ActiveVessel.orbit.referenceBody.name);
                    }

                    KSPSerialPort.VData.MaxOverHeat = GetMaxOverHeat(ActiveVessel);
                    KSPSerialPort.VData.MachNumber = (float)ActiveVessel.mach;
                    KSPSerialPort.VData.IAS = (float)ActiveVessel.indicatedAirSpeed;

                    KSPSerialPort.VData.CurrentStage = (byte)StageManager.CurrentStage;
                    KSPSerialPort.VData.TotalStage = (byte)StageManager.StageCount;

                    //target distance and velocity stuff                    

                    KSPSerialPort.VData.TargetDist = 0;
                    KSPSerialPort.VData.TargetV = 0;

                    if (TargetExists())
                    {
                        KSPSerialPort.VData.TargetDist = (float)Vector3.Distance(FlightGlobals.fetch.VesselTarget.GetVessel().transform.position, ActiveVessel.transform.position);
                        KSPSerialPort.VData.TargetV = (float)FlightGlobals.ship_tgtVelocity.magnitude;
                    }


                    KSPSerialPort.VData.NavballSASMode = (byte)(((int)FlightGlobals.speedDisplayMode + 1) << 4); //get navball speed display mode
                    if (ActiveVessel.ActionGroups[KSPActionGroup.SAS])
                    {
                        KSPSerialPort.VData.NavballSASMode = (byte)(((int)FlightGlobals.ActiveVessel.Autopilot.Mode + 1) | KSPSerialPort.VData.NavballSASMode);
                    }

                    KSPSerialPort.sendPacket(KSPSerialPort.VData);
                } //end refresh
                #endregion
                #region inputs
                if (KSPSerialPort.ControlReceived)
                {

                    if (KSPSerialPort.VControls.RCS != KSPSerialPort.VControlsOld.RCS)
                    {
                        //ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.RCS, KSPSerialPort.VControls.RCS);
                        KSPSerialPort.VControlsOld.RCS = KSPSerialPort.VControls.RCS;
                        //ScreenMessages.PostScreenMessage("RCS: " + KSPSerialPort.VControls.RCS.ToString(), 10f, KSPIOScreenStyle);
                    }

                    if (KSPSerialPort.VControls.SAS != KSPSerialPort.VControlsOld.SAS)
                    {
                        //ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, KSPSerialPort.VControls.SAS);
                        KSPSerialPort.VControlsOld.SAS = KSPSerialPort.VControls.SAS;
                        //ScreenMessages.PostScreenMessage("SAS: " + KSPSerialPort.VControls.SAS.ToString(), 10f, KSPIOScreenStyle);
                    }

                    if (KSPSerialPort.VControls.Lights != KSPSerialPort.VControlsOld.Lights)
                    {
                        //ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Light, KSPSerialPort.VControls.Lights);
                        KSPSerialPort.VControlsOld.Lights = KSPSerialPort.VControls.Lights;
                        //ScreenMessages.PostScreenMessage("Lights: " + KSPSerialPort.VControls.Lights.ToString(), 10f, KSPIOScreenStyle);
                    }

                    if (KSPSerialPort.VControls.Gear != KSPSerialPort.VControlsOld.Gear)
                    {
                        //ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Gear, KSPSerialPort.VControls.Gear);
                        KSPSerialPort.VControlsOld.Gear = KSPSerialPort.VControls.Gear;
                        //ScreenMessages.PostScreenMessage("SAS: " + KSPSerialPort.VControls.Gear.ToString(), 10f, KSPIOScreenStyle);
                    }

                    if (KSPSerialPort.VControls.Brakes != KSPSerialPort.VControlsOld.Brakes)
                    {
                        //ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, KSPSerialPort.VControls.Brakes);
                        KSPSerialPort.VControlsOld.Brakes = KSPSerialPort.VControls.Brakes;
                        //ScreenMessages.PostScreenMessage("Brakes: " + KSPSerialPort.VControls.Brakes.ToString(), 10f, KSPIOScreenStyle);
                    }

                    if (KSPSerialPort.VControls.Abort != KSPSerialPort.VControlsOld.Abort)
                    {
                        //ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, KSPSerialPort.VControls.Abort);
                        KSPSerialPort.VControlsOld.Abort = KSPSerialPort.VControls.Abort;
                        //ScreenMessages.PostScreenMessage("Abort: " + KSPSerialPort.VControls.Abort.ToString(), 10f, KSPIOScreenStyle);
                    }

                    if (KSPSerialPort.VControls.Stage != KSPSerialPort.VControlsOld.Stage)
                    {
                        //if (KSPSerialPort.VControls.Stage)
                        //    StageManager.ActivateNextStage();

                        //ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Stage, KSPSerialPort.VControls.Stage);
                        KSPSerialPort.VControlsOld.Stage = KSPSerialPort.VControls.Stage;
                        ScreenMessages.PostScreenMessage("Stage: " + KSPSerialPort.VControls.Stage.ToString(), 10f, KSPIOScreenStyle);
                        Debug.Log(KSPSerialPort.VControls.Stage.ToString());
                    }

                    //================ control groups

                    if (KSPSerialPort.VControls.ControlGroup[1] != KSPSerialPort.VControlsOld.ControlGroup[1])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, KSPSerialPort.VControls.ControlGroup[1]);
                        KSPSerialPort.VControlsOld.ControlGroup[1] = KSPSerialPort.VControls.ControlGroup[1];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[2] != KSPSerialPort.VControlsOld.ControlGroup[2])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, KSPSerialPort.VControls.ControlGroup[2]);
                        KSPSerialPort.VControlsOld.ControlGroup[2] = KSPSerialPort.VControls.ControlGroup[2];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[3] != KSPSerialPort.VControlsOld.ControlGroup[3])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, KSPSerialPort.VControls.ControlGroup[3]);
                        KSPSerialPort.VControlsOld.ControlGroup[3] = KSPSerialPort.VControls.ControlGroup[3];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[4] != KSPSerialPort.VControlsOld.ControlGroup[4])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, KSPSerialPort.VControls.ControlGroup[4]);
                        KSPSerialPort.VControlsOld.ControlGroup[4] = KSPSerialPort.VControls.ControlGroup[4];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[5] != KSPSerialPort.VControlsOld.ControlGroup[5])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, KSPSerialPort.VControls.ControlGroup[5]);
                        KSPSerialPort.VControlsOld.ControlGroup[5] = KSPSerialPort.VControls.ControlGroup[5];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[6] != KSPSerialPort.VControlsOld.ControlGroup[6])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, KSPSerialPort.VControls.ControlGroup[6]);
                        KSPSerialPort.VControlsOld.ControlGroup[6] = KSPSerialPort.VControls.ControlGroup[6];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[7] != KSPSerialPort.VControlsOld.ControlGroup[7])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, KSPSerialPort.VControls.ControlGroup[7]);
                        KSPSerialPort.VControlsOld.ControlGroup[7] = KSPSerialPort.VControls.ControlGroup[7];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[8] != KSPSerialPort.VControlsOld.ControlGroup[8])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, KSPSerialPort.VControls.ControlGroup[8]);
                        KSPSerialPort.VControlsOld.ControlGroup[8] = KSPSerialPort.VControls.ControlGroup[8];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[9] != KSPSerialPort.VControlsOld.ControlGroup[9])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, KSPSerialPort.VControls.ControlGroup[9]);
                        KSPSerialPort.VControlsOld.ControlGroup[9] = KSPSerialPort.VControls.ControlGroup[9];
                    }

                    if (KSPSerialPort.VControls.ControlGroup[10] != KSPSerialPort.VControlsOld.ControlGroup[10])
                    {
                        ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, KSPSerialPort.VControls.ControlGroup[10]);
                        KSPSerialPort.VControlsOld.ControlGroup[10] = KSPSerialPort.VControls.ControlGroup[10];
                    }

                    //Set sas mode
                    if (KSPSerialPort.VControls.SASMode != KSPSerialPort.VControlsOld.SASMode)
                    {
                        if (KSPSerialPort.VControls.SASMode != 0 && KSPSerialPort.VControls.SASMode < 11)
                        {
                            if (!ActiveVessel.Autopilot.CanSetMode((VesselAutopilot.AutopilotMode)(KSPSerialPort.VControls.SASMode - 1)))
                            {
                                ScreenMessages.PostScreenMessage("KSPSerialIO: SAS mode " + KSPSerialPort.VControls.SASMode.ToString() + " not avalible");
                            }
                            else
                            {
                                ActiveVessel.Autopilot.SetMode((VesselAutopilot.AutopilotMode)KSPSerialPort.VControls.SASMode - 1);
                            }
                        }
                        KSPSerialPort.VControlsOld.SASMode = KSPSerialPort.VControls.SASMode;
                    }

                    //set navball mode
                    if (KSPSerialPort.VControls.SpeedMode != KSPSerialPort.VControlsOld.SpeedMode)
                    {
                        if (!((KSPSerialPort.VControls.SpeedMode == 0) || ((KSPSerialPort.VControls.SpeedMode == 3) && !TargetExists())))
                        {
                            FlightGlobals.SetSpeedMode((FlightGlobals.SpeedDisplayModes)(KSPSerialPort.VControls.SpeedMode - 1));
                        }
                        KSPSerialPort.VControlsOld.SpeedMode = KSPSerialPort.VControls.SpeedMode;
                    }



                    if (Math.Abs(KSPSerialPort.VControls.Pitch) > SettingsNStuff.SASTol ||
                    Math.Abs(KSPSerialPort.VControls.Roll) > SettingsNStuff.SASTol ||
                    Math.Abs(KSPSerialPort.VControls.Yaw) > SettingsNStuff.SASTol)
                    {
                        //ActiveVessel.Autopilot.SAS.ManualOverride(true); 

                        if ((ActiveVessel.Autopilot.SAS.lockedMode == true) && (wasSASOn == false))
                        {
                            wasSASOn = true;
                        }
                        else if (wasSASOn != true)
                        {
                            wasSASOn = false;
                        }

                        if (wasSASOn == true)
                        {
                            ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                            //ActiveVessel.Autopilot.SAS.lockedMode = false;
                            //ActiveVessel.Autopilot.SAS.dampingMode = true;
                        }
                        /*                                              
                        
                        if (KSPSerialPort.VControls.SAS == true)
                        {
                            KSPSerialPort.VControls.SAS = false;
                            KSPSerialPort.VControlsOld.SAS = false;
                        }
                         */
                        //KSPSerialPort.VControlsOld.Pitch = KSPSerialPort.VControls.Pitch;
                        //KSPSerialPort.VControlsOld.Roll = KSPSerialPort.VControls.Roll;
                        //KSPSerialPort.VControlsOld.Yaw = KSPSerialPort.VControls.Yaw;
                    }
                    else
                    {
                        if (wasSASOn == true)
                        {
                            wasSASOn = false;
                            ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                            //ActiveVessel.Autopilot.SAS.lockedMode = true;
                            //ActiveVessel.Autopilot.SAS.dampingMode = false;
                        }
                    }

                    KSPSerialPort.ControlReceived = false;
                } //end ControlReceived
                #endregion
            }
        }

        #region utils

        private bool TargetExists()
        {
            return (FlightGlobals.fetch.VesselTarget != null) && (FlightGlobals.fetch.VesselTarget.GetVessel() != null);
        }

        private byte GetMaxOverHeat(Vessel V)
        {
            double sPercent, iPercent, percentD = 0, percentP;

            foreach (Part p in ActiveVessel.parts)
            {
                iPercent = p.temperature / p.maxTemp;
                sPercent = p.skinTemperature / p.skinMaxTemp;

                if(iPercent > sPercent)
                {
                    percentP = iPercent;
                }
                else
                {
                    percentP = sPercent;
                }

                if(percentD < percentP)
                {
                    percentD = percentP;
                }
            }

            return (byte)Math.Round(percentD * 100);
        }

        private IOResource GetResourceTotal(Vessel V, string resourceName)
        {
            IOResource R = new IOResource();

            foreach (Part p in V.parts)
            {
                foreach(PartResource pr in p.Resources)
                {
                    if(pr.resourceName.Equals(resourceName))
                    {
                        R.Current += (float)pr.amount;
                        R.Max += (float)pr.maxAmount;

                        break;
                    }
                }
            }

            if(R.Max == 0)
            {
                R.Current = 0;
            }

            return R;
        }

        private void AxisInput(FlightCtrlState s)
        {
            switch(SettingsNStuff.ThrottleEnable)
            {
                case 1:
                    s.mainThrottle = KSPSerialPort.VControls.Throttle;
                    break;
                case 2:
                    if(s.mainThrottle == 0)
                    {
                        s.mainThrottle = KSPSerialPort.VControls.Throttle;
                    }
                    break;
                case 3:
                    if(KSPSerialPort.VControls.Throttle != 0)
                    {
                        s.mainThrottle = KSPSerialPort.VControls.Throttle;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.PitchEnable)
            {
                case 1:
                    s.pitch = KSPSerialPort.VControls.Pitch;
                    break;
                case 2:
                    if (s.pitch == 0)
                    {
                        s.pitch = KSPSerialPort.VControls.Pitch;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.Pitch != 0)
                    {
                        s.pitch = KSPSerialPort.VControls.Pitch;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.RollEnable)
            {
                case 1:
                    s.roll = KSPSerialPort.VControls.Roll;
                    break;
                case 2:
                    if (s.roll == 0)
                    {
                        s.roll = KSPSerialPort.VControls.Roll;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.Roll != 0)
                    {
                        s.roll = KSPSerialPort.VControls.Roll;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.YawEnable)
            {
                case 1:
                    s.yaw = KSPSerialPort.VControls.Yaw;
                    break;
                case 2:
                    if (s.yaw == 0)
                    {
                        s.yaw = KSPSerialPort.VControls.Yaw;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.Yaw != 0)
                    {
                        s.yaw = KSPSerialPort.VControls.Yaw;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.TXEnable)
            {
                case 1:
                    s.X = KSPSerialPort.VControls.TX;
                    break;
                case 2:
                    if (s.X == 0)
                    {
                        s.X = KSPSerialPort.VControls.TX;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.TX != 0)
                    {
                        s.X = KSPSerialPort.VControls.TX;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.TYEnable)
            {
                case 1:
                    s.Y = KSPSerialPort.VControls.TY;
                    break;
                case 2:
                    if (s.Y == 0)
                    {
                        s.Y = KSPSerialPort.VControls.TY;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.Throttle != 0)
                    {
                        s.Y = KSPSerialPort.VControls.Throttle;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.TZEnable)
            {
                case 1:
                    s.Z = KSPSerialPort.VControls.TZ;
                    break;
                case 2:
                    if (s.Z == 0)
                    {
                        s.Z = KSPSerialPort.VControls.TZ;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.TZ != 0)
                    {
                        s.Z = KSPSerialPort.VControls.TZ;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.WheelSteerEnable)
            {
                case 1:
                    s.wheelSteer = KSPSerialPort.VControls.WheelSteer;
                    break;
                case 2:
                    if (s.wheelSteer == 0)
                    {
                        s.wheelSteer = KSPSerialPort.VControls.WheelSteer;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.WheelSteer != 0)
                    {
                        s.wheelSteer = KSPSerialPort.VControls.WheelSteer;
                    }
                    break;
                default:
                    break;
            }

            switch (SettingsNStuff.WheelThrottleEnable)
            {
                case 1:
                    s.wheelThrottle = KSPSerialPort.VControls.WheelThrottle;
                    break;
                case 2:
                    if (s.wheelThrottle == 0)
                    {
                        s.wheelThrottle = KSPSerialPort.VControls.WheelThrottle;
                    }
                    break;
                case 3:
                    if (KSPSerialPort.VControls.WheelThrottle != 0)
                    {
                        s.wheelThrottle = KSPSerialPort.VControls.WheelThrottle;
                    }
                    break;
                default:
                    break;
            }
        }

        private byte GetSOINumber(string name)
        {
            byte SOI;

            switch (name.ToLower())
            {
                case "sum":
                    SOI = 100;
                    break;
                case "moho":
                    SOI = 110;
                    break;
                case "eve":
                    SOI = 120;
                    break;
                case "gilly":
                    SOI = 121;
                    break;
                case "kerbin":
                    SOI = 130;
                    break;
                case "mun":
                    SOI = 131;
                    break;
                case "minmus":
                    SOI = 132;
                    break;
                case "duna":
                    SOI = 140;
                    break;
                case "ike":
                    SOI = 141;
                    break;
                case "dres":
                    SOI = 150;
                    break;
                case "jool":
                    SOI = 160;
                    break;
                case "laythe":
                    SOI = 161;
                    break;
                case "vall":
                    SOI = 162;
                    break;
                case "tylo":
                    SOI = 163;
                    break;
                case "bop":
                    SOI = 164;
                    break;
                case "pol":
                    SOI = 165;
                    break;
                case "eeloo":
                    SOI = 170;
                    break;
                default:
                    SOI = 0;
                    break;
            }

            return SOI;
        }

        public static List<Part> GetListOfActivatedEngines(Vessel vessel)
        {
            var retList = new List<Part>();

            foreach(var part in vessel.Parts)
            {
                foreach(PartModule module in part.Modules)
                {
                    var engineModule = module as ModuleEngines;
                    if(engineModule != null && engineModule.getIgnitionState)
                    {
                        retList.Add(part);
                    }

                    var engineModuleFx = module as ModuleEnginesFX;
                    if(engineModuleFx != null && engineModuleFx.getIgnitionState)
                    {
                        retList.Add(part);
                    }
                }
            }

            return retList;
        }

        public static double ProspectForResource(string resourceName, List<Part>engines)
        {
            var visited = new List<Part>();
            double total = 0;

            foreach(var part in engines)
            {
                total += ProspectForResource(resourceName, part, ref visited);
            }

            return total;
        }

        public static double ProspectForResource(string resourceName, Part engine)
        {
            var visited = new List<Part>();

            return ProspectForResource(resourceName, engine, ref visited);
        }

        public static double ProspectForResource(string resourceName, Part part, ref List<Part> visited)
        {
            double ret = 0;

            if (visited.Contains(part))
            {
                return 0;
            }

            visited.Add(part);

            foreach(var resource in part.Resources)
            {
                if(resource.resourceName.ToLower() == resourceName.ToLower())
                {
                    ret += resource.amount;
                }
            }

            foreach( var attachNode in part.attachNodes)
            {
                if(ValidAttachNode(attachNode, part))
                {
                    ret += ProspectForResource(resourceName, attachNode.attachedPart, ref visited);
                }
            }

            return ret;
        }

        public static double ProspectForResourceMax(string resourceName, List<Part> engines)
        {
            var visited = new List<Part>();
            double total = 0;

            foreach(var part in engines)
            {
                total += ProspectForResourceMax(resourceName, part, ref visited);
            }

            return total;
        }

        public static double ProspectForResourceMax(string resourceName, Part part, ref List<Part> visited)
        {
            double ret = 0;

            if(visited.Contains(part))
            {
                return 0;
            }

            visited.Add(part);

            foreach(var resource in part.Resources)
            {
                if(resource.resourceName.ToLower() == resourceName.ToLower())
                {
                    ret += resource.maxAmount;
                }
            }

            foreach(var attachNode in part.attachNodes)
            {
                if(ValidAttachNode(attachNode, part))
                {
                    ret += ProspectForResourceMax(resourceName, attachNode.attachedPart, ref visited);
                }
            }

            return ret;
        }

        private static bool ValidAttachNode(AttachNode attachNode, Part part)
        {
            return attachNode.attachedPart
                && attachNode.nodeType == AttachNode.NodeType.Stack
                && attachNode.attachedPart.fuelCrossFeed
                && !(
                    part.NoCrossFeedNodeKey.Length > 0 
                    && attachNode.id.Contains(part.NoCrossFeedNodeKey)
                );
        }

        private Quaternion updateHeadingPitchRollField(Vessel v)
        {
            Vector3d CoM, north, up;
            Quaternion rotationSurface;

            CoM = v.CoM;
            up = (CoM - v.mainBody.position).normalized;
            north = Vector3d.Exclude(up, (v.mainBody.position + v.mainBody.transform.up * (float)v.mainBody.Radius) - CoM).normalized;
            rotationSurface = Quaternion.LookRotation(north, up);
            return Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(v.GetTransform().rotation) * rotationSurface);
        }

        #endregion

        void OnDestroy()
        {
            if(KSPSerialPort.Port.IsOpen)
            {
                KSPSerialPort.PortCleanup();
                ScreenMessages.PostScreenMessage("Port closed", 10f, KSPIOScreenStyle);
            }

            ActiveVessel.OnFlyByWire -= new FlightInputCallback(AxisInput);
        }

    }
}
