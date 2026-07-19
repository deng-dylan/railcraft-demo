class_name InventoryManager
extends RefCounted

var _known_part_ids: Dictionary[String, bool] = {}
var _owned_part_ids: Dictionary[String, bool] = {}


## Replaces the known-part set and clears all runtime inventory progress.
func configure(parts: Array[PartData]) -> void:
	_known_part_ids.clear()
	_owned_part_ids.clear()
	for part: PartData in parts:
		_known_part_ids[part.part_id] = true


## Grants a known part at most once and reports a typed, side-effect-safe outcome.
func grant_part(part_id: String) -> GrantResult:
	if not _known_part_ids.has(part_id):
		return GrantResult.new(GrantResult.Status.UNKNOWN_PART)
	if _owned_part_ids.has(part_id):
		return GrantResult.new(GrantResult.Status.ALREADY_OWNED)

	_owned_part_ids[part_id] = true
	return GrantResult.new(GrantResult.Status.GRANTED)


func has_part(part_id: String) -> bool:
	return _owned_part_ids.has(part_id)


## Returns an insertion-ordered copy without exposing the mutable owned set.
func get_owned_part_ids() -> Array[String]:
	var owned_ids: Array[String] = []
	for part_id: String in _owned_part_ids:
		owned_ids.append(part_id)
	return owned_ids


## Clears owned progress while retaining the configured known-part set.
func reset() -> void:
	_owned_part_ids.clear()
