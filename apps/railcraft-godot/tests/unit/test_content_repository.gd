extends GutTest

const FIXTURE_DIR: String = "res://tests/fixtures/"
const INVALID_JSON_PATH: String = FIXTURE_DIR + "content_repository_invalid_json.json"
const ROOT_ARRAY_PATH: String = FIXTURE_DIR + "content_repository_root_array.json"
const EMPTY_PATH: String = FIXTURE_DIR + "content_repository_empty.json"
const MULTI_ISSUE_QUESTIONS_PATH: String = (
	FIXTURE_DIR + "content_repository_multi_issue_questions.json"
)
const EXPECTED_QUESTION_BASELINE_PATH: String = FIXTURE_DIR + "expected_question_baseline.json"
const MISSING_PATH: String = FIXTURE_DIR + "content_repository_missing.json"


func test_basic_dtos_preserve_types_defaults_and_zero_based_answer_index() -> void:
	var source := SourceData.new("测试机构", "测试资料", "https://example.invalid")
	var default_transform := TransformData.new()
	var options: Array[String] = ["甲", "乙", "丙", "丁"]
	var question := QuestionData.new("q01", 1, "题目", options, 0, "解析", source, "part_one")

	options[0] = "已修改"
	assert_eq(source.organization, "测试机构")
	assert_eq(default_transform.position, Vector3.ZERO)
	assert_eq(default_transform.rotation_degrees, Vector3.ZERO)
	assert_eq(default_transform.scale, Vector3.ONE)
	assert_eq(question.correct_option_index, 0)
	assert_eq(question.options[0], "甲")


func test_transform_data_converts_degrees_scale_and_position_to_transform3d() -> void:
	var data := (
		TransformData
		. new(
			Vector3(1.0, 2.0, 3.0),
			Vector3(0.0, 90.0, 0.0),
			Vector3(2.0, 3.0, 4.0),
		)
	)
	var converted: Transform3D = data.to_transform_3d()
	var expected_basis: Basis = Basis.from_euler(Vector3(0.0, deg_to_rad(90.0), 0.0)).scaled(
		Vector3(2.0, 3.0, 4.0)
	)

	assert_true(converted.origin.is_equal_approx(Vector3(1.0, 2.0, 3.0)))
	assert_true(converted.basis.is_equal_approx(expected_basis))


func test_recipe_dtos_copy_typed_id_arrays_and_keep_teaching_note() -> void:
	var part_ids: Array[String] = ["part_one", "part_two", "part_three"]
	var component := (
		ComponentRecipe
		. new(
			"component_one",
			"组件一",
			1,
			part_ids,
			"组件完成",
			"第三组件教学说明",
		)
	)
	var component_ids: Array[String] = ["component_one"]
	var train := TrainRecipe.new("train_one", "测试列车", component_ids)

	part_ids.clear()
	component_ids.clear()
	assert_eq(component.part_ids, ["part_one", "part_two", "part_three"])
	assert_eq(component.teaching_note, "第三组件教学说明")
	assert_eq(train.component_ids, ["component_one"])


func test_catalog_sorts_indexes_and_returns_isolated_array_copies() -> void:
	var source := SourceData.new("机构", "资料", "https://example.invalid")
	var questions: Array[QuestionData] = [
		QuestionData.new("q02", 2, "题二", ["甲", "乙", "丙", "丁"], 0, "解析", source, "p2"),
		QuestionData.new("q01", 1, "题一", ["甲", "乙", "丙", "丁"], 0, "解析", source, "p1"),
	]
	var parts: Array[PartData] = [
		_part("p2", 2, "p1"),
		_part("p1", 1),
	]
	var components: Array[ComponentRecipe] = [
		ComponentRecipe.new("c2", "组件二", 2, ["p2"], "完成", "说明"),
		ComponentRecipe.new("c1", "组件一", 1, ["p1"], "完成", "说明"),
	]
	var train := TrainRecipe.new("train", "列车", ["c1", "c2"])
	var catalog := ContentCatalog.new(questions, parts, components, train)

	var returned_questions: Array[QuestionData] = catalog.get_questions()
	returned_questions.clear()
	var returned_parts: Array[PartData] = catalog.get_parts()
	returned_parts.reverse()
	var returned_components: Array[ComponentRecipe] = catalog.get_components()
	returned_components.pop_back()

	assert_eq(
		catalog.get_questions().map(func(item: QuestionData) -> String: return item.question_id),
		["q01", "q02"]
	)
	assert_eq(
		catalog.get_parts().map(func(item: PartData) -> String: return item.part_id), ["p1", "p2"]
	)
	assert_eq(
		catalog.get_components().map(
			func(item: ComponentRecipe) -> String: return item.component_id
		),
		["c1", "c2"]
	)
	assert_eq(catalog.get_question("q02").prompt, "题二")
	assert_eq(catalog.get_part("p01"), null)
	assert_eq(catalog.get_component("c1").display_name, "组件一")
	assert_eq(catalog.get_component("missing"), null)
	assert_eq(catalog.get_train_recipe(), train)


