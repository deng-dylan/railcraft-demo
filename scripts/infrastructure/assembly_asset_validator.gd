class_name AssemblyAssetValidator
extends RefCounted

## Stable error codes for startup asset-contract validation.
const MODEL_SCENE_MISSING: String = "ASSET_MODEL_SCENE_MISSING"
const MODEL_SCENE_LOAD_FAILED: String = "ASSET_MODEL_SCENE_LOAD_FAILED"
const MODEL_SCENE_TYPE_INVALID: String = "ASSET_MODEL_SCENE_TYPE_INVALID"
const MODEL_ROOT_TYPE_INVALID: String = "ASSET_MODEL_ROOT_TYPE_INVALID"
const PART_ACTOR_SCRIPT_MISSING: String = "ASSET_PART_ACTOR_SCRIPT_MISSING"
const PART_ACTOR_CAPABILITY_MISSING: String = "ASSET_PART_ACTOR_CAPABILITY_MISSING"
const VISUAL_ROOT_MISSING: String = "ASSET_VISUAL_ROOT_MISSING"
const VISUAL_ROOT_TYPE_INVALID: String = "ASSET_VISUAL_ROOT_TYPE_INVALID"
const CLICK_AREA_MISSING: String = "ASSET_CLICK_AREA_MISSING"
const CLICK_AREA_TYPE_INVALID: String = "ASSET_CLICK_AREA_TYPE_INVALID"
const COLLISION_SHAPE_MISSING: String = "ASSET_COLLISION_SHAPE_MISSING"
const COLLISION_SHAPE_TYPE_INVALID: String = "ASSET_COLLISION_SHAPE_TYPE_INVALID"
const SNAP_TARGET_MISSING: String = "ASSET_SNAP_TARGET_MISSING"
const SNAP_TARGET_TYPE_INVALID: String = "ASSET_SNAP_TARGET_TYPE_INVALID"

const PART_ID_PROPERTY: StringName = &"part_id"
const INTERACTION_METHOD: StringName = &"set_interaction_enabled"


## Validates every part scene and snap target without retaining temporary instances.
func validate(
	catalog: ContentCatalog,
	train_assembly_root: Node,
) -> Array[ValidationIssue]:
	var issues: Array[ValidationIssue] = []
	if catalog == null:
		return issues

	var parts: Array[PartData] = catalog.get_parts()
	if parts.is_empty():
		return issues

	for part: PartData in parts:
		_validate_model_scene(part, issues)
		_validate_snap_target(part, train_assembly_root, issues)
	return issues


func _validate_model_scene(part: PartData, issues: Array[ValidationIssue]) -> void:
	var model_path: String = part.model_scene_path
	var field_path: String = _part_field_path(part.part_id, "model_scene_path")
	if not ResourceLoader.exists(model_path):
		_add_issue(
			issues,
			MODEL_SCENE_MISSING,
			field_path,
			"Model scene does not exist: %s" % model_path,
		)
		return

	var resource: Resource = ResourceLoader.load(model_path)
	if resource == null:
		_add_issue(
			issues,
			MODEL_SCENE_LOAD_FAILED,
			field_path,
			"Model scene could not be loaded: %s" % model_path,
		)
		return
	if not resource is PackedScene:
		_add_issue(
			issues,
			MODEL_SCENE_TYPE_INVALID,
			field_path,
			"Model resource must be a PackedScene: %s" % model_path,
		)
		return

	var scene: PackedScene = resource as PackedScene
	var instance: Node = scene.instantiate()
	if not instance is Node3D:
		_add_issue(
			issues,
			MODEL_ROOT_TYPE_INVALID,
			_asset_node_path(model_path, "."),
			"Model scene root must inherit Node3D.",
		)
		if instance != null:
			instance.free()
		return

	_validate_part_actor_contract(instance as Node3D, model_path, issues)
	instance.free()


