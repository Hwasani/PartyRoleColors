using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using System.Text.RegularExpressions;
using Dalamud.Game.Config;

namespace PartyRoleColors;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifeCycle { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;

    private Dictionary<string, Role> texturePathToClass = new Dictionary<string, Role>();

    public void PopulateTexturePathToClassDictionary()
    {
        texturePathToClass.Clear();
        texturePathToClass.Add("062106", Role.HEALER);
        texturePathToClass.Add("062124", Role.HEALER);
        texturePathToClass.Add("062128", Role.HEALER);
        texturePathToClass.Add("062133", Role.HEALER);
        texturePathToClass.Add("062140", Role.HEALER);
        texturePathToClass.Add("062101", Role.TANK);
        texturePathToClass.Add("062103", Role.TANK);
        texturePathToClass.Add("062119", Role.TANK);
        texturePathToClass.Add("062121", Role.TANK);
        texturePathToClass.Add("062132", Role.TANK);
        texturePathToClass.Add("062137", Role.TANK);
        texturePathToClass.Add("062102", Role.DPS);
        texturePathToClass.Add("062104", Role.DPS);
        texturePathToClass.Add("062105", Role.DPS);
        texturePathToClass.Add("062107", Role.DPS);
        texturePathToClass.Add("062120", Role.DPS);
        texturePathToClass.Add("062122", Role.DPS);
        texturePathToClass.Add("062123", Role.DPS);
        texturePathToClass.Add("062125", Role.DPS);
        texturePathToClass.Add("062126", Role.DPS);
        texturePathToClass.Add("062127", Role.DPS);
        texturePathToClass.Add("062129", Role.DPS);
        texturePathToClass.Add("062130", Role.DPS);
        texturePathToClass.Add("062131", Role.DPS);
        texturePathToClass.Add("062134", Role.DPS);
        texturePathToClass.Add("062135", Role.DPS);
        texturePathToClass.Add("062136", Role.DPS);
        texturePathToClass.Add("062138", Role.DPS);
        texturePathToClass.Add("062139", Role.DPS);
        texturePathToClass.Add("062141", Role.DPS);
        texturePathToClass.Add("062142", Role.DPS);
    }

    public string TextureIDFromPath(string path)
    {
        var match = Regex.Match(path, @"ui/icon/\d+/(\d+)");
        return match.Groups[1].Value;
    }

    public Role RoleFromTexturePath(string path)
    {
        return texturePathToClass.GetValueOrDefault(TextureIDFromPath(path), Role.OTHER);
    }

    public Plugin()
    {
        PopulateTexturePathToClassDictionary();
        AddonLifeCycle.RegisterListener(AddonEvent.PreDraw, new[] { "_PartyList" }, OnPreDraw);
    }

    private unsafe void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        Colorize();
        Log.Information($"{GameConfig.UiConfig.GetUInt("NamePlateColorDps")}");
    }
    public unsafe void Colorize()
    {
        var partyListAddon = (AddonPartyList*)GameGui.GetAddonByName("_PartyList");
        foreach (var member in partyListAddon->PartyMembers)
        {
            if (member.Name == null || String.IsNullOrEmpty(member.Name->NodeText.ToString()))
            {
                continue;
            }

            var isMissing = member.Name->NodeText.ToString()[1] == 63 && member.Name->NodeText.ToString()[2] == 63;

            if (isMissing == false)
            {
                var jobIcon = Marshal.PtrToStringAnsi(new(member.ClassJobIcon->PartsList->Parts[0].UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr));

                if (jobIcon == null)
                {
                    continue;
                }

                var job = RoleFromTexturePath(jobIcon);
                if (job == Role.DPS)
                {
                    member.Name->TextColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorDps");
                    member.Name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeDps");
                }
                else if (job == Role.HEALER)
                {
                    member.Name->TextColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorHealer");
                    member.Name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeHealer");
                }
                else if (job == Role.TANK)
                {
                    member.Name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorTank");
                    member.Name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeTank");
                }
                else
                {
                    continue;
                }

            }
            else
            {
                continue;
            }
        }
        foreach (var member in partyListAddon->TrustMembers)
        {
            member.Name->TextColor.R = 0;
            member.Name->TextColor.G = 255;
            member.Name->TextColor.B = 0;
        }
    }

    private void OnCommand(string command, string args)
    {
        Colorize();
    }

    public void Dispose()
    {

    }

    public enum Role
    {
        TANK,
        HEALER,
        DPS,
        OTHER
    }
}