func test_content_load_result_factories_cannot_expose_contradictory_states() -> void:
	var train := TrainRecipe.new("train", "列车", [])
	var catalog := ContentCatalog.new([], [], [], train)
	var success: ContentLoadResult = ContentLoadResult.success(catalog)
	var issue := ValidationIssue.new("TEST", "$", "failure")
	var original_issues: Array[ValidationIssue] = [issue]
	var failure: ContentLoadResult = ContentLoadResult.failure(original_issues)

	original_issues.clear()
	var returned_issues: Array[ValidationIssue] = failure.issues
	returned_issues.clear()
	assert_true(success.is_success)
	assert_eq(success.catalog, catalog)
	assert_true(success.issues.is_empty())
	assert_false(failure.is_success)
	assert_null(failure.catalog)
	assert_eq(failure.issues.size(), 1)
	assert_false(ContentLoadResult.success(null).is_success)


func test_real_first_release_json_loads_complete_typed_catalog() -> void:
	var result: ContentLoadResult = ContentRepository.new().load_catalog()

	assert_true(result.is_success, _issue_summary(result.issues))
	assert_not_null(result.catalog)
	assert_eq(result.catalog.get_questions().size(), 9)
	assert_eq(result.catalog.get_parts().size(), 9)
	assert_eq(result.catalog.get_components().size(), 3)

	var first_question: QuestionData = result.catalog.get_question("q01")
	assert_eq(first_question.order, 1)
	assert_eq(first_question.prompt, "动车组车体的主要作用是什么？")
	assert_eq(first_question.correct_option_index, 0)
	assert_eq(first_question.options.size(), 4)
	assert_eq(first_question.source.organization, "中国中车")
	assert_eq(first_question.reward_part_id, "body_shell")

	var first_part: PartData = result.catalog.get_part("body_shell")
	assert_eq(first_part.order, 1)
	assert_eq(first_part.target_transform.position, Vector3.ZERO)
	assert_eq(first_part.preview_transform.rotation_degrees, Vector3(0.0, 20.0, 0.0))
	assert_false(first_part.has_required_previous_part())
	assert_eq(result.catalog.get_part("passenger_door").required_previous_part_id, "body_shell")

	var third_component: ComponentRecipe = result.catalog.get_component("traction_power")
	assert_eq(third_component.order, 3)
	assert_eq(third_component.part_ids.size(), 3)
	assert_true(third_component.teaching_note.contains("具体布置因车型而异"))
	assert_eq(result.catalog.get_train_recipe().train_id, "generic_high_speed_emu")


func test_first_release_questions_match_the_complete_requirement_baseline() -> void:
	var result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(result.is_success, _issue_summary(result.issues))
	if not result.is_success:
		return

	var baseline_document: Dictionary = _load_json_dictionary(EXPECTED_QUESTION_BASELINE_PATH)
	assert_true(baseline_document.has("questions"))
	if not baseline_document.has("questions"):
		return
	var expected_questions: Array = baseline_document["questions"] as Array
	var actual_questions: Array[QuestionData] = result.catalog.get_questions()
	assert_eq(actual_questions.size(), expected_questions.size())

	for index: int in expected_questions.size():
		var expected: Dictionary = expected_questions[index] as Dictionary
		var actual: QuestionData = actual_questions[index]
		var expected_source: Dictionary = expected["source"] as Dictionary
		assert_eq(actual.question_id, expected["question_id"])
		assert_eq(actual.order, int(expected["order"]))
		assert_eq(actual.prompt, expected["prompt"])
		assert_eq(actual.options, expected["options"])
		assert_eq(actual.correct_option_index, int(expected["correct_option_index"]))
		assert_eq(actual.explanation, expected["explanation"])
		assert_eq(actual.source.organization, expected_source["organization"])
		assert_eq(actual.source.title, expected_source["title"])
		assert_eq(actual.source.url, expected_source["url"])
		assert_eq(actual.reward_part_id, expected["reward_part_id"])


