//using helpers;
using helpers.Patching;
using Mirror;
using Interactables.Interobjects.DoorUtils;

using PluginAPI.Events;
using PlayerRoles;

using UnityEngine;
using PlayerRoles.PlayableScps.Scp079;
using System;
using Interactables.Interobjects;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using System.Collections.Generic;
using helpers.Extensions;
using InventorySystem.Items.Usables.Scp244;
using LightContainmentZoneDecontamination;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using PluginAPI.Core;
using Respawning;

namespace AutoWarhead {
    public abstract class AutoWarheadPatch {

        public static readonly PatchInfo patch = new PatchInfo(
            new PatchTarget(typeof(Scp079DoorAbility), nameof(Scp079DoorAbility.ValidateAction)),
            new PatchTarget(typeof(AutoWarheadPatch), nameof(AutoWarheadPatch.DoorPatch)), PatchType.Prefix, "SCP-079 door patch");
        public static readonly PatchInfo patch2 = new PatchInfo(
            new PatchTarget(typeof(AlphaWarheadController), nameof(AlphaWarheadController.Detonate)),
            new PatchTarget(typeof(AutoWarheadPatch), nameof(AutoWarheadPatch.CameraChange)), PatchType.Prefix, "SCP-079 Camera change");

        public static bool DoorPatch(ref bool __result, DoorAction action, DoorVariant door, Scp079Camera currentCamera) {
            if (!Scp079DoorAbility.CheckVisibility(door, currentCamera)) {
                __result = false;
                return false;
            }

            if (NetworkServer.active && AlphaWarheadController.InProgress && !AlphaWarheadController.Detonated) {
                __result = false;
                return false;
            }
            DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)door.ActiveLocks);
            IDamageableDoor damageableDoor = door as IDamageableDoor;
            if (damageableDoor != null && damageableDoor.IsDestroyed) {
                __result = false;
                return false;
            }
            if (mode.HasFlagFast(DoorLockMode.ScpOverride)) {
                __result = true;
                return false;
            }
            switch (action) {
                case DoorAction.Opened:
                    __result = mode.HasFlagFast(DoorLockMode.CanOpen);
                    return false;
                case DoorAction.Closed:
                    __result = mode.HasFlagFast(DoorLockMode.CanClose);
                    return false;
                case DoorAction.Locked:
                    __result = mode != DoorLockMode.FullLock && !(door is CheckpointDoor);
                    return false;
                case DoorAction.Unlocked:
                    __result = true;
                    return false;
            }
            __result = false;
            return false;
        }

        public static bool CameraChange() {
            List<Scp079Camera> surfaceCameras = new List<Scp079Camera>();
            foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances) {
                Scp079Camera scp079Camera = scp079InteractableBase as Scp079Camera;
                if (scp079Camera != null && scp079Camera.Room.Zone == MapGeneration.FacilityZone.Surface) {
                    surfaceCameras.Add(scp079Camera);
                    //Log.Info($"Camera: {scp079Camera.name}");
                }
            }
            if (surfaceCameras.Count < 1) return true;
            foreach (Scp079Role scp079role in Scp079Role.ActiveInstances) {
                if (scp079role._curCamSync.CurrentCamera.Room.Zone != MapGeneration.FacilityZone.Surface)
                    scp079role._curCamSync.CurrentCamera = surfaceCameras[0];
            }
            return true;
        }
    }
}
