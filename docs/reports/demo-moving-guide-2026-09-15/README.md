# Moving direction-guide visibility check

A compressed throw frame made the guide look absent. The source was inspected
before changing it. A focused case now moves Nemu through a held right-spin throw
and compares actual camera pixels with the guide enabled and disabled.

Both runs passed. The final run samples early, middle, late and full charge while
moving. Every sample has an enabled432-vertex guide with37path points. Confidence
is0 and the horizon remains the intended short0.24seconds, so it continues to show
general direction rather than an exact landing marker. No scenery blocker was hit.
At full charge it contributes658 visible pixels at1280x720; earlier samples also
exceed the existing100-pixel visibility threshold. The final on/off images were
inspected. No rendering disappearance was reproduced in this controlled case.

The production guide, accuracy, horizon and flight equations are unchanged. This
does not establish every wall, weapon, camera angle or display condition. It closes
the specific moving Nemu concern raised during the new throw review.

Focused PlayMode results: v1=1/1, v2=1/1, Unity6000.5.8f1. Guard restoration receipts:
3e0af88575e4 and c3d884c2cad7. Exact state/visibility records and NUnit results included.
