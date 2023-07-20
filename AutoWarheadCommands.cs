using BetterCommands;
using BetterCommands.Permissions;

using Compendium.Extensions;
using Compendium.Helpers.Calls;

using PlayerRoles;

using PluginAPI.Core;

using System.Linq;

namespace AutoWarhead {
    public static class AutoWarheadCommands {
        [Command("autowarhead", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("autow")]
        [Description("Enable/disable Automatic Alpha Warhead")]
        public static string AWtoggle(Player sender) {
            AutoWarheadLogic.IsEnabled = !AutoWarheadLogic.IsEnabled;
            return AutoWarheadLogic.IsEnabled ?
                "Automatic Warhead enabled." :
                "Automatic Warhead disabled.";
        }

        [Command("autowarheadtime", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("autowtime", "autowt")]
        [Description("Change time of Automatic Alpha Warhead (in minutes)")]
        public static string AWtime(Player sender, float time) {
            AutoWarheadLogic.StartAfter = time;
            string ret = $"Automatic Warhead time set to {time} minutes.";
            if (!AutoWarheadLogic.IsEnabled) ret += " BUT PLUGIN IS NOT ACTIVE!";
            return ret;
        }

        [Command("autowarheadstatus", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("autowstatus", "autows")]
        [Description("Show status Automatic Alpha Warhead (in minutes)")]
        public static string AWstatus(Player sender) {
            string ret = $"Automatic Warhead time is set to {AutoWarheadLogic.WarheadTime()}.";
            if (!AutoWarheadLogic.IsEnabled) {
                ret += $" But plugin is not active.";
            }
            return ret;
        }
    }
}