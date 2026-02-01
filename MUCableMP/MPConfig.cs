using System.Runtime.CompilerServices;
using Bolt;
using PassengerMUCable;
using UnityEngine;

namespace PassengerMUCable.MP;


public class MPConfig
{
    private static object? Server;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetupServer(MPAPI.Interfaces.IServer server)
    {
        Server = server;
        server.OnPlayerConnected += ConfigSend;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetupClient(MPAPI.Interfaces.IClient client)
    {
        client.RegisterPacket<ConfigPacket>(ConfigReceive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConfigSend(MPAPI.Interfaces.IPlayer receiver)
    {
        if (Server is MPAPI.Interfaces.IServer s)
        {
            ConfigPacket packet = new ConfigPacket();
            packet.Init();
            s.SendPacketToPlayer(packet, receiver);
            UnityEngine.Debug.Log("Sending MU config to server.");
            Main.modEntry.Logger.Log("Sending MU Settings of : \n PatchAllMode: "+packet.AddMultipleUnitCablesToEverything+"\n AltMUPos: "+ packet.AlternateMultipleUnitCablePosition +"\n TenderMU: "+ packet.Enable282TenderMU+"\n Be2MU: "+packet.EnableBe2MU);

        }
    }
    
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConfigReceive(ConfigPacket packet)
    {
        UnityEngine.Debug.Log("Setting passMU config from server.");
        Main.modEntry.Logger.Log("Received Settings of : \n PatchAllMode: "+packet.AddMultipleUnitCablesToEverything+"\n AltMUPos: "+ packet.AlternateMultipleUnitCablePosition +"\n TenderMU: "+ packet.Enable282TenderMU+"\n Be2MU: "+packet.EnableBe2MU);

        
        CarSpawnerPatch.PatchAllMode = packet.AddMultipleUnitCablesToEverything;
        CarSpawnerPatch.AltMUPos = packet.AlternateMultipleUnitCablePosition;
        CarSpawnerPatch.TenderMU = packet.Enable282TenderMU;
        CarSpawnerPatch.Be2MU = packet.EnableBe2MU;
        
        if (!string.IsNullOrEmpty(packet.BlacklistData)) CarSpawnerPatch.BlacklistedItems = packet.BlacklistData.Split('|');
    
        if (!string.IsNullOrEmpty(packet.WhitelistData)) CarSpawnerPatch.WhitelistedItems = packet.WhitelistData.Split('|');
        
        CarSpawnerPatch.Prefix();
    }
    private class ConfigPacket : MPAPI.Interfaces.Packets.IPacket
    {
        public bool AddMultipleUnitCablesToEverything { get; set; }
        public bool AlternateMultipleUnitCablePosition { get; set; }
        public bool Enable282TenderMU { get; set; }
        public bool EnableBe2MU { get; set; }
    

        public string BlacklistData { get; set; } = "";
        public string WhitelistData { get; set; } = "";

        public ConfigPacket() { }

        public void Init()
        {
            AddMultipleUnitCablesToEverything = CarSpawnerPatch.PatchAllMode;
            AlternateMultipleUnitCablePosition = CarSpawnerPatch.AltMUPos;
            Enable282TenderMU = CarSpawnerPatch.TenderMU;
            EnableBe2MU = CarSpawnerPatch.Be2MU;
            
            BlacklistData = string.Join("|", CarSpawnerPatch.BlacklistedItems);
            WhitelistData = string.Join("|", CarSpawnerPatch.WhitelistedItems);
        }
    }
}
