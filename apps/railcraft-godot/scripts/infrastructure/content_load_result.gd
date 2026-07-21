class_name ContentLoadResult
extends RefCounted

var is_success: bool:
	get:
		return _is_success

var catalog: ContentCatalog:
	get:
		return _catalog

var issues: Array[ValidationIssue]:
	get:
		return _issues.duplicate()

var _is_success: bool = false
var _catalog: ContentCatalog = null
var _issues: Array[ValidationIssue] = []


## Creates the only successful state: a complete catalog with no issues.
static func success(loaded_catalog: ContentCatalog) -> ContentLoadResult:
	var result := ContentLoadResult.new()
	if loaded_catalog == null:
		return result
	result._is_success = true
	result._catalog = loaded_catalog
	return result


## Creates a failure state and guarantees that no partial catalog is exposed.
static func failure(load_issues: Array[ValidationIssue]) -> ContentLoadResult:
	var result := ContentLoadResult.new()
	result._issues = load_issues.duplicate()
	return result
