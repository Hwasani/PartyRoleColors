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
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

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
        texturePathToClass.Add("062231", Role.HEALER);
        texturePathToClass.Add("062572", Role.HEALER);
        texturePathToClass.Add("062101", Role.TANK);
        texturePathToClass.Add("062103", Role.TANK);
        texturePathToClass.Add("062119", Role.TANK);
        texturePathToClass.Add("062121", Role.TANK);
        texturePathToClass.Add("062132", Role.TANK);
        texturePathToClass.Add("062137", Role.TANK);
        texturePathToClass.Add("062226", Role.TANK);
        texturePathToClass.Add("062228", Role.TANK);
        texturePathToClass.Add("062571", Role.TANK);
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
        texturePathToClass.Add("062227", Role.DPS);
        texturePathToClass.Add("062229", Role.DPS);
        texturePathToClass.Add("062230", Role.DPS);
        texturePathToClass.Add("062232", Role.DPS);
        texturePathToClass.Add("062573", Role.DPS);
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
    }

    public unsafe void ColorTextNodes(AtkTextNode* name, AtkImageNode* jobIcon)
    {
        if (name == null || String.IsNullOrEmpty(name->NodeText.ToString()))
        {
            return;
        }

        var isMissing = name->NodeText.ToString()[1] == 63 && name->NodeText.ToString()[2] == 63;

        if (isMissing == false)
        {
            var jobIconPath = Marshal.PtrToStringAnsi(new(jobIcon->PartsList->Parts[0].UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr));

            if (jobIconPath == null)
            {
                return;
            }

            var job = RoleFromTexturePath(jobIconPath);
            if (job == Role.DPS)
            {
                name->TextColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorDps");
                name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeDps");
            }
            else if (job == Role.HEALER)
            {
                name->TextColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorHealer");
                name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeHealer");
            }
            else if (job == Role.TANK)
            {
                name->TextColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorTank");
                name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeTank");
            }
        }
    }
    public unsafe void Colorize()
    {
        var partyListAddon = (AddonPartyList*)GameGui.GetAddonByName("_PartyList");
        foreach (var member in partyListAddon->PartyMembers)
        {
            ColorTextNodes(member.Name, member.ClassJobIcon);
        }
        foreach (var member in partyListAddon->TrustMembers)
        {
            ColorTextNodes(member.Name, (AtkImageNode*)member.UnknownB0);
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
