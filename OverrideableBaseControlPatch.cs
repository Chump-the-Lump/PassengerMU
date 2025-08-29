using DV.CabControls.Spec;
using DV.Simulation.Controllers;
using DV.ThingTypes;
using HarmonyLib;
using LocoSim.Implementations;
using UnityEngine;


namespace PassengerMUCable;

[HarmonyPatch(typeof(OverridableBaseControl))]
public static class OverridableBaseControlPatch
{
    [HarmonyPatch(nameof(OverridableBaseControl.Init))]
    [HarmonyPostfix]
    public static void Postfix(TrainCar car, SimulationFlow simFlow, ControlSpec spec, OverridableBaseControl __instance)
    {
        if (CarSpawnerPatch.AddMUToTypes.Contains(car.carType)||(CarSpawnerPatch.TenderMU&&CarTypes.IsTender(car.carLivery)))
        {
            bool notched = true;
            float notches = 1;
                
            if (__instance is ThrottleControl)notches = 14;
            if (__instance is BrakeControl)notches = 14;
            if (__instance is IndependentBrakeControl) notches = 14;
            if (__instance is DynamicBrakeControl)notches = 14;
            if (__instance is ReverserControl)notches = 2;
            if (__instance is SanderControl)notches = 2;
                
            CarSpawnerPatch.SetPrivateField(__instance, "NotchCount",  notches);
            CarSpawnerPatch.SetPrivateField(__instance, "IsNotched", notched);

        }
    }
}