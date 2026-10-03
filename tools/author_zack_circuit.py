"""Append one restrained acquisition gesture; preserve Zack's model and old clips."""
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_hero_action import author

# Bladed preparation, free-hand direction, settled acquisition, quiet recovery.
# This is not a success animation: authority may cancel the lock at any instant.
SPEC = {'punch': .18, 'beats': [
    (0.00, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
    (0.08, 1, 0, 0, -2, 12, 2, 0, -12, 0, 0, 0, 0, -24, -15, -5, 4),
    (0.18, 1, 0, 0, 3, -8, -2, -2, 8, 0, 0, 0, 0, -78, -12, -8, 4),
    (0.36, 1, 0, 0, 2, -6, -1, -1, 6, 0, 0, 0, 0, -76, -10, -6, 3),
    (0.48, 1, 0, 0, 1, -3, 0, 0, 3, 0, 0, 0, 0, -35, -5, -3, 1),
    (0.64, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
], 'grounded': (0, .08, .18, .36, .48, .64)}

if __name__ == '__main__':
    path = Path(sys.argv[sys.argv.index('--') + 1])
    print(json.dumps(author(path, 'hero-zack-circuit', SPEC), indent=2))
