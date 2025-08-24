using System.Reflection;
using Cysharp.Threading.Tasks.Triggers;
using DV;
using DV.CabControls.Spec;
using DV.Customization;
using DV.Damage;
using DV.HUD;
using DV.Logic.Job;
using DV.MultipleUnit;
using DV.PitStops;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.ThingTypes;
using DV.Utils;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Ludiq;
using Unity.Linq;
using UnityEngine;
using UnityModManagerNet;
using Console = DV.Console;
using Object = UnityEngine.Object;

namespace PassengerMUCable;

    public static class Main
    {
        public static UnityModManager.ModEntry modEntry;
        public static Settings settings = new Settings();

        public static bool Load(UnityModManager.ModEntry entry)
        {
            modEntry = entry;
            
            try
            {
                Settings settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
                if (settings != null)
                {
                    Main.settings = settings;
                    modEntry.Logger.Log("Loaded existing settings");
                }
                else
                {
                    Main.settings = new Settings();
                    modEntry.Logger.Log("Created new settings (no existing file)");
                }
            }
            catch (Exception ex)
            {
                Main.settings = new Settings();
                modEntry.Logger.Log("Failed to load settings, using defaults: " + ex.Message);
            }
            modEntry.OnGUI = settings.Draw;
            modEntry.OnSaveGUI = settings.Save;
            

            modEntry.Logger.Log("PassengerMUCable loading!");
            var harmony = new Harmony(entry.Info.Id);


            harmony.PatchAll();

            modEntry.Logger.Log("PassengerMUCable loaded successfully!");

            return true;
        }
    }


