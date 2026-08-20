"""Export the teammate bogie .blend as a semantically reproducible Unity-ready FBX.

Run with Blender 5.2 LTS:

    blender --background --disable-autoexec source.blend \
      --python Tools/Blender/export_bogie_assembly_demo.py -- \
      --output Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/BogieAssemblyDemo.fbx

The source file is never saved. The script removes presentation-only objects,
clears the exploded animation, creates semantic groups and assembly anchors,
then exports one static FBX for the generated whitebox scene. FBX container
timestamps and record IDs may vary between runs; the manifest and geometry
contract are the reproducibility boundary.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from collections import deque
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


SOURCE_SHA256 = "f0fb4a7beb5d25122ca654871918efd7922a152534865afb5778dc780e13db3b"

REMOVE_OBJECTS = {"Camera", "Light", "Rail_L", "Rail_R", "Screw"}

GROUPS = {
    "Demo_WheelsetAxlebox": (
        "Axle_F",
        "Axle_R",
        "Wheels_F",
        "Wheels_R",
        "Axlebox_FL",
        "Axlebox_FR",
        "Axlebox_RL",
        "Axlebox_RR",
    ),
    "Demo_Frame": (
        "Frame",
        "BrakeDiscs_F",
        "BrakeDiscs_R",
        "Caliper_FL",
        "Caliper_FR",
        "Caliper_RL",
        "Caliper_RR",
    ),
    "Demo_PrimarySuspension": (
        "Spring_FL",
        "Spring_FR",
        "Spring_RL",
        "Spring_RR",
        "DamperV_FL",
        "DamperV_FR",
        "DamperV_RL",
        "DamperV_RR",
    ),
    "Demo_SecondarySuspension": (
        "AirSpring_L",
        "AirSpring_R",
        "DamperY_L",
        "DamperY_R",
        "DamperT_C",
    ),
    "Demo_Drive": (
        "Motor_L",
        "Motor_R",
        "Gearbox_F",
        "Gearbox_R",
        "DriveShaft_F",
        "DriveShaft_R",
    ),
    "Demo_CentralTraction": ("Traction",),
}

SOURCE_WHEELSETS = ("Wheelset_F", "Wheelset_R")
WHEELSET_OUTPUT_NAMES = {
    "Wheelset_F": ("Axle_F", "Wheels_F", "BrakeDiscs_F"),
    "Wheelset_R": ("Axle_R", "Wheels_R", "BrakeDiscs_R"),
}

FINAL_WHEELSET_OBJECTS = {name for names in WHEELSET_OUTPUT_NAMES.values() for name in names}
EXPECTED_SOURCE_MESHES = (
    {
        object_name
        for object_names in GROUPS.values()
        for object_name in object_names
    }
    - FINAL_WHEELSET_OBJECTS
) | set(SOURCE_WHEELSETS) | {"Rail_L", "Rail_R", "Screw"}


def parse_arguments() -> argparse.Namespace:
    argv = sys.argv
    script_args = argv[argv.index("--") + 1 :] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--manifest")
    return parser.parse_args(script_args)


def require_source_contract() -> None:
    source_path = Path(bpy.data.filepath)
    source_hash = hashlib.sha256(source_path.read_bytes()).hexdigest()
    if source_hash != SOURCE_SHA256:
        raise RuntimeError(
            f"Source hash changed: {source_hash}; expected {SOURCE_SHA256}"
        )
    actual_meshes = {obj.name for obj in bpy.context.scene.objects if obj.type == "MESH"}
    missing = sorted(EXPECTED_SOURCE_MESHES - actual_meshes)
    unexpected = sorted(actual_meshes - EXPECTED_SOURCE_MESHES)
    if missing or unexpected:
        raise RuntimeError(
            "Source object contract changed: "
            f"missing={missing or 'none'}; unexpected={unexpected or 'none'}"
        )


def bounds_for(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    points = [
        obj.matrix_world @ vertex.co
        for obj in objects
        if obj.type == "MESH"
        for vertex in obj.data.vertices
    ]
    if not points:
        raise RuntimeError("Cannot calculate bounds for an empty object list")
    minimum = Vector((min(point[index] for point in points) for index in range(3)))
    maximum = Vector((max(point[index] for point in points) for index in range(3)))
    return minimum, maximum


def create_empty(name: str, parent: bpy.types.Object | None, location: Vector) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(obj)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.18
    obj.location = location
    obj.parent = parent
    return obj


def connected_vertex_components(mesh: bpy.types.Mesh) -> list[set[int]]:
    adjacency = {vertex.index: set() for vertex in mesh.vertices}
    for edge in mesh.edges:
        first, second = edge.vertices
        adjacency[first].add(second)
        adjacency[second].add(first)

    remaining = set(adjacency)
    components = []
    while remaining:
        start = remaining.pop()
        queue = deque([start])
        component = {start}
        while queue:
            current = queue.popleft()
            for neighbor in adjacency[current]:
                if neighbor in remaining:
                    remaining.remove(neighbor)
                    component.add(neighbor)
                    queue.append(neighbor)
        components.append(component)
    return components


def component_center(mesh: bpy.types.Mesh, indices: set[int]) -> Vector:
    points = [mesh.vertices[index].co for index in indices]
    minimum = Vector((min(point[axis] for point in points) for axis in range(3)))
    maximum = Vector((max(point[axis] for point in points) for axis in range(3)))
    return (minimum + maximum) * 0.5


def create_mesh_subset(
    source: bpy.types.Object,
    output_name: str,
    retained_vertex_indices: set[int],
) -> bpy.types.Object:
    result = source.copy()
    result.data = source.data.copy()
    result.name = output_name
    result.data.name = output_name + "_Mesh"
    bpy.context.scene.collection.objects.link(result)

    editable = bmesh.new()
    editable.from_mesh(result.data)
    editable.verts.ensure_lookup_table()
    discarded = [
        vertex
        for vertex in editable.verts
        if vertex.index not in retained_vertex_indices
    ]
    bmesh.ops.delete(editable, geom=discarded, context="VERTS")
    editable.to_mesh(result.data)
    editable.free()
    result.data.update()
    return result


def split_and_repair_wheelsets() -> dict[str, list[float]]:
    """Split semantic axle/wheel meshes and align each wheel set to its axle.

    In the delivered blend, each F/R wheelset mesh contains an axle centered at
    Y=+/-1.25 m and Z=0.46 m, while both pairs of wheel/disc islands remain at
    Y=0 and Z=0. The wheel islands therefore overlap each other and miss their
    axles in frame 1. Connectivity and vertex-count gates keep the repair
    deterministic if the source file is replaced later.
    """

    applied_offsets = {}
    for object_name in SOURCE_WHEELSETS:
        obj = bpy.data.objects[object_name]
        obj.data = obj.data.copy()
        components = connected_vertex_components(obj.data)
        wheel_components = {
            index for component in components if len(component) == 720 for index in component
        }
        brake_disc_components = {
            index for component in components if len(component) == 200 for index in component
        }
        axle_components = {
            index for component in components if len(component) == 64 for index in component
        }
        moving_components = wheel_components | brake_disc_components
        if (
            len(components) != 7
            or len(wheel_components) != 1440
            or len(brake_disc_components) != 400
            or len(axle_components) != 192
            or len(moving_components | axle_components) != len(obj.data.vertices)
        ):
            raise RuntimeError(
                f"Unexpected {object_name} island contract: components={len(components)}, "
                f"wheel_vertices={len(wheel_components)}, "
                f"brake_disc_vertices={len(brake_disc_components)}, "
                f"axle_vertices={len(axle_components)}"
            )

        wheel_center = component_center(obj.data, moving_components)
        axle_center = component_center(obj.data, axle_components)
        offset = axle_center - wheel_center
        if abs(abs(offset.y) - 1.25) > 0.0001 or abs(offset.z - 0.46) > 0.0001:
            raise RuntimeError(f"Unexpected {object_name} wheel repair offset: {tuple(offset)}")

        for index in moving_components:
            obj.data.vertices[index].co += offset
        obj.data.update()
        axle_name, wheels_name, brake_discs_name = WHEELSET_OUTPUT_NAMES[object_name]
        create_mesh_subset(obj, axle_name, axle_components)
        create_mesh_subset(obj, wheels_name, wheel_components)
        create_mesh_subset(obj, brake_discs_name, brake_disc_components)
        applied_offsets[object_name] = [round(value, 6) for value in offset]
        bpy.data.objects.remove(obj, do_unlink=True)
    return applied_offsets


def main() -> None:
    args = parse_arguments()
    output_path = Path(args.output).expanduser().resolve()
    manifest_path = (
        Path(args.manifest).expanduser().resolve()
        if args.manifest
        else output_path.with_suffix(".manifest.json")
    )
    output_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.parent.mkdir(parents=True, exist_ok=True)

    scene = bpy.context.scene
    scene.frame_set(1)
    require_source_contract()
    wheelset_repair_offsets = split_and_repair_wheelsets()

    rail_objects = [bpy.data.objects[name] for name in ("Rail_L", "Rail_R")]
    _, rail_maximum = bounds_for(rail_objects)
    rail_top_z = rail_maximum.z

    axle_objects = [bpy.data.objects[name] for name in ("Axle_F", "Axle_R")]
    axle_centers = []
    for axle in axle_objects:
        minimum, maximum = bounds_for([axle])
        axle_centers.append((minimum + maximum) * 0.5)
    air_springs = [bpy.data.objects[name] for name in ("AirSpring_L", "AirSpring_R")]
    _, air_spring_maximum = bounds_for(air_springs)

    for name in REMOVE_OBJECTS:
        obj = bpy.data.objects.get(name)
        if obj is not None:
            bpy.data.objects.remove(obj, do_unlink=True)

    exported_meshes = [obj for obj in scene.objects if obj.type == "MESH"]
    for obj in exported_meshes:
        frame_one_matrix = obj.matrix_world.copy()
        obj.animation_data_clear()
        obj.matrix_world = frame_one_matrix

    overall_minimum, overall_maximum = bounds_for(exported_meshes)
    overall_center = (overall_minimum + overall_maximum) * 0.5
    bogie_center = (axle_centers[0] + axle_centers[1]) * 0.5

    root = create_empty("BogieAssemblyDemoRoot", None, Vector((0.0, 0.0, 0.0)))
    root["asset_role"] = "assembly_demonstration"
    root["source_sha256"] = SOURCE_SHA256
    root["engineering_identity"] = "reference_only"

    for group_name, object_names in GROUPS.items():
        group = create_empty(group_name, root, Vector((0.0, 0.0, 0.0)))
        for object_name in object_names:
            obj = bpy.data.objects[object_name]
            obj.parent = group

    create_empty(
        "BogieCenter",
        root,
        bogie_center,
    )
    create_empty(
        "RailContactPlane",
        root,
        Vector((bogie_center.x, bogie_center.y, rail_top_z)),
    )
    create_empty(
        "VehicleMount",
        root,
        Vector((bogie_center.x, bogie_center.y, air_spring_maximum.z)),
    )
    for index, axle_center in enumerate(axle_centers, start=1):
        create_empty(f"Axle_{index:02d}", root, axle_center)

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
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

    total_triangles = 0
    for obj in exported_meshes:
        obj.data.calc_loop_triangles()
        total_triangles += len(obj.data.loop_triangles)
    if len(exported_meshes) != 35 or total_triangles != 26860:
        raise RuntimeError(
            f"Export budget changed: meshes={len(exported_meshes)}, triangles={total_triangles}"
        )
    if abs(overall_minimum.z - (-0.018)) > 0.0001 or abs(rail_top_z - (-0.03)) > 0.0001:
        raise RuntimeError(
            f"Wheel/rail vertical contract changed: min_z={overall_minimum.z}, rail_top_z={rail_top_z}"
        )

    output_sha256 = hashlib.sha256(output_path.read_bytes()).hexdigest()

    manifest = {
        "schema": 2,
        "source_sha256": SOURCE_SHA256,
        "blender_version": bpy.app.version_string,
        "frame": 1,
        "removed_objects": sorted(REMOVE_OBJECTS),
        "repairs": {
            "wheelset_wheel_island_offsets_m": wheelset_repair_offsets,
            "split_objects": [
                "Axle_F", "Axle_R", "Wheels_F", "Wheels_R",
                "BrakeDiscs_F", "BrakeDiscs_R",
            ],
            "reason": "F/R wheel and disc islands overlapped at Y=0,Z=0 in the delivered frame-1 mesh",
        },
        "groups": {name: list(objects) for name, objects in GROUPS.items()},
        "anchors": [
            "BogieCenter",
            "RailContactPlane",
            "VehicleMount",
            "Axle_01",
            "Axle_02",
        ],
        "mesh_object_count": len(exported_meshes),
        "triangles": total_triangles,
        "bounds_blender_m": {
            "minimum": [round(value, 6) for value in overall_minimum],
            "maximum": [round(value, 6) for value in overall_maximum],
        },
        "rail_top_z_blender_m": round(rail_top_z, 6),
        "output": output_path.name,
        "output_sha256": output_sha256,
    }
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print("BOGIE_ASSEMBLY_DEMO_EXPORT_SUCCEEDED")
    print(json.dumps(manifest, ensure_ascii=False))


if __name__ == "__main__":
    main()
