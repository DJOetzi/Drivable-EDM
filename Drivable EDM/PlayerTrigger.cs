using HutongGames.PlayMaker;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Drivable_EDM
{
    public class GearIndicator : MonoBehaviour
    {
        public Drivetrain drivetrain;
        GameObject gearIndicator;
        TextMesh[] gearIndicatorText;

        string[] gears = new string[] { "R", "N", "1", "2", "3", "4", "5" };

        int tempGear = -1;
        public FsmBool gearIndicatorOn;

        public void Awake()
        {
            gearIndicator = Instantiate(GameObject.Find("GUI/Indicators").transform.Find("Gear").gameObject);
            gearIndicator.name = "EDMgear";
            gearIndicator.transform.SetParent(GameObject.Find("GUI/Indicators").transform);
            gearIndicator.transform.localPosition = new Vector3(12f, 0.21f, 0f);
            gearIndicator.transform.localEulerAngles = Vector3.zero;
            gearIndicator.transform.localScale = Vector3.one;

            Destroy(gearIndicator.GetComponent<PlayMakerFSM>());
            gearIndicatorText = gearIndicator.GetComponentsInChildren<TextMesh>(true);

            gameObject.SetActive(false);
        }

        void Update()
        {
            if (tempGear != drivetrain.gear)
            {
                tempGear = drivetrain.gear;
                gearIndicatorText[0].text = gears[tempGear];
                gearIndicatorText[1].text = gears[tempGear];
            }
        }

        void OnEnable()
        {
            gearIndicator.SetActive(gearIndicatorOn.Value);

            tempGear = drivetrain.gear;

            gearIndicatorText[0].text = gears[tempGear];
            gearIndicatorText[1].text = gears[tempGear];
        }

        void OnDisable()
        {
            if (gearIndicator != null) gearIndicator.SetActive(false);
        }

        public void ToggleIndicator()
        {
            if (gameObject.activeInHierarchy)
            {
                gearIndicator.SetActive(gearIndicatorOn.Value);
            }
        }
    }

    public class PlayerCarTrigger : MonoBehaviour
    {
        public InteractionRaycast Interaction;

        GameObject player;
        bool playerInCar = false;
        FsmBool FsmPlayerInCar;

        public GameObject driveTrigger;

        PlayMakerFSM MirrorSystem;
        Transform Disable;

        public Texture2D baseTex;

        public GameObject ambientVolume;

        public FsmBool MirrorsEnabled;

        public Transform leftMirror, leftMirrorCamera, leftMirrorCameraPivot;
        public Transform rightMirror, rightMirrorCamera, rightMirrorCameraPivot;

        void Start()
        {
            player = GameObject.Find("PLAYER");
            FsmPlayerInCar = player.GetComponents<PlayMakerFSM>()[1].FsmVariables.FindFsmBool("PlayerInCar");

            Disable = GameObject.Find("Systems").transform.Find("Mirrors/Disable");
            MirrorSystem = GameObject.Find("Systems").transform.Find("Mirrors").GetComponent<PlayMakerFSM>();

            Setup();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == player && !playerInCar && !FsmPlayerInCar.Value)
            {
                playerInCar = true;
                ambientVolume.SetActive(true);
                FsmPlayerInCar.Value = true;
                driveTrigger.SetActive(true);
                MirrorFunction();
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject == player)
            {
                playerInCar = false;
                ambientVolume.SetActive(false);
                FsmPlayerInCar.Value = false;
                driveTrigger.SetActive(false);
                MirrorReset();
            }
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.F3)) MirrorFunction();
        }

        void Setup()
        {
            rightMirrorCamera = MirrorSystem.FsmVariables.GetFsmGameObject("Mirror1").Value.transform;
            leftMirrorCamera = MirrorSystem.FsmVariables.GetFsmGameObject("Mirror2").Value.transform;

            MirrorReset();
        }

        public void MirrorFunction()
        {
            if (MirrorsEnabled.Value && playerInCar)
            {
                leftMirrorCamera.SetParent(leftMirrorCameraPivot);
                leftMirrorCamera.localPosition = Vector3.zero;
                leftMirrorCamera.localEulerAngles = Vector3.zero;

                rightMirrorCamera.SetParent(rightMirrorCameraPivot);
                rightMirrorCamera.localPosition = Vector3.zero;
                rightMirrorCamera.localEulerAngles = Vector3.zero;

                leftMirror.GetComponent<MeshRenderer>().materials[2].mainTexture = leftMirrorCamera.GetComponent<Camera>().targetTexture;
                rightMirror.GetComponent<MeshRenderer>().materials[2].mainTexture = rightMirrorCamera.GetComponent<Camera>().targetTexture;
            }

            if(!MirrorsEnabled.Value)
            {
                MirrorReset();
            }
        }

        public void MirrorReset()
        {
            Disable.gameObject.SetActive(true);

            leftMirrorCamera.SetParent(Disable, false);
            leftMirrorCamera.localEulerAngles = Vector3.zero;
            rightMirrorCamera.SetParent(Disable, false);
            rightMirrorCamera.localEulerAngles = Vector3.zero;

            leftMirror.GetComponent<MeshRenderer>().materials[2].mainTexture = baseTex;
            rightMirror.GetComponent<MeshRenderer>().materials[2].mainTexture = baseTex;

            Disable.gameObject.SetActive(false);
        }
    }

    public class DragRaceHandler : MonoBehaviour
    {
        FsmString Car;
        FsmInt ID;
        FsmString Name;

        FsmString PlayerName;

        PlayMakerArrayListProxy[] proxyLists;

        void Awake()
        {
            GameObject stagingWheel = Instantiate(GameObject.Find("HAYOSIKO(1500kg, 250)").transform.Find("StagingWheel").gameObject);
            stagingWheel.name = "StagingWheel";
            stagingWheel.transform.parent = transform.root;
            stagingWheel.transform.localPosition = new Vector3(0f, 0.74f, 1.21f);
            stagingWheel.transform.localEulerAngles = Vector3.zero;
            stagingWheel.transform.localScale = Vector3.one;

            PlayMakerFSM data = stagingWheel.GetComponent<PlayMakerFSM>();
            Car = data.FsmVariables.FindFsmString("Car");
            ID = data.FsmVariables.FindFsmInt("ID");
            Name = data.FsmVariables.FindFsmString("Name");

            Car.Value = "'90 EDM 500LX";
            ID.Value = 14;

            PlayerName = PlayMakerGlobals.Instance.Variables.FindFsmString("PlayerName");

            string[] stringProxies = new string[] { "Speeds", "Cars", "Names", "ResultsTimes", "ResultsSpeeds", "ResultsNames", "ResultsCars" };
            proxyLists = GameObject.Find("DRAGRACE").transform.Find("LOD/DRAGSTRIP/DragTiming").GetComponents<PlayMakerArrayListProxy>();
            foreach (PlayMakerArrayListProxy proxy in proxyLists)
            {
                if (proxy.referenceName == "Times")
                {
                    proxy.preFillCount++;
                    proxy.preFillFloatList.Add(0f);
                    if (proxy._arrayList != null && proxy._arrayList.Count > 0) proxy._arrayList.Add(0f);

                    continue;
                }
                if (stringProxies.Contains(proxy.referenceName))
                {
                    proxy.preFillCount++;
                    proxy.preFillStringList.Add("");
                    if (proxy._arrayList != null && proxy._arrayList.Count > 0) proxy._arrayList.Add("");

                    continue;
                }
            }

            PlayMakerHashTableProxy hashTable = GameObject.Find("DRAGRACE").transform.Find("LOD/DRAGSTRIP/DragTiming").GetComponents<PlayMakerHashTableProxy>().FirstOrDefault(x => x.referenceName == "Results");
            hashTable.preFillCount++;
            hashTable.preFillKeyList.Add("");
            hashTable.preFillFloatList.Add(0f);
            if (hashTable._hashTable != null && hashTable._hashTable.Count > 0)
                hashTable._hashTable.Add("", 0f);
        }

        public void SetName()
        {
            Name.Value = PlayerName.Value;
        }
    }

    public class PlayerCarWeight : MonoBehaviour
    {
        void OnEnable()
        {
            StartCoroutine(UpdateWeight());
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        IEnumerator UpdateWeight()
        {
            while (true)
            {
                GetComponent<Rigidbody>().mass = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerWeight").Value;
                yield return new WaitForSeconds(10f);
            }
        }
    }

    public class PlayerDriveTrigger : MonoBehaviour
    {
        public AxisCarController CarController;
        public Rigidbody rigidbody;
        public Drivetrain drivetrain;

        public SeatbeltBehaviour seatbelt;
        public PlayerDeathSystem deathSystem;

        GameObject player;
        FsmFloat PlayerSpeed;
        FsmBool FsmPlayerInCar;
        bool playerInDriveTrigger = false;
        string driveModeButton = "DrivingMode";

        FsmBool guiDrive;
        FsmString guiInteraction;
        string interaction = " Enter Driving Mode ";

        public bool playerDrivingMode = false;

        FsmString playerCurrentVehicle;
        FsmBool playerStop;
        FsmBool playerMuscleControl;
        FsmBool playerCarControl;

        public Transform driverPivot;

        ForceFeedback forceFeedback;

        public string vehicleName = "EDM";

        FsmVector3 PlayerVelocity;

        public GameObject TrafficTrigger;

        public GameObject PlayerWeight;
        public DragRaceHandler dragRace;
        public GearIndicator gearIndicator;

        void Start()
        {
            player = GameObject.Find("PLAYER");
            FsmPlayerInCar = player.GetComponents<PlayMakerFSM>()[1].FsmVariables.FindFsmBool("PlayerInCar");
            PlayerSpeed = player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmFloat("PlayerSpeed");

            guiDrive = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIdrive");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");

            playerCurrentVehicle = PlayMakerGlobals.Instance.Variables.FindFsmString("PlayerCurrentVehicle");
            playerStop = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerStop");
            playerMuscleControl = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerMuscleControl");
            playerCarControl = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerCarControl");

            forceFeedback = transform.root.GetComponent<ForceFeedback>();

            PlayerVelocity = PlayMakerGlobals.Instance.Variables.FindFsmVector3("PlayerVelocity");
        }

        void Update()
        {
            if (playerInDriveTrigger && rigidbody.velocity.magnitude < 0.15 && !seatbelt.IsFastened)
            {
                if (!playerDrivingMode)
                {
                    guiDrive.Value = true;
                    guiInteraction.Value = interaction;
                }

                if (cInput.GetButtonDown(driveModeButton))
                {
                    guiDrive.Value = false;
                    guiInteraction.Value = string.Empty;
                    ToggleDrivingMode(!playerDrivingMode);
                }
            }

            if (playerDrivingMode)
            {
                PlayerVelocity.Value = rigidbody.velocity;
                PlayerSpeed.Value = Mathf.Abs(drivetrain.differentialSpeed);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == player && !playerInDriveTrigger && FsmPlayerInCar.Value)
                playerInDriveTrigger = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject == player && playerInDriveTrigger)
            {
                playerInDriveTrigger = false;
                guiDrive.Value = false;
                guiInteraction.Value = string.Empty;
            }
        }

        void ToggleDrivingMode(bool enterDrivingMode)
        {
            playerDrivingMode = enterDrivingMode;
            CarController.throttleAxis = enterDrivingMode ? "Throttle" : "null";
            CarController.brakeAxis = enterDrivingMode ? "Brake" : "null";
            CarController.steerAxis = enterDrivingMode ? "Horizontal" : "null";
            CarController.handbrakeAxis = enterDrivingMode ? "Handbrake" : "null";
            CarController.clutchAxis = enterDrivingMode ? "Clutch" : "null";
            CarController.shiftUpButton = enterDrivingMode ? "ShiftUp" : "null";
            CarController.shiftDownButton = enterDrivingMode ? "ShiftDown" : "null";
            TrafficTrigger.SetActive(enterDrivingMode);
            playerStop.Value = enterDrivingMode;
            playerCarControl.Value = enterDrivingMode;
            forceFeedback.enabled = enterDrivingMode;

            playerCurrentVehicle.Value = enterDrivingMode ? vehicleName : "";
            dragRace.SetName();

            PlayerVelocity.Value = Vector3.zero;

            if (enterDrivingMode)
            {
                gearIndicator.gameObject.SetActive(true);
                player.transform.SetParent(driverPivot);
                player.transform.eulerAngles = new Vector3(driverPivot.eulerAngles.x, player.transform.eulerAngles.y, driverPivot.eulerAngles.z);
            }
            else
            {
                gearIndicator.gameObject.SetActive(false);
                player.transform.SetParent(null);
                player.transform.eulerAngles = new Vector3(0, player.transform.eulerAngles.y, 0);
                PlayerSpeed.Value = 0f;
            }

            deathSystem.CalculateBreakForce();
        }
    }

    public class SeatbeltBehaviour : MonoBehaviour
    {
        public PlayerDeathSystem deathSystem;
        public PlayerDriveTrigger driveTrigger;

        public bool IsFastened = false;
        FsmBool PlayerSeatbeltsOn;

        public InteractionRaycast Interaction;
        public Collider Trigger;

        public GameObject Unfastened;
        public GameObject Fastened;

        FsmBool guiUse;
        FsmString guiInteraction;
        bool mouseOver = false;

        void Start()
        {
            PlayerSeatbeltsOn = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerSeatbeltsOn");

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        }

        void Update()
        {
            if (driveTrigger.playerDrivingMode) Simulation();
        }

        public void Simulation()
        {
            if (Interaction.GetHit(Trigger))
            {
                guiUse.Value = true;
                guiInteraction.Value = IsFastened ? "Unfasten Lapbelt" : "Fasten Lapbelt";
                mouseOver = true;

                if(Input.GetMouseButtonDown(0))
                {
                    IsFastened = !IsFastened;

                    Fastened.SetActive(IsFastened);
                    Unfastened.SetActive(!IsFastened);
                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: IsFastened ? "seatbelt_fasten" : "seatbelt_unfasten", volumePercentage: 1f);
                    PlayerSeatbeltsOn.Value = IsFastened;
                    deathSystem.CalculateBreakForce();
                }
            }
            else if(mouseOver)
            {
                guiUse.Value = false;
                guiInteraction.Value = "";
                mouseOver = false;
            }
        }
    }

    public class PlayerDeathSystem : MonoBehaviour
    {
        public ConfigurableJoint driverJoint;

        public PlayerDriveTrigger driveTrigger;

        float baseForce = 300f;

        public SeatbeltBehaviour seatbelt;
        float seatbeltForce = 300f;

        bool hasHelmet = false;
        FsmBool playerHelmet;
        float helmetForce = 200f;

        public GameObject deadBody;
        FixedJoint deadBodyJoint;
        GameObject cameraPivot;
        FsmGameObject vehicleDeadBody;

        PlayMakerFSM fpsCameraDeath;

        FsmBool crash;
        FsmString playerCurrentVehicle;

        public InteractionRaycast Interaction;

        void Start()
        {
            playerHelmet = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerHelmet");

            // Death Mechanics
            deadBodyJoint = deadBody.GetComponent<FixedJoint>();
            cameraPivot = deadBody.transform.Find("CameraPivot").gameObject;
            vehicleDeadBody = PlayMakerGlobals.Instance.Variables.FindFsmGameObject("VehicleDeadBody");

            fpsCameraDeath = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera").GetComponents<PlayMakerFSM>().FirstOrDefault(x => x.Fsm.Name == "Death");

            crash = GameObject.Find("Systems").transform.Find("Death").GetComponent<PlayMakerFSM>().FsmVariables.FindFsmBool("Crash");
            playerCurrentVehicle = PlayMakerGlobals.Instance.Variables.FindFsmString("PlayerCurrentVehicle");
        }

        void Update()
        {
            if (playerHelmet.Value != hasHelmet)
            {
                hasHelmet = playerHelmet.Value;
                CalculateBreakForce();
            }
        }

        public void CalculateBreakForce()
        {
            if (driveTrigger.playerDrivingMode)
            {
                driverJoint.breakForce = baseForce + (seatbelt.IsFastened ? seatbeltForce : 0f) + (playerHelmet.Value ? helmetForce : 0f);
                driverJoint.breakTorque = driverJoint.breakForce;
            }
            else
            {
                driverJoint.breakForce = Mathf.Infinity;
                driverJoint.breakTorque = Mathf.Infinity;
            }
        }

        void OnJointBreak(float breakForce)
        {
            StartCoroutine(Death());
        }

        IEnumerator Death()
        {
            vehicleDeadBody.Value = cameraPivot;
            deadBody.SetActive(true);
            deadBody.transform.parent = null;

            fpsCameraDeath.SendEvent("DEATH");

            crash.Value = true;
            playerCurrentVehicle.Value = "Hayosiko";

            yield return new WaitForSeconds(0.1f);

            Destroy(deadBodyJoint);

            Interaction.enabled = false;
        }
    }
}