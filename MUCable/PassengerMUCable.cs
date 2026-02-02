using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace PassengerMUCable;

    public static class Main
    {
        public static UnityModManager.ModEntry modEntry = null!;
        public static Settings settings = new Settings();
        public static bool MPEnabled = false;
        public static bool NeedsRestart { get; set; }

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

            MPLoadCheck();

            return true;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MPLoadCheck()
        {
            try{if (Type.GetType("MPAPI.MultiplayerAPI, MultiplayerAPI") != null)Helper();}catch(Exception ex){modEntry.Logger.Log("MP not found "+ex);}
            
            [MethodImpl(MethodImplOptions.NoInlining)]
            void Helper()
            {
                if (MPAPI.MultiplayerAPI.IsMultiplayerLoaded)
                {
                    MPEnabled = true;
                    LoadMP();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]

        private static void LoadMP()
        {
            MonoBehaviour.print("Instance: "+MPAPI.MultiplayerAPI.Instance);
            MPAPI.MultiplayerAPI.Instance.SetModCompatibility(modEntry.Info.Id, MPAPI.Types.MultiplayerCompatibility.All);
            MPAPI.MultiplayerAPI.ServerStarted += PassengerMUCable.MP.MPConfig.SetupServer;
            MPAPI.MultiplayerAPI.ClientStarted += PassengerMUCable.MP.MPConfig.SetupClient;

        }
        
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BlockMP()
        {
            modEntry.Logger.Log("Patch already applied, blocking MP");
            MPAPI.MultiplayerAPI.Instance.SetModCompatibility(modEntry.Info.Id, MPAPI.Types.MultiplayerCompatibility.Incompatible);
        }
    }


public class Settings : UnityModManager.ModSettings, IDrawable
{
        [DrawHeader("All settings apply the first time a save is loaded, a restart is required if you want to change the settings after already loading in to a world")]
        
        [Draw("Enable 282 tender MU")]
        public bool Enable282TenderMU = false;
        
        [Draw("Main Line BB2")]
        public bool EnableBe2MU = false;
        
        [Draw("Use alternate positioning to reduce clipping")]
        public bool AlternateMultipleUnitCablePosition = false;
        
        [Draw("Add multiple unit cables to all cars")]
        public bool AddMultipleUnitCablesToEverything = false;

        public override void Save(UnityModManager.ModEntry entry)
        {
            Save(this, entry);
            OnChange();
        }

        public void OnChange()
        {
            CarSpawnerPatch.PatchAllMode = AddMultipleUnitCablesToEverything;
            CarSpawnerPatch.AltMUPos = AlternateMultipleUnitCablePosition;
            CarSpawnerPatch.TenderMU = Enable282TenderMU;
            CarSpawnerPatch.Be2MU = EnableBe2MU;
        }
    }
