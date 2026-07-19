class_name ContentRepository
extends RefCounted

const QUESTIONS_PATH: String = "res://data/questions.json"
const PARTS_PATH: String = "res://data/parts.json"
const RECIPES_PATH: String = "res://data/recipes.json"

const CONTENT_PATH_INVALID: String = "CONTENT_PATH_INVALID"
const CONTENT_FILE_MISSING: String = "CONTENT_FILE_MISSING"
const CONTENT_FILE_READ_FAILED: String = "CONTENT_FILE_READ_FAILED"
const CONTENT_FILE_EMPTY: String = "CONTENT_FILE_EMPTY"
const JSON_PARSE_FAILED: String = "JSON_PARSE_FAILED"
const JSON_ROOT_TYPE_INVALID: String = "JSON_ROOT_TYPE_INVALID"
const _JSON_INTEGER_FIELDS: Array[String] = [
	"schema_version",
	"order",
	"correct_option_index",
]

var _questions_path: String
var _parts_path: String
var _recipes_path: String
var _validator: ContentValidator


class _DocumentLoadResult:
	extends RefCounted
	var data: Dictionary
	var issue: ValidationIssue

	func _init(document_data: Dictionary = {}, document_issue: ValidationIssue = null) -> void:
		data = document_data
		issue = document_issue

	func is_success() -> bool:
		return issue == null


func _init(
	questions_path: String = QUESTIONS_PATH,
	parts_path: String = PARTS_PATH,
	recipes_path: String = RECIPES_PATH,
	validator: ContentValidator = null,
) -> void:
	_questions_path = questions_path
	_parts_path = parts_path
	_recipes_path = recipes_path
	_validator = validator if validator != null else ContentValidator.new()


## Loads, validates, and converts the complete three-document content catalog.
func load_catalog() -> ContentLoadResult:
	var questions_result: _DocumentLoadResult = _load_document(_questions_path)
	var parts_result: _DocumentLoadResult = _load_document(_parts_path)
	var recipes_result: _DocumentLoadResult = _load_document(_recipes_path)
	var load_issues: Array[ValidationIssue] = []

	_append_document_issue(questions_result, load_issues)
	_append_document_issue(parts_result, load_issues)
	_append_document_issue(recipes_result, load_issues)
	if not load_issues.is_empty():
		return ContentLoadResult.failure(load_issues)

	var validation_issues: Array[ValidationIssue] = (
		_validator
		. validate(
			questions_result.data,
			parts_result.data,
			recipes_result.data,
		)
	)
	if not validation_issues.is_empty():
		return ContentLoadResult.failure(validation_issues)

	return ContentLoadResult.success(
		_build_catalog(questions_result.data, parts_result.data, recipes_result.data)
	)


func _load_document(path: String) -> _DocumentLoadResult:
	if not path.begins_with("res://"):
		return (
			_DocumentLoadResult
			. new(
				{},
				(
					ValidationIssue
					. new(
						CONTENT_PATH_INVALID,
						path,
						"Content files must use a res:// resource path.",
					)
				),
			)
		)
	if not FileAccess.file_exists(path):
		return (
			_DocumentLoadResult
			. new(
				{},
				(
					ValidationIssue
					. new(
						CONTENT_FILE_MISSING,
						path,
						"Content file does not exist: %s" % path,
					)
				),
			)
		)

	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	if file == null:
		return (
			_DocumentLoadResult
			. new(
				{},
				(
					ValidationIssue
					. new(
						CONTENT_FILE_READ_FAILED,
						path,
						(
							"Content file could not be opened: %s"
							% error_string(FileAccess.get_open_error())
						),
					)
				),
			)
		)

	var text: String = file.get_as_text()
	var read_error: Error = file.get_error()
	if read_error != OK:
		return (
			_DocumentLoadResult
			. new(
				{},
				(
					ValidationIssue
					. new(
						CONTENT_FILE_READ_FAILED,
						path,
						"Content file could not be read: %s" % error_string(read_error),
					)
				),
			)
		)
	if text.strip_edges().is_empty():
		return (
			_DocumentLoadResult
			. new(
				{},
				(
					ValidationIssue
					. new(
						CONTENT_FILE_EMPTY,
						path,
						"Content file is empty: %s" % path,
					)
				),
			)
		)

	return _parse_document(path, text)


