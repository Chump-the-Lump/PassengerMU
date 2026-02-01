using System;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

public static class MultiplayerShim
{
    const string MULTIPLAYER_MOD_ID = "Multiplayer";
    const string MPAPI_ASSEMBLY_NAME = "MultiplayerAPI";

    const string MP_INTEGRATION_DLL = "PassengerMUCable.MP.dll";
    const string MP_INTEGRATION_BOOTSTRAP = "PassengerMUCable.MP.Bootstrap";
    const string MP_INTEGRATION_INIT_METHOD = "Initialize";

    internal static void Initialize(UnityModManager.ModEntry modEntry)
    {
        UnityModManager.ModEntry? multiplayer = UnityModManager.FindMod(MULTIPLAYER_MOD_ID);
        var mpapiAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == MPAPI_ASSEMBLY_NAME);
        var path = Path.Combine(modEntry.Path, MP_INTEGRATION_DLL);

        try
        {
            if (multiplayer?.Enabled == true && mpapiAssembly != null)
            {
                if (!File.Exists(path))
                {
                    modEntry.Logger.Warning($"{MP_INTEGRATION_DLL} was not found, unable to activate multiplayer integration.");
                    return;
                }

                var mpAssembly = Assembly.LoadFile(path);
                var bootstrap = mpAssembly.GetType(MP_INTEGRATION_BOOTSTRAP);

                if (bootstrap == null)
                {
                    modEntry.Logger.Warning($"Failed to find {MP_INTEGRATION_BOOTSTRAP} in {MP_INTEGRATION_DLL}, multiplayer support will be disabled.");
                    return;
                }

                var init = bootstrap.GetMethod(MP_INTEGRATION_INIT_METHOD, BindingFlags.Public | BindingFlags.Static);
                init?.Invoke(null, null);
            }

            modEntry.Logger.Log("Multiplayer API Loaded.");
        }
        catch (Exception ex)
        {
            modEntry.Logger.Warning($"Failed to load multiplayer API.\r\n{ex.Message}\r\n{ex.StackTrace}");
        }
    }
}