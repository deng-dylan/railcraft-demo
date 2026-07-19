class_name AssemblyAssetValidatorFixture
extends RefCounted

const FIXTURE_DIR: String = "res://tests/fixtures/assembly_asset_validator/"
const VALID_PART_PATH: String = FIXTURE_DIR + "valid_part_actor.tscn"
const TRAIN_ROOT_PATH: String = FIXTURE_DIR + "train_assembly_root.tscn"


static func catalog(
	model_scene_path: String = VALID_PART_PATH,
	snap_target_path: String = "SnapTargets/ValidTarget",
) -> ContentCatalog:
	var parts: Array[PartData] = [
		(
			PartData
			. new(
				"fixture_part",
				"测试零件",
				1,
				"fixture_component",
				model_scene_path,
				snap_target_path,
				TransformData.new(),
				TransformData.new(),
			)
		),
	]
	var components: Array[ComponentRecipe] = [
		(
			ComponentRecipe
			. new(
				"fixture_component",
				"测试组件",
				1,
				["fixture_part"],
				"完成",
				"测试说明",
			)
		),
	]
	var train := TrainRecipe.new("fixture_train", "测试列车", ["fixture_component"])
	return ContentCatalog.new([], parts, components, train)


static func empty_catalog() -> ContentCatalog:
	return ContentCatalog.new([], [], [], TrainRecipe.new("empty_train", "空列车", []))


static func instantiate_train_root() -> Node3D:
	var scene: PackedScene = load(TRAIN_ROOT_PATH) as PackedScene
	return scene.instantiate() as Node3D
