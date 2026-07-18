class_name AssemblyManagerFixture
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


static func parts() -> Array[PartData]:
	var result: Array[PartData] = []
	for index: int in PART_IDS.size():
		var prerequisite: String = "" if index == 0 else PART_IDS[index - 1]
		(
			result
			. append(
				_part(
					PART_IDS[index],
					index + 1,
					COMPONENT_IDS[index / 3],
					prerequisite,
				)
			)
		)
	return result


static func parts_with_first_prerequisite(prerequisite_id: String) -> Array[PartData]:
	var result: Array[PartData] = parts()
	result[0] = _part(PART_IDS[0], 1, COMPONENT_IDS[0], prerequisite_id)
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
						"组件完成",
						"教学说明",
					)
				)
			)
		)
	return result


static func train_recipe() -> TrainRecipe:
	return TrainRecipe.new("test_train", "测试列车", COMPONENT_IDS.duplicate())


static func _part(
	part_id: String,
	order: int,
	component_id: String,
	prerequisite_id: String,
) -> PartData:
	return (
		PartData
		. new(
			part_id,
			"测试零件 %d" % order,
			order,
			component_id,
			"res://tests/fixtures/%s.tscn" % part_id,
			"SnapTargets/%s" % part_id,
			TransformData.new(),
			TransformData.new(),
			prerequisite_id,
		)
	)
