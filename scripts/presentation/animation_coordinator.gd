class_name AnimationCoordinator
extends Node

signal part_snap_finished(part_id: String)
signal part_snap_failed(part_id: String, reason: String)
signal component_animation_finished(component_id: String)
signal final_animation_finished

const SNAP_LIFT_HEIGHT := 0.08
const SNAP_LIFT_SCALE := 1.06
const SNAP_SETTLE_HEIGHT := 0.025
const SNAP_SETTLE_SCALE := 1.015
const COMPONENT_LIFT_HEIGHT := 0.08
const PANTOGRAPH_LIFT_HEIGHT := 0.45
const WHEEL_ROTATION_RADIANS := 0.42

@export var snap_lift_duration: float = 0.15
@export var snap_move_duration: float = 0.60
@export var snap_settle_duration: float = 0.15
@export var component_hold_duration: float = 2.8
@export var teaching_hold_duration: float = 4.0
@export var final_step_duration: float = 0.75

var _view: AssemblyView
var _busy: bool = false
var _active_tween: Tween
var _active_component_id: String = ""
var _active_component_node: Node3D
var _active_component_start_position: Vector3
var _active_snap_actor: PartActor
var _active_snap_start_transform: Transform3D = Transform3D.IDENTITY
var _final_original_light_states: Dictionary = {}
var _final_original_transforms: Dictionary = {}


func inject_view(view: AssemblyView) -> bool:
	if _busy or view == null:
		return false
	_view = view
	return true


func is_busy() -> bool:
	return _busy


func play_part_snap(part_id: String) -> void:
	if not _begin_operation():
		return
	if _view == null:
		_fail_snap(part_id, "AssemblyView is unavailable")
		return
	var actor: PartActor = _view.get_part_actor(part_id)
	if actor == null:
		_fail_snap(part_id, "Part actor is unavailable")
		return
	var target_transform: Transform3D = _view.get_snap_transform(part_id)
	if target_transform == Transform3D.IDENTITY and _view.get_snap_target(part_id) == null:
		_fail_snap(part_id, _view.last_error)
		return

	_view.set_part_interaction_enabled(false)
	var start_transform: Transform3D = actor.global_transform
	_active_snap_actor = actor
	_active_snap_start_transform = start_transform
	var lifted_transform: Transform3D = start_transform
	lifted_transform.origin += Vector3.UP * SNAP_LIFT_HEIGHT
	lifted_transform.basis = lifted_transform.basis.scaled(Vector3.ONE * SNAP_LIFT_SCALE)
	var settle_transform: Transform3D = target_transform
	settle_transform.origin += Vector3.UP * SNAP_SETTLE_HEIGHT
	settle_transform.basis = settle_transform.basis.scaled(Vector3.ONE * SNAP_SETTLE_SCALE)

	_active_tween = create_tween()
	(
		_active_tween
		. tween_property(actor, "global_transform", lifted_transform, snap_lift_duration)
		. set_trans(Tween.TRANS_QUAD)
		. set_ease(Tween.EASE_OUT)
	)
	(
		_active_tween
		. tween_property(actor, "global_transform", target_transform, snap_move_duration)
		. set_trans(Tween.TRANS_CUBIC)
		. set_ease(Tween.EASE_IN_OUT)
	)
	var settle_half_duration: float = snap_settle_duration * 0.5
	(
		_active_tween
		. tween_property(
			actor,
			"global_transform",
			settle_transform,
			settle_half_duration,
		)
		. set_trans(Tween.TRANS_BACK)
		. set_ease(Tween.EASE_OUT)
	)
	(
		_active_tween
		. tween_property(
			actor,
			"global_transform",
			target_transform,
			settle_half_duration,
		)
		. set_trans(Tween.TRANS_QUAD)
		. set_ease(Tween.EASE_IN)
	)
	_active_tween.finished.connect(_finish_snap.bind(part_id))


