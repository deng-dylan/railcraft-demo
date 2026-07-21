class_name ValidationIssue
extends RefCounted

## A stable, machine-readable content validation result.
enum Severity {
	ERROR,
	WARNING,
}

var code: String
var json_path: String
var message: String
var severity: Severity


func _init(
	issue_code: String,
	issue_json_path: String,
	issue_message: String,
	issue_severity: Severity = Severity.ERROR,
) -> void:
	code = issue_code
	json_path = issue_json_path
	message = issue_message
	severity = issue_severity
