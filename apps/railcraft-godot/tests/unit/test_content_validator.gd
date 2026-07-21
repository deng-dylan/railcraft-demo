extends GutTest

const Fixture := preload("res://tests/fixtures/content_validator_fixture.gd")

var _validator: ContentValidator


func before_each() -> void:
	_validator = ContentValidator.new()


func test_validation_issue_constructor_preserves_typed_fields() -> void:
	var issue := ValidationIssue.new(
		"FIELD_MISSING", "$.questions[0].prompt", "Prompt is required."
	)

	assert_eq(issue.code, "FIELD_MISSING")
	assert_eq(issue.json_path, "$.questions[0].prompt")
	assert_eq(issue.message, "Prompt is required.")
	assert_eq(issue.severity, ValidationIssue.Severity.ERROR)


func test_valid_catalog_has_no_issues_and_validation_is_idempotent() -> void:
	var first: Array[ValidationIssue] = _validate_valid()
	var second: Array[ValidationIssue] = _validate_valid()

	assert_eq(first.size(), 0)
	assert_eq(_signatures(first), _signatures(second))


func test_valid_first_release_nine_part_catalog_has_no_issues() -> void:
	var issues: Array[ValidationIssue] = (
		_validator
		. validate(
			Fixture.valid_first_release_questions(),
			Fixture.valid_first_release_parts(),
			Fixture.valid_first_release_recipes(),
		)
	)

	assert_eq(issues.size(), 0)


func test_empty_roots_aggregate_locatable_issues_without_throwing() -> void:
	var issues: Array[ValidationIssue] = _validator.validate({}, {}, {})

	assert_true(issues.size() >= 9)
	assert_true(_has_issue(issues, "CONTENT_FILE_MISSING", "$"))
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.schema_version"))
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.questions"))
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.parts"))
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.components"))
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.train_recipe"))


func test_root_schema_and_container_errors_are_reported() -> void:
	var questions: Dictionary = Fixture.valid_questions()
	var parts: Dictionary = Fixture.valid_parts()
	var recipes: Dictionary = Fixture.valid_recipes()
	questions.erase("schema_version")
	parts["schema_version"] = 2
	recipes["components"] = "invalid"

	var issues: Array[ValidationIssue] = _validator.validate(questions, parts, recipes)

	assert_true(_has_issue(issues, "FIELD_MISSING", "$.schema_version"))
	assert_true(_has_issue(issues, "FIELD_TYPE_INVALID", "$.schema_version"))
	assert_true(_has_issue(issues, "FIELD_TYPE_INVALID", "$.components"))


func test_question_required_types_and_local_rules_aggregate() -> void:
	var questions: Dictionary = Fixture.valid_questions()
	var question: Dictionary = questions["questions"][0]
	question.erase("prompt")
	question["options"] = ["", "乙", "丙"]
	question["correct_option_index"] = 3
	question["explanation"] = 42
	question["source"] = {"organization": "", "title": "资料"}
	question["reward_part_id"] = ""

	var issues: Array[ValidationIssue] = _validator.validate(
		questions, Fixture.valid_parts(), Fixture.valid_recipes()
	)

	assert_true(_has_issue(issues, "FIELD_MISSING", "$.questions[0].prompt"))
	assert_true(_has_issue(issues, "FIELD_VALUE_INVALID", "$.questions[0].options"))
	assert_true(_has_issue(issues, "FIELD_VALUE_INVALID", "$.questions[0].options[0]"))
	assert_true(_has_issue(issues, "FIELD_VALUE_INVALID", "$.questions[0].correct_option_index"))
	assert_true(_has_issue(issues, "FIELD_TYPE_INVALID", "$.questions[0].explanation"))
	assert_true(_has_issue(issues, "FIELD_VALUE_INVALID", "$.questions[0].source.organization"))
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.questions[0].source.url"))
	assert_true(_has_issue(issues, "FIELD_VALUE_INVALID", "$.questions[0].reward_part_id"))


