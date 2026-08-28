"""Restore Nemu character assets and scripts to previous iteration or baseline.

Usage:
    python tools/restore_nemu.py             # Restores to pre-refinement snapshot
    python tools/restore_nemu.py --baseline  # Restores from build_nemu_voxel_baseline.py
"""
import os
import shutil
import subprocess
import sys

SNAPSHOT_DIR = os.path.join("tools", "nemu_snapshots", "pre_refinement")
FILES = [
    ("build_nemu_voxel.py", os.path.join("tools", "build_nemu_voxel.py")),
    ("team-nemu.glb", os.path.join("Assets", "TumbangPreso", "Art", "characters", "persons", "team-nemu.glb")),
    ("pet-nemu-ghost.glb", os.path.join("Assets", "TumbangPreso", "Art", "characters", "pets", "pet-nemu-ghost.glb")),
    ("build_ghost_pet_voxel.py", os.path.join("tools", "build_ghost_pet_voxel.py")),
    ("person_team-nemu.tres", os.path.join("MapSource", "materials_persons", "person_team-nemu.tres")),
    ("person_nemu.asset", os.path.join("Assets", "TumbangPreso", "Resources", "Roster", "person_nemu.asset")),
]


def restore_snapshot():
    if not os.path.isdir(SNAPSHOT_DIR):
        print(f"Error: Snapshot directory '{SNAPSHOT_DIR}' not found.")
        sys.exit(1)

    print("Restoring Nemu to pre-refinement snapshot...")
    for src_name, dst_path in FILES:
        src_path = os.path.join(SNAPSHOT_DIR, src_name)
        if os.path.exists(src_path):
            os.makedirs(os.path.dirname(dst_path), exist_ok=True)
            shutil.copy2(src_path, dst_path)
            print(f"  Restored: {dst_path}")
        else:
            print(f"  Warning: {src_path} not found in snapshot.")

    print("\nPre-refinement iteration restored successfully!")
    print("Run `python tools/build_nemu_voxel.py` to re-verify if needed.")


def restore_baseline():
    baseline_script = os.path.join("tools", "build_nemu_voxel_baseline.py")
    target_script = os.path.join("tools", "build_nemu_voxel.py")
    if not os.path.exists(baseline_script):
        print(f"Error: Baseline script '{baseline_script}' not found.")
        sys.exit(1)

    print("Restoring Nemu to initial baseline script...")
    shutil.copy2(baseline_script, target_script)
    print(f"  Copied {baseline_script} -> {target_script}")
    print("  Rebuilding team-nemu.glb from baseline...")
    subprocess.check_call([sys.executable, target_script])
    print("\nBaseline restored and rebuilt successfully!")


def main():
    if len(sys.argv) > 1 and sys.argv[1] in ("--baseline", "-b", "baseline"):
        restore_baseline()
    else:
        restore_snapshot()


if __name__ == "__main__":
    main()
