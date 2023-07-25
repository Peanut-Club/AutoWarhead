using BetterCommands;
using BetterCommands.Permissions;

using Compendium.Extensions;
using Compendium.Helpers.Calls;

using PlayerRoles;

using PluginAPI.Core;

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

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

        /* CHANGE TIME - not supported
        [Command("autowarheadtime", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("autowtime", "autowt")]
        [Description("Change time of Automatic Alpha Warhead (in minutes)")]
        public static string AWtime(Player sender, float time) {
            AutoWarheadLogic.StartAfter = time;
            string ret = $"Automatic Warhead time set to {time} minutes.";
            if (!AutoWarheadLogic.IsEnabled) ret += " BUT PLUGIN IS NOT ACTIVE!";
            return ret;
        }
        */

        [Command("autowarheadstatus", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("autowstatus", "autows")]
        [Description("Show status Automatic Alpha Warhead (in minutes)")]
        public static string AWstatus(Player sender) {
            var sb = new StringBuilder();
            if (AutoWarheadLogic.WarningEnabled) {
                string warning = $"Warning time set to {FormatTime(AutoWarheadLogic.WarningTime)}.";
                if (AutoWarheadLogic.IsRoundStarted() && AutoWarheadLogic.IsEnabled) {
                    double time = AutoWarheadLogic.WarningTime - Round.Duration.TotalMinutes;
                    if (time < 0) warning += " Already announced.";
                    else warning += $" Remaining {FormatTime(time)}.";
                }
                sb.AppendLine(warning);
            }

            string warhead = $"Auto Warhead time set to {FormatTime(AutoWarheadLogic.StartAfter)}.";
            if (Warhead.IsDetonated) warhead += " Is detonated.";
            else if (AutoWarheadLogic.IsDetonating) warhead += " Is being detonated.";
            else if (AutoWarheadLogic.IsRoundStarted() && AutoWarheadLogic.IsEnabled) {
                double time = AutoWarheadLogic.StartAfter - Round.Duration.TotalMinutes;
                if (time < 0) warhead += " Already detonated.";
                else warhead += $" Remaining {FormatTime(time)}.";
            }
            sb.AppendLine(warhead);

            if (!AutoWarheadLogic.IsEnabled) sb.AppendLine($"PLUGIN IS NOT ACTIVE!");
            return sb.ToString();
        }

        private static string FormatTime(double time) {
            int seconds = Convert.ToInt32(time * 60);
            return $"{seconds / 60}m:{seconds % 60}s";
        }
    }
}