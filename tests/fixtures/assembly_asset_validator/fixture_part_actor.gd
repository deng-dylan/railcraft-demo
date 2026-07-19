extends Node3D

@export var part_id: String = ""
var interaction_enabled: bool = false


func set_interaction_enabled(enabled: bool) -> void:
	interaction_enabled = enabled