func test_part_fields_and_transform_shape_are_checked() -> void:
	var parts: Dictionary = Fixture.valid_parts()
	var part: Dictionary = parts["parts"][1]
	part.erase("display_name")
	part["order"] = "two"
	part["model_scene_path"] = "tests/part.tscn"
	part["target_transform"] = {
		"position": [0.0, 0.0],
		"rotation_degrees": [0.0, "bad", 0.0],
	}
	part["required_previous_part_id"] = 7

	var issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), parts, Fixture.valid_recipes()
	)

	assert_true(_has_issue(issues, "FIELD_MISSING", "$.parts[1].display_name"))
	assert_true(_has_issue(issues, "FIELD_TYPE_INVALID", "$.parts[1].order"))
	assert_true(_has_issue(issues, "FIELD_VALUE_INVALID", "$.parts[1].model_scene_path"))
	assert_true(_has_issue(issues, "FIELD_VALUE_INVALID", "$.parts[1].target_transform.position"))
	assert_true(
		_has_issue(issues, "FIELD_TYPE_INVALID", "$.parts[1].target_transform.rotation_degrees[1]")
	)
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.parts[1].target_transform.scale"))
	assert_true(_has_issue(issues, "FIELD_TYPE_INVALID", "$.parts[1].required_previous_part_id"))


func test_component_and_train_required_fields_are_checked() -> void:
	var recipes: Dictionary = Fixture.valid_recipes()
	var component: Dictionary = recipes["components"][0]
	component.erase("completion_message")
	component["part_ids"] = ["part_one", 2, ""]
	var train_recipe: Dictionary = recipes["train_recipe"]
	train_recipe.erase("display_name")
	train_recipe["component_ids"] = "component_one"

	var issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), Fixture.valid_parts(), recipes
	)

	assert_true(_has_issue(issues, "FIELD_MISSING", "$.components[0].completion_message"))
	assert_true(_has_issue(issues, "FIELD_TYPE_INVALID", "$.components[0].part_ids[1]"))
	assert_true(_has_issue(issues, "FIELD_VALUE_INVALID", "$.components[0].part_ids[2]"))
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.train_recipe.display_name"))
	assert_true(_has_issue(issues, "FIELD_TYPE_INVALID", "$.train_recipe.component_ids"))


func test_id_format_and_every_duplicate_location_are_reported() -> void:
	var questions: Dictionary = Fixture.valid_questions()
	var parts: Dictionary = Fixture.valid_parts()
	var recipes: Dictionary = Fixture.valid_recipes()
	questions["questions"][0]["question_id"] = "Bad-ID"
	questions["questions"][2]["question_id"] = "q02"
	parts["parts"][2]["part_id"] = "part_two"
	recipes["components"].append(recipes["components"][0].duplicate(true))
	recipes["components"][1]["order"] = 2

	var issues: Array[ValidationIssue] = _validator.validate(questions, parts, recipes)

	assert_true(_has_issue(issues, "ID_FORMAT_INVALID", "$.questions[0].question_id"))
	assert_true(_has_issue(issues, "ID_DUPLICATE", "$.questions[1].question_id"))
	assert_true(_has_issue(issues, "ID_DUPLICATE", "$.questions[2].question_id"))
	assert_true(_has_issue(issues, "ID_DUPLICATE", "$.parts[1].part_id"))
	assert_true(_has_issue(issues, "ID_DUPLICATE", "$.parts[2].part_id"))
	assert_true(_has_issue(issues, "ID_DUPLICATE", "$.components[0].component_id"))
	assert_true(_has_issue(issues, "ID_DUPLICATE", "$.components[1].component_id"))


func test_all_cross_file_reference_kinds_are_checked() -> void:
	var questions: Dictionary = Fixture.valid_questions()
	var parts: Dictionary = Fixture.valid_parts()
	var recipes: Dictionary = Fixture.valid_recipes()
	questions["questions"][0]["reward_part_id"] = "missing_reward"
	parts["parts"][0]["component_id"] = "missing_component"
	parts["parts"][1]["required_previous_part_id"] = "missing_dependency"
	recipes["components"][0]["part_ids"][0] = "missing_recipe_part"
	recipes["train_recipe"]["component_ids"][0] = "missing_train_component"

	var issues: Array[ValidationIssue] = _validator.validate(questions, parts, recipes)

	assert_true(_has_issue(issues, "REFERENCE_NOT_FOUND", "$.questions[0].reward_part_id"))
	assert_true(_has_issue(issues, "REFERENCE_NOT_FOUND", "$.parts[0].component_id"))
	assert_true(_has_issue(issues, "REFERENCE_NOT_FOUND", "$.parts[1].required_previous_part_id"))
	assert_true(_has_issue(issues, "REFERENCE_NOT_FOUND", "$.components[0].part_ids[0]"))
	assert_true(_has_issue(issues, "REFERENCE_NOT_FOUND", "$.train_recipe.component_ids[0]"))


