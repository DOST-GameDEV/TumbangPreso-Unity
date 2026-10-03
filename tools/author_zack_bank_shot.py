"""Append Bank Shot's held-shoe load without changing Zack's model or old actions."""
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_hero_action import author

# Offer the held shoe, trace across it with the free hand, then settle the grip.
# Cosmetic only: loading is immediate and an early throw may interrupt this motion.
SPEC = {'punch': .28, 'beats': [
    (0.00, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
    (0.08, 1, 0, 0, 2, -10, -2, 8, 10, 0, 0, 0, 0, -18, -12, -22, 8),
    (0.18, 1, 0, 0, 4, 6, 1, 12, -4, 0, 0, 0, 0, -68, -38, -48, 14),
    (0.28, 1, 0, 0, 3, 4, 1, 8, -2, 0, 0, 0, 0, -58, -50, -50, 10),
    (0.42, 1, 0, 0, 1, 2, 0, 3, 0, 0, 0, 0, 0, -22, -18, -24, 4),
    (0.64, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
], 'grounded': (0, .08, .18, .28, .42, .64)}

if __name__ == '__main__':
    path = Path(sys.argv[sys.argv.index('--') + 1])
    print(json.dumps(author(path, 'hero-zack-bankshot', SPEC), indent=2))
