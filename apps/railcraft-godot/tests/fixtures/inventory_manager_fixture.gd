class_name InventoryManagerFixture
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


static func minimal_parts() -> Array[PartData]:
	return [
		_part("part_one", 1),
		_part("part_two", 2, "part_one"),
	]


static func first_release_parts() -> Array[PartData]:
	var parts: Array[PartData] = []
	for index: int in FIRST_RELEASE_PART_IDS.size():
		var dependency: String = "" if index == 0 else FIRST_RELEASE_PART_IDS[index - 1]
		parts.append(_part(FIRST_RELEASE_PART_IDS[index], index + 1, dependency))
	return parts


static func _part(part_id: String, order: int, dependency: String = "") -> PartData:
	return (
		PartData
		. new(
			part_id,
			"零件 %d" % order,
			order,
			"test_component",
			"res://tests/fixtures/%s.tscn" % part_id,
			"SnapTargets/%s" % part_id,
			TransformData.new(),
			TransformData.new(),
			dependency,
		)
	)
