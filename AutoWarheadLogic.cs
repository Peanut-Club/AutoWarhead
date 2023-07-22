using Compendium.Extensions;
using Compendium.Features;
using Compendium.Helpers.Calls;
using Compendium.Helpers.Events;

//using helpers;
using helpers.Configuration.Ini;
using helpers.Patching;

using PlayerRoles;

using PluginAPI.Enums;
using PluginAPI.Events;

using System;
using System.Timers;
using System.Collections.Generic;
using helpers.Network.Events.Client;
using PluginAPI.Core;
using PlayerStatsSystem;
using UnityEngine.PlayerLoop;

namespace AutoWarhead {
    public static class AutoWarheadLogic {
        private static bool _isEnabled;
        private static bool _isDetonating = false; // true only if automatic warhead started by itself
        private static double _startAfter = 25;
        private static Timer _warheadTimer;

        private static bool _079Regular = false;
        private static bool _079Auto = true;
        private static bool _079Checking = false;

        public static bool IsEnabled {
            get => _isEnabled;
            set {
                if (value == _isEnabled)
                    return;

                if (!value) {
                    _isEnabled = false;
                    _warheadTimer.Stop();
                    AutoWarheadStop();

                    ServerEventType.RoundRestart.RemoveHandler<Action>(OnRoundRestart);
                    ServerEventType.RoundStart.RemoveHandler<Action>(OnRoundStart);

                    Log.Info($"Auto Warhead disabled.");
                    return;
                }

                _isEnabled = true;
                if (IsRoundStarted()) OnRoundStart();
                Update079Check();

                ServerEventType.RoundRestart.AddHandler<Action>(OnRoundRestart);
                ServerEventType.RoundStart.AddHandler<Action>(OnRoundStart);

                Log.Info($"Auto Warhead enabled.");
            }
        }
        //Alpha Warhead is being automatically detonated and cannot be cancelled. <= WarheadBroadcastMessage

        [IniConfig(Name = "Default Enabled", Description = "Whether the Automatic Alpha Warhead is enabled on the start of the round. [Default: true]")]
        public static bool DefaultEnabled { get; set; } = true;

        [IniConfig(Name = "SCP-079 RegularNuke Survive", Description = "Whether SCP-079 survives regular Alpha Warhead detonation. [Default: false]")]
        public static bool SCP079RegularSurvive {
            get => _079Regular;
            set {
                _079Regular = value;
                Update079Check();
            }
        }

        [IniConfig(Name = "SCP-079 AutoNuke Survive", Description = "Whether SCP-079 survives Automatic Alpha Warhead detonation. [Default: true]")]
        public static bool SCP079AutoSurvive {
            get => _079Auto;
            set {
                _079Auto = value;
                Update079Check();
            }
        }


        [IniConfig(Name = "Auto Warhead Message", Description = "Message of Auto Warhead broadcast.")]
        public static string AutoWarheadMessage { get; set; } = AlphaWarheadController.WarheadBroadcastMessage;

        [IniConfig(Name = "Auto Warhead Message on Continue", Description = "Message of Auto Warhead broadcast, if Warhead was not started by Auto Warhead.")]
        public static string AutoWarheadMessageOnContinue { get; set; } = "Alpha Warhead is locked and cannot be cancelled.";

        [IniConfig(Name = "SCP-079 Will die Message", Description = "Additional message of Auto Warhead broadcast, if SCP-079 will survive.")]
        public static string SCP079DieMessage { get; set; } = "SCP-079 will die.";

        [IniConfig(Name = "SCP-079 Will survive Message", Description = "Additional message of Auto Warhead broadcast, if SCP-079 will survive.")]
        public static string SCP079SurviveMessage { get; set; } = "SCP-079 will survive.";

        [IniConfig(Name = "Auto Warhead Message Time", Description = "Time of Auto Warhead Message broadcast")]
        public static ushort AutoWarheadMessageTime { get; set; } = AlphaWarheadController.WarheadBroadcastMessageTime;

        [IniConfig(Name = "Announcement Message", Description = "Message to C.A.S.S.I.E before Auto Warhead activation.")]
        public static string CassieMessage { get; set; } = "pitch_.2 .g4.g4 pitch_1.Facility diagnostic anomaly detected. .g2 o 5password accepted .g3.automatic warhead detonation sequence authorized pitch_.9 .g3.pitch_1 detonation tminus 5 minutes.all personnel evacuate pitch_.8 . .g1. .g1. .g1 pitch_1 bell_end";

