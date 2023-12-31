﻿using Compendium.Extensions;
using Compendium.Features;
using Compendium.Events;

//using helpers;
using helpers.Configuration.Ini;
using helpers.Patching;

using PlayerRoles;

using PluginAPI.Core;
using PluginAPI.Enums;
using PluginAPI.Events;

using System;
using System.Timers;
using System.Collections.Generic;
using helpers.Network.Events.Client;
using PlayerStatsSystem;
using UnityEngine.PlayerLoop;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Map;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PluginAPI.Core.Zones;
using Compendium.Round;
using helpers;
using System.Reflection;

namespace AutoWarhead {
    public static class AutoWarheadLogic {
        private static bool _isEnabled;
        private static bool _isDetonating = false; // true only if automatic warhead started by itself
        private static bool _079EventsRegistered = true;
        private static Timer _warningTimer;
        private static Timer _warheadTimer;

        public static void Init() {
            _warningTimer = new Timer();
            _warningTimer.Elapsed += AutoWarheadWarningEvent;
            _warningTimer.AutoReset = false;
            _warheadTimer = new Timer();
            _warheadTimer.Elapsed += AutoWarheadStartEvent;
            _warheadTimer.AutoReset = false;

            RestartPrepare();
        }

        public static bool IsEnabled {
            get => _isEnabled;
            set {
                if (value == _isEnabled)
                    return;

                if (!value) {
                    _isEnabled = false;
                    _warheadTimer.Stop();
                    _warningTimer.Stop();
                    AutoWarheadStop();

                    EventRegistry.UnregisterEvents(Reflection.Method(typeof(AutoWarheadLogic), nameof(AutoWarheadLogic.StartTimers)));

                    FLog.Info($"Auto Warhead disabled.");
                } else {
                    _isEnabled = true;

                    EventRegistry.RegisterEvents(Reflection.Method(typeof(AutoWarheadLogic), nameof(AutoWarheadLogic.StartTimers)));

                    if (IsRoundStarted()) StartTimers();
                    FLog.Info($"Auto Warhead enabled.");
                }
            }
        }

        public static bool IsDetonating { get => _isDetonating; }

        [IniConfig(Name = "Default Enabled", Description = "Whether the Automatic Alpha Warhead is enabled on the start of the round. [Default: true]")]
        public static bool DefaultEnabled { get; set; } = true;

        [IniConfig(Name = "SCP-079 RegularNuke Survive", Description = "Whether SCP-079 survives regular Alpha Warhead detonation. [Default: false]")]
        public static bool SCP079RegularSurvive { get; set; } = false;

        [IniConfig(Name = "SCP-079 AutoNuke Survive", Description = "Whether SCP-079 survives Automatic Alpha Warhead detonation. [Default: true]")]
        public static bool SCP079AutoSurvive { get; set; } = true;

        [IniConfig(Name = "Auto Warhead Message", Description = "Message of Auto Warhead broadcast.")]
        public static string AutoWarheadMessage { get; set; } = "Alpha Warhead is being automatically detonated and cannot be cancelled.";

        [IniConfig(Name = "Auto Warhead Message on Continue", Description = "Message of Auto Warhead broadcast, if Warhead was not started by Auto Warhead.")]
        public static string AutoWarheadMessageOnContinue { get; set; } = "Alpha Warhead is locked and cannot be cancelled.";

        [IniConfig(Name = "SCP-079 Will die Message", Description = "Additional message of Auto Warhead broadcast, if SCP-079 will survive.")]
        public static string SCP079DieMessage { get; set; } = "SCP-079 will die.";

        [IniConfig(Name = "SCP-079 Will survive Message", Description = "Additional message of Auto Warhead broadcast, if SCP-079 will survive.")]
        public static string SCP079SurviveMessage { get; set; } = "SCP-079 will survive.";

        [IniConfig(Name = "Auto Warhead Message Time", Description = "Time of Auto Warhead Message broadcast (seconds) [Default: 10]")]
        public static ushort AutoWarheadMessageTime { get; set; } = 10;

        [IniConfig(Name = "Warning enable", Description = "Whether C.A.S.S.I.E warning message annouce is enabled. [Default: true]")]
        public static bool WarningEnabled { get; set; } = true;

        [IniConfig(Name = "Warning Message", Description = "Warning message to C.A.S.S.I.E before Auto Warhead activation.")]
        public static string WarningMessage { get; set; } = "pitch_.2 .g4 .g4 pitch_1. Facility diagnostic anomaly detected . .g2 o 5 password accepted . .g3 automatic warhead detonation sequence authorized pitch_.9 .g3 pitch_1 detonation tminus 90 seconds . all personnel evacuate pitch_.8 . .g1 . .g1 . .g1 pitch_1 bell_end";

