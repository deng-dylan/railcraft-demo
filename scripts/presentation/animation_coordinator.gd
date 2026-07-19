class_name AnimationCoordinator
extends Node

signal part_snap_finished(part_id: String)
signal part_snap_failed(part_id: String, reason: String)
signal component_animation_finished(component_id: String)
signal final_animation_finished

@export var snap_lift_duration: float = 0.15
@export var snap_move_duration: float = 0.60
@export var snap_settle_duration: float = 0.15
@export var component_hold_duration: float = 2.8
@export var teaching_hold_duration: float = 4.0
@export var final_step_duration: float = 0.35

var _view: AssemblyView
var _busy: bool = false
var _active_tween: Tween
var _active_component_id: String = ""
var _active_component_node: Node3D
var _active_component_start_position: Vector3


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
	var lifted_transform: Transform3D = start_transform
	lifted_transform.origin += Vector3.UP * 0.35
	lifted_transform.basis = lifted_transform.basis.scaled(Vector3.ONE * 1.08)
	var overshoot_transform: Transform3D = target_transform
	overshoot_transform.basis = overshoot_transform.basis.scaled(Vector3.ONE * 1.04)

	_active_tween = create_tween()
	_active_tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	_active_tween.tween_property(actor, "global_transform", lifted_transform, snap_lift_duration)
	_active_tween.tween_property(actor, "global_transform", overshoot_transform, snap_move_duration)
	_active_tween.tween_property(actor, "global_transform", target_transform, snap_settle_duration)
	_active_tween.finished.connect(_finish_snap.bind(part_id))


func play_component_complete(component: ComponentRecipe) -> void:
	if component == null or not _begin_operation():
		return
	if _view == null:
		_finish_operation()
		component_animation_finished.emit(component.component_id)
		return
	_active_component_id = component.component_id
	_view.show_component_highlight(component.component_id)
	var hold: float = (
		teaching_hold_duration
		if not component.teaching_note.is_empty()
		else component_hold_duration
	)
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
				_active_component_start_position.y + 0.18,
				movement_duration,
			)
		)
		(
			_active_tween
			. tween_property(
				_active_component_node,
				"position:y",
				_active_component_start_position.y,
				movement_duration,
			)
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

	_active_tween = create_tween()
	for component_id: String in train_recipe.component_ids:
		_active_tween.tween_callback(_view.show_component_highlight.bind(component_id))
		_active_tween.tween_interval(final_step_duration)
		_active_tween.tween_callback(_view.clear_component_highlight.bind(component_id))

	var left_light: Node = train_root.get_node_or_null(^"Headlights/LeftLight")
	var right_light: Node = train_root.get_node_or_null(^"Headlights/RightLight")
	if left_light != null:
		_active_tween.tween_callback(left_light.set.bind("visible", true))
	if right_light != null:
		_active_tween.tween_callback(right_light.set.bind("visible", true))

	var pantograph: PartActor = _view.get_part_actor("pantograph")
	if pantograph != null:
		var lift_root: Node3D = (
			pantograph.get_node_or_null(^"VisualRoot/PantographLiftRoot") as Node3D
		)
		if lift_root != null:
			_active_tween.tween_property(
				lift_root, "position:y", lift_root.position.y + 0.35, final_step_duration
			)

	var wheelset: PartActor = _view.get_part_actor("wheelset")
	if wheelset != null:
		var wheel_root: Node3D = (
			wheelset.get_node_or_null(^"VisualRoot/WheelRotationRoot") as Node3D
		)
		if wheel_root != null:
			_active_tween.tween_property(
				wheel_root, "rotation:x", wheel_root.rotation.x + TAU, final_step_duration
			)

	_active_tween.finished.connect(_finish_final)


func cancel_all_for_shutdown() -> void:
	if _active_tween != null and _active_tween.is_valid():
		_active_tween.kill()
	_active_tween = null
	if _view != null:
		_view.set_part_interaction_enabled(false)
		if not _active_component_id.is_empty():
			_view.clear_component_highlight(_active_component_id)
	_reset_component_position()
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
		_view.set_part_interaction_enabled(true)
	_finish_operation()
	part_snap_finished.emit(part_id)


func _fail_snap(part_id: String, reason: String) -> void:
	if _view != null:
		_view.set_part_interaction_enabled(true)
	_finish_operation()
	part_snap_failed.emit(part_id, reason)


func _finish_component(component_id: String) -> void:
	if _view != null:
		_view.clear_component_highlight(component_id)
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
	_finish_operation()
	final_animation_finished.emit()