func test_reward_and_component_recipe_coverage_report_missing_and_duplicate_parts() -> void:
	var questions: Dictionary = Fixture.valid_questions()
	var recipes: Dictionary = Fixture.valid_recipes()
	questions["questions"][1]["reward_part_id"] = "part_one"
	recipes["components"][0]["part_ids"] = ["part_one", "part_one", "part_three"]

	var issues: Array[ValidationIssue] = _validator.validate(
		questions, Fixture.valid_parts(), recipes
	)

	assert_true(_has_issue(issues, "CONTENT_COVERAGE_INVALID", "$.parts[0].part_id"))
	assert_true(_has_issue(issues, "CONTENT_COVERAGE_INVALID", "$.parts[1].part_id"))


func test_part_declared_component_must_match_its_recipe_owner() -> void:
	var recipes: Dictionary = Fixture.valid_recipes()
	(
		recipes["components"]
		. append(
			{
				"component_id": "component_two",
				"display_name": "组件二",
				"order": 2,
				"part_ids": [],
				"completion_message": "完成",
				"teaching_note": "说明",
			}
		)
	)
	recipes["train_recipe"]["component_ids"].append("component_two")
	var parts: Dictionary = Fixture.valid_parts()
	parts["parts"][0]["component_id"] = "component_two"

	var issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), parts, recipes
	)

	assert_true(_has_issue(issues, "CONTENT_COVERAGE_INVALID", "$.parts[0].component_id"))


func test_order_gaps_array_disorder_and_reward_order_mismatch_are_reported() -> void:
	var questions: Dictionary = Fixture.valid_questions()
	var parts: Dictionary = Fixture.valid_parts()
	var recipes: Dictionary = Fixture.valid_recipes()
	questions["questions"][1]["order"] = 3
	parts["parts"][0]["order"] = 2
	recipes["components"][0]["order"] = 2

	var issues: Array[ValidationIssue] = _validator.validate(questions, parts, recipes)

	assert_true(_has_issue(issues, "ORDER_INVALID", "$.questions[1].order"))
	assert_true(_has_issue(issues, "ORDER_INVALID", "$.parts[0].order"))
	assert_true(_has_issue(issues, "ORDER_INVALID", "$.components[0].order"))
	assert_true(_has_issue(issues, "ORDER_INVALID", "$.questions[0].reward_part_id"))


func test_train_recipe_must_cover_each_component_exactly_once() -> void:
	var recipes: Dictionary = Fixture.valid_recipes()
	(
		recipes["components"]
		. append(
			{
				"component_id": "component_two",
				"display_name": "组件二",
				"order": 2,
				"part_ids": [],
				"completion_message": "完成",
				"teaching_note": "说明",
			}
		)
	)
	recipes["train_recipe"]["component_ids"] = ["component_one", "component_one"]

	var issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), Fixture.valid_parts(), recipes
	)

	assert_true(_has_issue(issues, "CONTENT_COVERAGE_INVALID", "$.train_recipe.component_ids"))
	assert_true(_count_code(issues, "CONTENT_COVERAGE_INVALID") >= 2)


func test_dependency_chain_aggregates_first_null_and_skipped_edge_errors() -> void:
	var parts: Dictionary = Fixture.valid_parts()
	parts["parts"][0]["required_previous_part_id"] = "part_two"
	parts["parts"][1]["required_previous_part_id"] = null
	parts["parts"][2]["required_previous_part_id"] = "part_one"

	var issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), parts, Fixture.valid_recipes()
	)

	assert_eq(_count_code(issues, "DEPENDENCY_CHAIN_INVALID"), 3)
	assert_true(
		_has_issue(
			issues,
			"DEPENDENCY_CHAIN_INVALID",
			"$.parts[0].required_previous_part_id",
		)
	)
	assert_true(
		_has_issue(
			issues,
			"DEPENDENCY_CHAIN_INVALID",
			"$.parts[1].required_previous_part_id",
		)
	)
	assert_true(
		_has_issue(
			issues,
			"DEPENDENCY_CHAIN_INVALID",
			"$.parts[2].required_previous_part_id",
		)
	)


