# Prepare first nameplate font initialization behind loading

A development-player profiler trace attributes2,572ms of first match startup
to Font.CacheFontForText beneath CharacterNameplate.Awake/TextMesh creation.
No equivalent Nemu gameplay-cost conclusion is drawn from its profiler spikes.

The existing boot loading sequence now primes the legacy TextMesh initialization
and common nameplate glyphs at the unchanged96-pixel size. The temporary inactive
primer is destroyed before warmup returns. Font, appearance, world size, hero
mechanics and authored assets are unchanged.

Focused native warmup1/1 verifies real glyph availability at the nameplate size
and primer cleanup. The second focused integration check compiles the SplashScreen
hook too and passes1/1, normal exit/restoration/free lease, no fixture repair.
The retained original profiler samples prove the original loading cost; this
native check does not prove a numeric player hitch reduction. Refreshed player
first-entry measurements remain open. Current protocol1391003h excludes this fix.
