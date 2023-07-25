using System.Collections.Generic;

using helpers.Patching;
using Mirror;
using Interactables.Interobjects.DoorUtils;
using Compendium.Features;
using Interactables.Interobjects;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;

namespace AutoWarhead {
    public static class AutoWarheadPatch {

        [Patch(typeof(Scp079DoorAbility), nameof(Scp079DoorAbility.ValidateAction), PatchType.Prefix, "SCP-079 door patch")]
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

        [Patch(typeof(AlphaWarheadController), nameof(AlphaWarheadController.Detonate), PatchType.Prefix, "SCP-079 Camera change")]
        public static bool CameraChange() {
            List<Scp079Camera> surfaceCameras = new List<Scp079Camera>();
            foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances) {
                Scp079Camera scp079Camera = scp079InteractableBase as Scp079Camera;
                if (scp079Camera != null && scp079Camera.Room.Zone == MapGeneration.FacilityZone.Surface) {
                    surfaceCameras.Add(scp079Camera);
                    //FLog.Info($"Camera: {scp079Camera.name}");
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
