class_name AssemblyOutcome
extends RefCounted

enum Status {
	COMMITTED,
	NO_INSTALL_PENDING,
	PENDING_PART_MISMATCH,
	ALREADY_INSTALLED,
}

var status: Status
var installed_part_id: String
var completed_component_id: String
var train_completed: bool
var next_expected_part_id: String


func _init(
	outcome_status: Status,
	outcome_installed_part_id: String = "",
	outcome_completed_component_id: String = "",
	outcome_train_completed: bool = false,
	outcome_next_expected_part_id: String = "",
) -> void:
	status = outcome_status
	installed_part_id = outcome_installed_part_id
	completed_component_id = outcome_completed_component_id
	train_completed = outcome_train_completed
	next_expected_part_id = outcome_next_expected_part_id


func is_committed() -> bool:
	return status == Status.COMMITTED
