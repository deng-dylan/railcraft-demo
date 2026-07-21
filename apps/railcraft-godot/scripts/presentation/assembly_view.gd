class_name AssemblyView
extends Node3D

signal part_clicked(part_id: String)

const COMPONENT_CONTAINER_PATHS: Dictionary[String, NodePath] = {
	"carbody_connection": ^"Components/CarbodyConnection",
	"running_braking": ^"Components/RunningBraking",
	"traction_power": ^"Components/TractionPower",
}
const HINT_COLOR := Color(0.06, 0.86, 1.0, 0.42)
const HIGHLIGHT_COLOR := Color(1.0, 0.62, 0.12, 1.0)

@export var part_preview_anchor_path: NodePath = ^"PartPreviewAnchor"
@export var train_assembly_root_path: NodePath = ^"TrainAssemblyRoot"
@export var target_hints_path: NodePath = ^"TargetHints"

var last_error: String = ""

var _preview_anchor: Node3D
var _train_root: Node3D
var _target_hints: Node3D
var _current_actor: PartActor
var _current_part: PartData
var _current_hint: Node3D
var _hint_material: StandardMaterial3D
var _hint_tween: Tween
var _interaction_enabled: bool = true
var _part_data_by_id: Dictionary[String, PartData] = {}
var _installed_actors: Dictionary[String, PartActor] = {}
var _installed_component_ids: Dictionary[String, String] = {}
var _component_original_materials: Dictionary = {}


func _ready() -> void:
	_cache_required_nodes()


## Verifies and caches the scene nodes required by the public view API.
func is_ready_for_parts() -> bool:
	return _cache_required_nodes()


## Creates the current preview and its collision-free target hint.
func prepare_part(part: PartData) -> bool:
	last_error = ""
	if not _can_start_prepare(part):
		return false
	if _current_actor != null and _current_part != null and _current_part.part_id == part.part_id:
		return true

	cleanup_pending_part()
	_current_actor = _instantiate_part_actor(part)
	if _current_actor == null:
		return false
	_current_part = part
	_part_data_by_id[part.part_id] = part
	_preview_anchor.add_child(_current_actor)
	_current_actor.transform = part.preview_transform.to_transform_3d()
	_current_actor.part_id = part.part_id
	_current_actor.set_interaction_enabled(_interaction_enabled)
	_current_actor.part_clicked.connect(_on_actor_clicked)
	if not _create_target_hint(part):
		cleanup_pending_part()
		return false
	return true


func get_part_actor(part_id: String) -> PartActor:
	if _current_actor != null and _current_actor.part_id == part_id:
		return _current_actor
	if _installed_actors.has(part_id):
		return _installed_actors[part_id]
	return null


func get_snap_target(part_id: String) -> Marker3D:
	last_error = ""
	if not _cache_required_nodes():
		return null
	var part: PartData = _part_data_by_id.get(part_id)
	if part == null:
		last_error = "No PartData has been registered for part '%s'." % part_id
		return null
	var target: Node = _train_root.get_node_or_null(NodePath(part.snap_target_path))
	if not target is Marker3D:
		last_error = "Snap target is missing or has the wrong type: %s" % part.snap_target_path
		return null
	return target as Marker3D


## Returns the target marker combined with the content-defined local adjustment.
func get_snap_transform(part_id: String) -> Transform3D:
	var target: Marker3D = get_snap_target(part_id)
	if target == null:
		return Transform3D.IDENTITY
	var part: PartData = _part_data_by_id[part_id]
	return target.global_transform * part.target_transform.to_transform_3d()


