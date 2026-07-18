class_name SourceData
extends RefCounted

## Read-only-use attribution data displayed with a question explanation.
var organization: String
var title: String
var url: String


func _init(source_organization: String, source_title: String, source_url: String) -> void:
	organization = source_organization
	title = source_title
	url = source_url
