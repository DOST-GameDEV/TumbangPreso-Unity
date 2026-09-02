using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `multiplayer_setup.gd`.
    ///
    /// Hosting online and hosting on the LAN are two distinct buttons.
    /// Bottom row includes Back, LAN server browser, and Online server browser buttons.
    /// LAN and Online browsers display interactive discovery dialogs.
    /// </summary>
    public sealed class ConvertedMultiplayerSetup : ConvertedScreen
    {
        private NetSession _net;
        private InputField _address;

        private Button _lanBrowseButton;
        private Button _onlineBrowseButton;

        private GameObject _lanBox;
        private Text _lanBoxTitle;
        private Text _lanBoxHint;
        private readonly List<Button> _lanRows = new List<Button>();
        private readonly List<string> _lanAddresses = new List<string>();

        private GameObject _onlineBox;
        private Text _onlineBoxTitle;
        private Text _onlineBoxHint;
        private readonly List<Button> _onlineRows = new List<Button>();
        private readonly List<string> _onlineCodesOrAddrs = new List<string>();

        protected override string CancelTarget => SceneFlow.ModeSelect;

        protected override void Wire()
        {
            _net = NetSession.Ensure();

            SetText("BannerLabel", "MULTIPLAYER");

            // ⚠️ THIS SCREEN IS WHAT `FUTURE.md` § 3's FUNNEL CALLS "first queue". There is no
            // matchmaking queue to instrument yet (Phase 7 owns that), and the honest equivalent
            // is the first time somebody tries to play with other people at all. Reaching here is
            // that moment: hosting, browsing LAN and entering a join code all start on this
            // screen. `docs/TODO.md` § 90.3 records the substitution so the step is not later
            // read as a queue time.
            GameServices.Telemetry?.NoteQueueOpened();

            // ⚠️⚠️ WHY THE LAST JOIN ENDED, SHOWN ON THE SCREEN THAT CAN ACT ON IT. A refused
            // approval arrives seconds after `Join` has already navigated to the lobby, so the
            // reason used to be written to a status label on a screen nobody was looking at and
            // the player was returned here with a blank line and no idea what happened. A
            // protocol mismatch in particular is a thing they CAN fix, and it is the likeliest
            // one whenever two machines were built from different commits.
            //
            // ⚠️ READ ONCE AND CLEARED, so a stale reason cannot sit over a later good join.
            if (!string.IsNullOrWhiteSpace(NetSession.LastDisconnectReason))
            {
                SetStatus(NetSession.LastDisconnectReason);
                NetSession.LastDisconnectReason = "";
            }
            else
            {
                SetStatus("");
            }

            OnClick("HostOnlineButton", async () =>
            {
                SetStatus("Allocating online session...");
                bool ok = await _net.StartRelayHost();
                if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
                else SetStatus(Reason("Failed to allocate online Relay session. Check internet connection."));
            });

            OnClick("HostButton", async () =>
            {
                SetStatus("Starting LAN session...");
                if (await _net.StartHostAsync())
                {
                    SceneFlow.Go(SceneFlow.MatchSetup);
                }
                else
                {
                    SetStatus(Reason("Failed to open LAN host port. It may already be in use."));
                }
            });

            OnClick("JoinButton", Join);
            OnClick("BackButton", () =>
            {
                if (_lanBox != null && _lanBox.activeSelf)
                {
                    _lanBox.SetActive(false);
                    return;
                }
                if (_onlineBox != null && _onlineBox.activeSelf)
                {
                    _onlineBox.SetActive(false);
                    return;
                }
                SceneFlow.Go(SceneFlow.ModeSelect);
            });

            BindAddressField();
            BuildBottomBrowsers();

            _net.BrowseLan();
            _net.Query.StartBrowsing();

            if (_net.Beacon != null)
            {
                _net.Beacon.EntriesChanged += RefreshLanBrowser;
            }
            if (_net.Query != null)
            {
                _net.Query.ServersChanged += RefreshOnlineBrowser;
            }

            RefreshLanBrowser();
            RefreshOnlineBrowser();
        }

        /// <summary>
        /// Pairs the headline the player understands with the detail the session already worked
        /// out.
        ///
        /// ⚠️⚠️ EVERY FAILED START USED TO THROW THE REAL REASON AWAY. `NetSession` writes a
        /// precise status on the way out of each failure ("relay allocation failed: ...",
        /// "invalid relay join code", "cannot go online: no network route", "failed to start
        /// hosting"), and every caller here overwrote it with one fixed sentence. So a dead join
        /// code, a rate-limited lookup, a refused port and a machine with no internet were all
        /// the same line on screen, which is what makes an intermittent failure impossible for
        /// the player to describe or for the next session to reproduce. 🧑 2026-08-28: *"it
        /// sometimes says failed to join online host via relay ... sometimes i get it to work"*.
        ///
        /// ⚠️ THE HEADLINE STAYS FIRST. This is the same fix `Wire`'s `LastDisconnectReason`
        /// block already made for disconnects, in the same file, for the same reason.
        /// </summary>
        private string Reason(string headline)
        {
            string detail = _net != null ? _net.Status : null;
            if (string.IsNullOrWhiteSpace(detail)) return headline;
            return $"{headline}  ({detail})";
        }

        private void SetStatus(string msg)
        {
            var node = Node("StatusLabel");
            if (node == null) return;
            var txt = node.GetComponent<Text>();
            if (txt != null)
            {
                txt.color = UiTheme.Impact;
                txt.text = msg;
            }
        }

        private void BuildBottomBrowsers()
        {
            var backBtnNode = Node("BackButton");
            if (backBtnNode == null || backBtnNode.parent == null) return;

            Transform parent = backBtnNode.parent;

            // Browse LAN button (Middle)
            _lanBrowseButton = MenuKit.WoodButton(parent, "GAMES ON YOUR LAN  ·  searching…",
                                                  new Vector2(0, 1), new Vector2(720, -1009),
                                                  new Vector2(580, 58), OnLanBrowseClicked);
            _lanBrowseButton.name = "BrowseLanButton";

            // Browse Online button (Right)
            _onlineBrowseButton = MenuKit.WoodButton(parent, "ONLINE SERVERS  ·  searching…",
                                                     new Vector2(0, 1), new Vector2(1360, -1009),
                                                     new Vector2(580, 58), OnOnlineBrowseClicked);
            _onlineBrowseButton.name = "BrowseOnlineButton";

            BuildLanModal(parent);
            BuildOnlineModal(parent);
        }

        private void BuildLanModal(Transform parent)
        {
            _lanBox = new GameObject("LanBrowserBox");
            _lanBox.transform.SetParent(parent, false);

            var img = _lanBox.AddComponent<Image>();
            img.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var rt = _lanBox.GetComponent<RectTransform>();
            MenuKit.Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880, 500));

            var col = new GameObject("Column");
            col.transform.SetParent(_lanBox.transform, false);
            var colLayout = col.AddComponent<VerticalLayoutGroup>();
            colLayout.spacing = 14;
            colLayout.padding = new RectOffset(24, 24, 24, 24);
            colLayout.childControlWidth = true;
            colLayout.childControlHeight = false;
            colLayout.childForceExpandWidth = true;
            colLayout.childForceExpandHeight = false;
            MenuKit.Stretch(col.GetComponent<RectTransform>(), 0);

            _lanBoxTitle = MenuKit.Label(col.transform, "NO GAMES FOUND YET", 28, UiTheme.Amber,
                                         Vector2.zero, Vector2.zero, new Vector2(0, 36), TextAnchor.MiddleLeft);
            _lanBoxHint = MenuKit.Label(col.transform,
                                        "This finds games on your own network. If the host you want is missing - or is on Hamachi - type their address into the field above instead. Windows Firewall blocking the game is the usual reason a LAN game does not show up.",
                                        17, UiTheme.CreamMuted,
                                        Vector2.zero, Vector2.zero, new Vector2(0, 68), TextAnchor.MiddleLeft);
            _lanBoxHint.horizontalOverflow = HorizontalWrapMode.Wrap;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var rowBtn = MenuKit.WoodButton(col.transform, "", Vector2.zero, Vector2.zero,
                                                new Vector2(832, 54), () => OnLanRowClicked(index));
                rowBtn.gameObject.SetActive(false);
                var rowElement = rowBtn.gameObject.AddComponent<LayoutElement>();
                rowElement.preferredHeight = 54;
                rowElement.minHeight = 54;

                var rowTxt = rowBtn.GetComponentInChildren<Text>();
                if (rowTxt != null)
                {
                    rowTxt.fontSize = 18;
                    rowTxt.alignment = TextAnchor.MiddleLeft;
                    rowTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                    rowTxt.verticalOverflow = VerticalWrapMode.Overflow;
                    rowTxt.rectTransform.anchorMin = Vector2.zero;
                    rowTxt.rectTransform.anchorMax = Vector2.one;
                    rowTxt.rectTransform.offsetMin = new Vector2(20, 0);
                    rowTxt.rectTransform.offsetMax = new Vector2(-20, 0);
                }

                _lanRows.Add(rowBtn);
            }

            var closeBtn = MenuKit.WoodButton(col.transform, "CLOSE", Vector2.zero, Vector2.zero,
                                              new Vector2(0, 54), () => _lanBox.SetActive(false));
            var closeElement = closeBtn.gameObject.AddComponent<LayoutElement>();
            closeElement.preferredHeight = 54;
            closeElement.minHeight = 54;

            _lanBox.SetActive(false);
        }

        private void BuildOnlineModal(Transform parent)
        {
            _onlineBox = new GameObject("OnlineBrowserBox");
            _onlineBox.transform.SetParent(parent, false);

            var img = _onlineBox.AddComponent<Image>();
            img.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var rt = _onlineBox.GetComponent<RectTransform>();
            MenuKit.Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880, 500));

            var col = new GameObject("Column");
            col.transform.SetParent(_onlineBox.transform, false);
            var colLayout = col.AddComponent<VerticalLayoutGroup>();
            colLayout.spacing = 14;
            colLayout.padding = new RectOffset(24, 24, 24, 24);
            colLayout.childControlWidth = true;
            colLayout.childControlHeight = false;
            colLayout.childForceExpandWidth = true;
            colLayout.childForceExpandHeight = false;
            MenuKit.Stretch(col.GetComponent<RectTransform>(), 0);

            _onlineBoxTitle = MenuKit.Label(col.transform, "NO ONLINE SERVERS ANSWERED", 28, UiTheme.Amber,
                                           Vector2.zero, Vector2.zero, new Vector2(0, 36), TextAnchor.MiddleLeft);
            _onlineBoxHint = MenuKit.Label(col.transform,
                                          "Nothing in the online pool replied. It may be down, or your network may be blocking it. HOST GAME (LAN) and typing a host's address both still work - this screen keeps asking in the background.",
                                          17, UiTheme.CreamMuted,
                                          Vector2.zero, Vector2.zero, new Vector2(0, 68), TextAnchor.MiddleLeft);
            _onlineBoxHint.horizontalOverflow = HorizontalWrapMode.Wrap;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var rowBtn = MenuKit.WoodButton(col.transform, "", Vector2.zero, Vector2.zero,
                                                new Vector2(832, 54), () => OnOnlineRowClicked(index));
                rowBtn.gameObject.SetActive(false);
                var rowElement = rowBtn.gameObject.AddComponent<LayoutElement>();
                rowElement.preferredHeight = 54;
                rowElement.minHeight = 54;

                var rowTxt = rowBtn.GetComponentInChildren<Text>();
                if (rowTxt != null)
                {
                    rowTxt.fontSize = 18;
                    rowTxt.alignment = TextAnchor.MiddleLeft;
                    rowTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                    rowTxt.verticalOverflow = VerticalWrapMode.Overflow;
                    rowTxt.rectTransform.anchorMin = Vector2.zero;
                    rowTxt.rectTransform.anchorMax = Vector2.one;
                    rowTxt.rectTransform.offsetMin = new Vector2(20, 0);
                    rowTxt.rectTransform.offsetMax = new Vector2(-20, 0);
                }

                _onlineRows.Add(rowBtn);
            }

            var closeBtn = MenuKit.WoodButton(col.transform, "CLOSE", Vector2.zero, Vector2.zero,
                                              new Vector2(0, 54), () => _onlineBox.SetActive(false));
            var closeElement = closeBtn.gameObject.AddComponent<LayoutElement>();
            closeElement.preferredHeight = 54;
            closeElement.minHeight = 54;

            _onlineBox.SetActive(false);
        }

        private void OnLanBrowseClicked()
        {
            MenuSfx.Click();
            if (_onlineBox != null) _onlineBox.SetActive(false);
            if (_lanBox != null)
            {
                _lanBox.SetActive(!_lanBox.activeSelf);
                if (_lanBox.activeSelf) RefreshLanBrowser();
            }
        }

        private void OnOnlineBrowseClicked()
        {
            MenuSfx.Click();
            if (_lanBox != null) _lanBox.SetActive(false);
            if (_onlineBox != null)
            {
                _onlineBox.SetActive(!_onlineBox.activeSelf);
                if (_onlineBox.activeSelf) RefreshOnlineBrowser();
            }
        }

        private void RefreshLanBrowser()
        {
            var entries = _net?.Beacon?.SortedEntries ?? new List<LanEntry>();
            if (_lanBrowseButton != null)
            {
                var txt = _lanBrowseButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = entries.Count == 0
                        ? "GAMES ON YOUR LAN  ·  searching…"
                        : $"GAMES ON YOUR LAN  ·  {entries.Count} found";
                }
            }

            if (_lanBoxTitle != null)
            {
                _lanBoxTitle.text = entries.Count > 0 ? "GAMES ON YOUR NETWORK" : "NO GAMES FOUND YET";
            }
            if (_lanBoxHint != null)
            {
                _lanBoxHint.text = entries.Count > 0
                    ? "Click one to fill in its address, then press JOIN."
                    : "This finds games on your own network. If the host you want is missing - or is on Hamachi - type their address into the field above instead. Windows Firewall blocking the game is the usual reason a LAN game does not show up.";
            }

            _lanAddresses.Clear();
            for (int i = 0; i < _lanRows.Count; i++)
            {
                if (i < entries.Count)
                {
                    var entry = entries[i];
                    string addr = $"{entry.Address}:{entry.Port}";
                    _lanAddresses.Add(addr);

                    var rowTxt = _lanRows[i].GetComponentInChildren<Text>();
                    if (rowTxt != null)
                    {
                        string status = entry.InProgress ? "IN A MATCH" : "IN THE LOBBY";
                        rowTxt.text = $"{entry.HostName}   ·   {entry.Players}/{entry.MaxPlayers}   ·   {status}\n{addr}";
                        rowTxt.alignment = TextAnchor.MiddleLeft;
                        rowTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                        rowTxt.verticalOverflow = VerticalWrapMode.Overflow;
                        rowTxt.rectTransform.anchorMin = Vector2.zero;
                        rowTxt.rectTransform.anchorMax = Vector2.one;
                        rowTxt.rectTransform.offsetMin = new Vector2(20, 0);
                        rowTxt.rectTransform.offsetMax = new Vector2(-20, 0);
                    }
                    _lanRows[i].gameObject.SetActive(true);
                }
                else
                {
                    _lanRows[i].gameObject.SetActive(false);
                }
            }
        }

        private void RefreshOnlineBrowser()
        {
            var servers = _net?.Query?.Servers?.ToList() ?? new List<ServerQuery.Entry>();
            if (_onlineBrowseButton != null)
            {
                var txt = _onlineBrowseButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = servers.Count == 0
                        ? "ONLINE SERVERS  ·  no answer"
                        : $"ONLINE SERVERS  ·  {servers.Count} found";
                }
            }

            if (_onlineBoxTitle != null)
            {
                _onlineBoxTitle.text = servers.Count > 0 ? "ONLINE SERVERS" : "NO ONLINE SERVERS ANSWERED";
            }
            if (_onlineBoxHint != null)
            {
                _onlineBoxHint.text = servers.Count > 0
                    ? "Click one to fill in its address, then press JOIN. Once you are in a lobby it shows a four-character code: read that out and a friend can type it here instead of an address to land in the same game."
                    : "Nothing in the online pool replied. It may be down, or your network may be blocking it. HOST GAME (LAN) and typing a host's address both still work - this screen keeps asking in the background.";
            }

            _onlineCodesOrAddrs.Clear();
            for (int i = 0; i < _onlineRows.Count; i++)
            {
                if (i < servers.Count)
                {
                    var entry = servers[i];
                    string code = !string.IsNullOrEmpty(entry.JoinCode) ? entry.JoinCode : entry.Name;
                    _onlineCodesOrAddrs.Add(code);

                    var rowTxt = _onlineRows[i].GetComponentInChildren<Text>();
                    if (rowTxt != null)
                    {
                        string status = entry.InProgress ? "IN A MATCH" : "IN THE LOBBY";
                        string mapName = !string.IsNullOrEmpty(entry.Name) ? entry.Name.ToUpperInvariant() : "ESKINITA";
                        rowTxt.text = $"SERVER {i + 1}   ·   {entry.Players}/{entry.Capacity}   ·   {mapName}   ·   {status}";
                        rowTxt.alignment = TextAnchor.MiddleLeft;
                        rowTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                        rowTxt.verticalOverflow = VerticalWrapMode.Overflow;
                        rowTxt.rectTransform.anchorMin = Vector2.zero;
                        rowTxt.rectTransform.anchorMax = Vector2.one;
                        rowTxt.rectTransform.offsetMin = new Vector2(20, 0);
                        rowTxt.rectTransform.offsetMax = new Vector2(-20, 0);
                    }
                    _onlineRows[i].gameObject.SetActive(true);
                }
                else
                {
                    _onlineRows[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnLanRowClicked(int index)
        {
            if (index < 0 || index >= _lanAddresses.Count) return;
            MenuSfx.Click();
            if (_address != null) _address.text = _lanAddresses[index];
            SetStatus($"Picked {_lanAddresses[index]} - press JOIN.");
            if (_lanBox != null) _lanBox.SetActive(false);
        }

        private void OnOnlineRowClicked(int index)
        {
            if (index < 0 || index >= _onlineCodesOrAddrs.Count) return;
            MenuSfx.Click();
            if (_address != null) _address.text = _onlineCodesOrAddrs[index];
            SetStatus($"Picked code {_onlineCodesOrAddrs[index]} - press JOIN.");
            if (_onlineBox != null) _onlineBox.SetActive(false);
        }

        private void BindAddressField()
        {
            var t = Node("JoinAddressEdit");
            if (t == null) return;

            _address = t.GetComponent<InputField>();
            if (_address == null) return;

            if (string.IsNullOrWhiteSpace(_address.text)) _address.text = "";
            _address.onSubmit.RemoveAllListeners();
            _address.onSubmit.AddListener(_ => Join());
        }

        private async void Join()
        {
            string addr = _address == null || string.IsNullOrWhiteSpace(_address.text)
                ? ""
                : _address.text.Trim();

            if (string.IsNullOrEmpty(addr))
            {
                SetStatus("Enter a 4-character join code or IP address.");
                return;
            }

            // Direct IP or hostname
            if (addr.Contains(".") || addr.Contains(":") || addr.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus($"Connecting to {addr}...");
                bool ok = await _net.StartClientAsync(addr);
                if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
                else SetStatus(Reason($"Could not reach {addr}."));
                return;
            }

            // 4-character join code lookup
            if (addr.Length >= LobbySession.JoinCodeLength)
            {
                string code = addr.ToUpperInvariant();
                SetStatus($"Resolving join code {code}...");

                var resolved = await _net.Query.ResolveCodeAsync(code);
                if (resolved.HasValue && resolved.Value.Found)
                {
                    var match = resolved.Value;
                    if (match.IsLan)
                    {
                        SetStatus($"Joining LAN host {match.HostName} ({match.Address}:{match.Port})...");
                        _net.Lobby.SetJoinCode(code);
                        bool ok = await _net.StartClientAsync(match.Address, match.Port);
                        if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
                        else SetStatus(Reason($"Failed to connect to LAN host at {match.Address}:{match.Port}."));
                    }
                    else
                    {
                        SetStatus($"Joining online host {match.HostName} via Relay...");
                        _net.Lobby.SetJoinCode(code);
                        bool ok = await _net.StartRelayClient(match.RelayCode);
                        if (ok) SceneFlow.Go(SceneFlow.MatchSetup);
                        else SetStatus(Reason("Failed to join online host via Relay."));
                    }
                    return;
                }

                SetStatus($"No active match found for code '{code}'.");
                return;
            }

            SetStatus("Join code must be 4 characters.");
        }

        private void OnDestroy()
        {
            if (_net != null)
            {
                if (_net.Beacon != null) _net.Beacon.EntriesChanged -= RefreshLanBrowser;
                if (_net.Query != null)
                {
                    _net.Query.ServersChanged -= RefreshOnlineBrowser;
                    _net.Query.StopBrowsing();
                }
            }
        }
    }
}

