"""Compatibility entry point for the copied native voxel builder.

The rejected separate Geometry.bevel recipe is archived under ArtSource/rafi.
Only build_rafi_voxel.py now owns Rafi's authored recipe and native mesh pipeline.
"""
from build_rafi_voxel import main

if __name__ == '__main__':
    main()
