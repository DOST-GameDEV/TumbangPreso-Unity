#!/usr/bin/env python3
"""Find disconnected mesh islands that do not join a model's grounded structure.

Renderer bounds can prove that a whole prop rests on something while missing a loose part
inside that same renderer. This checker walks triangle connectivity, builds an AABB for every
island, and floods contact from the lowest islands. It supports the OBJ and binary glTF files
used by the map kit without third-party geometry packages.
"""

from __future__ import annotations

import argparse
import json
import math
import struct
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


COMPONENT_FORMAT = {
    5121: "B",
    5123: "H",
    5125: "I",
    5126: "f",
}


@dataclass(frozen=True)
class Island:
    vertices: int
    minimum: tuple[float, float, float]
    maximum: tuple[float, float, float]


class UnionFind:
    def __init__(self, count: int) -> None:
        self.parent = list(range(count))

    def find(self, value: int) -> int:
        while self.parent[value] != value:
            self.parent[value] = self.parent[self.parent[value]]
            value = self.parent[value]
        return value

    def union(self, left: int, right: int) -> None:
        left_root = self.find(left)
        right_root = self.find(right)
        if left_root != right_root:
            self.parent[right_root] = left_root


def islands(vertices: list[tuple[float, float, float]], triangles: Iterable[tuple[int, int, int]]) -> list[Island]:
    connected = UnionFind(len(vertices))
    used: set[int] = set()

    for a, b, c in triangles:
        connected.union(a, b)
        connected.union(b, c)
        used.update((a, b, c))

    groups: dict[int, list[int]] = {}
    for index in used:
        groups.setdefault(connected.find(index), []).append(index)

    result = []
    for indices in groups.values():
        points = [vertices[index] for index in indices]
        result.append(Island(
            len(indices),
            tuple(min(point[axis] for point in points) for axis in range(3)),
            tuple(max(point[axis] for point in points) for axis in range(3)),
        ))
    return result


def load_obj(path: Path) -> list[Island]:
    vertices: list[tuple[float, float, float]] = []
    triangles: list[tuple[int, int, int]] = []

    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("v "):
            _, x, y, z = line.split()[:4]
            vertices.append((float(x), float(y), float(z)))
        elif line.startswith("f "):
            face = [int(token.split("/")[0]) - 1 for token in line.split()[1:]]
            for index in range(1, len(face) - 1):
                triangles.append((face[0], face[index], face[index + 1]))

    return islands(vertices, triangles)


def glb_chunks(path: Path) -> tuple[dict, bytes]:
    raw = path.read_bytes()
    magic, version, length = struct.unpack_from("<III", raw, 0)
    if magic != 0x46546C67 or version != 2 or length != len(raw):
        raise ValueError(f"{path}: not a valid glTF 2 binary")

    offset = 12
    document = None
    binary = b""
    while offset < len(raw):
        chunk_length, chunk_type = struct.unpack_from("<II", raw, offset)
        chunk = raw[offset + 8:offset + 8 + chunk_length]
        if chunk_type == 0x4E4F534A:
            document = json.loads(chunk.rstrip(b"\x00 \t\r\n"))
        elif chunk_type == 0x004E4942:
            binary = chunk
        offset += 8 + chunk_length

    if document is None:
        raise ValueError(f"{path}: missing JSON chunk")
    return document, binary


def accessor_values(document: dict, binary: bytes, accessor_index: int) -> list[tuple | int]:
    accessor = document["accessors"][accessor_index]
    view = document["bufferViews"][accessor["bufferView"]]
    component_type = accessor["componentType"]
    fmt = COMPONENT_FORMAT[component_type]
    count_by_type = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}
    width = count_by_type[accessor["type"]]
    component_size = struct.calcsize(fmt)
    stride = view.get("byteStride", component_size * width)
    start = view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
    unpack = struct.Struct("<" + fmt * width)
    values = []

    for index in range(accessor["count"]):
        value = unpack.unpack_from(binary, start + index * stride)
        values.append(value[0] if width == 1 else value)
    return values


def load_glb(path: Path) -> list[Island]:
    document, binary = glb_chunks(path)
    result: list[Island] = []

    for mesh in document.get("meshes", []):
        for primitive in mesh.get("primitives", []):
            vertices = [tuple(float(axis) for axis in value)
                        for value in accessor_values(document, binary, primitive["attributes"]["POSITION"])]
            if "indices" in primitive:
                indices = [int(value) for value in accessor_values(document, binary, primitive["indices"])]
            else:
                indices = list(range(len(vertices)))
            triangles = [tuple(indices[index:index + 3]) for index in range(0, len(indices) - 2, 3)]
            result.extend(islands(vertices, triangles))
    return result


def aabb_gap(left: Island, right: Island) -> float:
    squared = 0.0
    for axis in range(3):
        separation = max(left.minimum[axis] - right.maximum[axis],
                         right.minimum[axis] - left.maximum[axis], 0.0)
        squared += separation * separation
    return math.sqrt(squared)


def inspect(path: Path, tolerance: float) -> int:
    found = load_obj(path) if path.suffix.lower() == ".obj" else load_glb(path)
    if not found:
        print(f"FAIL {path}: no triangle islands")
        return 1

    floor = min(island.minimum[1] for island in found)
    supported = {index for index, island in enumerate(found)
                 if island.minimum[1] <= floor + tolerance}
    changed = True
    while changed:
        changed = False
        for index, island in enumerate(found):
            if index in supported:
                continue
            if any(aabb_gap(island, found[other]) <= tolerance for other in supported):
                supported.add(index)
                changed = True

    orphans = [index for index in range(len(found)) if index not in supported]
    print(f"{path}: {len(found)} islands, floor {floor:.4f}, {len(orphans)} unsupported")
    for index in orphans:
        island = found[index]
        nearest = min(aabb_gap(island, other) for other in found if other is not island)
        print(f"  island {index}: {island.vertices} vertices, "
              f"y {island.minimum[1]:.4f}..{island.maximum[1]:.4f}, nearest gap {nearest:.4f}")
    return 1 if orphans else 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="+", type=Path)
    parser.add_argument("--tolerance", type=float, default=0.031,
                        help="maximum AABB join gap in model units")
    args = parser.parse_args()
    return max(inspect(path, args.tolerance) for path in args.paths)


if __name__ == "__main__":
    raise SystemExit(main())
