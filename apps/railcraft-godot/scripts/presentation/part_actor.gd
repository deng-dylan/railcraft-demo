class_name PartActor
extends Node3D

signal part_clicked(part_id: String)

@export var part_id: String = ""

var _interaction_enabled: bool = false
var _click_area: Area3D
var _collision_shape: CollisionShape3D


func _ready() -> void:
	_click_area = get_node_or_null(^"ClickArea") as Area3D
	if _click_area != null:
		_collision_shape = _click_area.get_node_or_null(^"CollisionShape3D") as CollisionShape3D
		if not _click_area.input_event.is_connected(_on_click_area_input_event):
			_click_area.input_event.connect(_on_click_area_input_event)
	set_interaction_enabled(_interaction_enabled)


## Enables mouse picking only while the part is waiting to be installed.
func set_interaction_enabled(enabled: bool) -> void:
	_interaction_enabled = enabled
	if _click_area == null:
		_click_area = get_node_or_null(^"ClickArea") as Area3D
	if _click_area == null:
		return
	_click_area.input_ray_pickable = enabled
	_click_area.collision_layer = 1 if enabled else 0
	_click_area.collision_mask = 0
	_click_area.monitoring = enabled
	_click_area.monitorable = enabled
	if _collision_shape == null:
		_collision_shape = _click_area.get_node_or_null(^"CollisionShape3D") as CollisionShape3D
	if _collision_shape != null:
		_collision_shape.disabled = not enabled


func is_interaction_enabled() -> bool:
	return _interaction_enabled


func get_visual_root() -> Node3D:
	return get_node_or_null(^"VisualRoot") as Node3D


func _on_click_area_input_event(
	_camera: Node,
	event: InputEvent,
	_position: Vector3,
	_normal: Vector3,
	_shape_index: int,
) -> void:
	if not _interaction_enabled:
		return
	if not event is InputEventMouseButton:
		return
	var mouse_event: InputEventMouseButton = event as InputEventMouseButton
	if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
		part_clicked.emit(part_id)