        [IniConfig(Name = "Announce before", Description = "Time of C.A.S.S.I.E warning message announce. (minutes) [Default: 23.25]")]
        public static double WarningTime { get; set; } = 23.25;

        [IniConfig(Name = "Warning Show Subtitles", Description = "Show subtitles on C.A.S.S.I.E warning message. [Default: true]")]
        public static bool WarningShowSubtitles { get; set; } = true;

        [IniConfig(Name = "Start After", Description = "Automatic Alpha Warhead start time (minutes) [Default: 25]")]
        public static double StartAfter { get; set; } = 25;

        [RoundStateChanged(RoundState.Restarting)]
        public static void RestartPrepare() {
            _isDetonating = false;
            IsEnabled = DefaultEnabled;
            if (IsEnabled) {
                _warningTimer.Stop();
                _warheadTimer.Stop();
            }
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void UnregisterOnStart() {
            if (_079EventsRegistered && (SCP079RegularSurvive || SCP079AutoSurvive)) {
                EventRegistry.UnregisterEvents(Reflection.Method(typeof(AutoWarheadLogic), nameof(AutoWarheadLogic.OnPlayerDamage)));
                EventRegistry.UnregisterEvents(Reflection.Method(typeof(AutoWarheadLogic), nameof(AutoWarheadLogic.CameraChangeCheck)));
                _079EventsRegistered = false;
            }
        }

        [Event(ServerEventType.WarheadDetonation)]
        private static void OnDetonate() {
            if (!_079EventsRegistered && (SCP079RegularSurvive || SCP079AutoSurvive)) {
                EventRegistry.RegisterEvents(Reflection.Method(typeof(AutoWarheadLogic), nameof(AutoWarheadLogic.OnPlayerDamage)));
                EventRegistry.RegisterEvents(Reflection.Method(typeof(AutoWarheadLogic), nameof(AutoWarheadLogic.CameraChangeCheck)));
                _079EventsRegistered = true;
            }
        }

        [Event(ServerEventType.RoundStart)]
        private static void StartTimers() {
            FLog.Info($"Auto Warhead Starting timers.");
            double interval = StartAfter * 60 * 1000 - Round.Duration.TotalMilliseconds;
            if (interval > 0) {
                _warheadTimer.Interval = interval;
                _warheadTimer.Start();
            } else {
                _warheadTimer.Stop();
                AutoWarheadStart();
            }

            if (!WarningEnabled) return;
            interval = WarningTime * 60 * 1000 - Round.Duration.TotalMilliseconds;
            if (interval > 0) {
                _warningTimer.Interval = interval;
                _warningTimer.Start();
            }
        }

        [Event]
        private static void OnPlayerDamage(PlayerDamageEvent ev, ValueContainer isAllowed) {
            if (ev.DamageHandler is WarheadDamageHandler && ev.Target.Role is RoleTypeId.Scp079 &&
               ((SCP079RegularSurvive && !_isDetonating) || (SCP079AutoSurvive && _isDetonating))) {
                isAllowed.Value = false;
            }
        }

        [Event]
        private static void CameraChangeCheck(Scp079CameraChangedEvent ev, ValueContainer isAllowed) {
            if (Warhead.IsDetonated && !(ev.Camera.Room.Zone is MapGeneration.FacilityZone.Surface)) {
                isAllowed.Value = false;
            }
        }

        private static void AutoWarheadStart() {
            if (Warhead.IsDetonated || _isDetonating || !IsEnabled) return;
            bool isSCP079InGame = !Scp079Role.ActiveInstances.IsEmpty();
            string broadcastMessage;
            if (Warhead.IsDetonationInProgress) {
                broadcastMessage = AutoWarheadMessageOnContinue;
                if (isSCP079InGame)
                    broadcastMessage += " " + (SCP079RegularSurvive ? SCP079SurviveMessage : SCP079DieMessage);
                FLog.Info("Automatic Alpha Warhead continuing on regular warhead detonation");
            } else {
                broadcastMessage = AutoWarheadMessage;
                if (isSCP079InGame)
                    broadcastMessage += " " + (SCP079AutoSurvive ? SCP079SurviveMessage : SCP079DieMessage);
                Warhead.Start(false, true);
                FLog.Info("Automatic Alpha Warhead start");
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
            FLog.Info("Automatic Alpha Warhead stopped");
        }


        private static void AutoWarheadStartEvent(Object source, ElapsedEventArgs e) {
            if (StartAfter > Round.Duration.TotalMinutes + 0.02) return;
            AutoWarheadStart();
        }

        private static void AutoWarheadWarningEvent(Object source, ElapsedEventArgs e) {
            Cassie.Message(WarningMessage, false, true, WarningShowSubtitles);
        }

        public static bool IsRoundStarted() {
            try {
                return Round.IsRoundStarted;
            } catch (NullReferenceException) { }
            return false;
        }
    }
}
