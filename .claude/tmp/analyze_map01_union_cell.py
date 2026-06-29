import json
from collections import deque

with open("map01.json", encoding="utf-8") as f:
    m = json.load(f)

walkable = {}
for ff in m.get("floorFaces", []):
    ax, ay, az = ff["x"], ff["y"], ff["z"]
    cy = ay + 1
    walkable[(ax, cy, az)] = ff.get("prefabId", "?")

structural = {}
for t in m.get("tiles", []):
    structural[(t["x"], t["y"], t["z"])] = t.get("prefabId", "?")

cell = (3, 0, 3)
print("=== Occupied cell (3,0,3) ===")
print("walkable floor:", walkable.get(cell))
print("structural box:", structural.get(cell))
for ff in m.get("floorFaces", []):
    ax, ay, az = ff["x"], ff["y"], ff["z"]
    if (ax, ay + 1, az) == cell:
        print("floor face anchor", (ax, ay, az), ff.get("prefabId"))

print("=== row y=0 z=3 x=1..5 ===")
for x in range(1, 6):
    print(
        f"  x={x} structural={structural.get((x, 0, 3), '-')} "
        f"floor={walkable.get((x, 0, 3), '-')}"
    )

minCellY = 0
cells_xz = {(x, z) for (x, y, z) in walkable if y == minCellY}
seed = min(cells_xz)
print("outdoor seed (min xz):", seed)
dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)]
q = deque([seed])
outdoor = {seed}
while q:
    x, z = q.popleft()
    for dx, dz in dirs:
        nx, nz = x + dx, z + dz
        if (nx, nz) not in cells_xz or (nx, nz) in outdoor:
            continue
        outdoor.add((nx, nz))
        q.append((nx, nz))
print("outdoor count:", len(outdoor), "contains (3,3):", (3, 3) in outdoor)

processed = set()
footprints = []
set_id = 0
for sx, sz in sorted((x, z) for (x, y, z) in walkable if y == minCellY and (x, z) not in outdoor):
    if (sx, sz) in processed:
        continue
    set_id += 1
    q = deque([(sx, sz)])
    fp = set()
    while q:
        x, z = q.popleft()
        if (x, z) in fp:
            continue
        if (x, z) in outdoor:
            continue
        if (x, minCellY, z) not in walkable:
            continue
        fp.add((x, z))
        for dx, dz in dirs:
            q.append((x + dx, z + dz))
    processed |= fp
    footprints.append((set_id, seed := (sx, sz), fp))

print("init footprints (simplified BFS, no wall):", len(footprints))
for sid, seed, fp in footprints:
    if sid in (1, 20) or (3, 3) in fp:
        print(f"  setId={sid} seed={seed} size={len(fp)} has(3,3)={(3,3) in fp}")