## Commits only the visual placement after the domain transaction has succeeded.
func finalize_visual_install(part_id: String) -> void:
	last_error = ""
	if _current_actor == null or _current_part == null or _current_part.part_id != part_id:
		last_error = "No pending visual part matches '%s'." % part_id
		return
	var target: Marker3D = get_snap_target(part_id)
	if target == null:
		cleanup_pending_part()
		return
	var container: Node3D = _get_component_container(_current_part.component_id)
	if container == null:
		last_error = "Component container is missing for '%s'." % _current_part.component_id
		cleanup_pending_part()
		return

	var actor: PartActor = _current_actor
	var part: PartData = _current_part
	var installed_transform: Transform3D = (
		target.global_transform * part.target_transform.to_transform_3d()
	)
	actor.set_interaction_enabled(false)
	actor.reparent(container)
	actor.global_transform = installed_transform
	_installed_actors[part_id] = actor
	_installed_component_ids[part_id] = part.component_id
	_current_actor = null
	_current_part = null
	_clear_target_hint()


## Applies an emissive material only to installed meshes in the requested component.
func show_component_highlight(component_id: String) -> void:
	clear_component_highlight(component_id)
	var original_materials: Dictionary = {}
	var highlight_material: StandardMaterial3D = _make_highlight_material()
	for part_id: String in _installed_actors:
		if _installed_component_ids.get(part_id, "") != component_id:
			continue
		_collect_and_override_meshes(
			_installed_actors[part_id], original_materials, highlight_material
		)
	_component_original_materials[component_id] = original_materials


func clear_component_highlight(component_id: String) -> void:
	if not _component_original_materials.has(component_id):
		return
	var original_materials: Dictionary = _component_original_materials[component_id]
	for mesh_value: Variant in original_materials:
		if is_instance_valid(mesh_value) and mesh_value is GeometryInstance3D:
			var geometry: GeometryInstance3D = mesh_value as GeometryInstance3D
			geometry.material_override = original_materials[mesh_value]
	_component_original_materials.erase(component_id)


func set_part_interaction_enabled(enabled: bool) -> void:
	_interaction_enabled = enabled
	if _current_actor != null:
		_current_actor.set_interaction_enabled(enabled)


## Removes an unfinished preview and hint so a later preparation starts cleanly.
func cleanup_pending_part() -> void:
	_clear_target_hint()
	if _current_actor != null:
		if _current_actor.part_clicked.is_connected(_on_actor_clicked):
			_current_actor.part_clicked.disconnect(_on_actor_clicked)
		_current_actor.free()
	_current_actor = null
	_current_part = null


func get_target_hint() -> Node3D:
	return _current_hint


func get_installed_part_ids() -> Array[String]:
	var result: Array[String] = []
	for part_id: String in _installed_actors:
		result.append(part_id)
	return result


func _cache_required_nodes() -> bool:
	last_error = ""
	_preview_anchor = get_node_or_null(part_preview_anchor_path) as Node3D
	if _preview_anchor == null:
		last_error = "AssemblyView requires a Node3D at '%s'." % part_preview_anchor_path
		return false
	_train_root = get_node_or_null(train_assembly_root_path) as Node3D
	if _train_root == null:
		last_error = "AssemblyView requires a Node3D at '%s'." % train_assembly_root_path
		return false
	_target_hints = get_node_or_null(target_hints_path) as Node3D
	if _target_hints == null:
		last_error = "AssemblyView requires a Node3D at '%s'." % target_hints_path
		return false
	for component_id: String in COMPONENT_CONTAINER_PATHS:
		if _get_component_container(component_id) == null:
			last_error = (
				"TrainAssemblyRoot is missing the component container for '%s'." % component_id
			)
			return false
	return true


func _get_component_container(component_id: String) -> Node3D:
	if _train_root == null or not COMPONENT_CONTAINER_PATHS.has(component_id):
		return null
	return _train_root.get_node_or_null(COMPONENT_CONTAINER_PATHS[component_id]) as Node3D


func _can_start_prepare(part: PartData) -> bool:
	if part == null:
		return _fail_prepare("Cannot prepare a null PartData value.")
	return _cache_required_nodes()


