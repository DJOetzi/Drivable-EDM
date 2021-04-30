using MSCLoader;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using System.Collections;

namespace Drivable_EDM
{
    public class SaveManager : MonoBehaviour
    {
        public Transform carTransform;

        public InteractionRaycast interactionRaycast;

        public InteriorLight interiorLight;

        public WindowOpener windowOpenerFL;
        public WindowOpener windowOpenerFR;
        public WindowOpener windowOpenerRL;
        public WindowOpener windowOpenerRR;

        public Handbrake handbrake;

        public FuelTank fuelTank;

        public JokkeGiveCar keyManager;

        public GameObject RadioPivot;
        //private CDplayer.CDPlayerFunctions playerFunctions;
        //private CDplayer.CDHandler handler;

        public void Save()
        {
            SaveUtility.Save<SaveData>(new SaveData()
            {
                carPosition = carTransform.position,
                carRotation = carTransform.eulerAngles,
                interiorLightState = interiorLight.lightState,
                windowOpenerFLstate = windowOpenerFL.windowState,
                windowOpenerFRstate = windowOpenerFR.windowState,
                windowOpenerRLstate = windowOpenerRL.windowState,
                windowOpenerRRstate = windowOpenerRR.windowState,
                handbrakePullUp = handbrake.handbrakeRot,
                fuelLevel = fuelTank.fuelLevel,
                playerHasKey = interactionRaycast.rayDistance >= 1f,
                //RADIOCD = playerFunctions.RADIOCD,
                //Channel = playerFunctions.Channel,
                //Partname = handler.Partname
            });
        }

        public void Load()
        {
            SaveData save = SaveUtility.Load<SaveData>();

            carTransform.position = save.carPosition;
            carTransform.eulerAngles = save.carRotation;

            interiorLight.lightState = save.interiorLightState;

            windowOpenerFL.windowState = save.windowOpenerFLstate;
            windowOpenerFR.windowState = save.windowOpenerFRstate;
            windowOpenerRL.windowState = save.windowOpenerRLstate;
            windowOpenerRR.windowState = save.windowOpenerRRstate;

            handbrake.handbrakeRot = save.handbrakePullUp;

            fuelTank.fuelLevel = save.fuelLevel;

            if(!save.playerHasKey)
            {
                interactionRaycast.rayDistance = 0f;
                keyManager.enabled = true;
            }

            cdFix(save);
        }

        void cdFix(SaveData save)
        {
            //CDplayer.CREATORSYSTEM.AddCDplayer(RadioPivot, save.Partname, save.RADIOCD, save.Channel);
            //playerFunctions = RadioPivot.transform.Find("CD_PLAYER(Clone)").GetComponent<CDplayer.CDPlayerFunctions>();
            //handler = RadioPivot.transform.Find("CD_PLAYER(Clone)").GetComponent<CDplayer.CDHandler>();



            //if (playerFunctions.sourcepivot == null) playerFunctions.sourcepivot = RadioPivot.transform.Find("Speaker");
        }
    }

    public class SaveData
    {
        public Vector3 carPosition = new Vector3(1940.531f, 6.720334f, -219.2795f);
        public Vector3 carRotation = new Vector3(358.5721f, 42.27071f, 0.3312485f);

        public int interiorLightState = 1;

        public int windowOpenerFLstate = 0;
        public int windowOpenerFRstate = 0;
        public int windowOpenerRLstate = 0;
        public int windowOpenerRRstate = 0;

        public float handbrakePullUp = 20f;

        public float fuelLevel = 7.5f;

        public bool playerHasKey = false;

        public bool RADIOCD = true;
        public bool Channel = false;
        public int Partname = 0;
    }

    public class SaveUtility
    {
        static string modName = typeof(SaveUtility).Namespace;
        static string path = Path.Combine(Application.persistentDataPath, modName + ".xml");

        public static void Save<T>(T saveData)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                XmlSerializerNamespaces xmlNamespace = new XmlSerializerNamespaces();
                xmlNamespace.Add("", "");
                StreamWriter output = new StreamWriter(path);
                XmlWriterSettings xmlSettings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "    ",
                    NewLineOnAttributes = false,
                    OmitXmlDeclaration = true
                };
                XmlWriter xmlWriter = XmlWriter.Create(output, xmlSettings);
                xmlSerializer.Serialize(xmlWriter, saveData, xmlNamespace);
                xmlWriter.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                ModConsole.Error(modName + ": " + ex.ToString());
            }
        }

        public static SaveData Load<T>()
        {
            try
            {
                if (File.Exists(path))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                    StreamReader input = new StreamReader(path);
                    XmlReader xmlReader = XmlReader.Create(input);
                    return xmlSerializer.Deserialize(xmlReader) as SaveData;
                }
                else return new SaveData();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                ModConsole.Error(modName + ": " + ex.ToString());
                return new SaveData();
            }
        }

        public static void Remove()
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                ModConsole.Print(modName + ": Savefile found and deleted, mod is reset.");
            }
            else ModConsole.Print(modName + ": Savefile not found, mod is already reset.");
        }
    }
}