func test_dependency_chain_uses_order_fields_when_part_array_is_reordered() -> void:
	var parts: Dictionary = Fixture.valid_parts()
	var part_items: Array = parts["parts"]
	var first_part: Variant = part_items[0]
	part_items[0] = part_items[1]
	part_items[1] = first_part

	var valid_chain_issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), parts, Fixture.valid_recipes()
	)
	assert_eq(_count_code(valid_chain_issues, "DEPENDENCY_CHAIN_INVALID"), 0)

	part_items[0]["required_previous_part_id"] = "part_three"
	var invalid_chain_issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), parts, Fixture.valid_recipes()
	)
	assert_true(
		_has_issue(
			invalid_chain_issues,
			"DEPENDENCY_CHAIN_INVALID",
			"$.parts[0].required_previous_part_id",
		)
	)


func test_dependency_self_cycle_is_reported_at_stable_path() -> void:
	var parts: Dictionary = Fixture.valid_parts()
	parts["parts"][0]["required_previous_part_id"] = "part_one"

	var issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), parts, Fixture.valid_recipes()
	)

	assert_true(_has_issue(issues, "DEPENDENCY_CYCLE", "$.parts[0].required_previous_part_id"))


func test_dependency_two_node_and_long_cycles_are_deterministic() -> void:
	var two_node_parts: Dictionary = Fixture.valid_parts()
	two_node_parts["parts"][0]["required_previous_part_id"] = "part_two"
	two_node_parts["parts"][1]["required_previous_part_id"] = "part_one"
	var two_node_issues: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), two_node_parts, Fixture.valid_recipes()
	)

	var long_cycle_parts: Dictionary = Fixture.valid_parts()
	long_cycle_parts["parts"][0]["required_previous_part_id"] = "part_three"
	var first: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), long_cycle_parts, Fixture.valid_recipes()
	)
	var second: Array[ValidationIssue] = _validator.validate(
		Fixture.valid_questions(), long_cycle_parts, Fixture.valid_recipes()
	)

	assert_eq(_count_code(two_node_issues, "DEPENDENCY_CYCLE"), 1)
	assert_eq(_count_code(first, "DEPENDENCY_CYCLE"), 1)
	assert_eq(_signatures(first), _signatures(second))


func test_multiple_independent_problems_are_returned_together() -> void:
	var questions: Dictionary = Fixture.valid_questions()
	var parts: Dictionary = Fixture.valid_parts()
	questions["questions"][0]["question_id"] = "INVALID-ID"
	questions["questions"][1].erase("prompt")
	parts["parts"][2]["required_previous_part_id"] = "missing"

	var issues: Array[ValidationIssue] = _validator.validate(
		questions, parts, Fixture.valid_recipes()
	)

	assert_true(_has_issue(issues, "ID_FORMAT_INVALID", "$.questions[0].question_id"))
	assert_true(_has_issue(issues, "FIELD_MISSING", "$.questions[1].prompt"))
	assert_true(_has_issue(issues, "REFERENCE_NOT_FOUND", "$.parts[2].required_previous_part_id"))


func _validate_valid() -> Array[ValidationIssue]:
	return _validator.validate(
		Fixture.valid_questions(), Fixture.valid_parts(), Fixture.valid_recipes()
	)


func _has_issue(issues: Array[ValidationIssue], code: String, json_path: String) -> bool:
	for issue: ValidationIssue in issues:
		if issue.code == code and issue.json_path == json_path:
			return true
	return false


func _count_code(issues: Array[ValidationIssue], code: String) -> int:
	var count: int = 0
	for issue: ValidationIssue in issues:
		if issue.code == code:
			count += 1
	return count


func _signatures(issues: Array[ValidationIssue]) -> Array[String]:
	var signatures: Array[String] = []
	for issue: ValidationIssue in issues:
		signatures.append(
			"%s|%s|%s|%d" % [issue.code, issue.json_path, issue.message, issue.severity]
		)
	return signatures
