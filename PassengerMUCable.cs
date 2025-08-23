using System.Reflection;
using Cysharp.Threading.Tasks.Triggers;
using DV;
using DV.Customization;
using DV.Damage;
using DV.Logic.Job;
using DV.MultipleUnit;
using DV.PitStops;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.ThingTypes;
using DV.Utils;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Ludiq;
using Unity.Linq;
using UnityEngine;
using UnityModManagerNet;
using Object = UnityEngine.Object;

namespace PassengerMUCable;

    public static class Main
    {
        public static UnityModManager.ModEntry modEntry;

        public static bool Load(UnityModManager.ModEntry entry)
        {
            modEntry = entry;

            modEntry.Logger.Log("PassengerMUCable loading!");
            var harmony = new Harmony(entry.Info.Id);


            harmony.PatchAll();

            modEntry.Logger.Log("Your mod has been loaded successfully!");

            return true;
        }
    }


    [HarmonyPatch(typeof(CarSpawner))]
    public static class CarSpawnerPatch
    {

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
            MuLiveries = new List<TrainCarLivery>();
            Debug.Log("Prefix started");

            TrainCarLivery de6 = null;
            foreach (TrainCarLivery livery in Globals.G.Types.Liveries)
            {
                TrainCar car = livery.prefab.GetComponent<TrainCar>();
                Debug.logger.Log(livery.name + " is " + car.carType);
                if (AddMUToTypes.Contains(car.carType)) MuLiveries.Add(livery);

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
            Debug.Log("w");

            if (carToMU.prefab.transform.Find("[sim]") == null)
            {
                GameObject sim = new GameObject("[sim]");
                sim.transform.SetParent(carToMU.prefab.transform);
                carToMU.prefab.gameObject.AddComponent<DamageController>();
                carToMU.prefab.gameObject.AddComponent<SimController>().connectionsDefinition = sim.AddComponent<SimConnectionDefinition>();
            }

            carToMU.prefab.gameObject.GetComponent<SimController>().controlsOverrider =
                carToMU.prefab.transform.Find("[sim]").AddComponent<BaseControlsOverrider>();

            Debug.Log("w1");


            carToMU.prefab.transform.Find("[sim]").AddComponent<ControlsBlockController>();

            Debug.Log("x");


            carToMU.prefab.AddComponent<MultipleUnitStateObserver>();
            MultipleUnitModule mu = carToMU.prefab.AddComponent<MultipleUnitModule>();

            Transform de6BuffsParent = de6.prefab.transform.Find("[buffers]");
            List<Transform> de6Buffs = GetAllChildrenByName(de6BuffsParent, "BuffersAndChainRigMU");


            Transform cabooBuffsParent = carToMU.prefab.transform.Find("[buffers]");
            List<Transform> cabooBuffs = GetAllChildrenByName(cabooBuffsParent, "BuffersAndChainRig");

            Transform newMUA = Object.Instantiate(de6Buffs[0].Find("hoses").Find("CouplingHoseRigMU"));


            Debug.Log("c1 " + de6.prefab.transform.Find("[buffers]").childCount);
            newMUA.transform.position = cabooBuffs[0].Find("hoses").Find("CouplingHoseRig").position;
            newMUA.transform.rotation = cabooBuffs[0].Find("hoses").Find("CouplingHoseRig").rotation;
            newMUA.parent = cabooBuffs[0].Find("hoses");


            Vector3 muApos = newMUA.transform.position;
            muApos = new Vector3(-muApos.x, cabooBuffs[0].Find("hoses").Find("CouplingHoseRig").transform.position.y,
                muApos.z);
            newMUA.position = muApos;
            mu.frontCableAdapter = newMUA.GetComponent<CouplingHoseMultipleUnitAdapter>();


            //other side

            Transform newMUB = Object.Instantiate(de6Buffs[1].Find("hoses").Find("CouplingHoseRigMU"));
            newMUB.transform.position = cabooBuffs[1].Find("hoses").Find("CouplingHoseRig").position;
            newMUB.transform.rotation = cabooBuffs[1].Find("hoses").Find("CouplingHoseRig").rotation;
            newMUB.parent = cabooBuffs[1].Find("hoses");
            ;



            Vector3 muBpos = newMUB.transform.position;
            muBpos = new Vector3(-muBpos.x, cabooBuffs[1].Find("hoses").Find("CouplingHoseRig").transform.position.y,
                muBpos.z);
            newMUB.position = muBpos;
            mu.rearCableAdapter = newMUB.GetComponent<CouplingHoseMultipleUnitAdapter>();

            cabooBuffs[0].Find("hoses").GetComponent<CouplingHoseDelayedEnable>().childrenToEnable = ExtendHoseChildren(
                cabooBuffs[0].Find("hoses").GetComponent<CouplingHoseDelayedEnable>().childrenToEnable, newMUA.gameObject);
            cabooBuffs[1].Find("hoses").GetComponent<CouplingHoseDelayedEnable>().childrenToEnable = ExtendHoseChildren(
                cabooBuffs[1].Find("hoses").GetComponent<CouplingHoseDelayedEnable>().childrenToEnable, newMUB.gameObject);

            cabooBuffs[0].name = "BuffersAndChainRigMU";
            cabooBuffs[1].name = "BuffersAndChainRigMU";

            Object.Destroy(de6);
        }

        public static List<Transform> GetAllChildrenByName(Transform parent, string childName)
        {
            // Create a new list to store the found transforms.
            List<Transform> foundChildren = new List<Transform>();

            // Call the recursive helper function to start the search.
            RecursiveFindAllChildren(parent, childName, foundChildren);

            // Return the list of found children.
            return foundChildren;
        }

        private static void RecursiveFindAllChildren(Transform parent, string childName, List<Transform> results)
        {
            // Iterate through all the children of the current parent.
            foreach (Transform child in parent)
            {
                // If the current child's name matches, add it to the results list.
                if (child.name == childName)
                {
                    results.Add(child);
                }

                // Recursively call this function on the current child to search its own children.
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
    }