func _instantiate_part_actor(part: PartData) -> PartActor:
	if not ResourceLoader.exists(part.model_scene_path):
		_fail_prepare("Part scene is unavailable: %s" % part.model_scene_path)
		return null
	var resource: Resource = ResourceLoader.load(part.model_scene_path)
	if resource == null or not resource is PackedScene:
		_fail_prepare("Part scene is unavailable: %s" % part.model_scene_path)
		return null
	var instance: Node = (resource as PackedScene).instantiate()
	if instance is PartActor:
		return instance as PartActor
	instance.free()
	_fail_prepare("Part scene root does not implement PartActor: %s" % part.model_scene_path)
	return null


func _create_target_hint(part: PartData) -> bool:
	var target: Marker3D = get_snap_target(part.part_id)
	if target == null:
		return false
	var visual_root: Node3D = _current_actor.get_visual_root()
	if visual_root == null:
		last_error = "PartActor is missing VisualRoot: %s" % part.model_scene_path
		return false
	var duplicate: Node = visual_root.duplicate()
	if not duplicate is Node3D:
		duplicate.free()
		last_error = "VisualRoot could not be duplicated for '%s'." % part.part_id
		return false
	_current_hint = duplicate as Node3D
	_current_hint.name = "%sTargetHint" % part.part_id.to_pascal_case()
	_target_hints.add_child(_current_hint)
	_current_hint.global_transform = (
		target.global_transform * part.target_transform.to_transform_3d()
	)
	_disable_collision_and_processing(_current_hint)
	_hint_material = _make_hint_material()
	_apply_material_recursively(_current_hint, _hint_material)
	_start_hint_pulse()
	return true


func _start_hint_pulse() -> void:
	if _hint_material == null or not is_inside_tree():
		return
	_hint_tween = create_tween().set_loops()
	_hint_tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	_hint_tween.tween_property(_hint_material, "albedo_color:a", 0.2, 0.4)
	_hint_tween.tween_property(_hint_material, "albedo_color:a", 0.48, 0.4)


func _clear_target_hint() -> void:
	if _hint_tween != null and _hint_tween.is_valid():
		_hint_tween.kill()
	_hint_tween = null
	_hint_material = null
	if _current_hint != null:
		_current_hint.free()
	_current_hint = null


func _disable_collision_and_processing(node: Node) -> void:
	node.set_process(false)
	node.set_physics_process(false)
	if node is CollisionObject3D:
		var collision_object: CollisionObject3D = node as CollisionObject3D
		collision_object.collision_layer = 0
		collision_object.collision_mask = 0
		collision_object.input_ray_pickable = false
	for child: Node in node.get_children():
		_disable_collision_and_processing(child)


func _apply_material_recursively(node: Node, material: Material) -> void:
	if node is GeometryInstance3D:
		(node as GeometryInstance3D).material_override = material
	for child: Node in node.get_children():
		_apply_material_recursively(child, material)


func _collect_and_override_meshes(
	node: Node,
	original_materials: Dictionary,
	material: Material,
) -> void:
	if node is GeometryInstance3D:
		var geometry: GeometryInstance3D = node as GeometryInstance3D
		original_materials[geometry] = geometry.material_override
		geometry.material_override = material
	for child: Node in node.get_children():
		_collect_and_override_meshes(child, original_materials, material)


func _make_hint_material() -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = HINT_COLOR
	material.emission_enabled = true
	material.emission = HINT_COLOR
	material.emission_energy_multiplier = 2.0
	return material


func _make_highlight_material() -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.albedo_color = HIGHLIGHT_COLOR
	material.emission_enabled = true
	material.emission = HIGHLIGHT_COLOR
	material.emission_energy_multiplier = 1.3
	return material


func _fail_prepare(message: String) -> bool:
	last_error = message
	return false


func _on_actor_clicked(part_id: String) -> void:
	if _interaction_enabled:
		part_clicked.emit(part_id)
