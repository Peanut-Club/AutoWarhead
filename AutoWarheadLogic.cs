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

namespace AutoWarhead {
    public static class AutoWarheadLogic {
        private static bool _isEnabled;
        private static bool _isDetonating = false;
        private static double _startAfter = 25;
        private static Timer _warheadTimer;

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
                    ServerEventType.RoundStart.RemoveHandler<Action>(StartTimer);

                    Log.Info($"Auto Warhead disabled.");
                    return;
                }

                _isEnabled = true;
                if (IsRoundStarted()) StartTimer();

                ServerEventType.RoundRestart.AddHandler<Action>(OnRoundRestart);
                ServerEventType.RoundStart.AddHandler<Action>(StartTimer);

                Log.Info($"Auto Warhead enabled.");
            }
        }

        [IniConfig(Name = "Default Enabled", Description = "Whether the Automatic Alpha Warhead is enabled on the start of the round. [Default: true]")]
        public static bool DefaultEnabled { get; set; } = true;

        [IniConfig(Name = "Start After", Description = "Start the Automatic Alpha Warhead after ... minutes. [Default: 25]")]
        public static double StartAfter {
            get => _startAfter;
            set {
                if (value <= 0) _startAfter = 0.001;
                else _startAfter = value;
                if (IsRoundStarted() && IsEnabled) StartTimer();
                Log.Info($"Automatic Warhead time set to {_startAfter} minutes.");
            }
        }

        public static void TimerInit() {
            _warheadTimer = new Timer();
            _warheadTimer.Elapsed += AutoWarheadStartEvent;
            _warheadTimer.AutoReset = false;
        }

        private static void StartTimer() {
            double interval = StartAfter * 60 * 1000 - Round.Duration.TotalMilliseconds;
            if (interval > 0) {
                _warheadTimer.Interval = interval;
                _warheadTimer.Start();
            } else {
                _warheadTimer.Stop();
                AutoWarheadStart();
            }
        }

        public static string WarheadTime() {
            int seconds = Convert.ToInt32(StartAfter * 60);
            return $"{seconds / 60} minutes and {seconds % 60} seconds";
        }

        private static void AutoWarheadStartEvent(Object source, ElapsedEventArgs e) {
            AutoWarheadStart();
        }

        private static void AutoWarheadStart() {
            if (Warhead.IsDetonationInProgress || Warhead.IsDetonated) return;
            _isDetonating = true;
            Warhead.Start();
            //Warhead.IsLocked = true;
            //Cassie.
            Log.Info("Automatic Alpha Warhead start");
        }

        public static void AutoWarheadStop() {
            if (Warhead.IsDetonated || !_isDetonating) return;
            _isDetonating = false;
            Warhead.IsLocked = false;
            Warhead.Stop();
            //Cassie.
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
    }
}
