using HutongGames.PlayMaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Drivable_EDM
{
    public class WaterCheck : MonoBehaviour
    {
        public bool inWater = true;
        float waterLevel = -4.5f;
        Transform waterCheck1;

        void Start()
        {
            waterCheck1 = GameObject.Find("WaterCheck1").transform;
            StartCoroutine(CheckWaterLevel());
        }

        IEnumerator CheckWaterLevel()
        {
            while (true)
            {
                inWater = transform.position.y < waterLevel && Vector3.Distance(transform.position, waterCheck1.position) > 500;

                yield return new WaitForSeconds(2f);
            }
        }
    }

    public class FauxCooling : MonoBehaviour
    {
        public float engineTemperature = 26f;
        public float coolingFactor = 0.75f;
        public float rpmFactor = 0.0001f;
        public float temperatureIncrease = 0f;
        public bool coolingOn = false;
        public bool engineOn = false;

        public Drivetrain drivetrain;
        public AudioSource cooldownTick;

        void Start()
        {
            cooldownTick.clip = GameObject.Find("block(Clone)").transform.Find("CooldownTick").GetComponent<AudioSource>().clip;
        }

        public void EngineStart()
        {
            cooldownTick.gameObject.SetActive(false);
            engineOn = true;
            if (!coolingOn)
            {
                StopAllCoroutines();
                StartCoroutine(Cooling());
            }
        }

        public void EngineStop()
        {
            engineOn = false;
            if (engineTemperature >= 50f) StartCoroutine(CooldownTick());
        }

        void OnEnable()
        {
            if (cooldownTick != null)
            {
                cooldownTick.gameObject.SetActive(false);
                StartCoroutine(Cooling());
            }
        }

        void OnDisable()
        {
            coolingOn = false;
        }

        IEnumerator Cooling()
        {
            coolingOn = true;

            while (engineTemperature >= 25f)
            {
                temperatureIncrease = ((engineTemperature > 80f && engineOn) ? ((drivetrain.rpm * rpmFactor) - coolingFactor) : (drivetrain.rpm * rpmFactor)) * Time.deltaTime;
                engineTemperature = Mathf.Clamp(engineTemperature + temperatureIncrease, 20f, 135f);
                yield return null;
            }

            engineTemperature = 25f;

            coolingOn = false;
        }

        IEnumerator CooldownTick()
        {
            cooldownTick.gameObject.SetActive(true);
            while (engineTemperature >= 50f && !engineOn)
            {
                cooldownTick.volume = Mathf.Clamp(engineTemperature / 170f, 0f, 1f);
                cooldownTick.pitch = Mathf.Clamp(engineTemperature / 90f, 0.8f, 1.1f);
                yield return null;
            }
            cooldownTick.gameObject.SetActive(false);
        }
    }

    public class ExhaustSmoke : MonoBehaviour
    {
        public Drivetrain drivetrain;

        public EllipsoidParticleEmitter exhaustSmoke;

        float velocityDivider = 125f;
        float emissionDivider = 150f;

        public void Start()
        {
            exhaustSmoke = GetComponent<EllipsoidParticleEmitter>();

            StartCoroutine(Exhaust());

            transform.GetComponent<ParticleAnimator>().colorAnimation = GameObject.Find("RCO_RUSCKO12(270)").transform.Find("LOD/Simulation/Exhaust/FromEngine").GetComponent<ParticleAnimator>().colorAnimation;
        }

        IEnumerator Exhaust()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);

            while (true)
            {
                exhaustSmoke.maxEmission = drivetrain.rpm / emissionDivider;
                exhaustSmoke.localVelocity = new Vector3(0f, 0f, Mathf.Clamp(drivetrain.rpm / velocityDivider, 5f, 15f));

                yield return wait;
            }
        }
    }

    public class Starter : MonoBehaviour
    {
        public Drivetrain drivetrain;
        public InteractionRaycast Interaction;
        public FauxCooling cooling;

        public Collider ignitionCollider;

        public GameObject key;
        public Transform keyhole;
        bool playerHasKey = true;

        Coroutine carRunning;

        public bool mouseHeld = false;

        public bool carEngineOn = false;
        public bool keyIgnition = false;
        public bool keyIgnitionOut = false;

        public float startTime = 0f;
        public float holdTime = 0.5f;
        public float engineStartTime = 2f;

        public float maxTorque = 100f;
        public float stallRPM = 650f;

        public bool startSoundPlaying = false;

        public GameObject starterSound;

        public GameObject lightObject;

        public WaterCheck waterCheck;
        public FuelTank fuelTank;

        public EllipsoidParticleEmitter exhaust;

        bool mouseOver = false;
        FsmBool guiUse;
        FsmString guiInteraction;
        string interaction = " Ignition ";

        void Start()
        {
            drivetrain.rpm = 0f;
            drivetrain.maxTorque = 0f;
            drivetrain.engineFrictionFactor = 5f;

            starterSound = transform.Find("StarterSound").gameObject;
            starterSound.GetComponent<AudioSource>().clip = GameObject.Find("SATSUMA(557kg, 248)/Sounds").transform.GetChild(0).GetComponent<AudioSource>().clip;

            lightObject.SetActive(false);

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        }

        void Update()
        {
            if (playerHasKey && Interaction.GetHit(ignitionCollider))
            {
                mouseOver = true;
                guiUse.Value = true;
                guiInteraction.Value = interaction;

                if (Input.GetMouseButtonDown(0))
                {
                    mouseHeld = true;
                    startTime = Time.time;

                    engineStartTime = Mathf.Clamp((cooling.engineTemperature * -0.025f) + 2.75f, 0.75f, 2f);

                    if (!keyIgnition)
                        KeyIgnitionHandler(true);
                    else
                    {
                        keyIgnitionOut = !carEngineOn;

                        if (carEngineOn)
                        {
                            StopCarEngine();
                            KeyIgnitionHandler(false);
                        }
                    }
                }
            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }

            if (mouseHeld) MouseHeld();
        }

        void KeyIgnitionHandler(bool keyIn)
        {
            if (keyIn)
            {
                PlaySound("CarFoley", keyhole, "carkeys_in", 0.5f);
                key.SetActive(true);
                keyhole.localEulerAngles = new Vector3(0f, 45f, 0f);

                keyIgnition = true;
                keyIgnitionOut = false;

                if (!waterCheck.inWater) lightObject.SetActive(true);

            }
            else
            {
                PlaySound("CarFoley", keyhole, "carkeys_out", 0.5f);
                key.SetActive(false);
                keyhole.localEulerAngles = Vector3.zero;

                keyIgnition = false;
                keyIgnitionOut = false;

                lightObject.SetActive(false);
            }
        }

        void MouseHeld()
        {
            if (Input.GetMouseButtonUp(0))
            {
                if (keyIgnitionOut && startTime + holdTime >= Time.time)
                    KeyIgnitionHandler(false);
                else if (keyIgnition)
                    keyhole.localEulerAngles = new Vector3(0f, 45f, 0f);
                PlaySound("Starting", transform, stopSound: true);
                startSoundPlaying = false;
                mouseHeld = false;
            }

            if (Input.GetMouseButton(0))
            {
                if (!carEngineOn && keyIgnition && startTime + holdTime <= Time.time)
                {
                    if (drivetrain.gear != 1 && !waterCheck.inWater)
                    {
                        drivetrain.gear = 1;
                    }

                    if (!startSoundPlaying)
                    {
                        startSoundPlaying = true;
                        StartCoroutine(StartSound());
                    }

                    if (startTime + holdTime + engineStartTime <= Time.time && !waterCheck.inWater && fuelTank.fuelLevel > 0.4f)
                    {
                        carRunning = StartCoroutine(CarRunning());
                    }
                }
            }
        }

        public void StopCarEngine()
        {
            carEngineOn = false;
            drivetrain.canStall = true;
            drivetrain.maxTorque = 0f;
            drivetrain.engineFrictionFactor = 5f;
            StopCoroutine(carRunning);

            exhaust.emit = false;

            cooling.EngineStop();
            fuelTank.StopConsumption();

            if (waterCheck.inWater)
                lightObject.SetActive(false);
        }

        IEnumerator CarRunning()
        {
            drivetrain.canStall = false;
            drivetrain.maxTorque = maxTorque;
            drivetrain.engineFrictionFactor = 0.25f;

            carEngineOn = true;

            fuelTank.StartConsumption();
            cooling.EngineStart();

            exhaust.emit = true;



            yield return new WaitForSeconds(1f);

            drivetrain.canStall = true;

            while (carEngineOn)
            {
                if (drivetrain.rpm < stallRPM || waterCheck.inWater) break;
                yield return null;
            }

            StopCarEngine();
        }

        IEnumerator StartSound()
        {
            PlaySound("CarFoley", keyhole, "ignition_keys", 0.5f);
            keyhole.localEulerAngles = new Vector3(0f, 90f, 0f);

            if (!waterCheck.inWater)
            {
                yield return new WaitForSeconds(0.2f);
                PlaySound("Starting", starterSound.transform, "start1", 0.45f);
                yield return new WaitForSeconds(0.3f);
                starterSound.SetActive(true);
                while (!carEngineOn && mouseHeld) yield return null;
                if (carEngineOn) PlaySound("Starting", starterSound.transform, "start3", 0.45f);
                starterSound.SetActive(false);
            }
        }
        void PlaySound(string type, Transform source, string sound = "", float volume = 1f, bool stopSound = false)
        {
            if (stopSound)
                MasterAudio.StopAllOfSound(type);
            else
                MasterAudio.PlaySound3DAndForget(type, transform, volumePercentage: volume, variationName: sound);
        }
    }

    public class FuelTank : MonoBehaviour
    {
        public float fuelConsumption;
        public float consumptionFactor = 300000f;

        public float fuelLevel = 35f;
        public float fuelCapacity = 40f;

        public bool lowFuelTriggered = false;

        public Rigidbody fueltankRigidbody;

        public Drivetrain drivetrain;
        public AxisCarController CarController;
        public Starter starter;

        public RefuelIndicator Indi;

        public void Start()
        {
            fueltankRigidbody = GetComponent<Rigidbody>();
            UpdateFuelTankMass();
        }

        public void StartConsumption()
        {
            StartCoroutine(Consumption());
        }

        public void StopConsumption()
        {
            StopAllCoroutines();

            fueltankRigidbody.mass = fuelLevel + 4f;

            lowFuelTriggered = false;

            drivetrain.revLimiterTriggered = false;
        }

        IEnumerator Consumption()
        {
            StartCoroutine(FuelTankMass());

            while (fuelLevel > 0.2f)
            {
                fuelConsumption = ((drivetrain.rpm / consumptionFactor) * ((drivetrain.rpm > 1500 && drivetrain.gear != 1 && drivetrain.clutchDragImpulse != 0) ? drivetrain.throttle : 1f)) * Time.deltaTime;
                fuelLevel = Mathf.Clamp(fuelLevel - fuelConsumption, 0f, fuelCapacity);

                if (!lowFuelTriggered && fuelLevel < 0.6f) StartCoroutine(LowFuel());

                yield return new WaitForFixedUpdate();
            }

            starter.StopCarEngine();
        }

        IEnumerator LowFuel()
        {
            lowFuelTriggered = true;

            while (fuelLevel < 0.6f)
            {
                drivetrain.revLimiterTriggered = true;

                yield return new WaitForSeconds(Random.Range(0.1f, 0.6f));
            }

            lowFuelTriggered = false;

            drivetrain.revLimiterTriggered = false;
        }

        IEnumerator FuelTankMass()
        {
            while (true)
            {
                fueltankRigidbody.mass = fuelLevel + 4f;
                yield return new WaitForSeconds(1f);
            }
        }

        public void UpdateFuelTankMass() => fueltankRigidbody.mass = fuelLevel + 4f;

    }

    public class FuelTankHatch : MonoBehaviour
    {
        public InteractionRaycast Interaction;

        public bool hatchOpened = false;
        public Collider hatchCollider;
        public Vector3 hatchOpen = new Vector3(0f, -80f, 0f);
        public Vector3 hatchClosed = Vector3.zero;

        public int capStage = 0; // 0 closed -> 12 opened
        public float capRotation = 15f;
        public float capDelay = 0.05f;
        public Transform capTransform;
        public Collider capCollider;

        public GameObject fuelFiller;

        public bool mouseOverHatch = false;
        public bool mouseOverCap = false;
        public FsmBool guiUse;
        public FsmString guiInteraction;
        public string interaction = " Screw Cap ";

        public string mouseWheel = "Mouse ScrollWheel";
        public float inputDelay = 0f;

        void Start()
        {
            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        }

        void Update()
        {
            HatchOpener();

            if (hatchOpened)
                CapOpener();
        }

        void HatchOpener()
        {
            if (Interaction.GetHit(hatchCollider))
            {
                mouseOverHatch = true;
                guiUse.Value = true;

                if (Input.GetMouseButtonDown(0))
                {
                    hatchOpened = !hatchOpened;
                    transform.localEulerAngles = hatchOpened ? hatchOpen : hatchClosed;
                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "filler_hatch", pitch: hatchOpened ? 1f : 0.75f);
                    capCollider.enabled = hatchOpened;

                    if (capStage == 12) fuelFiller.SetActive(hatchOpened);
                }
            }
            else if (mouseOverHatch)
            {
                mouseOverHatch = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }
        }

        void CapOpener()
        {
            inputDelay -= Time.deltaTime;

            if (Interaction.GetHit(capCollider))
            {
                mouseOverCap = true;
                guiUse.Value = true;
                guiInteraction.Value = interaction;

                if (capStage < 12 && Input.GetAxis(mouseWheel) < 0f && inputDelay <= 0f)
                {
                    capStage++;
                    capTransform.localEulerAngles = new Vector3(0f, capRotation * capStage, 0f);
                    inputDelay = capDelay;

                    if (capStage == 12)
                    {
                        MasterAudio.PlaySound3DAndForget("CarFoley", capTransform, variationName: "filler_cap");
                        fuelFiller.SetActive(true);
                        capTransform.gameObject.SetActive(false);
                    }
                }
                else if (capStage > 0 && Input.GetAxis(mouseWheel) > 0f && inputDelay <= 0f)
                {
                    capStage--;
                    capTransform.localEulerAngles = new Vector3(0f, capRotation * capStage, 0f);
                    inputDelay = capDelay;

                    if (capStage == 11)
                    {
                        MasterAudio.PlaySound3DAndForget("CarFoley", capTransform, variationName: "filler_cap");
                        fuelFiller.SetActive(false);
                        capTransform.gameObject.SetActive(true);
                    }
                }
            }
            else if (mouseOverCap)
            {
                mouseOverCap = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }
        }
    }

    public class RefuelIndicator : MonoBehaviour
    {
        Transform fuelBar;
        public FuelTank fuelTank;

        public void Setup()
        {
            transform.name = "EDM500LX Gasoline";
            transform.SetParent(GameObject.Find("GUI").transform.Find("Indicators/Fluids"));
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;

            fuelBar = transform.GetChild(1);

            Destroy(fuelBar.GetComponent<PlayMakerFSM>());
        }

        void Update()
        {
            fuelBar.localScale = new Vector3(fuelTank.fuelLevel / fuelTank.fuelCapacity, 1f, 1f);
        }
    }

    public class Refueling : MonoBehaviour
    {
        int fuelMode = 0; // 0 off, 1 jerrycan, 2 nozzle

        public FuelTank fuelTank;
        public GameObject refuelIndicator;

        public Collider jerrycanTrigger;

        public FsmBool jerrycanPouring;
        public FsmFloat jerrycanLevel;

        public Transform jerrycanPouringSound;
        public Transform jerrycanPouringSoundParent;

        public float jerrycanFuelRate = 0.7f;

        public Collider nozzleTrigger;

        public FsmBool nozzlePour;
        public FsmFloat nozzleFlow;

        void Start()
        {
            // Jerrycan
            GameObject jerrycanFluidTrigger = GameObject.Find("gasoline(itemx)/FluidTrigger");
            jerrycanTrigger = jerrycanFluidTrigger.GetComponent<Collider>();

            PlayMakerFSM jerrycan = jerrycanFluidTrigger.GetComponent<PlayMakerFSM>();
            jerrycanPouring = jerrycan.FsmVariables.FindFsmBool("Pouring");
            jerrycanLevel = jerrycan.FsmVariables.FindFsmFloat("Fluid");

            jerrycanPouringSoundParent = GameObject.Find("Systems").transform.Find("DisabledAudioParent");
            jerrycanPouringSound = jerrycanPouringSoundParent.Find("FuelPouringSound");

            // Fuel Nozzle
            Transform fuelNozzle = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera/Fuel/Pivot/hand/Armature/Bone/Bone_001/Pistol/Pistol 98/FuelNozzle");
            nozzleTrigger = fuelNozzle.GetComponent<Collider>();

            // GUI Indicator
            refuelIndicator = Instantiate(GameObject.Find("GUI").transform.Find("Indicators/Fluids/GasolineCar").gameObject);
            refuelIndicator.AddComponent<RefuelIndicator>().fuelTank = fuelTank;
            refuelIndicator.GetComponent<RefuelIndicator>().Setup();

            fuelTank.Indi = refuelIndicator.GetComponent<RefuelIndicator>();

            PlayMakerFSM nozzleFSM = fuelNozzle.GetComponent<PlayMakerFSM>();
            nozzlePour = nozzleFSM.FsmVariables.FindFsmBool("Pour");
            nozzleFlow = nozzleFSM.FsmVariables.FindFsmFloat("FuelFlow");

            gameObject.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (fuelMode == 0)
            {
                if (other == jerrycanTrigger && jerrycanPouring.Value && jerrycanLevel.Value > 0 && jerrycanTrigger.transform.parent.parent != null)
                    StartCoroutine(JerryCanFueling());
                else if (other == nozzleTrigger)
                    StartCoroutine(NozzleFueling());
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other == jerrycanTrigger && fuelMode == 1)
            {
                fuelMode = 0;
                StopAllCoroutines();
                refuelIndicator.SetActive(false);

                jerrycanPouringSound.SetParent(jerrycanPouringSoundParent);
                jerrycanPouringSound.localPosition = Vector3.zero;
                jerrycanPouringSound.localEulerAngles = Vector3.zero;
            }

            if (other == nozzleTrigger && fuelMode == 2)
            {
                fuelMode = 0;
                StopAllCoroutines();
                refuelIndicator.SetActive(false);

                nozzlePour.Value = false;
            }
        }

        void OnDisable()
        {
            if (fuelMode != 0)
            {
                fuelMode = 0;
                nozzlePour.Value = false;

                jerrycanPouringSound.SetParent(jerrycanPouringSoundParent);
                jerrycanPouringSound.localPosition = Vector3.zero;
                jerrycanPouringSound.localEulerAngles = Vector3.zero;

                refuelIndicator.SetActive(false);
            }
        }

        IEnumerator JerryCanFueling()
        {
            fuelMode = 1;

            refuelIndicator.SetActive(true);

            jerrycanPouringSound.SetParent(transform);
            jerrycanPouringSound.localPosition = Vector3.zero;
            jerrycanPouringSound.localEulerAngles = Vector3.zero;

            while (fuelMode == 1 && jerrycanTrigger.enabled && jerrycanTrigger.transform.parent.parent != null && jerrycanPouring.Value && jerrycanLevel.Value > 0 && fuelTank.fuelLevel < fuelTank.fuelCapacity)
            {
                fuelTank.fuelLevel = Mathf.Clamp(fuelTank.fuelLevel + (jerrycanFuelRate * Time.deltaTime), 0f, fuelTank.fuelCapacity);
                jerrycanLevel.Value -= jerrycanFuelRate * Time.deltaTime;
                fuelTank.UpdateFuelTankMass();

                yield return null;
            }

            fuelMode = 0;

            refuelIndicator.SetActive(false);

            jerrycanPouringSound.SetParent(jerrycanPouringSoundParent);
            jerrycanPouringSound.localPosition = Vector3.zero;
            jerrycanPouringSound.localEulerAngles = Vector3.zero;
        }

        IEnumerator NozzleFueling()
        {
            fuelMode = 2;

            nozzlePour.Value = true;

            refuelIndicator.SetActive(true);

            while (fuelMode == 2 && nozzlePour.Value && nozzleTrigger.gameObject.activeInHierarchy && fuelTank.fuelLevel < fuelTank.fuelCapacity)
            {
                fuelTank.fuelLevel = Mathf.Clamp(fuelTank.fuelLevel + (nozzleFlow.Value * Time.deltaTime), 0f, fuelTank.fuelCapacity);
                fuelTank.UpdateFuelTankMass();

                if (fuelTank.fuelLevel >= fuelTank.fuelCapacity) break;

                yield return null;
            }

            fuelMode = 0;

            nozzlePour.Value = false;

            refuelIndicator.SetActive(false);
        }
    }

    public class Indicators : MonoBehaviour
    {
        public AxisCarController CarController;
        public InteractionRaycast Interaction;

        string indicatorLeft = "IndicatorLeft";
        string indicatorRight = "IndicatorRight";

        int lightState = 0; // 0 off, 1 warninglights, 2 right Indicator, 3 left Indicator

        public GameObject Indicator, blinkersLeft, blinkersRight, lights;

        WaitForSeconds wait = new WaitForSeconds(0.4f);

        public Collider knobCollider;
        public PlayerDriveTrigger driveTrigger;

        bool mouseOver = false;
        FsmBool guiUse;
        FsmString guiInteraction;
        string interaction = " Hazard Lights ";

        void Start()
        {
            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        }

        void Update()
        {
            HazardKnob();

            if (driveTrigger.playerDrivingMode) KeyInput();
        }

        void HazardKnob()
        {
            if (Interaction.GetHit(knobCollider))
            {
                mouseOver = true;
                guiUse.Value = true;
                guiInteraction.Value = interaction;

                if (Input.GetMouseButtonDown(0))
                {
                    blinkersRight.SetActive(false);
                    blinkersLeft.SetActive(false);

                    StopAllCoroutines();

                    if (lightState != 1)
                    {
                        if (lightState > 1) RotateTurnLever(0f, "turn_signal_lever_off");

                        lightState = 1;

                        StartCoroutine(WarningLights());
                    }
                    else
                    {
                        lightState = 0;
                        Indicator.SetActive(false);
                    }

                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "dash_button", volumePercentage: 0.4f);
                }
            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }
        }

        void KeyInput()
        {
            if (lightState != 1)
            {
                if (cInput.GetButtonDown(indicatorRight))
                {
                    if (lightState == 0)
                        TurnOnBlinker(2, -8f, blinkersRight, true);
                    else if (lightState == 3)
                        TurnOffBlinker();
                }
                else if (cInput.GetButtonDown(indicatorLeft))
                {
                    if (lightState == 0)
                        TurnOnBlinker(3, 8f, blinkersLeft, false);
                    else if (lightState == 2)
                        TurnOffBlinker();
                }
            }
        }

        void RotateTurnLever(float axisZ, string soundVariation)
        {
            MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: soundVariation, volumePercentage: 0.5f);
        }

        void TurnOffBlinker()
        {
            lightState = 0;

            RotateTurnLever(0f, "turn_signal_lever_off");

            blinkersRight.SetActive(false);
            blinkersLeft.SetActive(false);
            Indicator.SetActive(false);

            StopAllCoroutines();
        }

        void TurnOnBlinker(int state, float axisZ, GameObject lights, bool right)
        {
            lightState = state;

            RotateTurnLever(axisZ, "turn_signal_lever_on");

            blinkersRight.SetActive(false);
            blinkersLeft.SetActive(false);
            Indicator.SetActive(true);

            StartCoroutine(Blinkers(lights));
            StartCoroutine(SteeringReturn(right));
        }

        IEnumerator Blinkers(GameObject blinkers)
        {
            while (lightState > 1)
            {
                blinkers.SetActive(!blinkers.activeSelf);
                Indicator.SetActive(!Indicator.activeSelf);

                if (lights.activeInHierarchy)
                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: blinkers.activeSelf ? "turn_signal_1" : "turn_signal_2", volumePercentage: 0.5f);

                yield return wait;
            }
        }

        IEnumerator SteeringReturn(bool right)
        {
            if (right) while (CarController.steerInput > -0.1f) yield return null;
            else while (CarController.steerInput < 0.1f) yield return null;

            TurnOffBlinker();
        }

        IEnumerator WarningLights()
        {
            while (lightState == 1)
            {
                blinkersRight.SetActive(!blinkersRight.activeSelf);
                blinkersLeft.SetActive(!blinkersLeft.activeSelf);
                Indicator.SetActive(!Indicator.activeSelf);

                if (lights.activeInHierarchy)
                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: blinkersRight.activeSelf ? "turn_signal_1" : "turn_signal_2", volumePercentage: 0.5f);

                yield return wait;
            }
        }
    }

    public class DashShite : MonoBehaviour
    {
        public Material needles;

        public Drivetrain drivetrain;
        public FuelTank fuelTank;
        public Starter starter;
        public FauxCooling fauxCooling;

        public Transform needleSpeed;
        public Transform needleFuel;
        public Transform needleRPM;
        public Transform needleTemp;

        Vector3 rot;

        public float fuelNeedleSmoothing = 150f;

        void Start()
        {
            needles.SetFloat("_Intensity", 0f);
        }

        void Update()
        {
            float rotSpeedFuelNeedle = fuelNeedleSmoothing * Time.deltaTime;

            needleSpeed.localEulerAngles = new Vector3(0f, Mathf.Clamp(Mathf.Abs(drivetrain.differentialSpeed) * -1.13f, -285f, 0f), 0f);
            if (starter.keyIgnition) 
            { 
                rot = Vector3.Lerp(rot, new Vector3(0f, fuelTank.fuelLevel, 0f), rotSpeedFuelNeedle * Time.deltaTime);
            }
            else rot = Vector3.Lerp(rot, new Vector3(0f, 0f, 0f), rotSpeedFuelNeedle * Time.deltaTime);
            needleRPM.localEulerAngles = new Vector3(0f, Mathf.Clamp(-(drivetrain.rpm / 30f), -272f, 0f), 0f);
            needleTemp.localEulerAngles = new Vector3(0f, Mathf.Clamp((Mathf.Clamp(fauxCooling.engineTemperature - 35f, 0f, 100f) / 100f) * -80f, -80f, 0f), 0f);

            needleFuel.localEulerAngles = -rot;
        }
    }

    public class MainLights : MonoBehaviour
    {
        public AxisCarController CarController;
        public Drivetrain drivetrain;
        public InteractionRaycast Interaction;

        string lightButton = "HighBeam";
        int lightState = 0; // 0 off, 1 gauges marker lights, 2 on
        public bool highBeam = false;

        bool holdButton = false;
        float holdTime = 0.5f;

        public GameObject DashLights;
        public GameObject DashIndicatorHi;

        public GameObject headLight;

        public GameObject rearLight;

        public GameObject markerLight;

        public GameObject brakeLight;
        public GameObject reverseLight;

        public PlayerDriveTrigger driveTrigger;

        public Collider switchCollider;

        bool mouseOver = false;
        FsmBool guiUse;
        FsmString guiInteraction;
        string interaction = " Lights ";

        void Start()
        {
            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        }

        void OnDisable()
        {
            reverseLight.SetActive(false);
            brakeLight.SetActive(false);
        }

        void Update()
        {
            LightKnob();

            if (driveTrigger.playerDrivingMode)
                KeyInput();

            reverseLight.SetActive(drivetrain.gear == 0);
        }

        void LightKnob()
        {
            if (Interaction.GetHit(switchCollider))
            {
                mouseOver = true;
                guiUse.Value = true;
                guiInteraction.Value = interaction;

                if (Input.GetMouseButtonDown(0))
                {
                    lightState++;
                    if (lightState > 2) lightState = 0;

                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "dash_button", volumePercentage: 0.4f);

                    SetLight();
                }
            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }
        }

        void KeyInput()
        {
            if (cInput.GetButtonDown(lightButton))
            {
                holdTime = 0.5f;
                holdButton = true;
            }
            else if (cInput.GetButton(lightButton) && holdButton)
            {
                holdTime -= Time.deltaTime;
                if (holdTime < 0f)
                {
                    holdButton = false;
                    if (lightState < 2)
                        lightState = 2; // Turn On Lights
                    else
                        lightState = 0; // Turn Off Lights
                    SetLight();

                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "dash_button", volumePercentage: 0.4f);
                }
            }
            else if (cInput.GetButtonUp(lightButton))
            {
                holdButton = false;
                if (holdTime > 0f)
                {
                    highBeam = !highBeam;
                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: highBeam ? "turn_signal_lever_on" : "turn_signal_lever_off", volumePercentage: 0.5f);
                    foreach (Light light in headLight.GetComponentsInChildren<Light>())
                    {
                        if(light.type == LightType.Spot)
                        {
                            light.range = highBeam ? 150f : 50f;
                            light.intensity = highBeam ? 4f : 3f;
                        }
                    }
                    DashIndicatorHi.SetActive(highBeam && lightState > 0);
                }
            }

            brakeLight.SetActive(CarController.brakeInput > 0.2f);
        }

        void SetLight()
        {
            switch (lightState)
            {
                case 0:
                    headLight.SetActive(false);
                    rearLight.SetActive(false);
                    DashIndicatorHi.SetActive(false);
                    DashLights.SetActive(false);
                    break;
                case 1:
                    markerLight.SetActive(true);
                    rearLight.SetActive(true);
                    DashIndicatorHi.SetActive(false);
                    DashLights.SetActive(true);
                    break;
                case 2:
                    headLight.SetActive(true);
                    rearLight.SetActive(true);
                    markerLight.SetActive(false);
                    DashIndicatorHi.SetActive(highBeam);
                    foreach (Light light in headLight.GetComponentsInChildren<Light>())
                    {
                        if (light.type == LightType.Spot)
                        {
                            light.range = highBeam ? 150f : 50f;
                            light.intensity = highBeam ? 4f : 3f;
                        }
                    }
                    DashLights.SetActive(true);
                    break;
            }
        }
    }

    public class CarStrafe : MonoBehaviour
    {
        public PlayerDriveTrigger driveTrigger;
        public Animation StrafeAnims;
        public Door leftDoor;
        public WindowOpener leftWindow;
        int Side = 0;
        bool TriggeredReturn = false;

        void Start()
        {
            GetClips();
        }

        void GetClips()
        {
            Animation SatsumaAnim = GameObject.Find("SATSUMA(557kg, 248)").transform.Find("DriverHeadPivot/CameraPivot").GetComponent<Animation>();

            StrafeAnims.AddClip(SatsumaAnim.GetClip("car_strafe_left1"), "leftout");
            StrafeAnims.AddClip(SatsumaAnim.GetClip("car_strafe_left2"), "leftin");
            StrafeAnims.AddClip(SatsumaAnim.GetClip("car_strafe_right1"), "rightout");
            StrafeAnims.AddClip(SatsumaAnim.GetClip("car_strafe_right2"), "rightin");
        }

        void Update()
        {
            if (driveTrigger.playerDrivingMode)
            {
                if (cInput.GetKey("ReachLeft") && (leftDoor.doorOpen || leftWindow.windowState >= 35))
                {
                    if (Side == 0)
                    {
                        StrafeAnims.Play("leftout");
                        Side = -1;
                    }
                }
                if (cInput.GetKey("ReachRight"))
                {
                    if (Side == 0)
                    {
                        StrafeAnims.Play("rightout");
                        Side = 1;
                    }
                }

                if (driveTrigger.playerDrivingMode)

                    if (TriggeredReturn && !StrafeAnims.isPlaying)
                    {
                        TriggeredReturn = false;
                        Side = 0;
                    }

                if (!cInput.GetKey("ReachLeft") && !TriggeredReturn && Side == -1)
                {
                    TriggeredReturn = true;
                    StrafeAnims.Play("leftin");
                }
                if (!cInput.GetKey("ReachRight") && !TriggeredReturn && Side == 1)
                {
                    TriggeredReturn = true;
                    StrafeAnims.Play("rightin");
                }
            }
            else
            {
                if (transform.localEulerAngles != Vector3.zero) transform.localEulerAngles = Vector3.zero;
            }
        }
    }

    public class TowingHook : MonoBehaviour
    {
        public Transform frontPivot;
        public Transform rearPivot;

        void Awake()
        {
            Transform hayosiko = GameObject.Find("HAYOSIKO(1500kg, 250)").transform;

            Transform frontHook = Instantiate(hayosiko.Find("HookFront").gameObject).transform;
            frontHook.parent = frontPivot;
            frontHook.localPosition = Vector3.zero;
            frontHook.localEulerAngles = Vector3.zero;
            frontHook.localScale = Vector3.one;
            frontHook.name = "HookFront";

            Transform rearHook = Instantiate(hayosiko.Find("HookRear").gameObject).transform;
            rearHook.parent = rearPivot;
            rearHook.localPosition = Vector3.zero;
            rearHook.localEulerAngles = Vector3.zero;
            rearHook.localScale = Vector3.one;
            rearHook.name = "HookRear";
        }
    }

    public class WindowOpener : MonoBehaviour
    {
        public InteractionRaycast Interaction;

        Collider openerCollider;
        Transform opener;

        public int windowState = 0;
        public Animation windowAnimation;
        public string animationName = "";

        bool mouseOver = false;
        FsmBool guiUse;
        FsmString guiInteraction;
        string interaction = " Open Window ";

        string mouseWheel = "Mouse ScrollWheel";
        float inputDelay = 0f;

        void Start()
        {
            openerCollider = GetComponent<Collider>();
            opener = transform.GetChild(0);

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");

            SetWindowPosition(windowState);
            opener.localEulerAngles = new Vector3(0f, -15 * windowState, 0f);
        }

        void Update()
        {
            inputDelay -= Time.deltaTime;

            if (Interaction.GetHit(openerCollider))
            {
                mouseOver = true;
                guiUse.Value = true;
                guiInteraction.Value = interaction;

                if (windowState < 60 && Input.GetAxis(mouseWheel) < 0f && inputDelay <= 0f)
                {
                    windowState++;
                    opener.localEulerAngles = new Vector3(0f, -15 * windowState, 0f);
                    inputDelay = 0.02f;
                    SetWindowPosition(windowState);
                }
                else if (windowState > 0 && Input.GetAxis(mouseWheel) > 0f && inputDelay <= 0f)
                {
                    windowState--;
                    opener.localEulerAngles = new Vector3(0f, -15 * windowState, 0f);
                    inputDelay = 0.02f;
                    SetWindowPosition(windowState);
                }
            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }
        }

        void SetWindowPosition(int state)
        {
            windowAnimation[animationName].time = (1f / 60f) * (state % 61);
            windowAnimation[animationName].speed = 0f;
            windowAnimation.Play(animationName);
        }

    }

    public class WindscreenWipers : MonoBehaviour
    {
        public PlayerDriveTrigger driveMode;
        public InteractionRaycast Interaction;
        public Starter starter;

        AudioSource wiperSound;
        int wiperState = 0; // 0 = Off, 1 = Slow, 2 = Fast

        public Transform wiperParent;
        public Animation wiperAnimations;
        string wiperButton = "Wipers";
        public Collider wiperKnobCollider;


        bool mouseOver = false;
        FsmBool guiUse;
        FsmString guiInteraction;
        string interactionString = " Wipers ";

        Coroutine wiperRoutine;

        void Start()
        {
            wiperSound = wiperParent.GetComponent<AudioSource>();
            wiperSound.clip = GameObject.Find("MasterAudio").transform.Find("CarFoley/wiper_loop").GetComponent<AudioSource>().clip;

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        }

        void Update()
        {
            WiperKnob();

            if (driveMode.playerDrivingMode)
                WiperButton();
        }

        void WiperKnob()
        {
            if (Interaction.GetHit(wiperKnobCollider))
            {
                mouseOver = true;
                guiUse.Value = true;
                guiInteraction.Value = interactionString;

                if (Input.GetMouseButtonDown(0)) WiperSwitch();
            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }
        }

        void WiperButton()
        {
            if (cInput.GetButtonDown(wiperButton))
                WiperSwitch();
        }


        void WiperSwitch()
        {
            MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "dash_button", volumePercentage: 0.4f);
            wiperState++;
            if (wiperState > 2) wiperState = 0;

            switch (wiperState)
            {
                case 0:
                    StopAllCoroutines();
                    wiperRoutine = null;
                    break;
                case 1:
                case 2:
                    if (wiperRoutine == null)
                        wiperRoutine = StartCoroutine(Wipers());
                    break;
            }
        }

        IEnumerator Wipers()
        {
            float wait = 0f;

            while (wiperAnimations.isPlaying || wiperSound.isPlaying) yield return null;

            while (wiperState > 0)
            {
                if (starter.keyIgnition && wait <= 0f)
                {
                    wiperAnimations.Play();

                    wiperSound.Play();

                    while (wiperAnimations.isPlaying || wiperSound.isPlaying) yield return null;

                    if (wiperState == 1) wait = 3f;
                }

                wait -= Time.deltaTime;

                if (wiperState == 2) wait = 0f;

                yield return null;
            }
        }

    }

    public class RainHandler : MonoBehaviour
    {
        public windshield rainScript;
        FsmFloat rainIntensity;
        public Rigidbody rigidbody;

        bool underRoof = false;
        float roofCheckDelay = 1f;
        int noRainMask = 1 << 27;
        float fadeSpeed = (1f / 6f);

        public Transform[] Wipers;
        float wiperMultiplier = (1f / 90f);

        void Start()
        {
            Material windowMaterial = Instantiate(GameObject.Find("HAYOSIKO(1500kg, 250)").transform.Find("RearDoor/doorear/GlassRear").GetComponent<MeshRenderer>().sharedMaterial);
            windowMaterial.name = "EDMwindow";
            foreach (MeshRenderer window in rainScript.windowRenderers) window.sharedMaterial = windowMaterial;

            rainScript.enabled = false;
            rainScript.enabled = true;

            rainIntensity = PlayMakerGlobals.Instance.Variables.FindFsmFloat("RainIntensity");
        }

        void Update()
        {
            roofCheckDelay -= Time.deltaTime;
            if (roofCheckDelay <= 0f && rainIntensity.Value > 0f)
            {
                roofCheckDelay = 1f;
                underRoof = Physics.Raycast(transform.position, Vector3.up, 20f, noRainMask);
            }

            if (rainScript.rain > 0f && (underRoof || rainIntensity.Value <= 0.05f))
            {
                rainScript.rain -= fadeSpeed * Time.deltaTime;
                rainScript.rain = Mathf.Clamp(rainScript.rain, 0f, 1f);
            }
            else if (!underRoof && rainIntensity.Value > 0f)
            {
                rainScript.rain = rainIntensity.Value;
            }

            rainScript.wind = (rigidbody.velocity.magnitude * 3.6f);

            rainScript.wiper1Position = Mathf.Abs(Wipers[0].localEulerAngles.y * wiperMultiplier);
            rainScript.wiper2Position = Mathf.Abs(Wipers[1].localEulerAngles.y * wiperMultiplier);
        }

    }

    public class GripSystem : MonoBehaviour
    {
        public Wheel[] wheels;

        Dictionary<CarDynamics.SurfaceType, float[]> terrainDictionary = new Dictionary<CarDynamics.SurfaceType, float[]>
        {
            { CarDynamics.SurfaceType.track, new float[] {1f, 1f} },
            { CarDynamics.SurfaceType.offroad, new float[] {1.15f, 1.15f} },
            { CarDynamics.SurfaceType.grass, new float[] {1.05f, 1.05f} },
            { CarDynamics.SurfaceType.sand, new float[] {1f, 1f} },
            { CarDynamics.SurfaceType.oil, new float[] {1f, 1f} }
        };

        bool calculateRain = true;
        float rainFactor = 1f;
        float rainIntensityMultiplier = 0.025f;
        FsmFloat rainIntensity;

        void Start()
        {
            rainIntensity = PlayMakerGlobals.Instance.Variables.FindFsmFloat("RainIntensity");
        }

        void FixedUpdate()
        {
            rainFactor = calculateRain ? (1f - rainIntensity.Value * rainIntensityMultiplier) : 1f;
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].sidewaysGripFactor = terrainDictionary[wheels[i].surfaceType][0] * rainFactor;
                wheels[i].forwardGripFactor = terrainDictionary[wheels[i].surfaceType][1] * rainFactor;
            }
        }
    }

    public class AutoClutch : MonoBehaviour
    {
        public AxisCarController CarController;
        public Drivetrain drivetrain;

        string ClutchInput = "Clutch";

        void Update()
        {
            if ((drivetrain.autoClutch && (CarController.brake > 0.5f || CarController.handbrakeInput > 0.5f)) || cInput.GetAxis(ClutchInput) > 0.9f)
                CarController.clutchInput = 1f;
        }
    }

    public class windshieldUVadjuster : MonoBehaviour
    {
        windshield Windshield;
        public float Wiper1x;
        public float Wiper1y;

        public float Wiper2x;
        public float Wiper2y;

        void Start()
        {
            Windshield = transform.GetComponent<windshield>();

            Wiper1x = Windshield.wiper1.x;
            Wiper1y = Windshield.wiper1.y;

            Wiper2x = Windshield.wiper2.x;
            Wiper2y = Windshield.wiper2.y;
        }

        void FixedUpdate()
        {
            Windshield.wiper1 = new Vector2(Wiper1x, Wiper1y);
            Windshield.wiper2 = new Vector2(Wiper2x, Wiper2y);
        }
    }

    public class materialSetColor : MonoBehaviour
    {
        public Material thismat;
        public string thisproperty;

        void OnDisable() => MaterialSetColor(false);
        void OnEnable() => MaterialSetColor(true);

        public void MaterialSetColor(bool enabled)
        {
            thismat.SetColor(thisproperty, enabled? Color.white: Color.black);
        }
    }

    public class materialSetFloat : MonoBehaviour
    {
        public Material thismat;
        public string thisproperty;

        void OnDisable() => MaterialSetFloat(false);
        void OnEnable() => MaterialSetFloat(true);

        public void MaterialSetFloat(bool enabled)
        {
            thismat.SetFloat(thisproperty, enabled ? 1f : 0f);
        }
    }
}