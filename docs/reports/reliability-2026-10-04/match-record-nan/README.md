# Invalid measurements cannot contaminate career totals

Baseline: cc96738d3590effb7f62babdebd8b17dc51a141f, October 4.

MatchRecordRules.Normalise promises bounded measurements. Its float clamp
compared each value to the lower and upper bound. Both comparisons are false for
NaN, so duration, distance and last-attacker duration remained NaN. Applying the
normalized record through ProfileRules then stored NaN in career time/distance
totals. A bad measurement could therefore contaminate later accumulated totals.

The existing float clamp now maps NaN to its existing lower bound. Finite values,
infinity saturation, counts, placements, unknown first-throw sentinel and all
gameplay balance remain unchanged. No record schema or wire change.

## Focused evidence

The shipping Core source compiled through its existing engine-free project.
The new nine-case fixture first ran against unchanged production source:
four failures and five controls. Three failures demonstrated NaN surviving
normalization; the fourth demonstrated contamination after actual profile
application. Controls retain negative/ordinary/upper-bound and both infinity
behaviors. [Original TRX](original.trx).

The same fixture with the one clamp change passed as part of 68 focused record,
profile and integrity checks: zero failures and skips. [Candidate TRX](candidate.trx).
No tooling retry, Unity Editor, player, service call or profile mutation occurred.

Commands:

```powershell
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --configuration Release --filter 'FullyQualifiedName~NormaliseRetiresNaNMeasurements|FullyQualifiedName~NormalisedNaNMeasurementsCannotPoisonCareerTotals|FullyQualifiedName~NormalisePreservesExistingDistanceBounds' --logger 'trx;LogFileName=original.trx' --results-directory Logs/match-record-nan1004 --nologo
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~MatchRecordTests|FullyQualifiedName~PlayerProfileTests|FullyQualifiedName~IntegrityTests' --logger 'trx;LogFileName=candidate.trx' --results-directory Logs/match-record-nan1004 --nologo
```

Exact local tested source SHA256 before Git line-ending normalization:

- Core.Tests/MatchRecordTests.cs: `fcea29627e8596ac2696d2e204b5991c843e0b0feda156753c254f8dc5306204`.
- Packages/com.tumbangpreso.core/Runtime/MatchRecord.cs candidate:
  `05ad3c7a92072fa0d0677dc5db0dd291c04a9492cb880dcf70105a2e915b4d14`.

This proves supplied Core record/profile behavior. It does not establish which
live gameplay path produced a bad measurement, repair already contaminated
profiles or prove Unity import, packaged playback or real-peer acceptance. The
current Windows release must incorporate this newer Core source before final
competition qualification.
