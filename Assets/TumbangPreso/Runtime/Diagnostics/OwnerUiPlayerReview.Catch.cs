using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator RecordCatchMotion(string name, float seconds)
        {
            string folder = Path.Combine(_folder, name); Directory.CreateDirectory(folder);
            var csv = new StringBuilder("frame,real_seconds\n");
            float start = Time.realtimeSinceStartup, next = 0; int frame = 0;
            File.WriteAllText(Path.Combine(folder, "capture-start.txt"), start.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            while (Time.realtimeSinceStartup - start < seconds)
            {
                yield return new WaitForEndOfFrame();
                float age = Time.realtimeSinceStartup - start;
                if (age < next) continue;
                var image = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, frame.ToString("00000") + ".jpg"), image.EncodeToJPG(93));
                Destroy(image); csv.AppendLine(FormattableString.Invariant($"{frame},{age:F6}"));
                next = age + 1f / 30; frame++;
            }
            File.WriteAllText(Path.Combine(folder, "frames.csv"), csv.ToString());
        }

        private IEnumerator ReviewVictimCatch(CharacterMotor taya, string label)
        {
            Stage(label + " staged native victim catch and taya continuation");
            var round = GameServices.Round;
            var tayaReader = taya.GetComponent<PlayerInputReader>();
            bool readerEnabled = tayaReader != null && tayaReader.enabled;
            if (tayaReader != null) tayaReader.enabled = false;
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
                var movie = StartCoroutine(RecordCatchMotion(label + "-catch-motion", 2.8f));
                yield return new WaitForSecondsRealtime(.35f);
                Vector3 before = taya.transform.position;
                if (!taya.GetComponent<CombatVerbs>().HostResolvePunch(before, taya.transform.forward))
                    throw new InvalidOperationException("Native catch was refused");
                var reconstruction = Object.FindAnyObjectByType<CatchReconstruction>();
                if (reconstruction == null || !reconstruction.Playing) throw new InvalidOperationException("Native victim reconstruction did not start");
                if (!taya.CanAct()) throw new InvalidOperationException("Reconstruction locked the taya");
                taya.Intent.Move = Vector2.up; yield return new WaitForSecondsRealtime(.22f); taya.Intent.Clear();
                if (Vector3.Distance(taya.transform.position, before) < .15f)
                    throw new InvalidOperationException("Taya did not continue moving while the victim watched the catch");
                yield return Shot(label + "-native-victim-catch");
                yield return WaitFor(() => !reconstruction.Playing, 2);
                if (victim.CanAct()) throw new InvalidOperationException("Catch playback shortened the real penalty");
                yield return Shot(label + "-native-victim-return");
                yield return movie;
                Stage(label + " native victim view returned inside recovery; taya kept moving");
            }
            finally
            {
                taya.Intent.Clear();
                if (tayaReader != null) tayaReader.enabled = readerEnabled;
                rig.Follow(followed, true); Hud.Instance.Bind(taya);
                victim.Intent.Clear(); victim.Intent.Parked = true;
                Settings.SettingsStore.Current.CinematicCameraMotion = motion;
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            }
        }
    }
}
