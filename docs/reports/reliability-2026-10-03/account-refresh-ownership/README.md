# Canonical online profile refresh ownership

A name or Cloud Save answer from an earlier account refresh could overwrite a
replacement profile. The same operation reread the mutable SDK PlayerId after
awaits, so an old reply could acquire another account's discriminator or identity.
Stale name failures also continued into a cloud load for the newly signed-in user.

The refresh now captures SDK identity once and checks the current service owner,
latest refresh request and active profile reference after each await and catch.
Guest responses belong to their retained primary profile. The exact local boot
fallback snapshot remains eligible for a legitimate late response; signing into
a different authenticated ID still works. Name generation, derived tag and cloud
identity use the captured owner. Reentrant account changes cannot trigger the
initial cloud save for an obsolete owner.

Three private dispatch seams let native checks pause these boundaries without
live authentication, cloud requests or destructive account operations. Existing
name-update and profile-save seams are reused. The seam-only original preserves
the old mutable identity reads and behavior; its changes are dispatch routing only.

Original source5e74518c6 reproduced nine causal failures and passed four controls.
The first candidate passed all13 with the same fixture and assertions. Controls
cover ordinary canonical adoption, primary guest return, boot timeout recovery
and adoption of a legitimately different authenticated ID. Independent static
review passed. Both guards terminated, restored profiles/preferences and released
their leases. Actual service timing and real online peers remain separate gates.
