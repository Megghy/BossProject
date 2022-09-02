using BossFramework;
using BossFramework.BInterfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerrariaApi.Server;

public class PacketStatistic : IScriptModule
{
    public class PacketStatisticInfo
    {
        public DateTime Date { get; set; }
        public Dictionary<PacketTypes, PacketInfo> SendPacket { get; set; } = new();
        public Dictionary<PacketTypes, PacketInfo> GetPacket { get; set; } = new();
    }
    public class PacketInfo
    {
        public long Count { get; set; } = 0;
        public long Size { get; set; } = 0;
    }

    public string Name => "PacketStatistic";
    public string Author => "Megghy";
    public string Version { get; } = "0.1";

    public PacketStatisticInfo CurrentInfo { get; set; }
    public string FileDirectory
        => Path.Combine(BInfo.FilePath, "PacketStatistic");
    public string FilePath
        => Path.Combine(FileDirectory, $"{DateTime.Now.Date:D}.json");

    public void Dispose()
    {
        ServerApi.Hooks.NetGetData.Deregister(BossPlugin.Instance, OnGetPacket);
        ServerApi.Hooks.NetSendBytes.Deregister(BossPlugin.Instance, OnSendPacket);
        ServerApi.Hooks.GameUpdate.Deregister(BossPlugin.Instance, OnUpdate);
    }

    public void Initialize()
    {
        if (!Directory.Exists(FileDirectory))
            Directory.CreateDirectory(FileDirectory);
        if (File.Exists(FilePath))
            CurrentInfo = File.ReadAllText(FilePath).DeserializeJson<PacketStatisticInfo>() ?? new();
        else
            CurrentInfo = new()
            {
                Date = DateTime.Now.Date
            };
        ServerApi.Hooks.NetGetData.Register(BossPlugin.Instance, OnGetPacket);
        ServerApi.Hooks.NetSendBytes.Register(BossPlugin.Instance, OnSendPacket);
        ServerApi.Hooks.GameUpdate.Register(BossPlugin.Instance, OnUpdate);
    }

    private void OnUpdate(EventArgs args)
    {
        if (BInfo.GameTick % 1000 == 0)
        {
            lock (CurrentInfo)
            {
                CurrentInfo.GetPacket = CurrentInfo.GetPacket.OrderByDescending(g => g.Value.Count).ToDictionary(p => p.Key, p => p.Value);
                CurrentInfo.SendPacket = CurrentInfo.SendPacket.OrderByDescending(g => g.Value.Count).ToDictionary(p => p.Key, p => p.Value);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(CurrentInfo, Formatting.Indented));
            }
        }
    }

    private void OnSendPacket(SendBytesEventArgs args)
    {
        if(CurrentInfo.Date.Date != DateTime.Now.Date)
            CurrentInfo = new()
            {
                Date = DateTime.Now.Date
            };
        try
        {
            var type = (PacketTypes)args.Buffer[args.Offset + 2];
            if (CurrentInfo.GetPacket.ContainsKey(type))
            {
                CurrentInfo.GetPacket[type].Count++;
                CurrentInfo.GetPacket[type].Size += args.Count;
            }
            else
                CurrentInfo.GetPacket.Add(type, new PacketInfo() { Count = 1, Size = args.Count });
            /*CurrentInfo.SendPacket.AddOrUpdate(type, new PacketInfo() { Count = 1, Size = args.Count }, (key, oldValue) =>
            {
                oldValue.Count++;
                oldValue.Size += args.Count;
                return oldValue;
            });*/
        }
        catch (Exception ex) { BLog.Error(ex); }
    }

    private void OnGetPacket(GetDataEventArgs args)
    {
        try
        {
            if (CurrentInfo.GetPacket.ContainsKey(args.MsgID))
            {
                CurrentInfo.GetPacket[args.MsgID].Count++;
                CurrentInfo.GetPacket[args.MsgID].Size += args.Length;
            }
            else
                CurrentInfo.GetPacket.Add(args.MsgID, new PacketInfo() { Count = 1, Size = args.Length });
            /*CurrentInfo.GetPacket.AddOrUpdate(args.MsgID, new PacketInfo() { Count = 1, Size = args.Length }, (key, oldValue) =>
            {
                oldValue.Count++;
                oldValue.Size += args.Length;
                return oldValue;
            });*/
        }
        catch { }
    }
}
