using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
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

    private Dictionary<ClassJob, Role> classJobToRole = new Dictionary<ClassJob, Role>() {
        {ClassJob.Scholar, Role.HEALER},
        {ClassJob.Astrologian, Role.HEALER},
        {ClassJob.Sage, Role.HEALER},
        {ClassJob.Conjurer, Role.HEALER},
        {ClassJob.WhiteMage, Role.HEALER},
        {ClassJob.Gladiator, Role.TANK},
        {ClassJob.Marauder, Role.TANK},
        {ClassJob.Warrior, Role.TANK},
        {ClassJob.Gunbreaker, Role.TANK},
        {ClassJob.DarkKnight, Role.TANK},
        {ClassJob.Paladin, Role.TANK},
        {ClassJob.Samurai, Role.DPS},
        {ClassJob.Pugilist, Role.DPS},
        {ClassJob.Monk, Role.DPS},
        {ClassJob.Lancer, Role.DPS},
        {ClassJob.Dragoon, Role.DPS},
        {ClassJob.Machinist, Role.DPS},
        {ClassJob.Rogue, Role.DPS},
        {ClassJob.Ninja, Role.DPS},
        {ClassJob.Pictomancer, Role.DPS},
        {ClassJob.Reaper, Role.DPS},
        {ClassJob.Viper, Role.DPS},
        {ClassJob.Archer, Role.DPS},
        {ClassJob.Arcanist, Role.DPS},
        {ClassJob.Bard, Role.DPS},
        {ClassJob.BlackMage, Role.DPS},
        {ClassJob.Thaumaturge, Role.DPS},
        {ClassJob.Dancer, Role.DPS},
        {ClassJob.Summoner, Role.DPS},
        {ClassJob.RedMage, Role.DPS},
        {ClassJob.BlueMage, Role.DPS},
    };

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

    public Plugin()
    {
        AddonLifeCycle.RegisterListener(AddonEvent.PreDraw, new[] { "_PartyList" }, OnPreDraw); // Add a PartyList PreDraw event listener
    }

    private unsafe void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        Colorize(); // Call our main function on PreDraw, which is every time the PartyList is ready to be updated.
    }

    public unsafe void ColorTextNodes(AtkTextNode* name, ClassJob job) // Function that takes an AtkTextNode* called name & an AtkImageNode* called jobIcon as args
    {
        if (name == null || string.IsNullOrEmpty(name->NodeText.ToString())) // If the party slot we're iterating over is empty, we skip it
        {
            return;
        }

        var isMissing = name->NodeText.ToString()[1] == 63 && name->NodeText.ToString()[2] == 63; // We check if the party member is missing by checking for 2 question mark characters. 

        if (isMissing == false) // If there is no question marks in the level, we execute everything in the if statement.
        {

            var role = classJobToRole[job];
            if (role == Role.DPS)
            {
                name->TextColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorDps");
                name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeDps");
            }
            else if (role == Role.HEALER)
            {
                name->TextColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorHealer");
                name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeHealer");
            }
            else if (role == Role.TANK)
            {
                name->TextColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateColorTank");
                name->EdgeColor.RGBA = GameConfig.UiConfig.GetUInt("NamePlateEdgeTank");
            }
        }
    }

    private unsafe bool isValidDancePartner(HudPartyMember member)
    {
        // If the object is null, it's not a valid member. (Carbuncle, etc)
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
        var agentHud = AgentHUD.Instance();
        var agentHudPartyMembers = agentHud->PartyMembers.ToArray();

        var partyListAddon = (AddonPartyList*)GameGui.GetAddonByName("_PartyList");

        foreach (var member in agentHudPartyMembers)
        {
            ColorTextNodes(partyListAddon->PartyMembers[member.Index].Name, (ClassJob)member.Object->ClassJob);
        }

        var player = ClientState.LocalPlayer;
        if ((ClassJob)player!.ClassJob.RowId == ClassJob.Dancer)
        {
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
    }

    public void Dispose()
    {

    }
}
