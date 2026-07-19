class_name AssemblyManager
extends RefCounted

var _ordered_parts: Array[PartData] = []
var _components: Array[ComponentRecipe] = []
var _train_recipe: TrainRecipe = null
var _parts_by_id: Dictionary[String, PartData] = {}
var _components_by_id: Dictionary[String, ComponentRecipe] = {}
var _installed_part_ids: Dictionary[String, bool] = {}
var _completed_component_ids: Dictionary[String, bool] = {}
var _pending_part_id: String = ""
var _next_part_index: int = 0
var _train_completed: bool = false


## Copies the content configuration and starts a fresh assembly transaction history.
func configure(
	parts: Array[PartData],
	components: Array[ComponentRecipe],
	train_recipe: TrainRecipe,
) -> void:
	_ordered_parts = parts.duplicate()
	_components = components.duplicate()
	_train_recipe = train_recipe
	_ordered_parts.sort_custom(_part_order_less_than)
	_components.sort_custom(_component_order_less_than)

	_parts_by_id.clear()
	_components_by_id.clear()
	for part: PartData in _ordered_parts:
		_parts_by_id[part.part_id] = part
	for component: ComponentRecipe in _components:
		_components_by_id[component.component_id] = component
	reset()


func get_expected_part_id() -> String:
	if _next_part_index >= _ordered_parts.size():
		return ""
	return _ordered_parts[_next_part_index].part_id


## Checks the complete begin boundary without mutating pending or committed state.
func can_begin_install(part_id: String) -> InstallCheck:
	if not _pending_part_id.is_empty():
		return InstallCheck.new(InstallCheck.Status.ANOTHER_INSTALL_PENDING)
	if not _parts_by_id.has(part_id):
		return InstallCheck.new(InstallCheck.Status.UNKNOWN_PART)
	if _installed_part_ids.has(part_id):
		return InstallCheck.new(InstallCheck.Status.ALREADY_INSTALLED)
	if part_id != get_expected_part_id():
		return InstallCheck.new(InstallCheck.Status.OUT_OF_ORDER)

	var part: PartData = _parts_by_id[part_id]
	if part.has_required_previous_part():
		if not _installed_part_ids.has(part.required_previous_part_id):
			return InstallCheck.new(InstallCheck.Status.PREREQUISITE_MISSING)
	return InstallCheck.new(InstallCheck.Status.ALLOWED)


## Opens one installation transaction while leaving the installed set unchanged.
func begin_install(part_id: String) -> InstallCheck:
	var check: InstallCheck = can_begin_install(part_id)
	if check.is_allowed():
		_pending_part_id = part_id
	return check


## Cancels only the matching open transaction; stale animation callbacks are ignored.
func abort_pending_install(part_id: String) -> void:
	if part_id == _pending_part_id:
		_pending_part_id = ""


## Atomically commits the matching pending part and derives one-time completions.
func commit_install(part_id: String) -> AssemblyOutcome:
	if _pending_part_id.is_empty():
		if _installed_part_ids.has(part_id):
			return _rejected_outcome(AssemblyOutcome.Status.ALREADY_INSTALLED)
		return _rejected_outcome(AssemblyOutcome.Status.NO_INSTALL_PENDING)
	if part_id != _pending_part_id:
		return _rejected_outcome(AssemblyOutcome.Status.PENDING_PART_MISMATCH)
	if _installed_part_ids.has(part_id):
		return _rejected_outcome(AssemblyOutcome.Status.ALREADY_INSTALLED)

	_installed_part_ids[part_id] = true
	_pending_part_id = ""
	_next_part_index += 1
	var completed_component_id: String = _complete_component_for_part(part_id)
	_complete_train_if_ready()
	return (
		AssemblyOutcome
		. new(
			AssemblyOutcome.Status.COMMITTED,
			part_id,
			completed_component_id,
			_train_completed,
			get_expected_part_id(),
		)
	)


func is_part_installed(part_id: String) -> bool:
	return _installed_part_ids.has(part_id)


func is_train_completed() -> bool:
	return _train_completed


func get_pending_part_id() -> String:
	return _pending_part_id


func get_installed_part_ids() -> Array[String]:
	var installed_ids: Array[String] = []
	for part: PartData in _ordered_parts:
		if _installed_part_ids.has(part.part_id):
			installed_ids.append(part.part_id)
	return installed_ids


func get_completed_component_ids() -> Array[String]:
	var completed_ids: Array[String] = []
	for component: ComponentRecipe in _components:
		if _completed_component_ids.has(component.component_id):
			completed_ids.append(component.component_id)
	return completed_ids


## Clears all mutable progress while retaining the current content configuration.
func reset() -> void:
	_installed_part_ids.clear()
	_completed_component_ids.clear()
	_pending_part_id = ""
	_next_part_index = 0
	_train_completed = false


func _complete_component_for_part(part_id: String) -> String:
	var part: PartData = _parts_by_id[part_id]
	if not _components_by_id.has(part.component_id):
		return ""
	if _completed_component_ids.has(part.component_id):
		return ""

	var component: ComponentRecipe = _components_by_id[part.component_id]
	for recipe_part_id: String in component.part_ids:
		if not _installed_part_ids.has(recipe_part_id):
			return ""
	_completed_component_ids[component.component_id] = true
	return component.component_id


func _complete_train_if_ready() -> void:
	if _train_completed or _train_recipe == null:
		return
	for component_id: String in _train_recipe.component_ids:
		if not _completed_component_ids.has(component_id):
			return
	_train_completed = true


func _rejected_outcome(status: AssemblyOutcome.Status) -> AssemblyOutcome:
	return AssemblyOutcome.new(status, "", "", false, get_expected_part_id())


static func _part_order_less_than(left: PartData, right: PartData) -> bool:
	return left.order < right.order


static func _component_order_less_than(left: ComponentRecipe, right: ComponentRecipe) -> bool:
	return left.order < right.order
