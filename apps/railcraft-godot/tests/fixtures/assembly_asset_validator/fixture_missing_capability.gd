extends Node3D

signal part_clicked(part_id: String)

@export var part_id: String = ""


func set_interaction_enabled(_enabled: bool) -> void:
	pass


func get_visual_root() -> Node3D:
	return get_node_or_null(^"VisualRoot") as Node3D
