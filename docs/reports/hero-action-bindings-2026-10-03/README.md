# Shipping hero-action binding regression coverage

Test-only change. No runtime, hero mechanics, authored assets or protocol changes.

Closed Circuit exposed a gap: the old whole-roster check accepted any nonempty
body name except three generic names, then checked only the owner dispatcher.
An unregistered `cast` body name could pass. Separately, the old fallback test
required every chain to have two entries even when its own serialized clip ships.
The baseline reproduced that false failure for the new dedicated Circuit clip;
the weak body-name test passed. Baseline1/2, exit2/no resource stop, preserved.

The stronger check is split into one case per current hero. Each live ability
must request a hero-specific registered action, try its own clip first, and have
that exact clip serialized on its own shipping roster. The clip must have duration,
authored curves and binding paths that exist in the actual model hierarchy. Its
owner action must resolve through the real ViewmodelArms dispatcher. Inactive
probe objects avoid building unrelated meshes, and cleanup runs on assertion failure.

Single-entry hero chains now require a real serialized clip. Aspirational names
without shipping assets retain the fallback requirement; blank or empty chains
still fail. No generic fallback was added to hide a missing animation.

Final native EditMode10/10 passes: nine heroes plus chain coverage,0.2619089s.
Unity6000.5.8f1 Linux; exit0/no guard. Peak tree3,316,436,992 and container
7,406,403,584bytes. All168 relevant source/asset inputs match main and validation
before/after. Both settings files and named profile restore.

The private validation mirror updated two existing Amihan inputs from the shipping
checkout (description text and authored storm clip); those production files were
read-only and are not changed by this commit. The initial compile completed and
reloaded before a late-import headroom stop; the first separate runtime succeeded.

This protects registrations, shipping references and curve bindings. It does not
prove every motion looks good, every gameplay branch works, packaged-player or
peer behavior, audio, performance or human acceptance. Those remain separate.
