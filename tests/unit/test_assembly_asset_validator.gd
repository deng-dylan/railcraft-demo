extends GutTest

const FIXTURE_DIR: String = AssemblyAssetValidatorFixture.FIXTURE_DIR


func test_empty_part_collection_returns_no_issues_without_a_train_root() -> void:
	var issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
		AssemblyAssetValidatorFixture.empty_catalog(), null
	)

	assert_true(issues.is_empty())


func test_minimal_fixture_loads_and_passes_in_headless_mode() -> void:
	assert_true(ResourceLoader.exists(AssemblyAssetValidatorFixture.VALID_PART_PATH))
	assert_true(ResourceLoader.exists(AssemblyAssetValidatorFixture.TRAIN_ROOT_PATH))
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()

	var issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
		AssemblyAssetValidatorFixture.catalog(), root
	)

	assert_true(issues.is_empty(), _issue_summary(issues))
	root.free()


func test_missing_model_path_has_stable_error_code_and_field_path() -> void:
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()
	var missing_path: String = FIXTURE_DIR + "missing_part.tscn"

	var issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
		AssemblyAssetValidatorFixture.catalog(missing_path), root
	)

	assert_true(
		_has_issue(
			issues,
			AssemblyAssetValidator.MODEL_SCENE_MISSING,
			"$.parts[fixture_part].model_scene_path",
		)
	)
	root.free()


func test_non_scene_resource_is_rejected_before_instantiation() -> void:
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()
	var resource_path: String = FIXTURE_DIR + "wrong_resource_type.tres"

	var issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
		AssemblyAssetValidatorFixture.catalog(resource_path), root
	)

	assert_true(
		_has_code(issues, AssemblyAssetValidator.MODEL_SCENE_TYPE_INVALID),
		_issue_summary(issues),
	)
	root.free()


func test_scene_root_must_inherit_node3d() -> void:
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()
	var scene_path: String = FIXTURE_DIR + "wrong_root_type.tscn"

	var issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
		AssemblyAssetValidatorFixture.catalog(scene_path), root
	)

	assert_true(
		_has_issue(
			issues,
			AssemblyAssetValidator.MODEL_ROOT_TYPE_INVALID,
			scene_path + "::.",
		),
		_issue_summary(issues),
	)
	root.free()


func test_part_actor_root_requires_script_and_public_capabilities() -> void:
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()
	var no_script_path: String = FIXTURE_DIR + "missing_script.tscn"
	var no_capability_path: String = FIXTURE_DIR + "missing_capability.tscn"

	var no_script_issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
		AssemblyAssetValidatorFixture.catalog(no_script_path), root
	)
	var no_capability_issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
		AssemblyAssetValidatorFixture.catalog(no_capability_path), root
	)

	assert_true(
		_has_code(no_script_issues, AssemblyAssetValidator.PART_ACTOR_SCRIPT_MISSING),
		_issue_summary(no_script_issues),
	)
	assert_true(
		_has_code(no_capability_issues, AssemblyAssetValidator.PART_ACTOR_CAPABILITY_MISSING),
		_issue_summary(no_capability_issues),
	)
	root.free()


func test_missing_contract_nodes_report_their_exact_asset_paths() -> void:
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()
	var cases: Array[Dictionary] = [
		{
			"scene": "missing_visual_root.tscn",
			"code": AssemblyAssetValidator.VISUAL_ROOT_MISSING,
			"node": "VisualRoot",
		},
		{
			"scene": "missing_click_area.tscn",
			"code": AssemblyAssetValidator.CLICK_AREA_MISSING,
			"node": "ClickArea",
		},
		{
			"scene": "missing_collision_shape.tscn",
			"code": AssemblyAssetValidator.COLLISION_SHAPE_MISSING,
			"node": "ClickArea/CollisionShape3D",
		},
	]

	for case: Dictionary in cases:
		var scene_path: String = FIXTURE_DIR + (case["scene"] as String)
		var issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
			AssemblyAssetValidatorFixture.catalog(scene_path), root
		)
		assert_true(
			_has_issue(issues, case["code"] as String, scene_path + "::" + case["node"]),
			_issue_summary(issues),
		)

	root.free()


