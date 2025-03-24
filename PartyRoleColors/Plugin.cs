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
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

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

    private Dictionary<string, Role> texturePathToClass = new Dictionary<string, Role>() {
        {"062106", Role.HEALER},
        {"062124", Role.HEALER},
        {"062128", Role.HEALER},
        {"062133", Role.HEALER},
        {"062140", Role.HEALER},
        {"062231", Role.HEALER},
        {"062572", Role.HEALER},
        {"062101", Role.TANK},
        {"062103", Role.TANK},
        {"062119", Role.TANK},
        {"062121", Role.TANK},
        {"062132", Role.TANK},
        {"062137", Role.TANK},
        {"062226", Role.TANK},
        {"062228", Role.TANK},
        {"062571", Role.TANK},
        {"062102", Role.DPS},
        {"062104", Role.DPS},
        {"062105", Role.DPS},
        {"062107", Role.DPS},
        {"062120", Role.DPS},
        {"062122", Role.DPS},
        {"062123", Role.DPS},
        {"062125", Role.DPS},
        {"062126", Role.DPS},
        {"062127", Role.DPS},
        {"062129", Role.DPS},
        {"062130", Role.DPS},
        {"062131", Role.DPS},
        {"062134", Role.DPS},
        {"062135", Role.DPS},
        {"062136", Role.DPS},
        {"062138", Role.DPS},
        {"062139", Role.DPS},
        {"062141", Role.DPS},
        {"062142", Role.DPS},
        {"062227", Role.DPS},
        {"062229", Role.DPS},
        {"062230", Role.DPS},
        {"062232", Role.DPS},
        {"062573", Role.DPS}
    }; // Dictionary that matches job icon filename to a role.

    private Dictionary<ClassJob, int> jobPriorities = new Dictionary<ClassJob, int>{
        {ClassJob.Pictomancer, 0},
        {ClassJob.Samurai, 0},
        {ClassJob.Reaper, 1},
        {ClassJob.Viper, 2},
        {ClassJob.Monk, 3},
        {ClassJob.Ninja, 4},
        {ClassJob.Dragoon, 5},
        {ClassJob.BlackMage, 6},
        {ClassJob.RedMage, 7},
        {ClassJob.Summoner, 8},
        {ClassJob.Machinist, 9},
        {ClassJob.Bard, 10},
        {ClassJob.Dancer, 15}
    };

    public string TextureIDFromPath(string path) //Takes a path arg and runs it through a regex to grab the .tex file name
    {
        var match = Regex.Match(path, @"ui/icon/\d+/(\d+)");
        return match.Groups[1].Value;
    }

    public Role RoleFromTexturePath(string tex) // Function that returns the role from a file name.
    {
        return texturePathToClass.GetValueOrDefault(TextureIDFromPath(tex), Role.OTHER); // Search the dictionary for the filename, if found return the Role, if not found, return OTHER, which is a role in FFXIV
    }

    public Plugin()
    {
        AddonLifeCycle.RegisterListener(AddonEvent.PreDraw, new[] { "_PartyList" }, OnPreDraw); // Add a PartyList PreDraw event listener
    }

    private unsafe void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        Colorize(); // Call our main function on PreDraw, which is every time the PartyList is ready to be updated.
    }

    public unsafe void ColorTextNodes(AtkTextNode* name, AtkImageNode* jobIcon) // Function that takes an AtkTextNode* called name & an AtkImageNode* called jobIcon as args
    {
        if (name == null || string.IsNullOrEmpty(name->NodeText.ToString())) // If the party slot we're iterating over is empty, we skip it
        {
            return;
        }

        var isMissing = name->NodeText.ToString()[1] == 63 && name->NodeText.ToString()[2] == 63; // We check if the party member is missing by checking for 2 question mark characters. 

        if (isMissing == false) // If there is no question marks in the level, we execute everything in the if statement.
        {
            var jobIconPath = Marshal.PtrToStringAnsi(new(jobIcon->PartsList->Parts[0].UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr)); // File name nested in a bunch of pointers in the AtkImageNode*

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

    private unsafe bool isValidDancePartner(HudPartyMember member)
    {
        // If the object is null, it's not a valid member. (Carbunkel, etc)
        if (member.Object == null)
        {
            return false;
        }

        // If the object is not a player character, it's not a valid member.
        if (member.Object->ObjectKind != ObjectKind.Pc)
        {
            return false;
        }

        // If the object is the local player, it's not a valid member.
        if (member.Object->ContentId == ClientState.LocalContentId)
        {
            return false;
        }

        // If the object is dead, it's not a valid member.
        if (member.Object->IsDead())
        {
            return false;
        }

        return true;
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

        var agentHud = AgentHUD.Instance();
        var agentHudPartyMembers = agentHud->PartyMembers.ToArray();
        var sortedParty = agentHudPartyMembers.Where(isValidDancePartner).OrderBy(member =>
        {
            return jobPriorities.GetValueOrDefault((ClassJob)member.Object->ClassJob, 100);
        });

        var mostImportant = sortedParty.First();
        var mostImportantAddon = partyListAddon->PartyMembers[mostImportant.Index];

        mostImportantAddon.Name->TextColor.R = 138;
        mostImportantAddon.Name->TextColor.G = 43;
        mostImportantAddon.Name->TextColor.B = 226;
    }

    public void Dispose()
    {

    }

    public enum ClassJob
    {
        // Tanks
        Gladiator = 1,
        Paladin = 19,

        Marauder = 3,
        Warrior = 21,

        DarkKnight = 32,
        Gunbreaker = 37,

        // Healers
        Conjurer = 6,
        WhiteMage = 24,

        Scholar = 28,
        Astrologian = 33,
        Sage = 40,

        // Melee DPS
        Pugilist = 2,
        Monk = 20,

        Lancer = 4,
        Dragoon = 22,

        Rogue = 29,
        Ninja = 30,

        Samurai = 34,
        Reaper = 39,
        Viper = 41,

        // Physical Ranged DPS
        Archer = 5,
        Bard = 23,
        Machinist = 31,
        Dancer = 38,

        // Magical Ranged DPS
        Thaumaturge = 7,
        BlackMage = 25,
        Arcanist = 26,
        Summoner = 27,
        RedMage = 35,
        Pictomancer = 42,
        BlueMage = 36,

        // Disciples of the Hand
        Carpenter = 8,
        Blacksmith = 9,
        Armorer = 10,
        Goldsmith = 11,
        Leatherworker = 12,
        Weaver = 13,
        Alchemist = 14,
        Culinarian = 15,

        // Disclipines of the Land
        Miner = 16,
        Botanist = 17,
        Fisher = 18
    }

    public enum Role
    {
        TANK,
        HEALER,
        DPS,
        OTHER
    }
}
