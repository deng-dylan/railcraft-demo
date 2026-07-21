class_name InstallCheck
extends RefCounted

enum Status {
	ALLOWED,
	OUT_OF_ORDER,
	ALREADY_INSTALLED,
	ANOTHER_INSTALL_PENDING,
	UNKNOWN_PART,
	PREREQUISITE_MISSING,
}

var status: Status


func _init(check_status: Status) -> void:
	status = check_status


func is_allowed() -> bool:
	return status == Status.ALLOWED