func test_contract_nodes_must_have_the_required_types() -> void:
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()
	var cases: Array[Dictionary] = [
		{
			"scene": "wrong_visual_root_type.tscn",
			"code": AssemblyAssetValidator.VISUAL_ROOT_TYPE_INVALID,
		},
		{
			"scene": "wrong_click_area_type.tscn",
			"code": AssemblyAssetValidator.CLICK_AREA_TYPE_INVALID,
		},
		{
			"scene": "wrong_collision_shape_type.tscn",
			"code": AssemblyAssetValidator.COLLISION_SHAPE_TYPE_INVALID,
		},
	]

	for case: Dictionary in cases:
		var scene_path: String = FIXTURE_DIR + (case["scene"] as String)
		var issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
			AssemblyAssetValidatorFixture.catalog(scene_path), root
		)
		assert_true(
			_has_code(issues, case["code"] as String),
			_issue_summary(issues),
		)

	root.free()


func test_snap_target_must_exist_and_be_a_marker3d() -> void:
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()

	var missing_issues: Array[ValidationIssue] = (
		AssemblyAssetValidator
		. new()
		. validate(
			(
				AssemblyAssetValidatorFixture
				. catalog(
					AssemblyAssetValidatorFixture.VALID_PART_PATH,
					"SnapTargets/MissingTarget",
				)
			),
			root,
		)
	)
	var wrong_type_issues: Array[ValidationIssue] = (
		AssemblyAssetValidator
		. new()
		. validate(
			(
				AssemblyAssetValidatorFixture
				. catalog(
					AssemblyAssetValidatorFixture.VALID_PART_PATH,
					"SnapTargets/WrongTarget",
				)
			),
			root,
		)
	)

	assert_true(
		_has_issue(
			missing_issues,
			AssemblyAssetValidator.SNAP_TARGET_MISSING,
			"TrainAssemblyRoot/SnapTargets/MissingTarget",
		),
		_issue_summary(missing_issues),
	)
	assert_true(
		_has_issue(
			wrong_type_issues,
			AssemblyAssetValidator.SNAP_TARGET_TYPE_INVALID,
			"TrainAssemblyRoot/SnapTargets/WrongTarget",
		),
		_issue_summary(wrong_type_issues),
	)
	root.free()


func test_repeated_validation_does_not_add_or_retain_scene_tree_nodes() -> void:
	var root: Node3D = AssemblyAssetValidatorFixture.instantiate_train_root()
	add_child_autofree(root)
	var tree_node_count_before: int = _count_tree_nodes(get_tree().root)

	for iteration: int in 20:
		var issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
			AssemblyAssetValidatorFixture.catalog(), root
		)
		assert_true(issues.is_empty(), "Iteration %d: %s" % [iteration, _issue_summary(issues)])

	assert_eq(_count_tree_nodes(get_tree().root), tree_node_count_before)


func _has_code(issues: Array[ValidationIssue], code: String) -> bool:
	for issue: ValidationIssue in issues:
		if issue.code == code:
			return true
	return false


func _has_issue(issues: Array[ValidationIssue], code: String, path: String) -> bool:
	for issue: ValidationIssue in issues:
		if issue.code == code and issue.json_path == path:
			return true
	return false


func _issue_summary(issues: Array[ValidationIssue]) -> String:
	var summaries: Array[String] = []
	for issue: ValidationIssue in issues:
		summaries.append("%s at %s: %s" % [issue.code, issue.json_path, issue.message])
	return "; ".join(summaries)


func _count_tree_nodes(node: Node) -> int:
	var count: int = 1
	for child: Node in node.get_children():
		count += _count_tree_nodes(child)
	return count
