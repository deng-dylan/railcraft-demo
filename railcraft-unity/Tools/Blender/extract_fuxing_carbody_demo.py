"""Extract one intermediate Fuxing car as an assembly-stage body demo.

The source train is kept intact.  The extractor classifies mesh objects by the
same source-X car boundaries used by FinalShowcaseSceneBuilder, then bakes the
selected car into a small standalone FBX.  The resulting asset is for visual
assembly guidance only and carries no engineering identity.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


SOURCE_SHA256 = "c5b0f5042eb33ed8cbd895ff7b126dadccccef9e999a7b65e93df79bd2cdd35e"
SOURCE_BOUNDARIES_X = (
    -1.02546479584,
    -0.760061795841,
    -0.465022795841,
    -0.169913795841,
    0.125195704159,
    0.420305204159,
    0.715414704159,
    1.01052370416,
    1.27592720416,
)
CAR_INDEX = 1  # car_02, intermediate car
BOUNDARY_TOLERANCE = 0.0001
TARGET_TRAIN_LENGTH_M = 200.0
SOURCE_TRAIN_LENGTH_M = SOURCE_BOUNDARIES_X[-1] - SOURCE_BOUNDARIES_X[0]
NORMALIZATION_SCALE = TARGET_TRAIN_LENGTH_M / SOURCE_TRAIN_LENGTH_M


def parse_arguments() -> argparse.Namespace:
    argv = sys.argv
    script_args = argv[argv.index("--") + 1 :] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--manifest")
    return parser.parse_args(script_args)


def find_descendant(root: bpy.types.Object, name: str) -> bpy.types.Object | None:
    if root.name == name:
        return root
    for child in root.children:
        match = find_descendant(child, name)
        if match is not None:
            return match
    return None


def world_bounds(obj: bpy.types.Object) -> tuple[Vector, Vector]:
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = Vector((min(point[index] for point in points) for index in range(3)))
    maximum = Vector((max(point[index] for point in points) for index in range(3)))
    return minimum, maximum


def object_center_x(obj: bpy.types.Object) -> float:
    minimum, maximum = world_bounds(obj)
    return (minimum.x + maximum.x) * 0.5


def select_car_meshes(root: bpy.types.Object) -> list[bpy.types.Object]:
    lower = SOURCE_BOUNDARIES_X[CAR_INDEX]
    upper = SOURCE_BOUNDARIES_X[CAR_INDEX + 1]
    meshes = [obj for obj in root.children_recursive if obj.type == "MESH"]
    selected = [
        obj
        for obj in meshes
        if object_center_x(obj) > lower + BOUNDARY_TOLERANCE
        and object_center_x(obj) <= upper + BOUNDARY_TOLERANCE
    ]
    return selected


def duplicate_and_normalize(source_objects: list[bpy.types.Object]) -> list[bpy.types.Object]:
    # Blender source: X=longitudinal, Y=up, Z=lateral.  The normalized demo
    # uses X=lateral, Y=up, Z=longitudinal and scales to the showcase metres.
    axis_conversion = Matrix(
        (
            (0.0, 0.0, NORMALIZATION_SCALE, 0.0),
            (0.0, NORMALIZATION_SCALE, 0.0, 0.0),
            (-NORMALIZATION_SCALE, 0.0, 0.0, 0.0),
            (0.0, 0.0, 0.0, 1.0),
        )
    )
    duplicates = []
    for index, source in enumerate(source_objects, start=1):
        mesh = source.data.copy()
        mesh.name = f"FuxingCarbodyMesh_{index:02d}_Mesh"
        mesh.transform(source.matrix_world)
        mesh.transform(axis_conversion)
        duplicate = bpy.data.objects.new(f"CarbodyMesh_{index:02d}", mesh)
        bpy.context.scene.collection.objects.link(duplicate)
        duplicates.append(duplicate)

    points = [
        duplicate.matrix_world @ vertex.co
        for duplicate in duplicates
        for vertex in duplicate.data.vertices
    ]
    minimum = Vector((min(point[index] for point in points) for index in range(3)))
    maximum = Vector((max(point[index] for point in points) for index in range(3)))
    offset = Vector(
        (
            (minimum.x + maximum.x) * 0.5,
            minimum.y,
            (minimum.z + maximum.z) * 0.5,
        )
    )
    for duplicate in duplicates:
        duplicate.data.transform(Matrix.Translation(-offset))
        duplicate.data.update()
    return duplicates


def main() -> None:
    args = parse_arguments()
    source_path = Path(args.source).expanduser().resolve()
    output_path = Path(args.output).expanduser().resolve()
    manifest_path = (
        Path(args.manifest).expanduser().resolve()
        if args.manifest
        else output_path.with_suffix(".manifest.json")
    )
    if hashlib.sha256(source_path.read_bytes()).hexdigest() != SOURCE_SHA256:
        raise RuntimeError("Fuxing source hash changed; inspect the source asset before extraction")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source_path), use_custom_normals=True)

    preferred = find_descendant(next(iter(bpy.context.scene.objects)), "空白_2")
    if preferred is None:
        preferred = next(
            (obj for obj in bpy.context.scene.objects if obj.name.startswith("空白_2")),
            None,
        )
    if preferred is None:
        raise RuntimeError("Could not find the inspected Fuxing subtree 空白_2")

    selected = select_car_meshes(preferred)
    if len(selected) != 10:
        raise RuntimeError(
            f"Expected 10 meshes for car_02, found {len(selected)}: "
            f"{[obj.name for obj in selected]}"
        )

    duplicates = duplicate_and_normalize(selected)
    root = bpy.data.objects.new("FuxingCarbodyAssemblyDemoRoot", None)
    bpy.context.scene.collection.objects.link(root)
    root["asset_role"] = "assembly_demonstration_carbody"
    root["engineering_identity"] = "reference_only"
    root["source_car_id"] = "car_02"
    root["source_sha256"] = SOURCE_SHA256
    for duplicate in duplicates:
        duplicate.parent = root

    rail_anchor = bpy.data.objects.new("RailContactPlane", None)
    bpy.context.scene.collection.objects.link(rail_anchor)
    rail_anchor.parent = root
    rail_anchor.empty_display_size = 0.25
    rail_anchor.empty_display_type = "PLAIN_AXES"
    mount_anchor = bpy.data.objects.new("VehicleMount", None)
    bpy.context.scene.collection.objects.link(mount_anchor)
    mount_anchor.parent = root
    mount_anchor.location = (0.0, 3.45, 0.0)
    mount_anchor.empty_display_size = 0.25
    mount_anchor.empty_display_type = "PLAIN_AXES"

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for duplicate in duplicates:
        duplicate.select_set(True)
    rail_anchor.select_set(True)
    mount_anchor.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        check_existing=False,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        use_space_transform=True,
        bake_space_transform=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_triangles=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=True,
    )

    minimum, maximum = world_bounds(duplicates[0])
    for duplicate in duplicates[1:]:
        item_min, item_max = world_bounds(duplicate)
        minimum.x = min(minimum.x, item_min.x)
        minimum.y = min(minimum.y, item_min.y)
        minimum.z = min(minimum.z, item_min.z)
        maximum.x = max(maximum.x, item_max.x)
        maximum.y = max(maximum.y, item_max.y)
        maximum.z = max(maximum.z, item_max.z)

    triangles = 0
    for duplicate in duplicates:
        duplicate.data.calc_loop_triangles()
        triangles += len(duplicate.data.loop_triangles)
    manifest = {
        "schema": 1,
        "source_sha256": SOURCE_SHA256,
        "source_car_id": "car_02",
        "blender_version": bpy.app.version_string,
        "mesh_object_count": len(duplicates),
        "triangles": triangles,
        "bounds_m": {
            "minimum": [round(value, 6) for value in minimum],
            "maximum": [round(value, 6) for value in maximum],
        },
        "output": output_path.name,
        "output_sha256": hashlib.sha256(output_path.read_bytes()).hexdigest(),
    }
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("FUXING_CARBODY_DEMO_EXPORT_SUCCEEDED")
    print(json.dumps(manifest, ensure_ascii=False))


if __name__ == "__main__":
    main()
