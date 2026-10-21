# Restore the owner's original TUMP logo

The owner requested the flat-colour red/lime logo shown in their screenshot.
The exact original transparent artwork remains in
`Resources/UI/owner-menu-edits/login3-logo.png` and is reused byte-for-byte in
both existing brand asset paths. SHA256:
`a8397cc2532652da2129de8aa83722e68883ec20d05a08e9940d2117e37889c2`.

The shared sprite now uses the complete tightly framed original export. Existing
login, loading, credits, settings and picker routes retain their layout and aspect
fitting. The previous temporary PNG's canvas crop is removed. The baked title
illustration and unrelated artwork remain intact.

The existing native PlayMode logo/credits check was updated for this actual
artwork: 1/1 passed on D3D11, PID19156, exit0. It verifies the shared owner/theme
routes, complete texture bounds, original proportions, white tint, preserved
aspect and the full HANS XAVIER LAO credit. Actual credits pixels were inspected.
Quality settings, editor/player input preferences and the isolated profile were
restored. Generated importer changes are retained separately and restored to
exact pre-run hashes. This is native UI acceptance; F489's already frozen peer
package still contains the earlier temporary logo and is not relabelled.