func _validate_part_actor_contract(
	actor: Node3D,
	model_path: String,
	issues: Array[ValidationIssue],
) -> void:
	if actor.get_script() == null:
		_add_issue(
			issues,
			PART_ACTOR_SCRIPT_MISSING,
			_asset_node_path(model_path, "."),
			"PartActor root must have an interaction script.",
		)
	elif not actor is PartActor:
		_add_issue(
			issues,
			PART_ACTOR_CAPABILITY_MISSING,
			_asset_node_path(model_path, "."),
			"PartActor root script must extend PartActor.",
		)
	else:
		var missing_capabilities: Array[String] = []
		if not _has_property(actor, PART_ID_PROPERTY):
			missing_capabilities.append(String(PART_ID_PROPERTY))
		if not actor.has_method(INTERACTION_METHOD):
			missing_capabilities.append("%s()" % INTERACTION_METHOD)
		if not missing_capabilities.is_empty():
			_add_issue(
				issues,
				PART_ACTOR_CAPABILITY_MISSING,
				_asset_node_path(model_path, "."),
				"PartActor script is missing: %s" % ", ".join(missing_capabilities),
			)

	_validate_named_child(
		actor,
		model_path,
		NodePath("VisualRoot"),
		Node3D,
		VISUAL_ROOT_MISSING,
		VISUAL_ROOT_TYPE_INVALID,
		issues,
	)
	var click_area: Node = _validate_named_child(
		actor,
		model_path,
		NodePath("ClickArea"),
		Area3D,
		CLICK_AREA_MISSING,
		CLICK_AREA_TYPE_INVALID,
		issues,
	)
	if click_area == null or not click_area is Area3D:
		return
	_validate_named_child(
		click_area,
		model_path,
		NodePath("CollisionShape3D"),
		CollisionShape3D,
		COLLISION_SHAPE_MISSING,
		COLLISION_SHAPE_TYPE_INVALID,
		issues,
		"ClickArea/CollisionShape3D",
	)


func _validate_named_child(
	parent: Node,
	model_path: String,
	relative_path: NodePath,
	expected_type: Variant,
	missing_code: String,
	invalid_type_code: String,
	issues: Array[ValidationIssue],
	reported_path: String = "",
) -> Node:
	var node_path: String = reported_path
	if node_path.is_empty():
		node_path = String(relative_path)
	var child: Node = parent.get_node_or_null(relative_path)
	if child == null:
		_add_issue(
			issues,
			missing_code,
			_asset_node_path(model_path, node_path),
			"Required node is missing: %s" % node_path,
		)
		return null
	if not is_instance_of(child, expected_type):
		_add_issue(
			issues,
			invalid_type_code,
			_asset_node_path(model_path, node_path),
			"Node has the wrong type: %s" % node_path,
		)
	return child


func _validate_snap_target(
	part: PartData,
	train_assembly_root: Node,
	issues: Array[ValidationIssue],
) -> void:
	var target: Node = null
	if train_assembly_root != null:
		target = train_assembly_root.get_node_or_null(NodePath(part.snap_target_path))
	var target_path: String = _train_node_path(part.snap_target_path)
	if target == null:
		_add_issue(
			issues,
			SNAP_TARGET_MISSING,
			target_path,
			"Snap target does not exist for part '%s'." % part.part_id,
		)
		return
	if not target is Marker3D:
		_add_issue(
			issues,
			SNAP_TARGET_TYPE_INVALID,
			target_path,
			"Snap target must be a Marker3D for part '%s'." % part.part_id,
		)


func _has_property(object: Object, property_name: StringName) -> bool:
	for property: Dictionary in object.get_property_list():
		if property.get("name", &"") == property_name:
			return true
	return false


func _add_issue(
	issues: Array[ValidationIssue],
	code: String,
	path: String,
	message: String,
) -> void:
	issues.append(ValidationIssue.new(code, path, message))


func _part_field_path(part_id: String, field_name: String) -> String:
	return "$.parts[%s].%s" % [part_id, field_name]


func _asset_node_path(model_path: String, node_path: String) -> String:
	return "%s::%s" % [model_path, node_path]


func _train_node_path(relative_path: String) -> String:
	return "TrainAssemblyRoot/%s" % relative_path
