using System;
using System.Collections;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator ReviewVictimCatch(CharacterMotor taya, string label)
        {
            Stage(label + " staged native victim catch and taya continuation");
            var round = GameServices.Round;
            var victim = round.Players.First(p => p != null && !p.IsDefender);
            var rig = Camera.main.GetComponent<CameraRig>(); var followed = rig.Following;
            bool motion = Settings.SettingsStore.Current.CinematicCameraMotion;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            var shoe = Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s => s.SeatOfOrigin == victim.PlayerSlot);
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            try
            {
                victim.ClearStun(); victim.ClearTrip(); victim.Intent.Clear(); victim.Intent.Parked = false;
                taya.ClearStun(); taya.ClearTrip(); taya.Intent.Clear(); taya.Intent.Parked = false;
                Vector3 can = round.Lata.transform.position;
                victim.Teleport(can + Vector3.back * 2.2f); victim.transform.forward = Vector3.forward;
                taya.Teleport(can + Vector3.back * 3.2f); taya.transform.forward = Vector3.forward;
                shoe.gameObject.SetActive(true);
                if (!shoe.HostForceEquip(victim)) throw new InvalidOperationException("Victim fixture could not equip its own shoe");
                rig.Follow(victim, true); Hud.Instance.Bind(victim);
                yield return new WaitForSeconds(.5f);
                if (!victim.IsTaggable() || !round.Lata.IsUpright) throw new InvalidOperationException("Victim fixture was not catchable");
                Vector3 before = taya.transform.position;
                if (!taya.GetComponent<CombatVerbs>().HostResolvePunch(before, taya.transform.forward))
                    throw new InvalidOperationException("Native catch was refused");
                var reconstruction = Object.FindAnyObjectByType<CatchReconstruction>();
                if (reconstruction == null || !reconstruction.Playing) throw new InvalidOperationException("Native victim reconstruction did not start");
                if (!taya.CanAct()) throw new InvalidOperationException("Reconstruction locked the taya");
                Keys(Key.W); yield return new WaitForSecondsRealtime(.22f); Keys();
                if (Vector3.Distance(taya.transform.position, before) < .15f)
                    throw new InvalidOperationException("Taya did not continue moving while the victim watched the catch");
                yield return Shot(label + "-native-victim-catch");
                yield return WaitFor(() => !reconstruction.Playing, 2);
                if (victim.CanAct()) throw new InvalidOperationException("Catch playback shortened the real penalty");
                yield return Shot(label + "-native-victim-return");
                Stage(label + " native victim view returned inside recovery; taya kept moving");
            }
            finally
            {
                Keys(); rig.Follow(followed, true); Hud.Instance.Bind(taya);
                victim.Intent.Clear(); victim.Intent.Parked = true;
                Settings.SettingsStore.Current.CinematicCameraMotion = motion;
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }
    }
}
