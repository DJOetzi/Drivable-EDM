using MSCLoader;
using UnityEngine;
using HutongGames.PlayMaker;
using System.IO;
using System;

namespace Drivable_EDM
{
    public class Drivable_EDM : Mod
    {
        public override string ID => "Drivable_EDM";
        public override string Name => "Drivable EDM";
        public override string Author => "Kubix & BrennFuchS";
        public override string Version => "1.4";
        public override bool UseAssetsFolder => false;
        public override bool LoadInMenu => true;

        GameObject edm;
        AxisCarController acc;
        public Drivetrain drivetrain;
        public SaveManager saveManager;
        GameObject MenuColls;

        static AdjustEDMOptions optionAdjuster;

        public override void OnMenuLoad()
        {
            AssetBundle bundle = AssetBundle.CreateFromMemoryImmediate(Properties.Resources.edmmenucolls);

            MenuColls = GameObject.Instantiate<GameObject>(bundle.LoadAsset<GameObject>("MenuColls.prefab"));
            SaveData saveData = SaveUtility.Load<SaveData>();

            MenuColls.transform.position = saveData.carPosition;
            MenuColls.transform.eulerAngles = saveData.carRotation;

            bundle.Unload(false);

            GameObject.DontDestroyOnLoad(MenuColls);
        }
        public override void OnNewGame() => SaveUtility.Remove();
        public override void OnSave() => saveManager.Save();

        public override void OnLoad()
        {
            AssetBundle ab = AssetBundle.CreateFromMemoryImmediate(Properties.Resources.edm);
            edm = GameObject.Instantiate(ab.LoadAsset<GameObject>("EDM.prefab"));
            ab.Unload(false);

            saveManager = edm.GetComponent<SaveManager>();
            GameObject.DestroyImmediate(MenuColls);
            saveManager.Load();

            acc = edm.GetComponent<AxisCarController>();
            drivetrain = edm.GetComponent<Drivetrain>();

            GameObject Shadow = GameObject.Instantiate(GameObject.Find("NPC_CARS").transform.Find("KUSKI/LOD/CarShadowProjector 3").gameObject);
            Shadow.transform.SetParent(edm.transform);
            Shadow.transform.localPosition = new Vector3(0f, 0.54f, -0.2f);
            Shadow.transform.localEulerAngles = new Vector3(90f, 90f, 0f);
            Shadow.transform.localScale = Vector3.one;
            Shadow.name = "CarShadowProjector";

            edm.AddComponent<CollisionSound>();

            //copy physics materials from hayosiko (can be any other car). (To know on what surface it will be driving)
            edm.GetComponent<CarDynamics>().physicMaterials = GameObject.Find("HAYOSIKO(1500kg, 250)").GetComponent<CarDynamics>().physicMaterials;

            //Add some skidmarks from hayosiko
            edm.GetComponent<CarDynamics>().skidmarks = GameObject.Find("HAYOSIKO(1500kg, 250)").GetComponent<CarDynamics>().skidmarks;

            #region Set up Lifts
            DrunkGuyLiftHandler liftHandler = edm.transform.Find("DrivingDoors").gameObject.AddComponent<DrunkGuyLiftHandler>();
            liftHandler.rightDoor = edm.transform.Find("DrivingDoors/Doors/saloon_front_door_right").GetComponent<Door>();
            liftHandler.driveTrigger = edm.transform.Find("PlayerTrigger/DriveTrigger").GetComponent<PlayerDriveTrigger>();
            DrunkGuyMovingHandler movingHandler = edm.transform.Find("DrivingDoors").gameObject.AddComponent<DrunkGuyMovingHandler>();
            movingHandler.rightDoor = edm.transform.Find("DrivingDoors/Doors/saloon_front_door_right").GetComponent<Door>();
            movingHandler.driveTrigger = edm.transform.Find("PlayerTrigger/DriveTrigger").GetComponent<PlayerDriveTrigger>();
            #endregion

            GameOptions();
            //edm.transform.Find("RainScript").gameObject.AddComponent<windshieldUVadjuster>();
        }

        public void GameOptions()
        {
            optionAdjuster = GameObject.Find("Systems").transform.Find("OptionsMenu").gameObject.AddComponent<AdjustEDMOptions>();
            optionAdjuster.Options = GameObject.Find("Systems/Options").GetComponents<PlayMakerFSM>()[0];
            optionAdjuster.CarController = acc;
            optionAdjuster.drivetrain = drivetrain;
            optionAdjuster.Dynamics = edm.GetComponent<CarDynamics>();
            optionAdjuster.carTrigger = edm.transform.Find("PlayerTrigger").GetComponent<PlayerCarTrigger>();
            optionAdjuster.Graphics = GameObject.Find("Systems/Options").GetComponents<PlayMakerFSM>()[1];
            optionAdjuster.driverHeadPivot = edm.transform.Find("DriverHeadPivot").GetComponent<ConfigurableJoint>();
            optionAdjuster.gearIndicator = edm.transform.Find("GearIndicator").GetComponent<GearIndicator>();
            optionAdjuster.SteeringWheel = edm.transform.Find("SteeringWheelRotation").GetComponent<SteeringWheel>();
            optionAdjuster.SetupFSMS();
        }

