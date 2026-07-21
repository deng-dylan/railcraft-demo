class_name AssemblyViewFixture
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
const SCENE_PATHS: Array[String] = [
	"res://scenes/train/parts/body_shell.tscn",
	"res://scenes/train/parts/passenger_door.tscn",
	"res://scenes/train/parts/coupler_buffer.tscn",
	"res://scenes/train/parts/bogie_frame.tscn",
	"res://scenes/train/parts/wheelset.tscn",
	"res://scenes/train/parts/brake_unit.tscn",
	"res://scenes/train/parts/pantograph.tscn",
	"res://scenes/train/parts/traction_converter_unit.tscn",
	"res://scenes/train/parts/traction_motor.tscn",
]
const TARGET_PATHS: Array[String] = [
	"SnapTargets/BodyShellTarget",
	"SnapTargets/PassengerDoorTarget",
	"SnapTargets/CouplerBufferTarget",
	"SnapTargets/BogieFrameTarget",
	"SnapTargets/WheelsetTarget",
	"SnapTargets/BrakeUnitTarget",
	"SnapTargets/PantographTarget",
	"SnapTargets/TractionConverterTarget",
	"SnapTargets/TractionMotorTarget",
]
const COMPONENT_IDS: Array[String] = [
	"carbody_connection",
	"carbody_connection",
	"carbody_connection",
	"running_braking",
	"running_braking",
	"running_braking",
	"traction_power",
	"traction_power",
	"traction_power",
]


static func instantiate_view() -> AssemblyView:
	var scene: PackedScene = load("res://scenes/assembly/assembly_view.tscn") as PackedScene
	return scene.instantiate() as AssemblyView


static func instantiate_train_root() -> Node3D:
	var scene: PackedScene = load("res://scenes/train/train_root.tscn") as PackedScene
	return scene.instantiate() as Node3D


static func part(index: int) -> PartData:
	return (
		PartData
		. new(
			PART_IDS[index],
			"测试零件 %d" % (index + 1),
			index + 1,
			COMPONENT_IDS[index],
			SCENE_PATHS[index],
			TARGET_PATHS[index],
			TransformData.new(),
			TransformData.new(),
			"" if index == 0 else PART_IDS[index - 1],
		)
	)


static func transformed_body() -> PartData:
	return (
		PartData
		. new(
			PART_IDS[0],
			"变换车体",
			1,
			COMPONENT_IDS[0],
			SCENE_PATHS[0],
			TARGET_PATHS[0],
			TransformData.new(Vector3(0.25, 0.5, -0.2), Vector3(0, 12, 0)),
			TransformData.new(Vector3(0.4, 0.3, -0.2), Vector3(0, 20, 0)),
		)
	)


static func body_with_target(target_path: String) -> PartData:
	var result: PartData = part(0)
	result.snap_target_path = target_path
	return result
