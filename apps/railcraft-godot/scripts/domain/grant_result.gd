class_name GrantResult
extends RefCounted

## Stable inventory reward outcomes for flow orchestration.
enum Status {
	GRANTED,
	ALREADY_OWNED,
	UNKNOWN_PART,
}

var status: Status


func _init(grant_status: Status) -> void:
	status = grant_status