func play_component_complete(component: ComponentRecipe) -> void:
	if component == null or not _begin_operation():
		return
	if _view == null:
		_finish_operation()
		component_animation_finished.emit(component.component_id)
		return
	_active_component_id = component.component_id
	_view.set_part_interaction_enabled(false)
	_view.show_component_highlight(component.component_id)
	var hold: float = teaching_hold_duration if component.order == 3 else component_hold_duration
	_active_component_node = _get_component_node(component.component_id)
	_active_tween = create_tween()
	if _active_component_node != null:
		_active_component_start_position = _active_component_node.position
		var movement_duration: float = minf(0.15, hold / 3.0)
		(
			_active_tween
			. tween_property(
				_active_component_node,
				"position:y",
				_active_component_start_position.y + COMPONENT_LIFT_HEIGHT,
				movement_duration,
			)
			. set_trans(Tween.TRANS_SINE)
			. set_ease(Tween.EASE_OUT)
		)
		(
			_active_tween
			. tween_property(
				_active_component_node,
				"position:y",
				_active_component_start_position.y,
				movement_duration,
			)
			. set_trans(Tween.TRANS_SINE)
			. set_ease(Tween.EASE_IN)
		)
		_active_tween.tween_interval(maxf(0.0, hold - movement_duration * 2.0))
	else:
		_active_tween.tween_interval(hold)
	_active_tween.finished.connect(_finish_component.bind(component.component_id))


func play_final_assembly(train_recipe: TrainRecipe) -> void:
	if train_recipe == null or not _begin_operation():
		return
	if _view == null:
		_finish_operation()
		final_animation_finished.emit()
		return
	var train_root: Node = _view.get_node_or_null(_view.train_assembly_root_path)
	if train_root == null:
		_finish_operation()
		final_animation_finished.emit()
		return

	_view.set_part_interaction_enabled(false)
	_active_tween = create_tween()
	for component_id: String in train_recipe.component_ids:
		_active_tween.tween_callback(_show_component_highlight.bind(component_id))
		_active_tween.tween_interval(final_step_duration)
		_active_tween.tween_callback(_clear_component_highlight.bind(component_id))

	var left_light: Light3D = train_root.get_node_or_null(^"Headlights/LeftLight") as Light3D
	var right_light: Light3D = train_root.get_node_or_null(^"Headlights/RightLight") as Light3D
	var has_light_animation := false
	if left_light != null:
		_prepare_light_animation(left_light)
		(
			_active_tween
			. tween_property(
				left_light,
				"light_energy",
				_final_original_light_states[left_light]["energy"],
				final_step_duration,
			)
		)
		has_light_animation = true
	if right_light != null:
		_prepare_light_animation(right_light)
		if has_light_animation:
			(
				_active_tween
				. parallel()
				. tween_property(
					right_light,
					"light_energy",
					_final_original_light_states[right_light]["energy"],
					final_step_duration,
				)
			)
		else:
			(
				_active_tween
				. tween_property(
					right_light,
					"light_energy",
					_final_original_light_states[right_light]["energy"],
					final_step_duration,
				)
			)
		has_light_animation = true
	if not has_light_animation:
		_active_tween.tween_interval(final_step_duration)

	var pantograph: PartActor = _view.get_part_actor("pantograph")
	var has_pantograph_animation := false
	if pantograph != null:
		var lift_root: Node3D = (
			pantograph.get_node_or_null(^"VisualRoot/PantographLiftRoot") as Node3D
		)
		if lift_root != null:
			_remember_final_transform(lift_root)
			(
				_active_tween
				. tween_property(
					lift_root,
					"position:y",
					lift_root.position.y + PANTOGRAPH_LIFT_HEIGHT,
					final_step_duration,
				)
			)
			has_pantograph_animation = true
	if not has_pantograph_animation:
		_active_tween.tween_interval(final_step_duration)

	var wheelset: PartActor = _view.get_part_actor("wheelset")
	var has_wheel_animation := false
	if wheelset != null:
		var wheel_root: Node3D = (
			wheelset.get_node_or_null(^"VisualRoot/WheelRotationRoot") as Node3D
		)
		if wheel_root != null:
			_remember_final_transform(wheel_root)
			(
				_active_tween
				. tween_property(
					wheel_root,
					"rotation:z",
					wheel_root.rotation.z + WHEEL_ROTATION_RADIANS,
					final_step_duration,
				)
			)
			has_wheel_animation = true
	if not has_wheel_animation:
		_active_tween.tween_interval(final_step_duration)

	_active_tween.finished.connect(_finish_final)


