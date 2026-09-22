# Rafi FPP mismatch survived the first routing patch

Native v50 routes passed their existing skill input checks, but actual owner frames
still show generic orange hands/black wrist patches. These are NOT passing FPP
appearance results. NormalizeCharacterId had no Rafi branch and returned classic,
so the newly added UseRosterArms branch was unreachable. Current source now keeps
Rafi's ID, and the native route checks the actual displayed left/right mesh assets.
The previous art capture also attempted a throw from inside the legal boundary;
current Rafi attacker staging is outside7m and asserts both charging and release.
No gameplay rule was weakened. Existing failed appearance images remain in Logs.
