using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    public sealed class HubReplays : HubScreen
    {
        public override bool WearsBoard => true;
        private InputField _folder;
        private Text _status;
        private RectTransform _rows;
        private List<LocalReplayEntry> _entries = new List<LocalReplayEntry>();
        private Task<List<LocalReplayEntry>> _listing;
        private int _page;
        private bool Touch => Application.isMobilePlatform || InputLayer.TouchHud.ForceVisible;
        private int PageSize => Touch ? 3 : 5;
        /// <summary>True when the player profile's REPLAYS tab opened this screen: BACK returns to the profile.</summary>
        public bool ReturnToProfile { get; set; }
        public override bool Back()
        {
            if (!ReturnToProfile) return false;
            Hub.Pop(this);
            Hub.Host.OpenProfile();
            return true;
        }
        public override float CourtShade => 1;
        public override bool ShowsQueuePlate => false;
        public override void Build()
        {
            // The boot kit (`boot_record.py`): the berry ground, dark rows, yellow buttons.
            HubKnit.Ground(Root);
            HubKnit.Back(Root, Hub);
            HubKnit.Title(Root, "LOCAL REPLAYS", "", out _status);
            _status.name = "ReplayHelp";
            _status.text = "Matches save automatically, including Custom rooms. Choose a replay to watch from any camera.";
            HubKnit.At(_status.rectTransform, 60, 126, 1700, 44);
            float small = Touch ? Mathf.Max(100, InputLayer.TouchMetrics.MinTargetUnits) : 92;
            if (Touch)
            {
                var storage = HubKnit.Words(Root, "ReplayStorage", "Saved on this device. Select a recording below to watch it.", HubStyle.Body, HubKnit.KitWhite, TextAnchor.MiddleLeft, true, 3);
                HubKnit.At(storage.rectTransform, 60, 190, 1740, 60);
            }
            else
            {
                _folder = HubKnit.Field(Root, "ReplayFolder", LocalReplayStore.Folder, "Absolute save folder", 2048, 30);
                HubKnit.At((RectTransform)_folder.transform, 60, 214, 1130, 100);
                var save = HubKnit.Button(Root, "ChangeReplayFolder", "USE FOLDER", Knit.Yellow, ChangeFolder, 38);
                HubKnit.At((RectTransform)save.transform, 1210, 222, 320, 92);
                var open = HubKnit.Button(Root, "OpenReplayFolder", "OPEN FOLDER", Knit.Yellow, OpenFolder, 38);
                HubKnit.At((RectTransform)open.transform, 1550, 222, 310, 92);
            }
            _rows = HubKit.Rect(Root, "ReplayList");
            HubKnit.At(_rows, 60, Touch ? 280 : 346, 1800, 560);
            var previous = HubKnit.Button(Root, "PreviousReplays", "PREVIOUS", Knit.Yellow, () => { _page = Mathf.Max(0, _page - 1); Draw(); }, 40);
            HubKnit.At((RectTransform)previous.transform, 60, 1080 - 60 - small, 320, small);
            var refresh = HubKnit.Button(Root, "RefreshReplays", "REFRESH", Knit.Yellow, Refresh, 40);
            HubKnit.At((RectTransform)refresh.transform, 410, 1080 - 60 - small, 320, small);
            var next = HubKnit.Button(Root, "NextReplays", "NEXT", Knit.Yellow,
                () => { _page = Mathf.Min(Mathf.Max(0, (_entries.Count - 1) / PageSize), _page + 1); Draw(); }, 40);
            HubKnit.At((RectTransform)next.transform, 1540, 1080 - 60 - small, 320, small);
            Refresh();
        }
        private void Refresh()
        {
            if (_listing != null) return;
            string folder = LocalReplayStore.Folder;
            _status.text = "Reading local replays…";
            _listing = Task.Run(() => LocalReplayStore.List(folder));
        }
        public override void Tick()
        {
            if (_listing == null || !_listing.IsCompleted) return;
            var task = _listing; _listing = null;
            if (task.IsFaulted) { _status.text = "Cannot read this folder: " + task.Exception.GetBaseException().Message; return; }
            _entries = task.Result; _page = Mathf.Min(_page, Mathf.Max(0, (_entries.Count - 1) / PageSize)); Draw();
        }
        private void Draw()
        {
            foreach (Transform child in _rows) Destroy(child.gameObject);
            _status.text = _entries.Count == 0 ? "No recordings in this folder yet. Completed matches and interrupted recordings appear here." :
                _entries.Count + " local replays · page " + (_page + 1) + " / " + Mathf.Max(1, Mathf.CeilToInt(_entries.Count / (float)PageSize));
            for (int i = 0; i < PageSize && _page * PageSize + i < _entries.Count; i++)
            {
                var entry = _entries[_page * PageSize + i]; var manifest = entry.Manifest;
                string date = DateTime.TryParse(manifest.CreatedUtc, out var utc) ? utc.ToLocalTime().ToString("MMM d · HH:mm") : "Recorded match";
                string length = Mathf.FloorToInt(manifest.Duration / 60) + ":" + ((int)manifest.Duration % 60).ToString("00");
                float rowH = Touch ? Mathf.Max(100, InputLayer.TouchMetrics.MinTargetUnits) : 100;
                var button = HubKnit.Button(_rows, "Replay" + i, null, Knit.DarkCard, () => Watch(entry));
                HubKnit.In((RectTransform)button.transform, 0, i * (rowH + 16), 1800, rowH + HubKnit.SlabHeight);
                var face = button.Body;
                void Column(string name, string words, float x, float width, Color colour, bool display)
                {
                    var text = display ? HubKnit.Display(face, name, words, 38, colour, TextAnchor.MiddleLeft)
                                       : HubKnit.Words(face, name, words, 38, colour, TextAnchor.MiddleLeft, true);
                    HubKnit.In(text.rectTransform, x, 0, width, rowH - 6);
                    HubKit.Fit(text, width);
                }
                Column("When", date, 48, 440, HubKnit.KitCream, true);
                Column("Court", SceneFlow.PreviewFor(manifest.Map).Name.ToUpperInvariant(), 500, 240, HubKnit.KitCream, true);
                Column("Mode", manifest.Mode.ToString(), 760, 340, HubKnit.KitTan, true);
                Column("Length", length, 1120, 200, HubKnit.KitCream, true);
                string tag = !manifest.Completed ? "PARTIAL" : manifest.Custom ? "CUSTOM" : null;
                if (tag != null)
                {
                    var chip = HubKnit.Tag(face, "State", tag, Knit.PillPlain, 62);
                    HubKnit.In(chip, 1420, rowH / 2 - 34, chip.sizeDelta.x, 62);
                }
            }
        }
        private void Watch(LocalReplayEntry entry)
        {
            if (Hub.Host.InRoom || Net.Matchmaker.Current?.IsQueueing == true)
            { _status.text = "Leave your room or queue before opening a replay."; return; }
            if (!LocalReplayPlayback.Open(entry)) _status.text = "This replay cannot be opened right now.";
        }
        private void ChangeFolder()
        {
            if (!LocalReplayStore.SetFolder(_folder.text, out string error)) { _status.text = error; return; }
            _folder.text = LocalReplayStore.Folder; _page = 0; Refresh();
        }
        private void OpenFolder()
        {
            try
            {
                string folder = LocalReplayStore.Folder; Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder) { UseShellExecute = true });
            }
            catch (Exception error) { _status.text = "Cannot open the folder: " + error.Message; }
        }
    }
}
