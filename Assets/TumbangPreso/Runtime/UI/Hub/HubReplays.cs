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
        private InputField _folder;
        private Text _status;
        private RectTransform _rows;
        private List<LocalReplayEntry> _entries = new List<LocalReplayEntry>();
        private Task<List<LocalReplayEntry>> _listing;
        private int _page;
        public override float CourtShade => 1;
        public override bool ShowsQueuePlate => false;
        public override void Build()
        {
            HubScenery.YeroGround(Root, 1450);
            HubChrome.Back(Root, Hub);
            HubScenery.SprayTitle(HubChrome.Title(Root, "LOCAL", "REPLAYS"));
            _status = HubKit.Text(Root, "ReplayHelp", "Matches save automatically, including Custom rooms. Choose a replay to watch from any camera.", HubStyle.Body, false, HubStyle.Paper);
            HubKit.Place(_status.rectTransform, HubKit.TopLeft, new Vector2(90, -192), new Vector2(1720, 70));
            _folder = HubField.Build(Root, "ReplayFolder", LocalReplayStore.Folder, "Absolute save folder", 2048, 1451);
            HubKit.Place((RectTransform)_folder.transform, HubKit.TopLeft, new Vector2(90, -280), new Vector2(1130, 80));
            var save = HubKit.Button(Root, "ChangeReplayFolder", "USE FOLDER", HubStyle.Honey, ChangeFolder);
            HubKit.Place((RectTransform)save.transform, HubKit.TopLeft, new Vector2(1240, -280), new Vector2(270, 80));
            var open = HubKit.Button(Root, "OpenReplayFolder", "OPEN FOLDER", HubStyle.Honey, OpenFolder);
            HubKit.Place((RectTransform)open.transform, HubKit.TopLeft, new Vector2(1530, -280), new Vector2(300, 80));
            _rows = HubKit.Span(HubKit.Rect(Root, "ReplayList"), Vector2.zero, Vector2.one,
                new Vector2(90, 160), new Vector2(90, 390));
            var previous = HubKit.Button(Root, "PreviousReplays", "PREVIOUS", HubStyle.Honey, () => { _page = Mathf.Max(0, _page - 1); Draw(); });
            HubKit.Place((RectTransform)previous.transform, HubKit.BottomLeft, new Vector2(90, 55), new Vector2(270, 80));
            var refresh = HubKit.Button(Root, "RefreshReplays", "REFRESH", HubStyle.Honey, Refresh);
            HubKit.Place((RectTransform)refresh.transform, HubKit.BottomLeft, new Vector2(380, 55), new Vector2(270, 80));
            var next = HubKit.Button(Root, "NextReplays", "NEXT", HubStyle.Honey,
                () => { _page = Mathf.Min(Mathf.Max(0, (_entries.Count - 1) / 5), _page + 1); Draw(); });
            HubKit.Place((RectTransform)next.transform, HubKit.BottomRight, new Vector2(-90, 55), new Vector2(270, 80));
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
            _entries = task.Result; _page = Mathf.Min(_page, Mathf.Max(0, (_entries.Count - 1) / 5)); Draw();
        }
        private void Draw()
        {
            foreach (Transform child in _rows) Destroy(child.gameObject);
            _status.text = _entries.Count == 0 ? "No recordings in this folder yet. Completed matches and interrupted recordings appear here." :
                _entries.Count + " local replays · page " + (_page + 1) + " / " + Mathf.Max(1, Mathf.CeilToInt(_entries.Count / 5f));
            for (int i = 0; i < 5 && _page * 5 + i < _entries.Count; i++)
            {
                var entry = _entries[_page * 5 + i]; var manifest = entry.Manifest;
                string date = DateTime.TryParse(manifest.CreatedUtc, out var utc) ? utc.ToLocalTime().ToString("MMM d · HH:mm") : "Recorded match";
                string words = date + "  ·  " + SceneFlow.PreviewFor(manifest.Map).Name + "  ·  " + manifest.Mode +
                    "  ·  " + Mathf.FloorToInt(manifest.Duration / 60) + ":" + ((int)manifest.Duration % 60).ToString("00") +
                    (manifest.Custom ? "  · CUSTOM" : "") + (manifest.Completed ? "" : "  · PARTIAL");
                var button = HubKit.Button(_rows, "Replay" + i, words, HubStyle.Honey, () => Watch(entry), HubStyle.Body, 1460 + i);
                HubKit.Place((RectTransform)button.transform, HubKit.TopLeft, new Vector2(0, -i * 96), new Vector2(1740, 84));
                HubKit.LabelOf(button).alignment = TextAnchor.MiddleLeft;
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
