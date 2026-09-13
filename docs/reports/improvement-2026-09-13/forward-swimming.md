# Forward swimming and idle sculling

The owner clarified that horizontal swimming should occur only when choosing to
move forward, referencing Minecraft and a breaststroke photograph. Idle floating
needs another motion: gentle hand sculling, small alternating kicks and bobbing.
Reference bytes and original path/hash are preserved under
ArtSource/maps/owner-swimming-2026-09-13.

SwimmingMotion now selects the forward stroke using observed planar movement
relative to body facing. Strafing/backward movement remains upright. This same
criterion drives body and first-person arms, including replicated bodies, without
introducing controls or changing the wire protocol. Diagonal forward movement
qualifies; a small stopping glide blends back into treading.

The forward cycle has a glide, simultaneous outward arm sweep, inward pull and
recovery, with a grouped outward/closing leg kick. The torso pitches76degrees,
the head counter-rotates and a modest visual lift keeps the face near the water.
Only the model moves; buoyancy/collision/owner eye remain unchanged. Palette,
body parts, simple hands and stable animation GUIDs are retained. The original
grip is preserved while the actual measured palm follows the new arm path.

Treading uses opposed sculling hands, gentle alternating leg motion and15mm
visual bob. The held hand steadies the slipper; forward strokes involve both
arms. FPP preserves the same phase and transitions instead of an unrelated
alternating crawl. Local water crests were strengthened slightly for contact.

## Focused evidence

- SwimmingAnimationAuthorv2 baked80clips over20rigs. V1 compile error in a local
  vector declaration was corrected before any clips were accepted.
- Logs/swim-breaststroke-motion-v1.xml passed1/1, exercising idle, forward,
  stop, strafe, backward and return to idle in both modes. Classic carries its
  slipper, Hero Strike swims empty-handed. All directional-state assertions and
  above-water owner camera assertions passed.
- Actual body/FPP recordings under the matching folder preserve timestamped
  ordinary-speed playback. Reviewed idle and separate forward glide/kick frames:
  the torso now lies along the surface during forward travel and feet extend
  behind, while idle stays upright. The chibi rig expresses a grouped kick
  without adding realistic knees or changing the approved anatomy.
- The prior all18person water/recoveryv3 fixture covers binding/recovery before
  this stroke revision. Do not mislabel that earlier art capture as the new stroke.
- Native UI picker/font/icon fixturev2 passed2/2 during the same source batch;
  the UI agent continues visual corrections and remaining new-native screens.

The opt-in NetRoofProbe and tools/net_roof_matrix.py now have a separate swim
scenario, requiring actual host/owner/observer wet motion, floating stock,
pickup, dry step exit and optional observer rejoin. That scenario has not yet
been built/run at this checkpoint. The existing fall scenario remains intact.
Fresh internal Windows validation is next; the Desktop player stays unchanged.