func _parse_document(path: String, text: String) -> _DocumentLoadResult:
	var parser := JSON.new()
	var parse_error: Error = parser.parse(text)
	if parse_error != OK:
		var error_line: int = parser.get_error_line()
		return (
			_DocumentLoadResult
			. new(
				{},
				(
					ValidationIssue
					. new(
						JSON_PARSE_FAILED,
						"%s:%d" % [path, error_line],
						(
							"JSON parsing failed on line %d: %s"
							% [error_line, parser.get_error_message()]
						),
					)
				),
			)
		)

	var parsed_data: Variant = parser.data
	if not parsed_data is Dictionary:
		return (
			_DocumentLoadResult
			. new(
				{},
				(
					ValidationIssue
					. new(
						JSON_ROOT_TYPE_INVALID,
						path,
						"JSON root must be an object.",
					)
				),
			)
		)
	var parsed_dictionary: Dictionary = parsed_data as Dictionary
	_normalize_json_integers(parsed_dictionary)
	return _DocumentLoadResult.new(parsed_dictionary)


func _normalize_json_integers(value: Variant) -> void:
	if value is Dictionary:
		var object: Dictionary = value as Dictionary
		for key_value: Variant in object.keys():
			var child: Variant = object[key_value]
			if (
				typeof(key_value) == TYPE_STRING
				and (key_value as String) in _JSON_INTEGER_FIELDS
				and typeof(child) == TYPE_FLOAT
				and is_equal_approx(child as float, round(child as float))
			):
				object[key_value] = int(child as float)
			else:
				_normalize_json_integers(child)
	elif value is Array:
		for child: Variant in value as Array:
			_normalize_json_integers(child)


func _append_document_issue(
	document_result: _DocumentLoadResult,
	issues: Array[ValidationIssue],
) -> void:
	if not document_result.is_success():
		issues.append(document_result.issue)


func _build_catalog(
	raw_questions: Dictionary,
	raw_parts: Dictionary,
	raw_recipes: Dictionary,
) -> ContentCatalog:
	var questions: Array[QuestionData] = []
	var parts: Array[PartData] = []
	var components: Array[ComponentRecipe] = []

	var question_values: Array = raw_questions["questions"] as Array
	for value: Variant in question_values:
		questions.append(_to_question(value as Dictionary))

	var part_values: Array = raw_parts["parts"] as Array
	for value: Variant in part_values:
		parts.append(_to_part(value as Dictionary))

	var component_values: Array = raw_recipes["components"] as Array
	for value: Variant in component_values:
		components.append(_to_component(value as Dictionary))

	var train_recipe: TrainRecipe = _to_train_recipe(raw_recipes["train_recipe"] as Dictionary)
	return ContentCatalog.new(questions, parts, components, train_recipe)


func _to_question(raw: Dictionary) -> QuestionData:
	var raw_source: Dictionary = raw["source"] as Dictionary
	var source := (
		SourceData
		. new(
			raw_source["organization"] as String,
			raw_source["title"] as String,
			raw_source["url"] as String,
		)
	)
	return (
		QuestionData
		. new(
			raw["question_id"] as String,
			raw["order"] as int,
			raw["prompt"] as String,
			_to_string_array(raw["options"] as Array),
			raw["correct_option_index"] as int,
			raw["explanation"] as String,
			source,
			raw["reward_part_id"] as String,
		)
	)


func _to_part(raw: Dictionary) -> PartData:
	var dependency: String = ""
	if raw["required_previous_part_id"] != null:
		dependency = raw["required_previous_part_id"] as String
	return (
		PartData
		. new(
			raw["part_id"] as String,
			raw["display_name"] as String,
			raw["order"] as int,
			raw["component_id"] as String,
			raw["model_scene_path"] as String,
			raw["snap_target_path"] as String,
			_to_transform(raw["target_transform"] as Dictionary),
			_to_transform(raw["preview_transform"] as Dictionary),
			dependency,
		)
	)


func _to_component(raw: Dictionary) -> ComponentRecipe:
	return (
		ComponentRecipe
		. new(
			raw["component_id"] as String,
			raw["display_name"] as String,
			raw["order"] as int,
			_to_string_array(raw["part_ids"] as Array),
			raw["completion_message"] as String,
			raw["teaching_note"] as String,
		)
	)


func _to_train_recipe(raw: Dictionary) -> TrainRecipe:
	return (
		TrainRecipe
		. new(
			raw["train_id"] as String,
			raw["display_name"] as String,
			_to_string_array(raw["component_ids"] as Array),
		)
	)


func _to_transform(raw: Dictionary) -> TransformData:
	return (
		TransformData
		. new(
			_to_vector3(raw["position"] as Array),
			_to_vector3(raw["rotation_degrees"] as Array),
			_to_vector3(raw["scale"] as Array),
		)
	)


func _to_vector3(values: Array) -> Vector3:
	return Vector3(float(values[0]), float(values[1]), float(values[2]))


func _to_string_array(values: Array) -> Array[String]:
	var converted: Array[String] = []
	for value: Variant in values:
		converted.append(value as String)
	return converted
