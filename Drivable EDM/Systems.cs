using UnityEngine;
using MSCLoader;
using HutongGames.PlayMaker;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.Audio;

namespace Drivable_EDM
{
    public class Dash : MonoBehaviour
    {
        public Drivetrain drivetrain;

        public FsmFloat Speedo;
        public FsmFloat Tacho;

        void Update()
        {
            Speedo.Value = Mathf.Abs(Mathf.Clamp(drivetrain.differentialSpeed - 10f, 0f, Mathf.Infinity));
            Tacho.Value = drivetrain.rpm;
        }
    }

    public class InteractionRaycast : MonoBehaviour
    {
        public RaycastHit hitInfo;

        public bool hasHit = false;
        public float rayDistance = 1.35f;
        public LayerMask layerMask;

        void Start()
        {
            hitInfo = new RaycastHit();
        }

        void FixedUpdate()
        {
            if (Camera.main != null) hasHit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, rayDistance, layerMask);
        }

        public bool GetHit(Collider collider) => hasHit && hitInfo.collider == collider;
        public bool GetHitAny(Collider[] colliders) => hasHit && colliders.Any(collider => collider == hitInfo.collider);
        public bool GetHitAny(List<Collider> colliders) => hasHit && colliders.Any(collider => collider == hitInfo.collider);
    }

    public class FourDoorFix : MonoBehaviour
    {
        public List<Collider> DoorColls;
        public GameObject DoorsParent;

        void Start()
        {
            Apply();
        }

        void OnEnable()
        {
            Apply();
        }

        void Apply()
        {
            DoorsParent.SetActive(true);

            Physics.IgnoreCollision(DoorColls[0], DoorColls[1]);
            Physics.IgnoreCollision(DoorColls[0], DoorColls[2]);
            Physics.IgnoreCollision(DoorColls[0], DoorColls[3]);

            Physics.IgnoreCollision(DoorColls[1], DoorColls[0]);
            Physics.IgnoreCollision(DoorColls[1], DoorColls[2]);
            Physics.IgnoreCollision(DoorColls[1], DoorColls[3]);

            Physics.IgnoreCollision(DoorColls[2], DoorColls[0]);
            Physics.IgnoreCollision(DoorColls[2], DoorColls[1]);
            Physics.IgnoreCollision(DoorColls[2], DoorColls[3]);

            Physics.IgnoreCollision(DoorColls[3], DoorColls[0]);
            Physics.IgnoreCollision(DoorColls[3], DoorColls[1]);
            Physics.IgnoreCollision(DoorColls[3], DoorColls[2]);
        }
    }

    public class Door : MonoBehaviour
    {
        public InteractionRaycast Interaction;
        public Rigidbody rigidbody;

        public bool doorOpen = false;

        Rigidbody doorRigidbody;

        public Collider handle;
        FixedJoint lockJoint;

        bool doorMoving = false;
        bool doorNPCMoving = false;

        public float openTorque = 150f;
        public float closeTorque = -150f;

        public float openPos = 80f;
        Quaternion openRot;
        public float closePos = 0f;
        Quaternion closeRot;

        float lockJointBreakForceClosed = 24000f;
        float lockJointBreakForceOpen = 280f;

        bool mouseOver = false;
        FsmBool guiUse;

        void Start()
        {
            rigidbody = transform.root.GetComponent<Rigidbody>();

            doorRigidbody = GetComponent<Rigidbody>();

            openRot = Quaternion.Euler(0f, openPos, 0f);
            closeRot = Quaternion.Euler(0f, closePos, 0f);

            lockJoint = gameObject.AddComponent<FixedJoint>();
            lockJoint.connectedBody = rigidbody;
            lockJoint.breakForce = lockJointBreakForceClosed;
            lockJoint.breakTorque = lockJointBreakForceClosed;

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
        }

        void Update()
        {
            if (Interaction.GetHit(handle))
            {
                mouseOver = true;
                guiUse.Value = true;

                if (Input.GetMouseButtonDown(0) && !doorMoving && !doorNPCMoving) StartCoroutine(DoorAction());
            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
            }

            if (Input.GetMouseButtonUp(0) && doorMoving) doorMoving = false;
        }

        void OnJointBreak(float breakForce)
        {
            Debug.Log($"EDM: Door Joint broken at {breakForce}");
            doorOpen = true;
        }

        IEnumerator DoorAction()
        {
            doorMoving = true;
            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            bool playOpenSound = false;
            if (lockJoint != null)
            {
                playOpenSound = true;
                Destroy(lockJoint);
            }

            if (!doorOpen)
            {
                doorOpen = true;
                if (playOpenSound) MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "open_door1");
                while (doorMoving)
                {
                    doorRigidbody.AddRelativeTorque(0f, openTorque, 0f, ForceMode.Force);

                    if (Quaternion.Angle(transform.localRotation, openRot) <= 0.01f)
                    {
                        lockJoint = gameObject.AddComponent<FixedJoint>();
                        lockJoint.connectedBody = rigidbody;
                        yield return new WaitForSeconds(0.1f);
                        lockJoint.breakTorque = lockJointBreakForceOpen;
                        break;
                    }
                    yield return wait;
                }
            }
            else
            {
                while (doorMoving)
                {
                    doorRigidbody.AddRelativeTorque(0f, closeTorque, 0f, ForceMode.Force);

                    if (Quaternion.Angle(transform.localRotation, closeRot) <= 0.01f)
                    {
                        doorOpen = false;
                        transform.localEulerAngles = new Vector3(0f, closePos, 0f);
                        MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "close_door1");

                        lockJoint = gameObject.AddComponent<FixedJoint>();
                        lockJoint.connectedBody = rigidbody;
                        lockJoint.breakForce = lockJointBreakForceClosed;
                        lockJoint.breakTorque = lockJointBreakForceClosed;
                        break;
                    }
                    yield return wait;
                }
            }
            doorMoving = false;
        }

        public void StartOpenNPC()
        {
            StartCoroutine(NPCOpen());
        }

        public IEnumerator NPCOpen()
        {
            Debug.Log("EDM: NPC Door open begun");

            doorNPCMoving = true;
            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            bool playOpenSound = false;
            if (lockJoint != null)
            {
                playOpenSound = true;
                Destroy(lockJoint);
            }

            doorOpen = true;
            if (playOpenSound) MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "open_door1");

            while (doorNPCMoving)
            {
                doorRigidbody.AddRelativeTorque(0f, openTorque, 0f, ForceMode.Force);

                if (Quaternion.Angle(transform.localRotation, openRot) <= 0.1f)
                {
                    Debug.Log("EDM: NPC Door open lock");

                    yield return new WaitForSeconds(0.1f);
                    lockJoint = gameObject.AddComponent<FixedJoint>();
                    lockJoint.connectedBody = rigidbody;
                    yield return new WaitForSeconds(0.1f);
                    lockJoint.breakTorque = lockJointBreakForceOpen;

                    break;
                }

                yield return wait;
            }

            yield return new WaitForSeconds(2.7f);

            Destroy(lockJoint);

            yield return null;

            Debug.Log("EDM: NPC Door close begun");
            while (doorNPCMoving)
            {
                doorRigidbody.AddRelativeTorque(0f, closeTorque, 0f, ForceMode.Force);

                if (Quaternion.Angle(transform.localRotation, closeRot) <= 0.1f)
                {
                    Debug.Log("EDM: NPC Door close lock");

                    doorOpen = false;
                    transform.localEulerAngles = new Vector3(0f, closePos, 0f);
                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "close_door1");

                    lockJoint = gameObject.AddComponent<FixedJoint>();
                    lockJoint.connectedBody = rigidbody;
                    lockJoint.breakForce = lockJointBreakForceClosed;
                    lockJoint.breakTorque = lockJointBreakForceClosed;

                    break;
                }

                yield return wait;
            }

            doorNPCMoving = false;

            Debug.Log("TANGERINE: NPC Door done");
        }
    }

    public class Bootlid : MonoBehaviour
    {
        public InteractionRaycast Interaction;
        public Rigidbody rigidbody;

        public bool BootlidOpen = false;

        Rigidbody BootlidRigidbody;

        public Collider handle;
        FixedJoint lockJoint;

        bool BootlidMoving = false;

        public float openTorque = 150f;
        public float closeTorque = -150f;

        public float openPos = 80f;
        Quaternion openRot;
        public float closePos = 0f;
        Quaternion closeRot;

        float lockJointBreakForceClosed = 24000f;
        float lockJointBreakForceOpen = 280f;

        bool mouseOver = false;
        FsmBool guiUse;

        void Start()
        {
            rigidbody = transform.root.GetComponent<Rigidbody>();

            BootlidRigidbody = GetComponent<Rigidbody>();

            openRot = Quaternion.Euler(openPos, 0f, 0f);
            closeRot = Quaternion.Euler(closePos, 0f, 0f);

            lockJoint = gameObject.AddComponent<FixedJoint>();
            lockJoint.connectedBody = rigidbody;
            lockJoint.breakForce = lockJointBreakForceClosed;
            lockJoint.breakTorque = lockJointBreakForceClosed;

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
        }

        void Update()
        {
            if (Interaction.GetHit(handle))
            {
                mouseOver = true;
                guiUse.Value = true;

                if (Input.GetMouseButtonDown(0) && !BootlidMoving) StartCoroutine(BootlidAction());
            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
            }

            if (Input.GetMouseButtonUp(0) && BootlidMoving) BootlidMoving = false;
        }

        void OnJointBreak(float breakForce)
        {
            Debug.Log($"EDM: Bootlid Joint broken at {breakForce}");
            BootlidOpen = true;
        }

        IEnumerator BootlidAction()
        {
            BootlidMoving = true;
            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            bool playOpenSound = false;
            if (lockJoint != null)
            {
                playOpenSound = true;
                Destroy(lockJoint);
            }

            if (!BootlidOpen)
            {
                BootlidOpen = true;
                if (playOpenSound) MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "open_trunk1");
                while (BootlidMoving)
                {
                    BootlidRigidbody.AddRelativeTorque(openTorque, 0f, 0f, ForceMode.Force);

                    if (Quaternion.Angle(transform.localRotation, openRot) <= 0.01f)
                    {
                        lockJoint = gameObject.AddComponent<FixedJoint>();
                        lockJoint.connectedBody = rigidbody;
                        yield return new WaitForSeconds(0.1f);
                        lockJoint.breakTorque = lockJointBreakForceOpen;
                        break;
                    }
                    yield return wait;
                }
            }
            else
            {
                while (BootlidMoving)
                {
                    BootlidRigidbody.AddRelativeTorque(closeTorque, 0f, 0f, ForceMode.Force);

                    if (Quaternion.Angle(transform.localRotation, closeRot) <= 0.01f)
                    {
                        BootlidOpen = false;
                        transform.localEulerAngles = new Vector3(closePos, 0f, 0f);
                        if (playOpenSound) MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "close_trunk1");

                        lockJoint = gameObject.AddComponent<FixedJoint>();
                        lockJoint.connectedBody = rigidbody;
                        lockJoint.breakForce = lockJointBreakForceClosed;
                        lockJoint.breakTorque = lockJointBreakForceClosed;
                        break;
                    }
                    yield return wait;
                }
            }
            BootlidMoving = false;
        }
    }

    public class Handbrake : MonoBehaviour
    {
        public Wheel[] HandbrakeWheels;
        public InteractionRaycast Interaction;
        public AxisCarController CarController;
        public Drivetrain drivetrain;

        public GameObject HandbrakeIndicator;

        MeshCollider handbrakeCollider;

        bool handbrakeInAction = false;
        bool handbrakeOn = false;

        public float handbrakeRot = 20f;
        float handbrakeUp = 20f;
        float handbrakeDown = 0f;

        bool mouseOver = false;
        FsmBool guiUse;
        FsmString guiInteraction;
        string interaction = " Handbrake ";

        Coroutine currentHandbrake;

        void Start()
        {
            transform.localEulerAngles = new Vector3(handbrakeRot, 0f, 0f);
            handbrakeOn = (handbrakeRot * 0.05f) > 0.1f;

            handbrakeCollider = GetComponent<MeshCollider>();

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        }

        void Update()
        {
            if (Interaction.GetHit(handbrakeCollider))
            {
                mouseOver = true;
                guiUse.Value = true;
                guiInteraction.Value = interaction;

                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && !handbrakeInAction)
                {
                    if (currentHandbrake != null) StopCoroutine(currentHandbrake);

                    currentHandbrake = StartCoroutine(Input.GetMouseButtonDown(0) ? HandbrakeAction("handbrake_on", 0, 100f) : HandbrakeAction("handbrake_off", 1, -100f));
                }
            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }

            HandbrakeIndicator.SetActive(handbrakeOn);

            if (handbrakeOn)
            {
                foreach(Wheel wheel in HandbrakeWheels)
                {
                    wheel.handbrake = Mathf.Clamp(handbrakeRot * 0.05f, 0f, 1f);
                }
                CarController.handbrakeInput = Mathf.Clamp(handbrakeRot * 0.05f, 0f, 1f);
            }
        }

        IEnumerator HandbrakeAction(string sound, int mouseButton, float direction)
        {
            handbrakeInAction = true;

            MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: sound);

            while (handbrakeInAction)
            {
                if (Input.GetMouseButtonUp(mouseButton)) break;

                handbrakeRot = Mathf.Clamp(handbrakeRot + direction * Time.deltaTime, handbrakeDown, handbrakeUp);
                transform.localEulerAngles = new Vector3(handbrakeRot, 0f, 0f);

                handbrakeOn = (handbrakeRot * 0.05f) > 0.1f;

                yield return null;
            }

            handbrakeInAction = false;
        }
    }

    public class GearStick : MonoBehaviour
    {
        public Transform Pivot;
        public Vector3[] Rotations;
        public Drivetrain drivetrain;

        void FixedUpdate()
        {
            Pivot.localEulerAngles = Rotations[drivetrain.gear];
        }
    }

    public class CollisionSound : MonoBehaviour
    {
        int[] layers = new int[] { 0, 8, 10, 26 };

        string[] sounds = new string[] { "crash_hi1", "crash_hi2", "crash_low1", "crash_low2" };

        float maxVolume = 1.25f;
        float minVolume = 0.25f;

        void OnCollisionEnter(Collision collision)
        {
            if (layers.Contains(collision.gameObject.layer) && collision.relativeVelocity.magnitude > 1)
                MasterAudio.PlaySound3DAtVector3AndForget("Crashes", collision.contacts[0].point, volumePercentage: Mathf.Clamp(collision.relativeVelocity.magnitude / 5f, minVolume, maxVolume), variationName: sounds[Random.Range(0, 4)]);
        }
    }

    public class AmbientVolume : MonoBehaviour
    {
        AudioMixer ambience;
        string ambienceName = "VolAmbience";

        float ambienceVolumeMax = 0f;
        float ambienceVolumeMin = -8f;

        public float adjustedAmbientVolume = 0f;

        public Door[] doors;
        public WindowOpener[] windows;

        public void Awake()
        {
            ambience = (PlayMakerFSM.FindFsmOnGameObject(GameObject.Find("SATSUMA(557kg, 248)/PlayerTrigger"), "PlayerTrigger").FsmStates[0].Actions[0] as HutongGames.PlayMaker.Actions.AudioMixerSetFloatValue).theMixer.Value as AudioMixer;
        }

        void OnEnable()
        {
            StartCoroutine(VolumeControl());
        }

        void OnDisable() => StopAllCoroutines();

        IEnumerator VolumeControl()
        {
            WaitForSeconds wait = new WaitForSeconds(0.3f);
            while (true)
            {
                if (doors.Any(door => door.doorOpen))
                    ambience.SetFloat(ambienceName, ambienceVolumeMax);
                else
                {
                    adjustedAmbientVolume = Mathf.Clamp(ambienceVolumeMin + (windows[0].windowState * (4f / 60f)) + (windows[1].windowState * (4f / 60f) + windows[2].windowState * (4f / 60f)) + (windows[3].windowState * (4f / 60f)), ambienceVolumeMin, ambienceVolumeMax);
                    ambience.SetFloat(ambienceName, adjustedAmbientVolume);
                }
                yield return wait;
            }
        }
    }

    public class InteriorLight : MonoBehaviour
    {
        public int lightState = 1;

        public InteractionRaycast Interaction;

        public Collider switchCollider;

        public Material lightMaterial;
        public GameObject lightObject;

        public Door[] doors;

        bool mouseOver = false;
        FsmBool guiUse;
        FsmString guiInteraction;
        string[] interactionText = new string[]
        {
            " *ON* | DOOR OPEN | OFF ",
            " ON | *DOOR OPEN* | OFF ",
            " ON | DOOR OPEN | *OFF* "
        };

        void Start()
        {
            if (lightState == 1) StartCoroutine(DoorChecking());
            else SwitchLight(lightState == 0);

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
        }

        void Update()
        {
            if (Interaction.GetHit(switchCollider))
            {
                mouseOver = true;
                guiUse.Value = true;
                guiInteraction.Value = interactionText[lightState];

                if (Input.GetMouseButtonDown(0))
                {
                    lightState = (lightState + 1) % 3;
                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "dash_button", volumePercentage: 0.4f);

                    StopAllCoroutines();
                    if (lightState == 1) StartCoroutine(DoorChecking());
                    else SwitchLight(lightState == 0);

                }

            }
            else if (mouseOver)
            {
                mouseOver = false;
                guiUse.Value = false;
                guiInteraction.Value = string.Empty;
            }
        }

        void SwitchLight(bool lightOn)
        {
            lightObject.SetActive(lightOn);
            lightMaterial.SetFloat("_Intensity", lightOn ? 1f : 0f);
        }

        IEnumerator DoorChecking()
        {
            WaitForSeconds wait = new WaitForSeconds(0.2f);
            while (lightState == 1)
            {
                SwitchLight(doors.Any(door => door.doorOpen));
                yield return wait;
            }
        }
    }
}