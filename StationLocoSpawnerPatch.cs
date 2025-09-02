using DV;
using DV.CabControls.Spec;
using DV.Simulation.Controllers;
using DV.ThingTypes;
using HarmonyLib;
using LocoSim.Implementations;
using UnityEngine;
using Exception = Mono.WebBrowser.Exception;


namespace PassengerMUCable;

[HarmonyPatch(typeof(StationLocoSpawner))]
public static class StationLocoSpawnerPatch
{
    public static TrainCarLivery? BE2;

    private static Dictionary<string, int[]> SpawnPools = new Dictionary<string, int[]>()
    {
        { "[Y]_[FF]_[A-02-P]", new []{1,4} },
        { "[Y]_[CW]_[A-02-D]", new []{1,4} },
        { "[Y]_[MF]_[B-07-P]", new []{1,4} },
        { "[Y]_[SM]_[T1-01-P]", new []{1,4} },
        { "[Y]_[GF]_[A-04-P]", new []{1,4} },
        { "[Y]_[HB]_[A-01-P]", new []{1,4} },
        { "[Y]_[CP]_[P1]", new []{1,6} },
        { "[Y]_[CS]_[P1]", new []{1,4} }
    };
    
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void Postfix(StationLocoSpawner __instance)
    {
        if(!CarSpawnerPatch.Be2MU) return;
        if (SpawnPools.TryGetValue(__instance.locoSpawnTrackName, out int[] value))
        {
            for (int i = 0; i < value[0]; i++) __instance.locoTypeGroupsToSpawn.Add(GetBe2Spawn(value[1]));

        }
    }

    private static ListTrainCarTypeWrapper GetBe2Spawn(int amount)
    {
        if(BE2 ==null) foreach (TrainCarLivery? livery in Globals.G.Types.Liveries)
        {
            TrainCar car = livery.prefab.GetComponent<TrainCar>();
            if (car.carType == TrainCarType.LocoMicroshunter)
            {
                BE2 = livery;
                break;
            }
        }
            
        List<TrainCarLivery> cars = new List<TrainCarLivery?>()!;
        while (amount>0)
        {
            amount--;
            cars.Add(BE2!);
        }
        return new ListTrainCarTypeWrapper(cars);
    }
}