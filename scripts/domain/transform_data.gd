class_name TransformData
extends RefCounted

## Typed transform values loaded from content JSON.
var position: Vector3
var rotation_degrees: Vector3
var scale: Vector3


func _init(
	transform_position: Vector3 = Vector3.ZERO,
	transform_rotation_degrees: Vector3 = Vector3.ZERO,
	transform_scale: Vector3 = Vector3.ONE,
) -> void:
	position = transform_position
	rotation_degrees = transform_rotation_degrees
	scale = transform_scale


## Converts the JSON-friendly degree representation into a Godot Transform3D.
func to_transform_3d() -> Transform3D:
	var rotation_radians: Vector3 = rotation_degrees * (PI / 180.0)
	var basis: Basis = Basis.from_euler(rotation_radians).scaled(scale)
	return Transform3D(basis, position)
