"""Prepare the inspected CW-200K passenger-bogie STEP conversion for Unity.

The STEP file is converted to a hierarchy-preserving GLB with OpenCascade
before this script runs.  This Blender stage removes tiny fasteners, reduces
CAD tessellation, assigns restrained PBR-style materials, recentres the model
at rail level, and exports a Unity-ready FBX.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


SOURCE_STEP_SHA256 = "36f1413e2d04be22d234ba74832a54047a12f6f505202b7e9d55b883be74a3f1"

HARDWARE_NAME_PARTS = (
    "washer",
    "nuts",
    "screws",
    "retaining rings",
    "kahuang",
)


def parse_arguments() -> argparse.Namespace:
    argv = sys.argv
    script_args = argv[argv.index("--") + 1 :] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-glb", required=True)
    parser.add_argument("--source-step", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--manifest")
    return parser.parse_args(script_args)


def world_bounds(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        if obj.type == "MESH"
        for corner in obj.bound_box
    ]
    if not points:
        raise RuntimeError("Cannot calculate bounds for an empty mesh list")
    minimum = Vector(min(point[index] for point in points) for index in range(3))
    maximum = Vector(max(point[index] for point in points) for index in range(3))
    return minimum, maximum


def triangles(objects: list[bpy.types.Object]) -> int:
    total = 0
    for obj in objects:
        obj.data.calc_loop_triangles()
        total += len(obj.data.loop_triangles)
    return total


def create_material(
    name: str,
    color: tuple[float, float, float, float],
    metallic: float,
    roughness: float,
) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Metallic"].default_value = metallic
        principled.inputs["Roughness"].default_value = roughness
    return material


def hierarchy_name(obj: bpy.types.Object) -> str:
    names = []
    current = obj
    while current is not None:
        names.append(current.name.lower())
        current = current.parent
    return " ".join(names)


def select_material(
    obj: bpy.types.Object,
    materials: dict[str, bpy.types.Material],
) -> bpy.types.Material:
    name = hierarchy_name(obj)
    if "chelun" in name or "wheel" in name:
        return materials["wheel"]
    if "tanhuang" in name or "spring" in name:
        return materials["spring"]
    if "zhidong" in name or "brake" in name:
        return materials["brake"]
    if "yeya" in name or "damper" in name:
        return materials["rubber"]
    if "qianyin" in name or "traction" in name:
        return materials["traction"]
    return materials["steel"]


def main() -> None:
    args = parse_arguments()
    source_glb = Path(args.source_glb).expanduser().resolve()
    source_step = Path(args.source_step).expanduser().resolve()
    output = Path(args.output).expanduser().resolve()
    manifest = (
        Path(args.manifest).expanduser().resolve()
        if args.manifest
        else output.with_suffix(".manifest.json")
    )

    source_hash = hashlib.sha256(source_step.read_bytes()).hexdigest()
    if source_hash != SOURCE_STEP_SHA256:
        raise RuntimeError(
            f"CW-200K source hash changed: {source_hash}; expected {SOURCE_STEP_SHA256}"
        )

    output.parent.mkdir(parents=True, exist_ok=True)
    manifest.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source_glb))

    imported_objects = list(bpy.context.scene.objects)
    imported_meshes = [obj for obj in imported_objects if obj.type == "MESH"]
    before_objects = len(imported_meshes)
    before_triangles = triangles(imported_meshes)

    removed_names = []
    for obj in list(imported_meshes):
        normalized = hierarchy_name(obj)
        if any(part in normalized for part in HARDWARE_NAME_PARTS):
            removed_names.append(obj.name)
            bpy.data.objects.remove(obj, do_unlink=True)

    materials = {
        "steel": create_material("CW200K_Steel", (0.19, 0.25, 0.30, 1.0), 0.72, 0.28),
        "wheel": create_material("CW200K_WheelSteel", (0.055, 0.07, 0.085, 1.0), 0.82, 0.22),
        "spring": create_material("CW200K_SpringSteel", (0.14, 0.24, 0.31, 1.0), 0.66, 0.32),
        "brake": create_material("CW200K_Brake", (0.30, 0.12, 0.075, 1.0), 0.58, 0.38),
        "rubber": create_material("CW200K_Rubber", (0.035, 0.042, 0.048, 1.0), 0.04, 0.68),
        "traction": create_material("CW200K_Traction", (0.16, 0.22, 0.19, 1.0), 0.62, 0.34),
    }

    remaining_meshes = [
        obj for obj in bpy.context.scene.objects if obj.type == "MESH"
    ]
    for obj in remaining_meshes:
        polygon_count = len(obj.data.polygons)
        if polygon_count > 25_000:
            ratio = 0.28
        elif polygon_count > 8_000:
            ratio = 0.40
        elif polygon_count > 2_000:
            ratio = 0.58
        else:
            ratio = 1.0

        if ratio < 1.0:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            modifier = obj.modifiers.new("RailCraft_CadReduction", "DECIMATE")
            modifier.ratio = ratio
            modifier.use_collapse_triangulate = True
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            obj.select_set(False)

        for polygon in obj.data.polygons:
            polygon.use_smooth = True
        obj.data.materials.clear()
        obj.data.materials.append(select_material(obj, materials))

    minimum, maximum = world_bounds(remaining_meshes)
    center = (minimum + maximum) * 0.5
    translation = Vector((-center.x, -center.y, -minimum.z))
    top_level = [
        obj for obj in bpy.context.scene.objects if obj.parent is None
    ]
    for obj in top_level:
        obj.location += translation

    root = bpy.data.objects.new("CW200KReferenceRoot", None)
    bpy.context.scene.collection.objects.link(root)
    root["asset_role"] = "passenger_bogie_reference"
    root["engineering_identity"] = "CW-200K_reference_only"
    root["source_step_sha256"] = SOURCE_STEP_SHA256
    for obj in top_level:
        obj.parent = root

    size = maximum - minimum
    for name, location in (
        ("BogieCenter", Vector((0.0, 0.0, size.z * 0.5))),
        ("RailContactPlane", Vector((0.0, 0.0, 0.0))),
        ("VehicleMount", Vector((0.0, 0.0, size.z))),
    ):
        anchor = bpy.data.objects.new(name, None)
        bpy.context.scene.collection.objects.link(anchor)
        anchor.parent = root
        anchor.location = location
        anchor.empty_display_type = "PLAIN_AXES"
        anchor.empty_display_size = 0.18

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=str(output),
        check_existing=False,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        use_space_transform=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_subsurf=False,
        use_mesh_edges=False,
        use_tspace=False,
        use_triangles=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )

    final_meshes = [
        obj for obj in root.children_recursive if obj.type == "MESH"
    ]
    final_minimum, final_maximum = world_bounds(final_meshes)
    report = {
        "schema": 1,
        "source_file": source_step.name,
        "source_sha256": SOURCE_STEP_SHA256,
        "source_identity": "CW-200K passenger bogie reference",
        "engineering_identity": "reference_only",
        "blender_version": bpy.app.version_string,
        "input_mesh_objects": before_objects,
        "input_triangles": before_triangles,
        "removed_hardware_objects": len(removed_names),
        "output_mesh_objects": len(final_meshes),
        "output_triangles": triangles(final_meshes),
        "bounds_m": {
            "minimum": [round(value, 6) for value in final_minimum],
            "maximum": [round(value, 6) for value in final_maximum],
            "size": [
                round(final_maximum[index] - final_minimum[index], 6)
                for index in range(3)
            ],
        },
        "materials": [material.name for material in materials.values()],
        "output": output.name,
        "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
    }
    manifest.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print("CW200K_REFERENCE_EXPORT_SUCCEEDED")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
