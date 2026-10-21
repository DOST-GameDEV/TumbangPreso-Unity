# Late history responses respect menu navigation

RefreshMatches always assigned its awaited response and called Show(Matches).
A native baseline using actual Matches, Profile and Close controls reproduced
stale completion after navigation, closing, a newer request and a page change.
The current-response control still worked. One native case checks all five states.

The existing refresh now captures its page and request identity. Completion and
failure-footer updates require the latest request, same page, open hub and Matches
tab. Closing, tab navigation and account identity changes invalidate older work.
Final1/1 passes all five states. Existing account-cache cancellation remains.
No layout, control bindings, service endpoints or authored UI changes.

Unity6000.5.8f1 PlayMode/D3D11, isolated tump-feedback-0930, named
history-navigation1002 profile. Three hashes unchanged, zero fixture repairs.
Guarded profile/shared-input restoration completed; native logs remain in
isolated Logs/history-navigation1002. These checks invoke production completion
against live menus; no real HTTP response, physical input or backend claim.
