class_name GameFlowFixture
extends RefCounted

const PART_IDS: Array[String] = [
	"body_shell",
	"passenger_door",
	"coupler_buffer",
	"bogie_frame",
	"wheelset",
	"brake_unit",
	"pantograph",
	"traction_converter_unit",
	"traction_motor",
]
const COMPONENT_IDS: Array[String] = [
	"carbody_connection",
	"running_braking",
	"traction_power",
]
const TRAIN_ID: String = "railcraft_train"
const TRAIN_NAME: String = "轨道匠心号"


class RecordingInventoryManager:
	extends InventoryManager
	var grant_call_count: int = 0

	func grant_part(part_id: String) -> GrantResult:
		grant_call_count += 1
		return super.grant_part(part_id)


class SignalRecorder:
	extends RefCounted
	var events: Array[String] = []
	var state_changes: Array[Vector2i] = []
	var question_ids: Array[String] = []
	var question_numbers: Array[int] = []
	var question_totals: Array[int] = []
	var wrong_messages: Array[String] = []
	var correct_question_ids: Array[String] = []
	var assembly_part_ids: Array[String] = []
	var snap_part_ids: Array[String] = []
	var component_ids: Array[String] = []
	var final_train_ids: Array[String] = []
	var end_train_names: Array[String] = []
	var error_codes: Array[String] = []
	var error_messages: Array[String] = []
	var exit_call_count: int = 0

	func connect_to(flow: GameFlowManager) -> void:
		flow.state_changed.connect(Callable(self, "_on_state_changed"))
		flow.question_presented.connect(Callable(self, "_on_question_presented"))
		flow.wrong_feedback_requested.connect(Callable(self, "_on_wrong_feedback_requested"))
		flow.correct_feedback_requested.connect(Callable(self, "_on_correct_feedback_requested"))
		flow.assembly_preparation_requested.connect(
			Callable(self, "_on_assembly_preparation_requested")
		)
		flow.snap_animation_requested.connect(Callable(self, "_on_snap_animation_requested"))
		flow.component_animation_requested.connect(
			Callable(self, "_on_component_animation_requested")
		)
		flow.final_animation_requested.connect(Callable(self, "_on_final_animation_requested"))
		flow.end_view_requested.connect(Callable(self, "_on_end_view_requested"))
		flow.recoverable_error_occurred.connect(Callable(self, "_on_recoverable_error_occurred"))

	func record_exit() -> void:
		exit_call_count += 1
		events.append("exit")

	func _on_state_changed(
		previous: GameFlowManager.GameState,
		current: GameFlowManager.GameState,
	) -> void:
		state_changes.append(Vector2i(previous, current))
		events.append(
			(
				"state:%s>%s"
				% [
					GameFlowManager.GameState.keys()[previous],
					GameFlowManager.GameState.keys()[current]
				]
			)
		)

	func _on_question_presented(
		question: QuestionData,
		current_number: int,
		total: int,
	) -> void:
		question_ids.append(question.question_id)
		question_numbers.append(current_number)
		question_totals.append(total)
		events.append("question:%s:%d/%d" % [question.question_id, current_number, total])

	func _on_wrong_feedback_requested(message: String) -> void:
		wrong_messages.append(message)
		events.append("wrong:%s" % message)

	func _on_correct_feedback_requested(question: QuestionData) -> void:
		correct_question_ids.append(question.question_id)
		events.append("correct:%s" % question.question_id)

	func _on_assembly_preparation_requested(part: PartData) -> void:
		assembly_part_ids.append(part.part_id)
		events.append("assembly:%s" % part.part_id)

	func _on_snap_animation_requested(part_id: String) -> void:
		snap_part_ids.append(part_id)
		events.append("snap:%s" % part_id)

	func _on_component_animation_requested(component: ComponentRecipe) -> void:
		component_ids.append(component.component_id)
		events.append("component:%s" % component.component_id)

	func _on_final_animation_requested(train_recipe: TrainRecipe) -> void:
		final_train_ids.append(train_recipe.train_id)
		events.append("final:%s" % train_recipe.train_id)

	func _on_end_view_requested(train_name: String) -> void:
		end_train_names.append(train_name)
		events.append("end:%s" % train_name)

	func _on_recoverable_error_occurred(code: String, message: String) -> void:
		error_codes.append(code)
		error_messages.append(message)
		events.append("error:%s" % code)


static func catalog(first_reward_override: String = "") -> ContentCatalog:
	return (
		ContentCatalog
		. new(
			questions(first_reward_override),
			parts(),
			components(),
			train_recipe(),
		)
	)


static func questions(first_reward_override: String = "") -> Array[QuestionData]:
	var result: Array[QuestionData] = []
	for index: int in PART_IDS.size():
		var reward_part_id: String = PART_IDS[index]
		if index == 0 and not first_reward_override.is_empty():
			reward_part_id = first_reward_override
		(
			result
			. append(
				(
					QuestionData
					. new(
						"q%02d" % (index + 1),
						index + 1,
						"测试题目 %d" % (index + 1),
						["选项甲", "选项乙", "选项丙", "选项丁"],
						index % 4,
						"测试解析 %d" % (index + 1),
						(
							SourceData
							. new(
								"测试机构",
								"测试资料 %d" % (index + 1),
								"https://example.invalid/q%02d" % (index + 1),
							)
						),
						reward_part_id,
					)
				)
			)
		)
	return result


static func parts() -> Array[PartData]:
	var result: Array[PartData] = []
	for index: int in PART_IDS.size():
		var prerequisite_id: String = "" if index == 0 else PART_IDS[index - 1]
		(
			result
			. append(
				(
					PartData
					. new(
						PART_IDS[index],
						"测试零件 %d" % (index + 1),
						index + 1,
						COMPONENT_IDS[index / 3],
						"res://tests/fixtures/%s.tscn" % PART_IDS[index],
						"SnapTargets/%s" % PART_IDS[index],
						TransformData.new(),
						TransformData.new(),
						prerequisite_id,
					)
				)
			)
		)
	return result


static func components() -> Array[ComponentRecipe]:
	var result: Array[ComponentRecipe] = []
	for index: int in COMPONENT_IDS.size():
		var first_part_index: int = index * 3
		var component_part_ids: Array[String] = [
			PART_IDS[first_part_index],
			PART_IDS[first_part_index + 1],
			PART_IDS[first_part_index + 2],
		]
		(
			result
			. append(
				(
					ComponentRecipe
					. new(
						COMPONENT_IDS[index],
						"测试组件 %d" % (index + 1),
						index + 1,
						component_part_ids,
						"组件完成 %d" % (index + 1),
						"教学说明 %d" % (index + 1),
					)
				)
			)
		)
	return result


static func train_recipe() -> TrainRecipe:
	return TrainRecipe.new(TRAIN_ID, TRAIN_NAME, COMPONENT_IDS.duplicate())


static func wrong_option(question: QuestionData) -> int:
	return (question.correct_option_index + 1) % question.options.size()
