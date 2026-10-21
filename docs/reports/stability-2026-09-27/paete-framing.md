# Paete framing and HeroShop role tiles

The first QA2 native comparison failed after saving eight images: at 1600x680, Paete was smaller than Sean in both shop and selector. Its [failed XML](checks/qa2-play-paete-sean.xml) and [shop before samples](native/qa2/) remain unchanged. The root-approved UI-only amendment was tested once in the isolated `work/stability-validation` candidate with a graphics-enabled Win64/D3D11 guarded PlayMode run. Fresh [paete-framing1 XML](checks/qa2-paete-framing1.xml) reports **1/1 passed**, 0 failed/skipped, Unity exit 0, 20.889 s. Minimum monitored C: free space was 7,892,680,704 bytes; the named test profile and Editor input preferences were restored.

| 1600x680 | Sean | Cheska | Paete |
| --- | ---: | ---: | ---: |
| Shop silhouette share | 0.705 | 0.695 | **0.729** |
| Selector silhouette share | 0.680 | 0.670 | **0.700** |

| 2340x1080 | Sean | Cheska | Paete |
| --- | ---: | ---: | ---: |
| Shop silhouette share | 0.700 | 0.684 | **0.730** |
| Selector silhouette share | 0.682 | 0.680 | **0.709** |

These are the exact three-decimal values logged by `HubFlowTests.PaeteAndSeanKeepTheirRelativeSizeInShopAndCharacterSelect`. Paete exceeds Sean and differs from Cheska by less than 10% on both views at both shapes. The test checks head/feet crop and four-slot role-caption preferred size, transformed tile/row containment, adjacent gaps, LargerText floor 28, and focused/tilted states. All 16 new screenshots were visually inspected: full ATTACKING and DEFENDING labels remain separate, and no compared subject is cropped.

The only presentation parameters changed are in `HubHero.cs` and `HubCharacterSelect.cs`: four-slot HeroShop tiles use 190x136 within the existing 820-wide row at step 206/left inset 4, with 174x40 captions; the three-slot path is unchanged. Paete alone uses `SetTileFraming(0.86f)` in shop/hero and `0.89f` in selector; every other hero retains `0.92f` and `0.95f`. No `ModelPreview`, model, gait, roster, scene, or gameplay scale was changed by this unit.

Representative [after shop Sean](native/qa2/qa2-paete-shop-sean-after-1600x680.png), [Cheska](native/qa2/qa2-paete-shop-cheska-after-1600x680.png), [Paete](native/qa2/qa2-paete-shop-paete-after-1600x680.png), [tilted](native/qa2/qa2-paete-shop-tilted-after-1600x680.png), and [LargerText](native/qa2/qa2-paete-shop-larger-after-1600x680.png) captures are here with [selector Sean](native/qa2/qa2-paete-selector-sean-after-1600x680.png), [Cheska](native/qa2/qa2-paete-selector-cheska-after-1600x680.png), and [Paete](native/qa2/qa2-paete-selector-paete-after-1600x680.png). [Checksums](qa2-evidence.sha256) cover this small published set. The full 16-image after set, XML, logs and disk receipt are preserved at `work/qa-candidate-20260927/qa2-paete-after/checkpoint.json` (20/20 hashes); the previous three files, eight original images and original failure receipt remain at `qa2-paete-before/checkpoint.json` (16/16 hashes). The isolated candidate manifest records the three before/after source hashes and 52/52 matching files.

This is a targeted native UI result, not a full-suite, player-build, model/animation, or human gameplay verdict.