        [IniConfig(Name = "Announcement Time", Description = "Time before C.A.S.S.I.E message. (minutes) [default: 5]")]
        public static double CassieMessageTime { get; set; } = 5;

        [IniConfig(Name = "Start After", Description = "Start the Automatic Alpha Warhead after ... minutes. [Default: 25]")]
        public static double StartAfter {
            get => _startAfter;
            set {
                if (value <= 0) _startAfter = 0.001;
                else _startAfter = value;
                if (IsRoundStarted() && IsEnabled) OnRoundStart();
                Log.Info($"Automatic Warhead time set to {_startAfter} minutes.");
            }
        }

        public static void TimerInit() {
            _warheadTimer = new Timer();
            _warheadTimer.Elapsed += AutoWarheadStartEvent;
            _warheadTimer.AutoReset = false;
        }

        private static void OnRoundStart() {
            double interval = StartAfter * 60 * 1000 - Round.Duration.TotalMilliseconds;
            if (interval > 0) {
                _warheadTimer.Interval = interval;
                _warheadTimer.Start();
            } else {
                _warheadTimer.Stop();
                AutoWarheadStart();
            }
            //Cassie.Message(CassieMessage, false, true, true);
        }

        public static string WarheadTime() {
            int seconds = Convert.ToInt32(StartAfter * 60);
            return $"{seconds / 60} minutes and {seconds % 60} seconds";
        }

        private static void AutoWarheadStartEvent(Object source, ElapsedEventArgs e) {
            AutoWarheadStart();
        }

        private static void AutoWarheadStart() {
            if (Warhead.IsDetonated || _isDetonating) return;
            string broadcastMessage;
            if (Warhead.IsDetonationInProgress) {
                broadcastMessage = AutoWarheadMessageOnContinue;
                broadcastMessage += " " + (SCP079RegularSurvive ? SCP079SurviveMessage : SCP079DieMessage);
                Log.Info("Automatic Alpha Warhead continuing on regular warhead detonation");
            } else {
                broadcastMessage = AutoWarheadMessage;
                broadcastMessage += " " + (SCP079AutoSurvive ? SCP079SurviveMessage : SCP079DieMessage);
                Warhead.Start(false, true);
                Log.Info("Automatic Alpha Warhead start");
                _isDetonating = true;
            }
            Warhead.IsLocked = true;
            if (AlphaWarheadController.Singleton.TryGetBroadcaster(out var broadcaster)) {
                broadcaster.RpcAddElement(broadcastMessage, AutoWarheadMessageTime, Broadcast.BroadcastFlags.Normal);
            }
        }

        public static void AutoWarheadStop() {
            Warhead.IsLocked = false;
            if (Warhead.IsDetonated || !_isDetonating) return;
            _isDetonating = false;
            Warhead.Stop();
            Log.Info("Automatic Alpha Warhead stopped");
        }

        private static void OnRoundRestart() {
            AutoWarheadStop();
            TimerInit();
            IsEnabled = DefaultEnabled;
        }

        private static bool IsRoundStarted() {
            try {
                return Round.IsRoundStarted;
            } catch (NullReferenceException) { }
            return false;
        }

        private static void Update079Check() {
            bool shouldBeActive = SCP079RegularSurvive || SCP079AutoSurvive;
            if (shouldBeActive == _079Checking) return;
            if (shouldBeActive) {
                ServerEventType.PlayerDamage.AddHandler<Action<PlayerDamageEvent, ValueContainer>>(OnPlayerDamage);
            } else {
                ServerEventType.PlayerDamage.RemoveHandler<Action<PlayerDamageEvent, ValueContainer>>(OnPlayerDamage);
            }
        }

        private static void OnPlayerDamage(PlayerDamageEvent ev, ValueContainer val) {
            if (ev.DamageHandler is WarheadDamageHandler && ev.Target.Role is RoleTypeId.Scp079 &&
               ((SCP079RegularSurvive && !_isDetonating) || (SCP079AutoSurvive && _isDetonating))) {
                val.Value = false;
            }
        }
    }
}
