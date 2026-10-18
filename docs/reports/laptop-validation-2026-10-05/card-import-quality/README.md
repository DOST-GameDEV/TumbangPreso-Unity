# UI card import quality: desktop candidate qualification

Actual reimport changed Arena's authored960x540 card to1024x512 DXT1 through
nearest-power-of-two scaling and compression. The exact desktop importer extension
preserves960x540 RGB24/uncompressed/NPOTNone. All three source image hashes are
unchanged; no pixel resampling, replacement or world texture downgrade was made.

The HeroStrikeChoice source already imported at1040x920 RGB24/uncompressed/None.
Its failure was ignoreMipmapLimit=False, not an observed current-size or compression
defect. The candidate protects that UI art from world mip limits. The existing
2048x1349 RGBA32 brand control passes before and after.

## Exact native comparison

Base8c48b58f0, laptop gamergmae/Windows11, Unity6000.5.8f1, isolated qa-a/named
profile, EditMode three actual AssetDatabase reimports. Original Unity29940/parent3103
ends2 with two card failures/one brand control pass. Exact candidate Unity43816/
parent62787 ends0,3/3PASS, no skips. Dimension, format, compression and NPOT log
lines are retained in the observation files.

Original editor normalized SHA d8a10ff1d2228541a90a811c3ded1acb4582585fa85a412c3cd44e74b31c3143;
candidatecd4a2e6750e4b0ccc6897f0a78b68ef797058731d16dc1e73516f1f3ac8b4220.
The existing importer covers map-cards and mode-cards in addition to its current
menu/painted/backdrop scope; version4 causes proper reapplication on reimport.
World assets and source art remain unchanged. Desktop owns editor/fixture/source
publication; laptop main importer remains original.

The supplied fixture initially used unsupported NUnit Assert.Multiple. Before
this unit executed, its wrapper was replaced with the same sequential assertions
per desktop correction; exact qualified fixture SHA
79e7959bc266e31dd6eea6b6cd65fa53de9eb36c407b60afcb0fb437208c055a,
metadata353764fcd5ffb03a673b8d250566e735e1e57367a29e771fbcbae6227bce3a6a.
No assertions were weakened or cases skipped. Exact qualified fixture text and
source/art hashes are retained for adoption.

## Preservation and limits

Both parents are terminal,21124inputs protected. Original265 and candidate279
generated metadata/Auditor changes were preserved/restored, with Quality and13
existing preferences. All original/candidate source art hashes match. This is
native import/quality-policy evidence, not a current player build, visual taste,
all image acceptance, network recovery or full tournament readiness. The coherent
protocol152 package must include any subsequently published source/import changes;
old61a player results do not qualify those changed imports.

Baseline HeroStrikeChoice metadata has enableMipMap0. Its missing mip-limit
exemption is future resilience/policy, not demonstrated current GPU shrink or blur.
