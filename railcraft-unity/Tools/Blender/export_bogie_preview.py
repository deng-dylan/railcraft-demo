"""Optimize an OpenCascade GLB and export a Unity-ready bogie FBX."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


def parse_arguments() -> argparse.Namespace:
    argv = sys.argv
    script_args = argv[argv.index("--") + 1 :] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-glb", required=True)
    parser.add_argument("--source-cad", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--asset-key", required=True)
    parser.add_argument("--display-name", required=True)
    parser.add_argument("--preview-image")
    return parser.parse_args(script_args)


def mesh_objects() -> list[bpy.types.Object]:
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def triangles(objects: list[bpy.types.Object]) -> int:
    total = 0
    for obj in objects:
        obj.data.calc_loop_triangles()
        total += len(obj.data.loop_triangles)
    return total


def world_bounds(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        for corner in obj.bound_box
    ]
    if not points:
        raise RuntimeError("Cannot calculate bounds for an empty model")
    minimum = Vector(min(point[index] for point in points) for index in range(3))
    maximum = Vector(max(point[index] for point in points) for index in range(3))
    return minimum, maximum


def fallback_material() -> bpy.types.Material:
    material = bpy.data.materials.new("RailCraft_CadPreviewSteel")
    material.diffuse_color = (0.18, 0.24, 0.29, 1.0)
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = material.diffuse_color
        metallic = principled.inputs.get("Metallic IOR Level") or principled.inputs.get("Metallic")
        if metallic is not None:
            metallic.default_value = 0.72
        principled.inputs["Roughness"].default_value = 0.3
    return material


def orient_shortest_dimension_up(objects: list[bpy.types.Object]) -> str:
    minimum, maximum = world_bounds(objects)
    size = maximum - minimum
    shortest_axis = min(range(3), key=lambda index: size[index])
    if shortest_axis == 2:
        return "Z_up_preserved"

    if shortest_axis == 1:
        rotation = Matrix.Rotation(math.radians(90.0), 4, "X")
        description = "Y_up_rotated_positive_90_X"
    else:
        rotation = Matrix.Rotation(math.radians(-90.0), 4, "Y")
        description = "X_up_rotated_negative_90_Y"

    for obj in [item for item in bpy.context.scene.objects if item.parent is None]:
        obj.matrix_world = rotation @ obj.matrix_world
    bpy.context.view_layer.update()
    return description


def optimize(objects: list[bpy.types.Object]) -> tuple[int, int]:
    before = triangles(objects)
    fallback = fallback_material()
    for obj in objects:
        obj.data.calc_loop_triangles()
        count = len(obj.data.loop_triangles)
        if count > 80_000:
            ratio = 0.28
        elif count > 30_000:
            ratio = 0.42
        elif count > 10_000:
            ratio = 0.62
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
        if not obj.data.materials:
            obj.data.materials.append(fallback)

    return before, triangles(objects)


def flatten_visual_hierarchy(
    objects: list[bpy.types.Object],
    asset_key: str,
) -> list[bpy.types.Object]:
    """Bake CAD hierarchy transforms into one review mesh.

    Unity's FBX importer can reinterpret an unbaked CAD root rotation.  The
    review asset does not need per-fastener animation, so flattening gives the
    editor a stable Y-up mesh while preserving material slots and geometry.
    """
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.hide_set(False)
        obj.hide_viewport = False
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = f"{asset_key}_CadPreviewMesh"
    world_matrix = joined.matrix_world.copy()
    joined.parent = None
    joined.matrix_world = world_matrix
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    for obj in list(bpy.context.scene.objects):
        if obj is not joined:
            bpy.data.objects.remove(obj, do_unlink=True)
    return [joined]


def create_render_preview(
    output: Path,
    display_name: str,
    minimum: Vector,
    maximum: Vector,
) -> None:
    size = maximum - minimum
    span = max(size.x, size.y, size.z)
    target = Vector((0.0, 0.0, size.z * 0.42))

    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 720
    bpy.context.scene.render.resolution_percentage = 100
    bpy.context.scene.render.image_settings.file_format = "PNG"
    bpy.context.scene.render.filepath = str(output)
    if bpy.context.scene.world is None:
        bpy.context.scene.world = bpy.data.worlds.new("CadPreviewWorld")
    bpy.context.scene.world.color = (0.018, 0.024, 0.034)

    camera_data = bpy.data.cameras.new("CadPreviewCamera")
    camera = bpy.data.objects.new("CadPreviewCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = Vector((span * 1.15, -span * 1.55, span * 0.82))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera_data.lens = 55
    bpy.context.scene.camera = camera

    for name, location, energy, size_value in (
        ("Key", Vector((span * 0.5, -span * 0.7, span * 1.5)), 1500.0, span * 0.8),
        ("Fill", Vector((-span, -span * 0.3, span * 0.8)), 900.0, span * 0.7),
        ("Rim", Vector((0.0, span, span * 1.2)), 1200.0, span * 0.6),
    ):
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = size_value
        light = bpy.data.objects.new(name, light_data)
        light.location = location
        light.rotation_euler = (target - light.location).to_track_quat("-Z", "Y").to_euler()
        bpy.context.scene.collection.objects.link(light)

    floor_material = bpy.data.materials.new("CadPreviewFloor")
    floor_material.diffuse_color = (0.035, 0.045, 0.06, 1.0)
    bpy.ops.mesh.primitive_plane_add(size=span * 5.0, location=(0.0, 0.0, -0.025))
    floor = bpy.context.object
    floor.name = f"{display_name}_PreviewFloor"
    floor.data.materials.append(floor_material)

    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.render.render(write_still=True)


def main() -> None:
    args = parse_arguments()
    source_glb = Path(args.source_glb).resolve()
    source_cad = Path(args.source_cad).resolve()
    output = Path(args.output).resolve()
    manifest = Path(args.manifest).resolve()
    preview_image = Path(args.preview_image).resolve() if args.preview_image else None

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source_glb))
    imported = mesh_objects()
    if not imported:
        raise RuntimeError("The GLB did not contain any mesh objects")

    input_meshes = len(imported)
    orientation = orient_shortest_dimension_up(imported)
    input_triangles, output_triangles = optimize(imported)
    imported = flatten_visual_hierarchy(imported, args.asset_key)
    minimum, maximum = world_bounds(imported)
    center = (minimum + maximum) * 0.5
    translation = Vector((-center.x, -center.y, -minimum.z))
    top_level = [obj for obj in bpy.context.scene.objects if obj.parent is None]
    for obj in top_level:
        obj.location += translation

    root = bpy.data.objects.new(f"{args.asset_key}PreviewRoot", None)
    bpy.context.scene.collection.objects.link(root)
    root["asset_key"] = args.asset_key
    root["display_name"] = args.display_name
    root["source_sha256"] = hashlib.sha256(source_cad.read_bytes()).hexdigest()
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
    output.parent.mkdir(parents=True, exist_ok=True)
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

    final_meshes = [obj for obj in root.children_recursive if obj.type == "MESH"]
    final_minimum, final_maximum = world_bounds(final_meshes)
    report = {
        "schema": 1,
        "asset_key": args.asset_key,
        "display_name": args.display_name,
        "source_file": source_cad.name,
        "source_sha256": hashlib.sha256(source_cad.read_bytes()).hexdigest(),
        "blender_version": bpy.app.version_string,
        "orientation": orientation,
        "input_mesh_objects": input_meshes,
        "input_triangles": input_triangles,
        "output_mesh_objects": len(final_meshes),
        "output_triangles": output_triangles,
        "bounds_m": {
            "minimum": [round(value, 6) for value in final_minimum],
            "maximum": [round(value, 6) for value in final_maximum],
            "size": [
                round(final_maximum[index] - final_minimum[index], 6)
                for index in range(3)
            ],
        },
        "output": output.name,
        "output_bytes": output.stat().st_size,
        "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
    }
    manifest.parent.mkdir(parents=True, exist_ok=True)
    manifest.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    if preview_image is not None:
        create_render_preview(preview_image, args.display_name, final_minimum, final_maximum)

    print("BOGIE_PREVIEW_EXPORT_SUCCEEDED")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
