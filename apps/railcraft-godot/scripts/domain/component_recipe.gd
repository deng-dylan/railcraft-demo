class_name ComponentRecipe
extends RefCounted

## Typed recipe for one system-level train component.
var component_id: String
var display_name: String
var order: int
var part_ids: Array[String]
var completion_message: String
var teaching_note: String


func _init(
	data_component_id: String,
	data_display_name: String,
	data_order: int,
	data_part_ids: Array[String],
	data_completion_message: String,
	data_teaching_note: String,
) -> void:
	component_id = data_component_id
	display_name = data_display_name
	order = data_order
	part_ids = data_part_ids.duplicate()
	completion_message = data_completion_message
	teaching_note = data_teaching_note