func cancel_all_for_shutdown() -> void:
	if _active_tween != null and _active_tween.is_valid():
		_active_tween.kill()
	_active_tween = null
	if _view != null:
		_view.set_part_interaction_enabled(true)
		if not _active_component_id.is_empty():
			_view.clear_component_highlight(_active_component_id)
	if is_instance_valid(_active_snap_actor):
		_active_snap_actor.global_transform = _active_snap_start_transform
	_active_snap_actor = null
	_reset_component_position()
	_restore_final_states()
	_active_component_id = ""
	_busy = false


func _begin_operation() -> bool:
	if _busy:
		return false
	_busy = true
	return true


func _finish_operation() -> void:
	_active_tween = null
	_busy = false


func _finish_snap(part_id: String) -> void:
	if _view != null:
		_view.finalize_visual_install(part_id)
		if not _view.last_error.is_empty():
			_fail_snap(part_id, _view.last_error)
			return
		_view.set_part_interaction_enabled(true)
	_active_snap_actor = null
	_finish_operation()
	part_snap_finished.emit(part_id)


func _fail_snap(part_id: String, reason: String) -> void:
	if is_instance_valid(_active_snap_actor):
		_active_snap_actor.global_transform = _active_snap_start_transform
	_active_snap_actor = null
	if _view != null:
		_view.set_part_interaction_enabled(true)
	_finish_operation()
	part_snap_failed.emit(part_id, reason)


func _finish_component(component_id: String) -> void:
	if _view != null:
		_view.clear_component_highlight(component_id)
		_view.set_part_interaction_enabled(true)
	_reset_component_position()
	_active_component_id = ""
	_finish_operation()
	component_animation_finished.emit(component_id)


func _get_component_node(component_id: String) -> Node3D:
	if _view == null or not AssemblyView.COMPONENT_CONTAINER_PATHS.has(component_id):
		return null
	var train_root: Node = _view.get_node_or_null(_view.train_assembly_root_path)
	if train_root == null:
		return null
	var component_path: NodePath = AssemblyView.COMPONENT_CONTAINER_PATHS[component_id]
	return train_root.get_node_or_null(component_path) as Node3D


func _reset_component_position() -> void:
	if _active_component_node != null:
		_active_component_node.position = _active_component_start_position
	_active_component_node = null
	_active_component_start_position = Vector3.ZERO


func _finish_final() -> void:
	_final_original_light_states.clear()
	_final_original_transforms.clear()
	if _view != null:
		_view.set_part_interaction_enabled(true)
	_finish_operation()
	final_animation_finished.emit()


func _show_component_highlight(component_id: String) -> void:
	_active_component_id = component_id
	if _view != null:
		_view.show_component_highlight(component_id)


func _clear_component_highlight(component_id: String) -> void:
	if _view != null:
		_view.clear_component_highlight(component_id)
	if _active_component_id == component_id:
		_active_component_id = ""


func _prepare_light_animation(light: Light3D) -> void:
	_final_original_light_states[light] = {
		"visible": light.visible,
		"energy": light.light_energy,
	}
	light.visible = true
	light.light_energy = 0.0


func _remember_final_transform(node: Node3D) -> void:
	if not _final_original_transforms.has(node):
		_final_original_transforms[node] = node.transform


func _restore_final_states() -> void:
	for light_value: Variant in _final_original_light_states:
		if is_instance_valid(light_value) and light_value is Light3D:
			var light: Light3D = light_value as Light3D
			var state: Dictionary = _final_original_light_states[light]
			light.visible = state["visible"]
			light.light_energy = state["energy"]
	_final_original_light_states.clear()
	for node_value: Variant in _final_original_transforms:
		if is_instance_valid(node_value) and node_value is Node3D:
			(node_value as Node3D).transform = _final_original_transforms[node_value]
	_final_original_transforms.clear()
