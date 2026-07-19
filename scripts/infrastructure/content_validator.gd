# gdlint: disable = max-file-lines
class_name ContentValidator
extends RefCounted

const CONTENT_FILE_MISSING: String = "CONTENT_FILE_MISSING"
const FIELD_MISSING: String = "FIELD_MISSING"
const FIELD_TYPE_INVALID: String = "FIELD_TYPE_INVALID"
const FIELD_VALUE_INVALID: String = "FIELD_VALUE_INVALID"
const ID_FORMAT_INVALID: String = "ID_FORMAT_INVALID"
const ID_DUPLICATE: String = "ID_DUPLICATE"
const REFERENCE_NOT_FOUND: String = "REFERENCE_NOT_FOUND"
const ORDER_INVALID: String = "ORDER_INVALID"
const DEPENDENCY_CHAIN_INVALID: String = "DEPENDENCY_CHAIN_INVALID"
const DEPENDENCY_CYCLE: String = "DEPENDENCY_CYCLE"
const CONTENT_COVERAGE_INVALID: String = "CONTENT_COVERAGE_INVALID"

const _QUESTION_FIELDS: Array[String] = [
	"question_id",
	"order",
	"prompt",
	"options",
	"correct_option_index",
	"explanation",
	"source",
	"reward_part_id",
]
const _PART_FIELDS: Array[String] = [
	"part_id",
	"display_name",
	"order",
	"component_id",
	"model_scene_path",
	"snap_target_path",
	"target_transform",
	"preview_transform",
	"required_previous_part_id",
]
const _COMPONENT_FIELDS: Array[String] = [
	"component_id",
	"display_name",
	"order",
	"part_ids",
	"completion_message",
	"teaching_note",
]
const _TRAIN_FIELDS: Array[String] = ["train_id", "display_name", "component_ids"]


## Validates already-parsed content without reading files, scenes, or network state.
func validate(
	raw_questions: Dictionary,
	raw_parts: Dictionary,
	raw_recipes: Dictionary,
) -> Array[ValidationIssue]:
	var issues: Array[ValidationIssue] = []

	_validate_root(raw_questions, "questions.json", "questions", issues)
	_validate_root(raw_parts, "parts.json", "parts", issues)
	_validate_root(raw_recipes, "recipes.json", "components", issues)
	_validate_required_dictionary(raw_recipes, "train_recipe", "$", issues)

	var questions: Array = _array_or_empty(raw_questions, "questions")
	var parts: Array = _array_or_empty(raw_parts, "parts")
	var components: Array = _array_or_empty(raw_recipes, "components")
	var train_recipe: Dictionary = _dictionary_or_empty(raw_recipes, "train_recipe")

	_validate_questions(questions, issues)
	_validate_parts(parts, issues)
	_validate_components(components, issues)
	_validate_train_recipe(
		train_recipe,
		raw_recipes.has("train_recipe") and raw_recipes["train_recipe"] is Dictionary,
		issues,
	)

	_validate_unique_ids(questions, "question_id", "questions", issues)
	_validate_unique_ids(parts, "part_id", "parts", issues)
	_validate_unique_ids(components, "component_id", "components", issues)

	_validate_references_and_coverage(questions, parts, components, train_recipe, issues)
	_validate_orders(questions, parts, components, issues)
	_validate_question_reward_order(questions, parts, issues)
	_validate_dependency_chain(parts, issues)
	_validate_dependency_cycles(parts, issues)

	return issues


func _validate_root(
	root: Dictionary,
	document_name: String,
	array_field: String,
	issues: Array[ValidationIssue],
) -> void:
	if root.is_empty():
		_add_issue(
			issues,
			CONTENT_FILE_MISSING,
			"$",
			"No parsed root object was supplied for %s." % document_name,
		)

	if not root.has("schema_version"):
		_add_issue(
			issues,
			FIELD_MISSING,
			"$.schema_version",
			"%s is missing schema_version." % document_name,
		)
	else:
		var schema_value: Variant = root["schema_version"]
		if typeof(schema_value) != TYPE_INT or schema_value != 1:
			_add_issue(
				issues,
				FIELD_TYPE_INVALID,
				"$.schema_version",
				"%s schema_version must be the integer 1." % document_name,
			)

	_validate_required_array(root, array_field, "$", issues)