public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Add Multiple Unit Cables to everything (Applies on first load)")]
        public bool AddMultipleUnitCablesToEverything = false;
        public override void Save(UnityModManager.ModEntry entry)
        {
            Save(this, entry);
            CarSpawnerPatch.PatchAllMode = AddMultipleUnitCablesToEverything;
        }

        public void OnChange()
        {
            CarSpawnerPatch.PatchAllMode = AddMultipleUnitCablesToEverything;
        }
    }

    [HarmonyPatch(typeof(OverridableBaseControl))]
    public static class OverridableBaseControlPatch
    {
        [HarmonyPatch(nameof(OverridableBaseControl.Init))]
        [HarmonyPostfix]
        public static void Postfix(TrainCar car, SimulationFlow simFlow, ControlSpec spec, OverridableBaseControl __instance)
        {
            if (CarSpawnerPatch.AddMUToTypes.Contains(car.carType))
            {
                bool notched = true;
                float notches = 1;
                Debug.LogWarning(__instance.GetType());
                
                if (__instance is ThrottleControl)notches = 14;
                if (__instance is BrakeControl)notches = 14;
                if (__instance is IndependentBrakeControl)notches = 14;
                if (__instance is DynamicBrakeControl)notches = 14;
                if (__instance is ReverserControl)notches = 2;
                if (__instance is SanderControl)notches = 2;
                
                CarSpawnerPatch.SetPrivateField(__instance, "NotchCount",  notches);
                CarSpawnerPatch.SetPrivateField(__instance, "IsNotched", notched);

            }
        }
    }

    [HarmonyPatch(typeof(CarSpawner))]
    public static class CarSpawnerPatch
    {
        private static bool HasPatched = false;

        public static bool PatchAllMode = Main.settings.AddMultipleUnitCablesToEverything;

        public static TrainCarType[] AddMUToTypes = new TrainCarType[]
        {
            TrainCarType.CabooseRed,
            TrainCarType.PassengerBlue,
            TrainCarType.PassengerRed,
            TrainCarType.PassengerGreen
        };

        public static List<TrainCarLivery> MuLiveries;

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (HasPatched) return;
            HasPatched = true;
            
            MuLiveries = new List<TrainCarLivery>();

            TrainCarLivery de6 = null;
            foreach (TrainCarLivery livery in Globals.G.Types.Liveries)
            {
                TrainCar car = livery.prefab.GetComponent<TrainCar>();
                if (!PatchAllMode && AddMUToTypes.Contains(car.carType)) MuLiveries.Add(livery);
                else if (PatchAllMode && !CarTypes.IsLocomotive(livery)) MuLiveries.Add(livery);

                TrainCar loco = livery.prefab.GetComponent<TrainCar>();
                if (loco.carType == TrainCarType.LocoDE6Slug) de6 = livery;
            }

            foreach (var car in MuLiveries)
            {
                AddMuTo(car, Object.Instantiate(de6));
            }
        }
        
        private static void AddMuTo(TrainCarLivery carToMU, TrainCarLivery de6)
        {
            Debug.Log("MU-ing Car Type of: "+carToMU.prefab.name);
            
            Transform carToMuBuffsParent = carToMU.prefab.transform.Find("[buffers]");
            if (carToMuBuffsParent == null|| carToMU.prefab.gameObject.GetComponent<MultipleUnitModule>()!=null)
            {
                Debug.Log("Car Type of: "+carToMU.prefab.name+ " is incompatible");

                return;
            }
            List<Transform> carToMuBuffs = GetAllChildrenByName(carToMuBuffsParent, "BuffersAndChainRig");
            
            if (carToMuBuffs.Count < 2)
            {
                Debug.Log("Car Type of: "+carToMU.prefab.name+ " is incompatible");

                return;
            }
            
            Transform de6BuffsParent = de6.prefab.transform.Find("[buffers]");
            List<Transform> de6Buffs = GetAllChildrenByName(de6BuffsParent, "BuffersAndChainRigMU");

            if (carToMU.prefab.transform.Find("[sim]") == null)
            {
                GameObject sim = new GameObject("[sim]");
                sim.transform.SetParent(carToMU.prefab.transform);
                carToMU.prefab.gameObject.AddComponent<DamageController>();
                carToMU.prefab.gameObject.AddComponent<SimController>().connectionsDefinition = sim.AddComponent<SimConnectionDefinition>();
            }
            
            GameObject controles = carToMU.prefab.transform.Find("[sim]").gameObject;
            
            carToMU.interiorPrefab?.AddComponent<InteriorControlsManager>();

            carToMU.prefab.gameObject.GetComponent<SimController>().controlsOverrider = controles.AddComponent<BaseControlsOverrider>();

            if (carToMU.interiorPrefab != null)
            {
                RegisterControles(controles);
            }
            


            carToMU.prefab.transform.Find("[sim]").AddComponent<ControlsBlockController>();

            carToMU.prefab.AddComponent<MultipleUnitStateObserver>();
            MultipleUnitModule mu = carToMU.prefab.AddComponent<MultipleUnitModule>();
            

            Transform newMUA = Object.Instantiate(de6Buffs[0].Find("hoses").Find("CouplingHoseRigMU"));

            
            newMUA.transform.position = carToMuBuffs[0].Find("hoses").Find("CouplingHoseRig").position;
            newMUA.transform.rotation = carToMuBuffs[0].Find("hoses").Find("CouplingHoseRig").rotation;
            newMUA.parent = carToMuBuffs[0].Find("hoses");


            Vector3 muApos = newMUA.transform.position;
            muApos = new Vector3(-muApos.x, carToMuBuffs[0].Find("hoses").Find("CouplingHoseRig").transform.position.y,muApos.z);
            newMUA.position = muApos;
            mu.frontCableAdapter = newMUA.GetComponent<CouplingHoseMultipleUnitAdapter>();


            //other side

            Transform newMUB = Object.Instantiate(de6Buffs[1].Find("hoses").Find("CouplingHoseRigMU"));
            newMUB.transform.position = carToMuBuffs[1].Find("hoses").Find("CouplingHoseRig").position;
            newMUB.transform.rotation = carToMuBuffs[1].Find("hoses").Find("CouplingHoseRig").rotation;
            newMUB.parent = carToMuBuffs[1].Find("hoses");
            



            Vector3 muBpos = newMUB.transform.position;
            muBpos = new Vector3(-muBpos.x, carToMuBuffs[1].Find("hoses").Find("CouplingHoseRig").transform.position.y,muBpos.z);
            newMUB.position = muBpos;
            mu.rearCableAdapter = newMUB.GetComponent<CouplingHoseMultipleUnitAdapter>();

            carToMuBuffs[0].Find("hoses").GetComponent<CouplingHoseDelayedEnable>().childrenToEnable = ExtendHoseChildren(
                carToMuBuffs[0].Find("hoses").GetComponent<CouplingHoseDelayedEnable>().childrenToEnable, newMUA.gameObject);
            carToMuBuffs[1].Find("hoses").GetComponent<CouplingHoseDelayedEnable>().childrenToEnable = ExtendHoseChildren(
                carToMuBuffs[1].Find("hoses").GetComponent<CouplingHoseDelayedEnable>().childrenToEnable, newMUB.gameObject);

            carToMuBuffs[0].name = "BuffersAndChainRigMU";
            carToMuBuffs[1].name = "BuffersAndChainRigMU";

            Object.Destroy(de6);
        }
        
        private static void RegisterControles(GameObject controles)
        {
            SimComponentDefinition[] oldSimDefs = controles.gameObject.GetComponent<SimConnectionDefinition>().executionOrder;
            int size = +6;
            if(oldSimDefs!=null) size += oldSimDefs.Length;
            SimComponentDefinition[] simDefs = new SimComponentDefinition[size];

            
            BaseControlsOverrider controlesOverrider = controles.GetComponent<BaseControlsOverrider>();
            
            GameObject throttle = new GameObject("Throttle", typeof(ThrottleControl));
            throttle.transform.SetParent(controles.transform);
            string throttlePortID = "throttle.EXT_IN";
            string throttleMainID = "throttle";
            ThrottleControl throttleControl = throttle.GetComponent<ThrottleControl>();
            throttleControl.portId = throttlePortID;
            simDefs[0] = throttle.AddComponent<ExternalControlDefinition>();
            simDefs[0].ID = throttleMainID;
            SetPrivateField(controlesOverrider, "throttle", throttleControl);
            
            GameObject brake = new GameObject("Brake", typeof(BrakeControl));
            brake.transform.SetParent(controles.transform);
            string brakePortID = "brake.EXT_IN";
            string brakeMainID = "brake";
            BrakeControl brakeControl = brake.GetComponent<BrakeControl>();
            brakeControl.portId = brakePortID;
            SetPrivateField<OverridableBaseControl>(brakeControl, "NotchCount", 14);
            SetPrivateField<OverridableBaseControl>(brakeControl, "IsNotched", true);
            simDefs[1] = brake.AddComponent<ExternalControlDefinition>();
            simDefs[1].ID = brakeMainID;
            SetPrivateField(controlesOverrider, "brake", brakeControl);

            GameObject independentBrake = new GameObject("IndependentBrake", typeof(IndependentBrakeControl));
            independentBrake.transform.SetParent(controles.transform);
            string independentBrakePortID = "indBrake.EXT_IN";
            string independentBrakeMainID = "indBrake";
            IndependentBrakeControl independentBrakeControl = independentBrake.GetComponent<IndependentBrakeControl>();
            independentBrakeControl.portId = independentBrakePortID;
            SetPrivateField<OverridableBaseControl>(independentBrakeControl, "NotchCount", 14);
            SetPrivateField<OverridableBaseControl>(independentBrakeControl, "IsNotched", true);
            simDefs[2] = independentBrake.AddComponent<ExternalControlDefinition>();
            simDefs[2].ID = independentBrakeMainID;
            SetPrivateField(controlesOverrider, "independentBrake", independentBrakeControl);
            
            GameObject dynamicBrake = new GameObject("DynamicBrake", typeof(DynamicBrakeControl));
            dynamicBrake.transform.SetParent(controles.transform);
            string dynamicBrakePortID = "dynamicBrake.EXT_IN";
            string dynamicBrakeMainID = "dynamicBrake";
            DynamicBrakeControl dynamicBrakeControl = dynamicBrake.GetComponent<DynamicBrakeControl>();
            dynamicBrakeControl.portId = dynamicBrakePortID;
            SetPrivateField<OverridableBaseControl>(dynamicBrakeControl, "NotchCount", 14);
            SetPrivateField<OverridableBaseControl>(dynamicBrakeControl, "IsNotched", true);
            simDefs[3] = dynamicBrake.AddComponent<ExternalControlDefinition>();
            simDefs[3].ID = dynamicBrakeMainID;
            SetPrivateField(controlesOverrider, "dynamicBrake", dynamicBrakeControl);
                
            GameObject reverser = new GameObject("Reverser", typeof(ReverserControl));
            reverser.transform.SetParent(controles.transform);
            string reverserPortID = "reverser.CONTROL_EXT_IN";
            string reverserMainID = "reverser";
            ReverserControl reverserControl = reverser.GetComponent<ReverserControl>();
            reverserControl.portId = reverserPortID;
            SetPrivateField<OverridableBaseControl>(reverserControl, "NotchCount", 2);
            SetPrivateField<OverridableBaseControl>(reverserControl, "IsNotched", true);
            simDefs[4] = reverser.AddComponent<ReverserDefinition>();
            simDefs[4].ID = reverserMainID;
            SetPrivateField(controlesOverrider, "reverser", reverserControl);

            GameObject sander = new GameObject("Sand", typeof(SanderControl));
            sander.transform.SetParent(controles.transform);
            string sanderPortID = "sander.CONTROL_EXT_IN";
            string sanderMainID = "sander";
            SanderControl sanderControl = sander.GetComponent<SanderControl>();
            sanderControl.portId = sanderPortID;
            simDefs[5] = sander.AddComponent<SanderDefinition>();
            simDefs[5].ID = sanderMainID;
            SetPrivateField(controlesOverrider, "sander", sanderControl);
            
            if(oldSimDefs!=null) for (int i = 0; i < oldSimDefs.Length; i++) simDefs[i+6] = oldSimDefs[i];
            
            controles.gameObject.GetComponent<SimConnectionDefinition>().executionOrder = simDefs;
        }
        
        
        public static List<Transform> GetAllChildrenByName(Transform parent, string childName)
        {
            List<Transform> foundChildren = new List<Transform>();
            
            RecursiveFindAllChildren(parent, childName, foundChildren);
            
            return foundChildren;
        }

        private static void RecursiveFindAllChildren(Transform parent, string childName, List<Transform> results)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    results.Add(child);
                }

                RecursiveFindAllChildren(child, childName, results);
            }
        }

        private static GameObject[] ExtendHoseChildren(GameObject[] hoses, GameObject newHose)
        {
            GameObject[] newChildren = new GameObject[hoses.Length + 1];

            for (int i = 0; i < hoses.Length; i++)
            {
                newChildren[i] = hoses[i];
            }

            newChildren[newChildren.Length - 1] = newHose;
            return newChildren;
        }
        
        public static void SetPrivateField<TInstance>(TInstance instance, string fieldName, object value)
        {
            PropertyInfo property = AccessTools.Property(typeof(TInstance), fieldName);
        
            if (property != null)
            {
                property.SetValue(instance, value, null);
                return;
            }

            FieldInfo field = AccessTools.Field(typeof(TInstance), fieldName);
        
            if (field == null)
            {
                string backingFieldName = $"<{fieldName}>k__BackingField";
                field = AccessTools.Field(typeof(TInstance), backingFieldName);
            }

            // Check if the field was found.
            if (field == null)
            {
                Debug.LogError($"Could not find a property, private field, or property backing field named '{fieldName}' on instance of type {typeof(TInstance).FullName}.");
                return;
            }

            // Use the SetValue method to assign the new value to the field on the given instance.
            field.SetValue(instance, value);
        }
        
    }
