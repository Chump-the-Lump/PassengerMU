using HarmonyLib;
using UnityModManagerNet;

namespace PassengerMUCable;

    public static class Main
    {
        public static UnityModManager.ModEntry modEntry = null!;
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
        [DrawHeader("All settings apply the first time a save is loaded, a restart is required if you want to change the settings after already loading in to a world")]
        [Draw("Enable 282 tender MU")]
        public bool Enable282TenderMU = false;
        
        [Draw("Use alternate positioning to reduce clipping")]
        public bool AlternateMultipleUnitCablePosition = false;
        
        [Draw("Add multiple unit cables to all cars")]
        public bool AddMultipleUnitCablesToEverything = false;

        public override void Save(UnityModManager.ModEntry entry)
        {
            Save(this, entry);
            CarSpawnerPatch.PatchAllMode = AddMultipleUnitCablesToEverything;
            CarSpawnerPatch.AltMUPos = AlternateMultipleUnitCablePosition;
            CarSpawnerPatch.TenderMU = Enable282TenderMU;
        }

        public void OnChange()
        {
            CarSpawnerPatch.PatchAllMode = AddMultipleUnitCablesToEverything;
            CarSpawnerPatch.AltMUPos = AlternateMultipleUnitCablePosition;
            CarSpawnerPatch.TenderMU = Enable282TenderMU;
        }
    }