        public class AdjustEDMOptions : MonoBehaviour
        {
            public PlayMakerFSM Options;
            public CarDynamics Dynamics;
            public AxisCarController CarController;
            public Drivetrain drivetrain;
            public GearIndicator gearIndicator;
            public PlayerCarTrigger carTrigger;

            public PlayMakerFSM Graphics;
            public ConfigurableJoint driverHeadPivot;
            public SteeringWheel SteeringWheel;

            ForceFeedback forceFeedback;

            FsmInt FFBFactor;
            FsmFloat FFBMultiplier;
            FsmInt FFBClamp;
            FsmBool FFBInverted;

            FsmBool SteeringAid;
            FsmFloat SteeringAidMinVelo, SteeringTime, SteeringVeloTime;
            FsmFloat SteeringRotation;
            FsmBool AutoClutch;
            FsmBool HShifter;
            FsmBool Mirrors;

            FsmInt HeadBobDrive;
            FsmBool GearIndicator;

            public void SetupFSMS()
            {
                forceFeedback = drivetrain.gameObject.GetComponent<ForceFeedback>();

                FFBFactor = Options.FsmVariables.GetFsmInt("FFBFactor");
                FFBMultiplier = Options.FsmVariables.GetFsmFloat("FFBMultiplier");
                FFBClamp = Options.FsmVariables.GetFsmInt("FFBClamp");
                FFBInverted = Options.FsmVariables.GetFsmBool("FFBInverted");

                SteeringAid = Options.FsmVariables.GetFsmBool("SteeringAid");
                SteeringAidMinVelo = Options.FsmVariables.GetFsmFloat("SteeringAidMinVelo");
                SteeringTime = Options.FsmVariables.GetFsmFloat("SteeringTime");
                SteeringVeloTime = Options.FsmVariables.GetFsmFloat("SteeringVeloTime");
                SteeringRotation = Options.FsmVariables.GetFsmFloat("SteeringRotation");
                AutoClutch = Options.FsmVariables.GetFsmBool("AutoClutch");
                HShifter = Options.FsmVariables.GetFsmBool("HShifter");

                Mirrors = Graphics.FsmVariables.GetFsmBool("CarMirrors");
                HeadBobDrive = Graphics.FsmVariables.GetFsmInt("HeadBobDrive");
                GearIndicator = Graphics.FsmVariables.GetFsmBool("GearIndicator");

                AdjustOptions();
            }

            void OnDisable()
            {
                AdjustOptions();
            }

            public void AdjustOptions()
            {
                try
                {
                    // Force Feedback
                    Dynamics.enableForceFeedback = true;
                    forceFeedback.factor = FFBFactor.Value * 100;
                    forceFeedback.multiplier = FFBMultiplier.Value;
                    forceFeedback.clampValue = FFBClamp.Value;
                    forceFeedback.invertForceFeedback = FFBInverted.Value;

                    // Steering Assistance
                    CarController.steerAssistance = SteeringAid.Value;
                    CarController.smoothInput = SteeringAid.Value;
                    CarController.SteerAssistanceMinVelocity = SteeringAidMinVelo.Value;
                    CarController.steerTime = SteeringTime.Value;
                    CarController.steerReleaseTime = SteeringTime.Value;
                    CarController.veloSteerTime = SteeringVeloTime.Value;
                    CarController.veloSteerReleaseTime = SteeringVeloTime.Value;

                    // Steering wheel rotation
                    SteeringWheel.maxSteeringAngle = SteeringRotation.Value;

                    // AutoClutch
                    drivetrain.autoClutch = AutoClutch.Value;

                    // H-Shifter
                    drivetrain.shifter = HShifter.Value;

                    // HeadBob
                    driverHeadPivot.yMotion = (HeadBobDrive.Value == 2) ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
                    driverHeadPivot.angularXMotion = (HeadBobDrive.Value > 0) ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;

                    // Gear Indicator
                    gearIndicator.gearIndicatorOn = GearIndicator;
                    gearIndicator.ToggleIndicator();

                    //Mirrors
                    carTrigger.MirrorsEnabled = Mirrors;
                    carTrigger.MirrorFunction();

                    Debug.Log("EDM: Car settings applied!");
                }
                catch { }
            }
        }
    }
}