func test_first_release_parts_and_recipes_preserve_every_json_field() -> void:
	var result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(result.is_success, _issue_summary(result.issues))
	if not result.is_success:
		return

	var raw_parts: Array = _load_json_dictionary(ContentRepository.PARTS_PATH)["parts"] as Array
	var actual_parts: Array[PartData] = result.catalog.get_parts()
	assert_eq(actual_parts.size(), raw_parts.size())
	for index: int in raw_parts.size():
		var expected: Dictionary = raw_parts[index] as Dictionary
		var actual: PartData = actual_parts[index]
		assert_eq(actual.part_id, expected["part_id"])
		assert_eq(actual.display_name, expected["display_name"])
		assert_eq(actual.order, int(expected["order"]))
		assert_eq(actual.component_id, expected["component_id"])
		assert_eq(actual.model_scene_path, expected["model_scene_path"])
		assert_eq(actual.snap_target_path, expected["snap_target_path"])
		_assert_transform_matches(actual.target_transform, expected["target_transform"])
		_assert_transform_matches(actual.preview_transform, expected["preview_transform"])
		var expected_dependency: String = ""
		if expected["required_previous_part_id"] != null:
			expected_dependency = expected["required_previous_part_id"] as String
		assert_eq(actual.required_previous_part_id, expected_dependency)

	var raw_recipes: Dictionary = _load_json_dictionary(ContentRepository.RECIPES_PATH)
	var raw_components: Array = raw_recipes["components"] as Array
	var actual_components: Array[ComponentRecipe] = result.catalog.get_components()
	assert_eq(actual_components.size(), raw_components.size())
	for index: int in raw_components.size():
		var expected: Dictionary = raw_components[index] as Dictionary
		var actual: ComponentRecipe = actual_components[index]
		assert_eq(actual.component_id, expected["component_id"])
		assert_eq(actual.display_name, expected["display_name"])
		assert_eq(actual.order, int(expected["order"]))
		assert_eq(actual.part_ids, expected["part_ids"])
		assert_eq(actual.completion_message, expected["completion_message"])
		assert_eq(actual.teaching_note, expected["teaching_note"])

	var expected_train: Dictionary = raw_recipes["train_recipe"] as Dictionary
	var actual_train: TrainRecipe = result.catalog.get_train_recipe()
	assert_eq(actual_train.train_id, expected_train["train_id"])
	assert_eq(actual_train.display_name, expected_train["display_name"])
	assert_eq(actual_train.component_ids, expected_train["component_ids"])


func test_repository_rejects_non_resource_paths_without_partial_catalog() -> void:
	var result: ContentLoadResult = (
		ContentRepository
		. new(
			"C:/questions.json",
			ContentRepository.PARTS_PATH,
			ContentRepository.RECIPES_PATH,
		)
		. load_catalog()
	)

	assert_false(result.is_success)
	assert_null(result.catalog)
	assert_true(
		_has_issue(result.issues, ContentRepository.CONTENT_PATH_INVALID, "C:/questions.json")
	)


func test_each_missing_document_is_reported_without_partial_catalog() -> void:
	for missing_index: int in 3:
		var paths: Array[String] = [
			ContentRepository.QUESTIONS_PATH,
			ContentRepository.PARTS_PATH,
			ContentRepository.RECIPES_PATH,
		]
		paths[missing_index] = MISSING_PATH
		var result: ContentLoadResult = (
			ContentRepository.new(paths[0], paths[1], paths[2]).load_catalog()
		)

		assert_false(result.is_success)
		assert_null(result.catalog)
		assert_true(_has_issue(result.issues, ContentRepository.CONTENT_FILE_MISSING, MISSING_PATH))