func _validate_questions(questions: Array, issues: Array[ValidationIssue]) -> void:
	for index: int in questions.size():
		var path: String = _item_path("questions", index)
		var value: Variant = questions[index]
		if not value is Dictionary:
			_add_issue(issues, FIELD_TYPE_INVALID, path, "Question entries must be objects.")
			continue

		var question: Dictionary = value as Dictionary
		_validate_required_fields(question, _QUESTION_FIELDS, path, issues)
		_validate_non_empty_string(question, "question_id", path, issues)
		_validate_integer(question, "order", path, issues)
		_validate_non_empty_string(question, "prompt", path, issues)
		_validate_required_array(question, "options", path, issues, false)
		_validate_integer(question, "correct_option_index", path, issues)
		_validate_non_empty_string(question, "explanation", path, issues)
		_validate_required_dictionary(question, "source", path, issues, false)
		_validate_non_empty_string(question, "reward_part_id", path, issues)

		_validate_question_options(question, path, issues)
		_validate_correct_option_index(question, path, issues)
		_validate_question_source(question, path, issues)
		_validate_entity_id(question, "question_id", path, issues)


func _validate_question_options(
	question: Dictionary,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not question.has("options") or not question["options"] is Array:
		return
	var options: Array = question["options"] as Array
	var options_path: String = _field_path(path, "options")
	if options.size() != 4:
		_add_issue(
			issues,
			FIELD_VALUE_INVALID,
			options_path,
			"Question options must contain exactly four entries.",
		)
	for option_index: int in options.size():
		var option_path: String = "%s[%d]" % [options_path, option_index]
		var option: Variant = options[option_index]
		if typeof(option) != TYPE_STRING:
			_add_issue(issues, FIELD_TYPE_INVALID, option_path, "Question options must be strings.")
		elif (option as String).is_empty():
			_add_issue(
				issues, FIELD_VALUE_INVALID, option_path, "Question options cannot be empty."
			)


func _validate_correct_option_index(
	question: Dictionary,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not question.has("correct_option_index"):
		return
	var correct_value: Variant = question["correct_option_index"]
	if typeof(correct_value) != TYPE_INT:
		return
	if not question.has("options") or not question["options"] is Array:
		return
	var options: Array = question["options"] as Array
	var correct_index: int = correct_value as int
	if correct_index < 0 or correct_index >= options.size():
		_add_issue(
			issues,
			FIELD_VALUE_INVALID,
			_field_path(path, "correct_option_index"),
			"correct_option_index must use a valid zero-based option index.",
		)


func _validate_question_source(
	question: Dictionary,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not question.has("source") or not question["source"] is Dictionary:
		return
	var source: Dictionary = question["source"] as Dictionary
	var source_path: String = _field_path(path, "source")
	for field_name: String in ["organization", "title", "url"]:
		if not source.has(field_name):
			_add_issue(
				issues,
				FIELD_MISSING,
				_field_path(source_path, field_name),
				"Question source is missing %s." % field_name,
			)
		else:
			_validate_non_empty_string(source, field_name, source_path, issues)


func _validate_parts(parts: Array, issues: Array[ValidationIssue]) -> void:
	for index: int in parts.size():
		var path: String = _item_path("parts", index)
		var value: Variant = parts[index]
		if not value is Dictionary:
			_add_issue(issues, FIELD_TYPE_INVALID, path, "Part entries must be objects.")
			continue

		var part: Dictionary = value as Dictionary
		_validate_required_fields(part, _PART_FIELDS, path, issues)
		_validate_non_empty_string(part, "part_id", path, issues)
		_validate_non_empty_string(part, "display_name", path, issues)
		_validate_integer(part, "order", path, issues)
		_validate_non_empty_string(part, "component_id", path, issues)
		_validate_non_empty_string(part, "model_scene_path", path, issues)
		_validate_non_empty_string(part, "snap_target_path", path, issues)
		_validate_required_dictionary(part, "target_transform", path, issues, false)
		_validate_required_dictionary(part, "preview_transform", path, issues, false)
		_validate_nullable_id(part, "required_previous_part_id", path, issues)
		_validate_transform(part, "target_transform", path, issues)
		_validate_transform(part, "preview_transform", path, issues)
		_validate_model_scene_path(part, path, issues)
		_validate_entity_id(part, "part_id", path, issues)


func _validate_transform(
	part: Dictionary,
	field_name: String,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not part.has(field_name) or not part[field_name] is Dictionary:
		return
	var transform: Dictionary = part[field_name] as Dictionary
	var transform_path: String = _field_path(path, field_name)
	for vector_field: String in ["position", "rotation_degrees", "scale"]:
		if not transform.has(vector_field):
			_add_issue(
				issues,
				FIELD_MISSING,
				_field_path(transform_path, vector_field),
				"Transform is missing %s." % vector_field,
			)
			continue
		var vector_value: Variant = transform[vector_field]
		var vector_path: String = _field_path(transform_path, vector_field)
		if not vector_value is Array:
			_add_issue(issues, FIELD_TYPE_INVALID, vector_path, "Transform vectors must be arrays.")
			continue
		var values: Array = vector_value as Array
		if values.size() != 3:
			_add_issue(
				issues,
				FIELD_VALUE_INVALID,
				vector_path,
				"Transform vectors must contain exactly three numbers.",
			)
		for value_index: int in values.size():
			var coordinate: Variant = values[value_index]
			if typeof(coordinate) != TYPE_INT and typeof(coordinate) != TYPE_FLOAT:
				_add_issue(
					issues,
					FIELD_TYPE_INVALID,
					"%s[%d]" % [vector_path, value_index],
					"Transform coordinates must be numbers.",
				)


func _validate_model_scene_path(
	part: Dictionary,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not _has_non_empty_string(part, "model_scene_path"):
		return
	var scene_path: String = part["model_scene_path"] as String
	if not scene_path.begins_with("res://"):
		_add_issue(
			issues,
			FIELD_VALUE_INVALID,
			_field_path(path, "model_scene_path"),
			"model_scene_path must begin with res://.",
		)


func _validate_components(components: Array, issues: Array[ValidationIssue]) -> void:
	for index: int in components.size():
		var path: String = _item_path("components", index)
		var value: Variant = components[index]
		if not value is Dictionary:
			_add_issue(issues, FIELD_TYPE_INVALID, path, "Component entries must be objects.")
			continue

		var component: Dictionary = value as Dictionary
		_validate_required_fields(component, _COMPONENT_FIELDS, path, issues)
		_validate_non_empty_string(component, "component_id", path, issues)
		_validate_non_empty_string(component, "display_name", path, issues)
		_validate_integer(component, "order", path, issues)
		_validate_required_array(component, "part_ids", path, issues, false)
		_validate_non_empty_string(component, "completion_message", path, issues)
		_validate_non_empty_string(component, "teaching_note", path, issues)
		_validate_string_array(component, "part_ids", path, issues)
		_validate_entity_id(component, "component_id", path, issues)


func _validate_train_recipe(
	train_recipe: Dictionary,
	is_dictionary: bool,
	issues: Array[ValidationIssue],
) -> void:
	if not is_dictionary:
		return
	var path: String = "$.train_recipe"
	_validate_required_fields(train_recipe, _TRAIN_FIELDS, path, issues)
	_validate_non_empty_string(train_recipe, "train_id", path, issues)
	_validate_non_empty_string(train_recipe, "display_name", path, issues)
	_validate_required_array(train_recipe, "component_ids", path, issues, false)
	_validate_string_array(train_recipe, "component_ids", path, issues)
	_validate_entity_id(train_recipe, "train_id", path, issues)


func _validate_string_array(
	object: Dictionary,
	field_name: String,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not object.has(field_name) or not object[field_name] is Array:
		return
	var values: Array = object[field_name] as Array
	var array_path: String = _field_path(path, field_name)
	for index: int in values.size():
		var item: Variant = values[index]
		var item_path: String = "%s[%d]" % [array_path, index]
		if typeof(item) != TYPE_STRING:
			_add_issue(issues, FIELD_TYPE_INVALID, item_path, "Reference IDs must be strings.")
		elif (item as String).is_empty():
			_add_issue(issues, FIELD_VALUE_INVALID, item_path, "Reference IDs cannot be empty.")


func _validate_unique_ids(
	items: Array,
	id_field: String,
	container_name: String,
	issues: Array[ValidationIssue],
) -> void:
	var counts: Dictionary[String, int] = {}
	for item_value: Variant in items:
		if not item_value is Dictionary:
			continue
		var item: Dictionary = item_value as Dictionary
		if _has_non_empty_string(item, id_field):
			var item_id: String = item[id_field] as String
			counts[item_id] = counts.get(item_id, 0) + 1

	for index: int in items.size():
		var item_value: Variant = items[index]
		if not item_value is Dictionary:
			continue
		var item: Dictionary = item_value as Dictionary
		if not _has_non_empty_string(item, id_field):
			continue
		var item_id: String = item[id_field] as String
		if counts[item_id] > 1:
			_add_issue(
				issues,
				ID_DUPLICATE,
				_field_path(_item_path(container_name, index), id_field),
				"ID '%s' appears %d times in %s." % [item_id, counts[item_id], container_name],
			)


func _validate_references_and_coverage(
	questions: Array,
	parts: Array,
	components: Array,
	train_recipe: Dictionary,
	issues: Array[ValidationIssue],
) -> void:
	var part_ids: Dictionary[String, int] = _build_id_counts(parts, "part_id")
	var component_ids: Dictionary[String, int] = _build_id_counts(components, "component_id")

	_validate_question_part_references(questions, part_ids, issues)
	_validate_part_references(parts, part_ids, component_ids, issues)
	_validate_component_part_references(components, part_ids, issues)
	_validate_train_component_references(train_recipe, component_ids, issues)
	_validate_reward_coverage(questions, parts, issues)
	_validate_recipe_part_coverage(parts, components, issues)
	_validate_train_component_coverage(components, train_recipe, issues)


func _validate_question_part_references(
	questions: Array,
	part_ids: Dictionary[String, int],
	issues: Array[ValidationIssue],
) -> void:
	for index: int in questions.size():
		var value: Variant = questions[index]
		if not value is Dictionary:
			continue
		var question: Dictionary = value as Dictionary
		if not _has_non_empty_string(question, "reward_part_id"):
			continue
		var part_id: String = question["reward_part_id"] as String
		if not part_ids.has(part_id):
			_add_issue(
				issues,
				REFERENCE_NOT_FOUND,
				_field_path(_item_path("questions", index), "reward_part_id"),
				"Reward part '%s' does not exist." % part_id,
			)


func _validate_part_references(
	parts: Array,
	part_ids: Dictionary[String, int],
	component_ids: Dictionary[String, int],
	issues: Array[ValidationIssue],
) -> void:
	for index: int in parts.size():
		var value: Variant = parts[index]
		if not value is Dictionary:
			continue
		var part: Dictionary = value as Dictionary
		var path: String = _item_path("parts", index)
		if _has_non_empty_string(part, "component_id"):
			var component_id: String = part["component_id"] as String
			if not component_ids.has(component_id):
				_add_issue(
					issues,
					REFERENCE_NOT_FOUND,
					_field_path(path, "component_id"),
					"Component '%s' does not exist." % component_id,
				)
		if part.has("required_previous_part_id"):
			var dependency_value: Variant = part["required_previous_part_id"]
			if typeof(dependency_value) == TYPE_STRING:
				var dependency_id: String = dependency_value as String
				if not dependency_id.is_empty() and not part_ids.has(dependency_id):
					_add_issue(
						issues,
						REFERENCE_NOT_FOUND,
						_field_path(path, "required_previous_part_id"),
						"Required previous part '%s' does not exist." % dependency_id,
					)


func _validate_component_part_references(
	components: Array,
	part_ids: Dictionary[String, int],
	issues: Array[ValidationIssue],
) -> void:
	for component_index: int in components.size():
		var value: Variant = components[component_index]
		if not value is Dictionary:
			continue
		var component: Dictionary = value as Dictionary
		if not component.has("part_ids") or not component["part_ids"] is Array:
			continue
		var recipe_parts: Array = component["part_ids"] as Array
		for part_index: int in recipe_parts.size():
			var part_value: Variant = recipe_parts[part_index]
			if typeof(part_value) != TYPE_STRING or (part_value as String).is_empty():
				continue
			var part_id: String = part_value as String
			if not part_ids.has(part_id):
				_add_issue(
					issues,
					REFERENCE_NOT_FOUND,
					"%s.part_ids[%d]" % [_item_path("components", component_index), part_index],
					"Recipe part '%s' does not exist." % part_id,
				)


func _validate_train_component_references(
	train_recipe: Dictionary,
	component_ids: Dictionary[String, int],
	issues: Array[ValidationIssue],
) -> void:
	if not train_recipe.has("component_ids") or not train_recipe["component_ids"] is Array:
		return
	var train_components: Array = train_recipe["component_ids"] as Array
	for index: int in train_components.size():
		var value: Variant = train_components[index]
		if typeof(value) != TYPE_STRING or (value as String).is_empty():
			continue
		var component_id: String = value as String
		if not component_ids.has(component_id):
			_add_issue(
				issues,
				REFERENCE_NOT_FOUND,
				"$.train_recipe.component_ids[%d]" % index,
				"Train component '%s' does not exist." % component_id,
			)


func _validate_reward_coverage(
	questions: Array,
	parts: Array,
	issues: Array[ValidationIssue],
) -> void:
	var reward_counts: Dictionary[String, int] = {}
	for value: Variant in questions:
		if value is Dictionary:
			var question: Dictionary = value as Dictionary
			if _has_non_empty_string(question, "reward_part_id"):
				var part_id: String = question["reward_part_id"] as String
				reward_counts[part_id] = reward_counts.get(part_id, 0) + 1

	for index: int in parts.size():
		var value: Variant = parts[index]
		if not value is Dictionary:
			continue
		var part: Dictionary = value as Dictionary
		if not _has_non_empty_string(part, "part_id"):
			continue
		var part_id: String = part["part_id"] as String
		var reward_count: int = reward_counts.get(part_id, 0)
		if reward_count != 1:
			_add_issue(
				issues,
				CONTENT_COVERAGE_INVALID,
				_field_path(_item_path("parts", index), "part_id"),
				"Part '%s' must be rewarded exactly once; found %d." % [part_id, reward_count],
			)


func _validate_recipe_part_coverage(
	parts: Array,
	components: Array,
	issues: Array[ValidationIssue],
) -> void:
	var recipe_counts: Dictionary[String, int] = {}
	var recipe_owner: Dictionary[String, String] = {}
	for component_value: Variant in components:
		if not component_value is Dictionary:
			continue
		var component: Dictionary = component_value as Dictionary
		var owner_id: String = ""
		if _has_non_empty_string(component, "component_id"):
			owner_id = component["component_id"] as String
		if not component.has("part_ids") or not component["part_ids"] is Array:
			continue
		var recipe_parts: Array = component["part_ids"] as Array
		for part_value: Variant in recipe_parts:
			if typeof(part_value) != TYPE_STRING or (part_value as String).is_empty():
				continue
			var part_id: String = part_value as String
			recipe_counts[part_id] = recipe_counts.get(part_id, 0) + 1
			if not recipe_owner.has(part_id):
				recipe_owner[part_id] = owner_id

	for index: int in parts.size():
		var value: Variant = parts[index]
		if not value is Dictionary:
			continue
		var part: Dictionary = value as Dictionary
		if not _has_non_empty_string(part, "part_id"):
			continue
		var part_id: String = part["part_id"] as String
		var recipe_count: int = recipe_counts.get(part_id, 0)
		var path: String = _item_path("parts", index)
		if recipe_count != 1:
			_add_issue(
				issues,
				CONTENT_COVERAGE_INVALID,
				_field_path(path, "part_id"),
				(
					"Part '%s' must appear in exactly one component recipe; found %d."
					% [part_id, recipe_count]
				),
			)
		elif _has_non_empty_string(part, "component_id"):
			var declared_owner: String = part["component_id"] as String
			if recipe_owner.get(part_id, "") != declared_owner:
				_add_issue(
					issues,
					CONTENT_COVERAGE_INVALID,
					_field_path(path, "component_id"),
					"Part '%s' is listed under a different component recipe." % part_id,
				)


func _validate_train_component_coverage(
	components: Array,
	train_recipe: Dictionary,
	issues: Array[ValidationIssue],
) -> void:
	var train_counts: Dictionary[String, int] = {}
	if train_recipe.has("component_ids") and train_recipe["component_ids"] is Array:
		var train_components: Array = train_recipe["component_ids"] as Array
		for value: Variant in train_components:
			if typeof(value) == TYPE_STRING and not (value as String).is_empty():
				var component_id: String = value as String
				train_counts[component_id] = train_counts.get(component_id, 0) + 1

	for index: int in components.size():
		var value: Variant = components[index]
		if not value is Dictionary:
			continue
		var component: Dictionary = value as Dictionary
		if not _has_non_empty_string(component, "component_id"):
			continue
		var component_id: String = component["component_id"] as String
		var train_count: int = train_counts.get(component_id, 0)
		if train_count != 1:
			_add_issue(
				issues,
				CONTENT_COVERAGE_INVALID,
				"$.train_recipe.component_ids",
				(
					"Component '%s' must appear in the train recipe exactly once; found %d."
					% [component_id, train_count]
				),
			)


func _validate_orders(
	questions: Array,
	parts: Array,
	components: Array,
	issues: Array[ValidationIssue],
) -> void:
	_validate_order_sequence(questions, "questions", issues)
	_validate_order_sequence(parts, "parts", issues)
	_validate_order_sequence(components, "components", issues)


func _validate_order_sequence(
	items: Array,
	container_name: String,
	issues: Array[ValidationIssue],
) -> void:
	for index: int in items.size():
		var value: Variant = items[index]
		if not value is Dictionary:
			continue
		var item: Dictionary = value as Dictionary
		if not item.has("order") or typeof(item["order"]) != TYPE_INT:
			continue
		var actual_order: int = item["order"] as int
		var expected_order: int = index + 1
		if actual_order != expected_order:
			_add_issue(
				issues,
				ORDER_INVALID,
				_field_path(_item_path(container_name, index), "order"),
				"Expected order %d at this position, found %d." % [expected_order, actual_order],
			)


func _validate_question_reward_order(
	questions: Array,
	parts: Array,
	issues: Array[ValidationIssue],
) -> void:
	var part_orders: Dictionary[String, int] = {}
	for value: Variant in parts:
		if not value is Dictionary:
			continue
		var part: Dictionary = value as Dictionary
		if _has_non_empty_string(part, "part_id") and typeof(part.get("order")) == TYPE_INT:
			var part_id: String = part["part_id"] as String
			if not part_orders.has(part_id):
				part_orders[part_id] = part["order"] as int

	for index: int in questions.size():
		var value: Variant = questions[index]
		if not value is Dictionary:
			continue
		var question: Dictionary = value as Dictionary
		if not _has_non_empty_string(question, "reward_part_id"):
			continue
		if typeof(question.get("order")) != TYPE_INT:
			continue
		var reward_part_id: String = question["reward_part_id"] as String
		if not part_orders.has(reward_part_id):
			continue
		var question_order: int = question["order"] as int
		if question_order != part_orders[reward_part_id]:
			_add_issue(
				issues,
				ORDER_INVALID,
				_field_path(_item_path("questions", index), "reward_part_id"),
				(
					"Question order %d does not match reward part order %d."
					% [question_order, part_orders[reward_part_id]]
				),
			)


## Ensures the dependency chain follows explicit part order, independent of array layout.
func _validate_dependency_chain(parts: Array, issues: Array[ValidationIssue]) -> void:
	var id_counts: Dictionary[String, int] = _build_id_counts(parts, "part_id")
	var order_counts: Dictionary[int, int] = {}
	var part_id_by_order: Dictionary[int, String] = {}

	for value: Variant in parts:
		if not value is Dictionary:
			continue
		var part: Dictionary = value as Dictionary
		if not _has_non_empty_string(part, "part_id"):
			continue
		var part_id: String = part["part_id"] as String
		if not _is_valid_id(part_id) or id_counts.get(part_id, 0) != 1:
			continue
		if typeof(part.get("order")) != TYPE_INT:
			continue
		var order: int = part["order"] as int
		if order < 1:
			continue
		order_counts[order] = order_counts.get(order, 0) + 1
		if not part_id_by_order.has(order):
			part_id_by_order[order] = part_id

	for index: int in parts.size():
		var value: Variant = parts[index]
		if not value is Dictionary:
			continue
		var part: Dictionary = value as Dictionary
		if not _has_non_empty_string(part, "part_id"):
			continue
		var part_id: String = part["part_id"] as String
		if not _is_valid_id(part_id) or id_counts.get(part_id, 0) != 1:
			continue
		if typeof(part.get("order")) != TYPE_INT:
			continue
		var order: int = part["order"] as int
		if order < 1 or order_counts.get(order, 0) != 1:
			continue
		if not part.has("required_previous_part_id"):
			continue

		var dependency: Variant = part["required_previous_part_id"]
		var dependency_path: String = _field_path(
			_item_path("parts", index), "required_previous_part_id"
		)
		if order == 1:
			if typeof(dependency) == TYPE_STRING:
				_add_issue(
					issues,
					DEPENDENCY_CHAIN_INVALID,
					dependency_path,
					"The order-1 part must have required_previous_part_id set to null.",
				)
			continue

		if typeof(dependency) == TYPE_NIL:
			_add_issue(
				issues,
				DEPENDENCY_CHAIN_INVALID,
				dependency_path,
				"Part order %d must reference the part at order %d." % [order, order - 1],
			)
			continue
		if typeof(dependency) != TYPE_STRING:
			continue
		var dependency_id: String = dependency as String
		if dependency_id.is_empty() or not _is_valid_id(dependency_id):
			continue
		var previous_order: int = order - 1
		if order_counts.get(previous_order, 0) != 1:
			continue
		if not part_id_by_order.has(previous_order):
			continue
		var expected_dependency_id: String = part_id_by_order[previous_order]
		if dependency_id != expected_dependency_id:
			_add_issue(
				issues,
				DEPENDENCY_CHAIN_INVALID,
				dependency_path,
				(
					"Part order %d must depend on '%s', found '%s'."
					% [order, expected_dependency_id, dependency_id]
				),
			)


## Detects cycles in deterministic ID order and reports each distinct cycle once.
func _validate_dependency_cycles(parts: Array, issues: Array[ValidationIssue]) -> void:
	var dependency_by_id: Dictionary[String, String] = {}
	var path_by_id: Dictionary[String, String] = {}
	for index: int in parts.size():
		var value: Variant = parts[index]
		if not value is Dictionary:
			continue
		var part: Dictionary = value as Dictionary
		if not _has_non_empty_string(part, "part_id"):
			continue
		var part_id: String = part["part_id"] as String
		if path_by_id.has(part_id):
			continue
		path_by_id[part_id] = _field_path(_item_path("parts", index), "required_previous_part_id")
		if part.has("required_previous_part_id"):
			var dependency_value: Variant = part["required_previous_part_id"]
			if (
				typeof(dependency_value) == TYPE_STRING
				and not (dependency_value as String).is_empty()
			):
				dependency_by_id[part_id] = dependency_value as String

	var sorted_ids: Array[String] = []
	for part_id: String in path_by_id:
		sorted_ids.append(part_id)
	sorted_ids.sort()
	var complete: Dictionary[String, bool] = {}

	for start_id: String in sorted_ids:
		if complete.has(start_id):
			continue
		var chain: Array[String] = []
		var position: Dictionary[String, int] = {}
		var current_id: String = start_id
		while path_by_id.has(current_id) and not complete.has(current_id):
			if position.has(current_id):
				var cycle: Array[String] = []
				var cycle_start: int = position[current_id]
				for cycle_index: int in range(cycle_start, chain.size()):
					cycle.append(chain[cycle_index])
				_report_cycle(cycle, path_by_id, issues)
				break
			position[current_id] = chain.size()
			chain.append(current_id)
			if not dependency_by_id.has(current_id):
				break
			current_id = dependency_by_id[current_id]
		for visited_id: String in chain:
			complete[visited_id] = true


func _report_cycle(
	cycle: Array[String],
	path_by_id: Dictionary[String, String],
	issues: Array[ValidationIssue],
) -> void:
	if cycle.is_empty():
		return
	var canonical_id: String = cycle[0]
	var canonical_index: int = 0
	for index: int in range(1, cycle.size()):
		if cycle[index] < canonical_id:
			canonical_id = cycle[index]
			canonical_index = index

	var canonical_cycle: Array[String] = []
	for offset: int in cycle.size():
		canonical_cycle.append(cycle[(canonical_index + offset) % cycle.size()])
	canonical_cycle.append(canonical_cycle[0])
	_add_issue(
		issues,
		DEPENDENCY_CYCLE,
		path_by_id[canonical_id],
		"Dependency cycle: %s." % " -> ".join(canonical_cycle),
	)


func _validate_required_fields(
	object: Dictionary,
	field_names: Array[String],
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	for field_name: String in field_names:
		if not object.has(field_name):
			_add_issue(
				issues,
				FIELD_MISSING,
				_field_path(path, field_name),
				"Required field '%s' is missing." % field_name,
			)


func _validate_non_empty_string(
	object: Dictionary,
	field_name: String,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not object.has(field_name):
		return
	var value: Variant = object[field_name]
	var field_path: String = _field_path(path, field_name)
	if typeof(value) != TYPE_STRING:
		_add_issue(
			issues, FIELD_TYPE_INVALID, field_path, "Field '%s' must be a string." % field_name
		)
	elif (value as String).is_empty():
		_add_issue(
			issues, FIELD_VALUE_INVALID, field_path, "Field '%s' cannot be empty." % field_name
		)


func _validate_integer(
	object: Dictionary,
	field_name: String,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if object.has(field_name) and typeof(object[field_name]) != TYPE_INT:
		_add_issue(
			issues,
			FIELD_TYPE_INVALID,
			_field_path(path, field_name),
			"Field '%s' must be an integer." % field_name,
		)


func _validate_nullable_id(
	object: Dictionary,
	field_name: String,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not object.has(field_name):
		return
	var value: Variant = object[field_name]
	if typeof(value) != TYPE_NIL and typeof(value) != TYPE_STRING:
		_add_issue(
			issues,
			FIELD_TYPE_INVALID,
			_field_path(path, field_name),
			"Field '%s' must be null or a non-empty ID string." % field_name,
		)
	elif typeof(value) == TYPE_STRING and (value as String).is_empty():
		_add_issue(
			issues,
			FIELD_VALUE_INVALID,
			_field_path(path, field_name),
			"Field '%s' must be null or a non-empty ID string." % field_name,
		)


func _validate_entity_id(
	object: Dictionary,
	field_name: String,
	path: String,
	issues: Array[ValidationIssue],
) -> void:
	if not _has_non_empty_string(object, field_name):
		return
	var value: String = object[field_name] as String
	if not _is_valid_id(value):
		_add_issue(
			issues,
			ID_FORMAT_INVALID,
			_field_path(path, field_name),
			"ID '%s' must match [a-z0-9_]+." % value,
		)


func _validate_required_array(
	object: Dictionary,
	field_name: String,
	path: String,
	issues: Array[ValidationIssue],
	report_missing: bool = true,
) -> void:
	if not object.has(field_name):
		if report_missing:
			_add_issue(
				issues,
				FIELD_MISSING,
				_field_path(path, field_name),
				"Required array '%s' is missing." % field_name,
			)
	elif not object[field_name] is Array:
		_add_issue(
			issues,
			FIELD_TYPE_INVALID,
			_field_path(path, field_name),
			"Field '%s' must be an array." % field_name,
		)


func _validate_required_dictionary(
	object: Dictionary,
	field_name: String,
	path: String,
	issues: Array[ValidationIssue],
	report_missing: bool = true,
) -> void:
	if not object.has(field_name):
		if report_missing:
			_add_issue(
				issues,
				FIELD_MISSING,
				_field_path(path, field_name),
				"Required object '%s' is missing." % field_name,
			)
	elif not object[field_name] is Dictionary:
		_add_issue(
			issues,
			FIELD_TYPE_INVALID,
			_field_path(path, field_name),
			"Field '%s' must be an object." % field_name,
		)


func _build_id_counts(items: Array, id_field: String) -> Dictionary[String, int]:
	var counts: Dictionary[String, int] = {}
	for value: Variant in items:
		if value is Dictionary:
			var item: Dictionary = value as Dictionary
			if _has_non_empty_string(item, id_field):
				var item_id: String = item[id_field] as String
				counts[item_id] = counts.get(item_id, 0) + 1
	return counts


func _array_or_empty(root: Dictionary, field_name: String) -> Array:
	if root.has(field_name) and root[field_name] is Array:
		return root[field_name] as Array
	return []


func _dictionary_or_empty(root: Dictionary, field_name: String) -> Dictionary:
	if root.has(field_name) and root[field_name] is Dictionary:
		return root[field_name] as Dictionary
	return {}


func _has_non_empty_string(object: Dictionary, field_name: String) -> bool:
	return (
		object.has(field_name)
		and typeof(object[field_name]) == TYPE_STRING
		and not (object[field_name] as String).is_empty()
	)


func _is_valid_id(value: String) -> bool:
	if value.is_empty():
		return false
	for index: int in value.length():
		var character: int = value.unicode_at(index)
		var is_lowercase: bool = character >= 97 and character <= 122
		var is_digit: bool = character >= 48 and character <= 57
		if not is_lowercase and not is_digit and character != 95:
			return false
	return true


func _item_path(container_name: String, index: int) -> String:
	return "$.%s[%d]" % [container_name, index]


func _field_path(base_path: String, field_name: String) -> String:
	return "%s.%s" % [base_path, field_name]


func _add_issue(
	issues: Array[ValidationIssue],
	code: String,
	json_path: String,
	message: String,
) -> void:
	issues.append(ValidationIssue.new(code, json_path, message))
