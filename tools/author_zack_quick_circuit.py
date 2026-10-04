"""Replace only Quick Circuit's obsolete two-cycle skate with one braced cut."""
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_hero_action import author

# A short gather, release at the .15s gameplay tell, then one recovery.
# A balanced stance works for either lateral direction; the motor owns travel.
# No root X/Z displacement, ghost body or repeated running/skating cycles.
SPEC = {'punch': .15, 'beats': [
    (0.00, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
    (0.08, 1, 0, 0, 10, -6, 0, -5, 6, -5, 7, -5, -7, -22, -10, -18, 10),
    (0.15, 1, 0, 0, 22, 4, 0, -12, -4, 7, 12, -7, -12, 32, -8, -32, 8),
    (0.28, 1, 0, 0, 18, 3, 0, -10, -3, 5, 10, -5, -10, 25, -6, -26, 6),
    (0.46, 1, 0, 0, 7, 1, 0, -4, -1, 2, 4, -2, -4, 9, -2, -10, 2),
    (0.64, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
], 'grounded': (0, .08, .15, .28, .46, .64)}

if __name__ == '__main__':
    path = Path(sys.argv[sys.argv.index('--') + 1])
    print(json.dumps(author(path, 'hero-zack-sprint', SPEC, replace=True), indent=2))
