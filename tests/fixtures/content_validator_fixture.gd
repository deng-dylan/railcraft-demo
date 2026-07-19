class_name ContentValidatorFixture
extends RefCounted

const FIRST_RELEASE_PART_IDS: Array[String] = [
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
const FIRST_RELEASE_COMPONENT_IDS: Array[String] = [
	"carbody_connection",
	"running_braking",
	"traction_power",
]


static func valid_questions() -> Dictionary:
	return {
		"schema_version": 1,
		"questions":
		[
			_question("q01", 1, "part_one"),
			_question("q02", 2, "part_two"),
			_question("q03", 3, "part_three"),
		],
	}


static func valid_parts() -> Dictionary:
	return {
		"schema_version": 1,
		"parts":
		[
			_part("part_one", 1, null),
			_part("part_two", 2, "part_one"),
			_part("part_three", 3, "part_two"),
		],
	}


static func valid_recipes() -> Dictionary:
	return {
		"schema_version": 1,
		"components":
		[
			{
				"component_id": "component_one",
				"display_name": "组件一",
				"order": 1,
				"part_ids": ["part_one", "part_two", "part_three"],
				"completion_message": "组件完成",
				"teaching_note": "教学说明",
			}
		],
		"train_recipe":
		{
			"train_id": "train_one",
			"display_name": "测试列车",
			"component_ids": ["component_one"],
		},
	}


static func valid_first_release_questions() -> Dictionary:
	var questions: Array[Dictionary] = []
	for index: int in FIRST_RELEASE_PART_IDS.size():
		questions.append(_question("q%02d" % (index + 1), index + 1, FIRST_RELEASE_PART_IDS[index]))
	return {"schema_version": 1, "questions": questions}


static func valid_first_release_parts() -> Dictionary:
	var parts: Array[Dictionary] = []
	for index: int in FIRST_RELEASE_PART_IDS.size():
		var dependency: Variant = null if index == 0 else FIRST_RELEASE_PART_IDS[index - 1]
		(
			parts
			. append(
				_part(
					FIRST_RELEASE_PART_IDS[index],
					index + 1,
					dependency,
					FIRST_RELEASE_COMPONENT_IDS[index / 3],
				)
			)
		)
	return {"schema_version": 1, "parts": parts}


static func valid_first_release_recipes() -> Dictionary:
	var components: Array[Dictionary] = []
	for index: int in FIRST_RELEASE_COMPONENT_IDS.size():
		var first_part_index: int = index * 3
		(
			components
			. append(
				{
					"component_id": FIRST_RELEASE_COMPONENT_IDS[index],
					"display_name": "首版组件 %d" % (index + 1),
					"order": index + 1,
					"part_ids":
					[
						FIRST_RELEASE_PART_IDS[first_part_index],
						FIRST_RELEASE_PART_IDS[first_part_index + 1],
						FIRST_RELEASE_PART_IDS[first_part_index + 2],
					],
					"completion_message": "首版组件完成",
					"teaching_note": "首版教学说明",
				}
			)
		)
	return {
		"schema_version": 1,
		"components": components,
		"train_recipe":
		{
			"train_id": "generic_high_speed_emu",
			"display_name": "通用高速电力动车组",
			"component_ids": FIRST_RELEASE_COMPONENT_IDS.duplicate(),
		},
	}


static func _question(question_id: String, order: int, reward_part_id: String) -> Dictionary:
	return {
		"question_id": question_id,
		"order": order,
		"prompt": "测试题目 %d" % order,
		"options": ["选项甲", "选项乙", "选项丙", "选项丁"],
		"correct_option_index": 0,
		"explanation": "测试解析",
		"source":
		{
			"organization": "测试机构",
			"title": "测试资料",
			"url": "https://example.invalid/source",
		},
		"reward_part_id": reward_part_id,
	}


static func _part(
	part_id: String,
	order: int,
	dependency: Variant,
	component_id: String = "component_one",
) -> Dictionary:
	return {
		"part_id": part_id,
		"display_name": "零件 %d" % order,
		"order": order,
		"component_id": component_id,
		"model_scene_path": "res://tests/fixtures/%s.tscn" % part_id,
		"snap_target_path": "SnapTargets/%s" % part_id,
		"target_transform": _transform(),
		"preview_transform": _transform(),
		"required_previous_part_id": dependency,
	}


static func _transform() -> Dictionary:
	return {
		"position": [0.0, 0.0, 0.0],
		"rotation_degrees": [0.0, 0.0, 0.0],
		"scale": [1.0, 1.0, 1.0],
	}
