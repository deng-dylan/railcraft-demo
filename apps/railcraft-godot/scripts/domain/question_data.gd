class_name QuestionData
extends RefCounted

## Typed question content. correct_option_index remains zero-based.
var question_id: String
var order: int
var prompt: String
var options: Array[String]
var correct_option_index: int
var explanation: String
var source: SourceData
var reward_part_id: String


func _init(
	data_question_id: String,
	data_order: int,
	data_prompt: String,
	data_options: Array[String],
	data_correct_option_index: int,
	data_explanation: String,
	data_source: SourceData,
	data_reward_part_id: String,
) -> void:
	question_id = data_question_id
	order = data_order
	prompt = data_prompt
	options = data_options.duplicate()
	correct_option_index = data_correct_option_index
	explanation = data_explanation
	source = data_source
	reward_part_id = data_reward_part_id
