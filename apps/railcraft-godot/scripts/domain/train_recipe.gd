class_name TrainRecipe
extends RefCounted

## Typed recipe defining the component composition of the completed train.
var train_id: String
var display_name: String
var component_ids: Array[String]


func _init(
	data_train_id: String,
	data_display_name: String,
	data_component_ids: Array[String],
) -> void:
	train_id = data_train_id
	display_name = data_display_name
	component_ids = data_component_ids.duplicate()
