using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Drivable_EDM
{
    public class JokkeGiveCar : MonoBehaviour
    {
        public InteractionRaycast Interaction;
        public bool givekey = true;

        void Start()
        {
            if (Interaction.rayDistance == 0f)
            {
                GameObject.Find("KILJUGUY").transform.Find("HikerPivot/Hitchhiker/Char/skeleton/pelvis/spine_middle/spine_upper/collar_right/shoulder_right/arm_right/hand_right/PayMoney").gameObject.AddComponent<PayMoneySetup>().giveCar = this;
            }
        }

        public void GiveKey()
        {
            Interaction.rayDistance = 1.35f;
            givekey = false;
        }

        public class PayMoneySetup : MonoBehaviour
        {
            public JokkeGiveCar giveCar;

            void OnEnable()
            {
                PlayMakerFSM payMoney = GetComponent<PlayMakerFSM>();

                FsmString Value = payMoney.FsmVariables.FindFsmString("Value");
                FsmState state = payMoney.FsmStates.FirstOrDefault(x => x.Name == "Wait button");
                List<FsmStateAction> actions = state.Actions.ToList();
                actions.Add(new SetKeyString { giveCar = giveCar, Value = Value });
                state.Actions = actions.ToArray();

                state = payMoney.FsmStates.FirstOrDefault(x => x.Name == "State 1");
                actions = state.Actions.ToList();
                actions.Add(new GiveKey { giveCar = giveCar });
                state.Actions = actions.ToArray();

                Destroy(this);
            }

            public class SetKeyString : FsmStateAction
            {
                public JokkeGiveCar giveCar;
                public FsmString Value;

                public override void OnEnter()
                {
                    if (giveCar.givekey)
                        Value.Value += " AND CAR KEYS";

                    Finish();
                }
            }

            public class GiveKey : FsmStateAction
            {
                public JokkeGiveCar giveCar;

                public override void OnEnter()
                {
                    if (giveCar.givekey)
                        giveCar.GiveKey();

                    Finish();
                }
            }
        }
    }

    // Complete!
    public class DrunkGuyLiftHandler : MonoBehaviour
    {
        public PlayerDriveTrigger driveTrigger;
        public FsmString CurrentVehicle;

        public bool customVehicle = false;

        FsmGameObject GetInPivot;
        FsmGameObject PassengerMassObject;

        FsmString AnimGetIn;
        FsmString AnimGetOut;
        FsmString AnimIdle1;
        FsmString AnimIdle2;
        FsmString AnimPay1;
        FsmString AnimPay2;

        GameObject carGetInPivot;
        GameObject carMassPassenger;

        public Door rightDoor;

        SendEvent openDoor1;
        SendEvent openDoor2;

        PlayMakerFSM hitchHiker;

        void Start()
        {
            CurrentVehicle = PlayMakerGlobals.Instance.Variables.FindFsmString("PlayerCurrentVehicle");

            // SET UP ACTIONS
            hitchHiker = GameObject.Find("KILJUGUY").transform.Find("HikerPivot/Hitchhiker").GetComponents<PlayMakerFSM>().FirstOrDefault(fsm => fsm.FsmName == "Logic");
            openDoor1 = hitchHiker.FsmStates.FirstOrDefault(x => x.Name == "Open door").Actions[6] as SendEvent;
            openDoor2 = hitchHiker.FsmStates.FirstOrDefault(x => x.Name == "Get out").Actions[4] as SendEvent;

            StartCoroutine(Delay());
        }

        IEnumerator Delay()
        {
            while (GameObject.Find("TangerinePickup(Clone)") == null) yield return null;
            yield return new WaitForSeconds(2f);
            Setup();
            yield break;
        }

        void Setup()
        {
            InsertAction(hitchHiker, "Which car", new CarCheck { liftHandler = this });
            InsertAction(hitchHiker, "Van", new SetCarVariables { liftHandler = this }, 0);
            InsertAction(hitchHiker, "Open door", new OpenCarDoor { liftHandler = this }, 6);
            InsertAction(hitchHiker, "Get out", new OpenCarDoor { liftHandler = this }, 4);
            InsertAction(hitchHiker, "Start walking", new ResetCarLift { liftHandler = this }, 0);

            // SET UP VARIABLES
            GetInPivot = hitchHiker.FsmVariables.FindFsmGameObject("GetInPivot");
            PassengerMassObject = hitchHiker.FsmVariables.FindFsmGameObject("PassengerMassObject");

            AnimGetIn = hitchHiker.FsmVariables.FindFsmString("AnimGetIn");
            AnimGetOut = hitchHiker.FsmVariables.FindFsmString("AnimGetOut");
            AnimIdle1 = hitchHiker.FsmVariables.FindFsmString("AnimIdle1");
            AnimIdle2 = hitchHiker.FsmVariables.FindFsmString("AnimIdle2");
            AnimPay1 = hitchHiker.FsmVariables.FindFsmString("AnimPay1");
            AnimPay2 = hitchHiker.FsmVariables.FindFsmString("AnimPay2");

            carGetInPivot = transform.Find("GetInPivot").gameObject;
            carMassPassenger = transform.Find("MassPassenger").gameObject;
        }

        public void CustomVehicle()
        {
            customVehicle = true;
            openDoor1.Enabled = false;
            openDoor2.Enabled = false;
        }

        public void SetVariables()
        {
            GetInPivot.Value = carGetInPivot;
            PassengerMassObject.Value = carMassPassenger;

            AnimGetIn.Value = "fat_get_car_in";
            AnimGetOut.Value = "fat_get_car_out";
            AnimIdle1.Value = "fat_get_car_idle1";
            AnimIdle2.Value = "fat_get_car_idle2";
            AnimPay1.Value = "fat_get_car_pay1";
            AnimPay2.Value = "fat_get_car_pay2";
        }

        public void OpenDoor()
        {
            rightDoor.StartOpenNPC();
        }

        public void ResetLift()
        {
            customVehicle = false;
            openDoor1.Enabled = true;
            openDoor2.Enabled = true;
        }

        public class CarCheck : FsmStateAction
        {
            public DrunkGuyLiftHandler liftHandler;
            string sendEvent = "VAN";

            public override void OnEnter()
            {
                if (liftHandler.CurrentVehicle.Value == liftHandler.driveTrigger.vehicleName)
                {
                    liftHandler.CustomVehicle();
                    Fsm.Event(sendEvent);
                }
               
                Finish();
            }
        }

        public class SetCarVariables : FsmStateAction
        {
            public DrunkGuyLiftHandler liftHandler;
            string sendEvent = "FINISHED";
            public override void OnEnter()
            {
                if (liftHandler.customVehicle)
                {
                    liftHandler.SetVariables();
                    Fsm.Event(sendEvent);
                }

                Finish();
            }
        }

        public class OpenCarDoor : FsmStateAction
        {
            public DrunkGuyLiftHandler liftHandler;
            public override void OnEnter()
            {
                if (liftHandler.customVehicle) liftHandler.OpenDoor();

                Finish();
            }

        }

        public class ResetCarLift : FsmStateAction
        {
            public DrunkGuyLiftHandler liftHandler;
            public override void OnEnter()
            {
                if (liftHandler.customVehicle) liftHandler.ResetLift();

                Finish();
            }
        }

        void InsertAction(PlayMakerFSM fsm, string stateName, FsmStateAction action, int insert = -1)
        {
            FsmState state = fsm.FsmStates.FirstOrDefault(x => x.Name == stateName);

            List<FsmStateAction> actions = state.Actions.ToList();

            if (insert != -1) actions.Insert(insert, action);
            else actions.Add(action);

            state.Actions = actions.ToArray();
        }
    }

    // Complete!
    public class DrunkGuyMovingHandler : MonoBehaviour
    {
        public PlayerDriveTrigger driveTrigger;
        public FsmString CurrentVehicle;

        public bool customVehicle = false;

        FsmGameObject GetInPivot;
        FsmGameObject PassengerMassObject;

        FsmString AnimGetIn;
        FsmString AnimGetOut;
        FsmString AnimIdle1;
        FsmString AnimIdle2;
        FsmString AnimPay1;
        FsmString AnimPay2;

        GameObject carGetInPivot;
        GameObject carMassPassenger;

        Transform kiljuguy;

        public Door rightDoor;

        SendEvent openDoor1;
        SendEvent openDoor2;

        PlayMakerFSM mover;

        void Start()
        {
            CurrentVehicle = PlayMakerGlobals.Instance.Variables.FindFsmString("PlayerCurrentVehicle");

            // SET UP ACTIONS
            try
            {
                if (GameObject.Find("JOBS").transform.Find("HouseDrunk/Moving/Hitcher"))
                    mover = GameObject.Find("JOBS").transform.Find("HouseDrunk/Moving/Hitcher").GetComponents<PlayMakerFSM>().FirstOrDefault(fsm => fsm.FsmName == "Hitch");
                else mover = GameObject.Find("Hitcher").GetComponents<PlayMakerFSM>().FirstOrDefault(fsm => fsm.FsmName == "Hitch");
            }
            catch { Debug.Log("EDM: Can't Find moving job."); }

            if (mover != null)
            {
                mover.gameObject.AddComponent<CarSetup>().movingHandler = this;

                // SET UP VARIABLES
                GetInPivot = mover.FsmVariables.FindFsmGameObject("GetInPivot");
                PassengerMassObject = mover.FsmVariables.FindFsmGameObject("PassengerMassObject");

                AnimGetIn = mover.FsmVariables.FindFsmString("AnimGetIn");
                AnimGetOut = mover.FsmVariables.FindFsmString("AnimGetOut");
                AnimIdle1 = mover.FsmVariables.FindFsmString("AnimIdle1");
                AnimIdle2 = mover.FsmVariables.FindFsmString("AnimIdle2");
                AnimPay1 = mover.FsmVariables.FindFsmString("AnimPay1");
                AnimPay2 = mover.FsmVariables.FindFsmString("AnimPay2");

                carGetInPivot = transform.Find("GetInPivot").gameObject;
                carMassPassenger = transform.Find("MassPassenger").gameObject;
                kiljuguy = GameObject.Find("KILJUGUY").transform.Find("HikerPivot/Hitchhiker");
            }
        }

        public void SetupActions()
        {
            openDoor1 = mover.FsmStates.FirstOrDefault(x => x.Name == "Open door 2").Actions[7] as SendEvent;
            openDoor2 = mover.FsmStates.FirstOrDefault(x => x.Name == "Get out").Actions[0] as SendEvent;

            InsertAction(mover, "Which car", new CarCheck { moveHandler = this });
            InsertAction(mover, "Van", new SetCarVariables { moveHandler = this }, 0);
            InsertAction(mover, "Open door 2", new OpenCarDoor { moveHandler = this }, 7);
            InsertAction(mover, "Get out", new OpenCarDoor { moveHandler = this }, 0);
            InsertAction(mover, "Idle", new ResetCarLift { moveHandler = this }, 0);
        }

        public void CustomVehicle()
        {
            customVehicle = true;
            openDoor1.Enabled = false;
            openDoor2.Enabled = false;
        }

        public void SetVariables()
        {
            GetInPivot.Value = carGetInPivot;
            PassengerMassObject.Value = carMassPassenger;

            AnimGetIn.Value = "fat_get_car_in";
            AnimGetOut.Value = "fat_get_car_out";
            AnimIdle1.Value = "fat_get_car_idle1";
            AnimIdle2.Value = "fat_get_car_idle2";
            AnimPay1.Value = "fat_get_car_pay1";
            AnimPay2.Value = "fat_get_car_pay2";
        }

        public void OpenDoor()
        {
            rightDoor.StartOpenNPC();
        }

        public void ResetLift()
        {
            customVehicle = false;
            openDoor1.Enabled = true;
            openDoor2.Enabled = true;
        }

        public class CarSetup : MonoBehaviour
        {
            public DrunkGuyMovingHandler movingHandler;
            void Start()
            {
                movingHandler.SetupActions();
                //Destroy(this);
            }
        }

        public class CarCheck : FsmStateAction
        {
            public DrunkGuyMovingHandler moveHandler;
            string sendEvent = "VAN";

            public override void OnEnter()
            {
                if (moveHandler.CurrentVehicle.Value == moveHandler.driveTrigger.vehicleName)
                {
                    moveHandler.CustomVehicle();
                    Fsm.Event(sendEvent);
                }

                Finish();
            }
        }

        public class SetCarVariables : FsmStateAction
        {
            public DrunkGuyMovingHandler moveHandler;
            string sendEvent = "FINISHED";
            public override void OnEnter()
            {
                if (moveHandler.customVehicle)
                {
                    moveHandler.SetVariables();
                    Fsm.Event(sendEvent);
                }

                Finish();
            }
        }

        public class OpenCarDoor : FsmStateAction
        {
            public DrunkGuyMovingHandler moveHandler;
            public override void OnEnter()
            {
                if (moveHandler.customVehicle)
                    moveHandler.OpenDoor();

                Finish();
            }

        }

        public class ResetCarLift : FsmStateAction
        {
            public DrunkGuyMovingHandler moveHandler;
            public override void OnEnter()
            {
                if (moveHandler.customVehicle)
                    moveHandler.ResetLift();

                Finish();
            }
        }

        void InsertAction(PlayMakerFSM fsm, string stateName, FsmStateAction action, int insert = -1)
        {
            FsmState state = fsm.FsmStates.FirstOrDefault(x => x.Name == stateName);

            List<FsmStateAction> actions = state.Actions.ToList();

            if (insert != -1) actions.Insert(insert, action);
            else actions.Add(action);

            state.Actions = actions.ToArray();
        }
    }
}