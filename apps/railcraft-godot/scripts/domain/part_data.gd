class_name PartData
extends RefCounted

## Typed train-part metadata and its two content-defined transforms.
var part_id: String
var display_name: String
var order: int
var component_id: String
var model_scene_path: String
var snap_target_path: String
var target_transform: TransformData
var preview_transform: TransformData
var required_previous_part_id: String


func _init(
	data_part_id: String,
	data_display_name: String,
	data_order: int,
	data_component_id: String,
	data_model_scene_path: String,
	data_snap_target_path: String,
	data_target_transform: TransformData,
	data_preview_transform: TransformData,
	data_required_previous_part_id: String = "",
) -> void:
	part_id = data_part_id
	display_name = data_display_name
	order = data_order
	component_id = data_component_id
	model_scene_path = data_model_scene_path
	snap_target_path = data_snap_target_path
	target_transform = data_target_transform
	preview_transform = data_preview_transform
	required_previous_part_id = data_required_previous_part_id


func has_required_previous_part() -> bool:
	return not required_previous_part_id.is_empty()
