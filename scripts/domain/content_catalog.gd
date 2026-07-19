class_name ContentCatalog
extends RefCounted

var _questions: Array[QuestionData]
var _parts: Array[PartData]
var _components: Array[ComponentRecipe]
var _train_recipe: TrainRecipe
var _questions_by_id: Dictionary[String, QuestionData]
var _parts_by_id: Dictionary[String, PartData]
var _components_by_id: Dictionary[String, ComponentRecipe]


## Builds deterministic order and ID indexes without exposing mutable containers.
func _init(
	catalog_questions: Array[QuestionData],
	catalog_parts: Array[PartData],
	catalog_components: Array[ComponentRecipe],
	catalog_train_recipe: TrainRecipe,
) -> void:
	_questions = catalog_questions.duplicate()
	_parts = catalog_parts.duplicate()
	_components = catalog_components.duplicate()
	_train_recipe = catalog_train_recipe

	_questions.sort_custom(_question_order_less_than)
	_parts.sort_custom(_part_order_less_than)
	_components.sort_custom(_component_order_less_than)

	_questions_by_id = {}
	_parts_by_id = {}
	_components_by_id = {}
	for question: QuestionData in _questions:
		_questions_by_id[question.question_id] = question
	for part: PartData in _parts:
		_parts_by_id[part.part_id] = part
	for component: ComponentRecipe in _components:
		_components_by_id[component.component_id] = component


func get_questions() -> Array[QuestionData]:
	return _questions.duplicate()


func get_parts() -> Array[PartData]:
	return _parts.duplicate()


func get_components() -> Array[ComponentRecipe]:
	return _components.duplicate()


func get_train_recipe() -> TrainRecipe:
	return _train_recipe


func get_question(question_id: String) -> QuestionData:
	if _questions_by_id.has(question_id):
		return _questions_by_id[question_id]
	return null


func get_part(part_id: String) -> PartData:
	if _parts_by_id.has(part_id):
		return _parts_by_id[part_id]
	return null


func get_component(component_id: String) -> ComponentRecipe:
	if _components_by_id.has(component_id):
		return _components_by_id[component_id]
	return null


static func _question_order_less_than(left: QuestionData, right: QuestionData) -> bool:
	return left.order < right.order


static func _part_order_less_than(left: PartData, right: PartData) -> bool:
	return left.order < right.order


static func _component_order_less_than(left: ComponentRecipe, right: ComponentRecipe) -> bool:
	return left.order < right.order