func test_empty_document_has_a_distinct_failure_code() -> void:
	var result: ContentLoadResult = (
		ContentRepository
		. new(
			EMPTY_PATH,
			ContentRepository.PARTS_PATH,
			ContentRepository.RECIPES_PATH,
		)
		. load_catalog()
	)

	assert_false(result.is_success)
	assert_null(result.catalog)
	assert_true(_has_issue(result.issues, ContentRepository.CONTENT_FILE_EMPTY, EMPTY_PATH))


func test_each_syntax_error_reports_json_line_and_no_partial_catalog() -> void:
	for invalid_index: int in 3:
		var paths: Array[String] = [
			ContentRepository.QUESTIONS_PATH,
			ContentRepository.PARTS_PATH,
			ContentRepository.RECIPES_PATH,
		]
		paths[invalid_index] = INVALID_JSON_PATH
		var result: ContentLoadResult = (
			ContentRepository.new(paths[0], paths[1], paths[2]).load_catalog()
		)

		assert_false(result.is_success)
		assert_null(result.catalog)
		assert_eq(result.issues.size(), 1)
		assert_eq(result.issues[0].code, ContentRepository.JSON_PARSE_FAILED)
		assert_true(result.issues[0].json_path.begins_with("%s:" % INVALID_JSON_PATH))
		assert_true(result.issues[0].message.contains("line"))


func test_json_array_root_is_rejected_before_validation_or_conversion() -> void:
	var result: ContentLoadResult = (
		ContentRepository
		. new(
			ROOT_ARRAY_PATH,
			ContentRepository.PARTS_PATH,
			ContentRepository.RECIPES_PATH,
		)
		. load_catalog()
	)

	assert_false(result.is_success)
	assert_null(result.catalog)
	assert_true(
		_has_issue(result.issues, ContentRepository.JSON_ROOT_TYPE_INVALID, ROOT_ARRAY_PATH)
	)


func test_validator_aggregates_multiple_issues_and_repository_forwards_them() -> void:
	var result: ContentLoadResult = (
		ContentRepository
		. new(
			MULTI_ISSUE_QUESTIONS_PATH,
			ContentRepository.PARTS_PATH,
			ContentRepository.RECIPES_PATH,
		)
		. load_catalog()
	)

	assert_false(result.is_success)
	assert_null(result.catalog)
	assert_true(_has_issue(result.issues, ContentValidator.FIELD_MISSING, "$.questions[0].prompt"))
	assert_true(
		_has_issue(result.issues, ContentValidator.ID_FORMAT_INVALID, "$.questions[0].question_id")
	)
	assert_true(result.issues.size() > 2)


func _part(part_id: String, order: int, dependency: String = "") -> PartData:
	return (
		PartData
		. new(
			part_id,
			"零件 %d" % order,
			order,
			"component",
			"res://tests/fixtures/%s.tscn" % part_id,
			"SnapTargets/%s" % part_id,
			TransformData.new(),
			TransformData.new(),
			dependency,
		)
	)


func _load_json_dictionary(path: String) -> Dictionary:
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
	assert_true(parsed is Dictionary, "Expected a JSON object fixture at %s." % path)
	if parsed is Dictionary:
		return parsed as Dictionary
	return {}


func _assert_transform_matches(actual: TransformData, expected: Dictionary) -> void:
	assert_eq(actual.position, _vector3_from_json(expected["position"] as Array))
	assert_eq(
		actual.rotation_degrees,
		_vector3_from_json(expected["rotation_degrees"] as Array),
	)
	assert_eq(actual.scale, _vector3_from_json(expected["scale"] as Array))


func _vector3_from_json(values: Array) -> Vector3:
	return Vector3(float(values[0]), float(values[1]), float(values[2]))


func _has_issue(issues: Array[ValidationIssue], code: String, json_path: String) -> bool:
	for issue: ValidationIssue in issues:
		if issue.code == code and issue.json_path == json_path:
			return true
	return false


func _issue_summary(issues: Array[ValidationIssue]) -> String:
	var summaries: Array[String] = []
	for issue: ValidationIssue in issues:
		summaries.append("%s at %s: %s" % [issue.code, issue.json_path, issue.message])
	return "; ".join(summaries)
