using HarmonyLib;

namespace PassengerMUCable.MP;

using MPAPI;
using MPAPI.Types;
using MPAPI.Interfaces;
using MPAPI.Util;
using PassengerMUCable;

public static class Bootstrap
{
    public static void Initialize()
    {
        if (!MultiplayerAPI.IsMultiplayerLoaded)
            return;

        // Set compatibility state for your mod
        MultiplayerAPI.Instance.SetModCompatibility(PassengerMUCable.Main.modEntry.Info.Id, MultiplayerCompatibility.All);

        // Apply any extra patches required for multiplayer that can't be applied in the your core mod
        Harmony harmony = new("PassengerMUCable.MP");
        harmony.PatchAll();

        // Register custom task type serialisers

        // Setup listeners for server and client starting / stopping
    }
}