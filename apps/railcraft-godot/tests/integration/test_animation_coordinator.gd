extends GutTest

const AssemblyFixture := preload("res://tests/fixtures/assembly_view_fixture.gd")


class RecordingAssemblyView:
	extends AssemblyView
	var highlight_order: Array[String] = []
	var clear_order: Array[String] = []

	func show_component_highlight(component_id: String) -> void:
		highlight_order.append(component_id)

	func clear_component_highlight(component_id: String) -> void:
		clear_order.append(component_id)


func test_default_durations_and_motion_limits_match_design() -> void:
	var coordinator := AnimationCoordinator.new()

	assert_eq(coordinator.snap_lift_duration, 0.15)
	assert_eq(coordinator.snap_move_duration, 0.60)
	assert_eq(coordinator.snap_settle_duration, 0.15)
	assert_eq(coordinator.component_hold_duration, 2.8)
	assert_eq(coordinator.teaching_hold_duration, 4.0)
	assert_eq(coordinator.final_step_duration * 6.0, 4.5)
	assert_lte(AnimationCoordinator.COMPONENT_LIFT_HEIGHT, 0.08)
	coordinator.free()


func test_missing_view_fails_snap_and_releases_busy_lock() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	watch_signals(coordinator)

	coordinator.play_part_snap("body_shell")

	assert_signal_emitted_with_parameters(
		coordinator,
		"part_snap_failed",
		["body_shell", "AssemblyView is unavailable"],
	)
	assert_false(coordinator.is_busy())


func test_inject_view_rejects_null_and_accepts_ready_view() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	var view: AssemblyView = _add_view()

	assert_false(coordinator.inject_view(null))
	assert_true(coordinator.inject_view(view))


func test_snap_first_stage_lifts_and_scales_actor() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.snap_lift_duration = 0.30
	coordinator.snap_move_duration = 0.30
	coordinator.snap_settle_duration = 0.30
	var view: AssemblyView = _add_view()
	var part: PartData = AssemblyFixture.part(0)
	assert_true(view.prepare_part(part), view.last_error)
	assert_true(coordinator.inject_view(view))
	var actor: PartActor = view.get_part_actor(part.part_id)
	var start_transform: Transform3D = actor.global_transform

	coordinator.play_part_snap(part.part_id)
	await wait_seconds(0.12)

	assert_true(coordinator.is_busy())
	assert_gt(actor.global_transform.origin.y, start_transform.origin.y)
	assert_gt(
		actor.global_transform.basis.get_scale().length(),
		start_transform.basis.get_scale().length(),
	)
	coordinator.cancel_all_for_shutdown()


func test_part_snap_finishes_once_and_commits_visual_install() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.snap_lift_duration = 0.01
	coordinator.snap_move_duration = 0.01
	coordinator.snap_settle_duration = 0.01
	var view: AssemblyView = _add_view()
	var part: PartData = AssemblyFixture.part(0)
	assert_true(view.prepare_part(part), view.last_error)
	assert_true(coordinator.inject_view(view))
	watch_signals(coordinator)

	coordinator.play_part_snap(part.part_id)
	coordinator.play_part_snap(part.part_id)
	assert_true(coordinator.is_busy())
	await _wait_until_idle(coordinator)

	assert_signal_emit_count(coordinator, "part_snap_finished", 1)
	assert_signal_emitted_with_parameters(coordinator, "part_snap_finished", [part.part_id])
	assert_false(coordinator.is_busy())
	assert_true(part.part_id in view.get_installed_part_ids())
	assert_false(view.get_part_actor(part.part_id).is_interaction_enabled())


func test_component_without_view_completes_deterministically() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	var component := (
		ComponentRecipe
		. new(
			"carbody_connection",
			"车体与连接组件",
			1,
			["body_shell", "passenger_door", "coupler_buffer"],
			"组件完成",
			"",
		)
	)
	watch_signals(coordinator)

	coordinator.play_component_complete(component)

	assert_signal_emitted_with_parameters(
		coordinator,
		"component_animation_finished",
		[component.component_id],
	)
	assert_false(coordinator.is_busy())


func test_component_animation_restores_component_position_and_lock() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.component_hold_duration = 0.03
	var view: AssemblyView = _add_view()
	for index: int in 3:
		var part: PartData = AssemblyFixture.part(index)
		assert_true(view.prepare_part(part), view.last_error)
		view.finalize_visual_install(part.part_id)
	assert_true(coordinator.inject_view(view))
	var component_node: Node3D = (
		view.get_node(^"TrainAssemblyRoot/Components/CarbodyConnection") as Node3D
	)
	var start_position: Vector3 = component_node.position
	var component := (
		ComponentRecipe
		. new(
			"carbody_connection",
			"车体与连接组件",
			1,
			["body_shell", "passenger_door", "coupler_buffer"],
			"组件完成",
			"",
		)
	)
	watch_signals(coordinator)

	coordinator.play_component_complete(component)
	assert_true(coordinator.is_busy())
	await _wait_until_idle(coordinator)

	assert_signal_emitted_with_parameters(
		coordinator,
		"component_animation_finished",
		[component.component_id],
	)
	assert_false(coordinator.is_busy())
	assert_true(component_node.position.is_equal_approx(start_position))


func test_only_third_component_uses_longer_hold_duration() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.component_hold_duration = 0.01
	coordinator.teaching_hold_duration = 0.08
	var view: AssemblyView = _add_view()
	assert_true(coordinator.inject_view(view))
	var ordinary_component := (
		ComponentRecipe
		. new(
			"carbody_connection",
			"车体与连接组件",
			1,
			[],
			"组件完成",
			"普通组件也有教学说明",
		)
	)
	var third_component := (
		ComponentRecipe
		. new(
			"traction_power",
			"牵引供电组件",
			3,
			[],
			"组件完成",
			"教学说明",
		)
	)

	coordinator.play_component_complete(ordinary_component)
	await _wait_until_idle(coordinator)
	assert_false(coordinator.is_busy())

	coordinator.play_component_complete(third_component)
	await wait_seconds(0.03)
	assert_true(coordinator.is_busy())
	await _wait_until_idle(coordinator)
	assert_false(coordinator.is_busy())


func test_final_animation_sets_train_feedback_nodes() -> void:
	var load_result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(load_result.is_success)
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.final_step_duration = 0.005
	var view: AssemblyView = _add_view()
	for part: PartData in load_result.catalog.get_parts():
		assert_true(view.prepare_part(part), view.last_error)
		view.finalize_visual_install(part.part_id)
	assert_true(coordinator.inject_view(view))
	watch_signals(coordinator)
	var train_root: Node3D = view.get_node(^"TrainAssemblyRoot") as Node3D
	var pantograph_root: Node3D = (
		view.get_part_actor("pantograph").get_node(^"VisualRoot/PantographLiftRoot") as Node3D
	)
	var wheel_root: Node3D = (
		view.get_part_actor("wheelset").get_node(^"VisualRoot/WheelRotationRoot") as Node3D
	)
	var pantograph_start_y: float = pantograph_root.position.y
	var wheel_start_z: float = wheel_root.rotation.z

	coordinator.play_final_assembly(load_result.catalog.get_train_recipe())
	await _wait_until_idle(coordinator)

	assert_signal_emitted(coordinator, "final_animation_finished")
	assert_true((train_root.get_node(^"Headlights/LeftLight") as Light3D).visible)
	assert_true((train_root.get_node(^"Headlights/RightLight") as Light3D).visible)
	assert_almost_eq(
		pantograph_root.position.y,
		pantograph_start_y + AnimationCoordinator.PANTOGRAPH_LIFT_HEIGHT,
		0.001,
	)
	assert_almost_eq(
		wheel_root.rotation.z,
		wheel_start_z + AnimationCoordinator.WHEEL_ROTATION_RADIANS,
		0.001,
	)
	assert_false(coordinator.is_busy())


func test_final_component_highlights_follow_recipe_order() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.final_step_duration = 0.001
	var view := RecordingAssemblyView.new()
	var train_root := Node3D.new()
	train_root.name = "TrainAssemblyRoot"
	view.add_child(train_root)
	add_child_autofree(view)
	assert_true(coordinator.inject_view(view))
	var recipe := (
		TrainRecipe
		. new(
			"train",
			"测试列车",
			["running_braking", "carbody_connection", "traction_power"],
		)
	)

	coordinator.play_final_assembly(recipe)
	await _wait_until_idle(coordinator)

	assert_eq(view.highlight_order, recipe.component_ids)
	assert_eq(view.clear_order, recipe.component_ids)


func test_cancel_restores_snap_transform_interaction_and_busy_lock() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.snap_lift_duration = 1.0
	coordinator.snap_move_duration = 1.0
	coordinator.snap_settle_duration = 1.0
	var view: AssemblyView = _add_view()
	var part: PartData = AssemblyFixture.part(0)
	assert_true(view.prepare_part(part), view.last_error)
	assert_true(coordinator.inject_view(view))
	var actor: PartActor = view.get_part_actor(part.part_id)
	var start_transform: Transform3D = actor.global_transform

	coordinator.play_part_snap(part.part_id)
	assert_true(coordinator.is_busy())
	await wait_seconds(0.03)
	coordinator.cancel_all_for_shutdown()

	assert_false(coordinator.is_busy())
	assert_true(actor.global_transform.is_equal_approx(start_transform))
	assert_true(actor.is_interaction_enabled())


func test_cancel_restores_component_highlight_and_position() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.component_hold_duration = 1.0
	var view: AssemblyView = _add_view()
	for index: int in 3:
		var part: PartData = AssemblyFixture.part(index)
		assert_true(view.prepare_part(part), view.last_error)
		view.finalize_visual_install(part.part_id)
	assert_true(coordinator.inject_view(view))
	var component_node: Node3D = (
		view.get_node(^"TrainAssemblyRoot/Components/CarbodyConnection") as Node3D
	)
	var start_position: Vector3 = component_node.position
	var highlighted_mesh: GeometryInstance3D = _first_geometry(view.get_part_actor("body_shell"))
	var start_material: Material = highlighted_mesh.material_override
	var component := (
		ComponentRecipe
		. new(
			"carbody_connection",
			"车体与连接组件",
			1,
			["body_shell", "passenger_door", "coupler_buffer"],
			"组件完成",
			"教学说明",
		)
	)

	coordinator.play_component_complete(component)
	await wait_seconds(0.03)
	assert_ne(highlighted_mesh.material_override, start_material)
	coordinator.cancel_all_for_shutdown()

	assert_false(coordinator.is_busy())
	assert_true(component_node.position.is_equal_approx(start_position))
	assert_eq(highlighted_mesh.material_override, start_material)


func test_cancel_restores_final_feedback_states() -> void:
	var load_result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(load_result.is_success)
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.final_step_duration = 0.5
	var view: AssemblyView = _add_view()
	for part: PartData in load_result.catalog.get_parts():
		assert_true(view.prepare_part(part), view.last_error)
		view.finalize_visual_install(part.part_id)
	assert_true(coordinator.inject_view(view))
	var train_root: Node3D = view.get_node(^"TrainAssemblyRoot") as Node3D
	var left_light: Light3D = train_root.get_node(^"Headlights/LeftLight") as Light3D
	var right_light: Light3D = train_root.get_node(^"Headlights/RightLight") as Light3D
	var left_visible: bool = left_light.visible
	var right_visible: bool = right_light.visible
	var left_energy: float = left_light.light_energy
	var right_energy: float = right_light.light_energy
	var pantograph_root: Node3D = (
		view.get_part_actor("pantograph").get_node(^"VisualRoot/PantographLiftRoot") as Node3D
	)
	var wheel_root: Node3D = (
		view.get_part_actor("wheelset").get_node(^"VisualRoot/WheelRotationRoot") as Node3D
	)
	var pantograph_transform: Transform3D = pantograph_root.transform
	var wheel_transform: Transform3D = wheel_root.transform

	coordinator.play_final_assembly(load_result.catalog.get_train_recipe())
	await wait_seconds(0.03)
	coordinator.cancel_all_for_shutdown()

	assert_false(coordinator.is_busy())
	assert_eq(left_light.visible, left_visible)
	assert_eq(right_light.visible, right_visible)
	assert_eq(left_light.light_energy, left_energy)
	assert_eq(right_light.light_energy, right_energy)
	assert_true(pantograph_root.transform.is_equal_approx(pantograph_transform))
	assert_true(wheel_root.transform.is_equal_approx(wheel_transform))


func _wait_until_idle(coordinator: AnimationCoordinator, timeout_msec: int = 2500) -> void:
	var deadline: int = Time.get_ticks_msec() + timeout_msec
	while coordinator.is_busy() and Time.get_ticks_msec() < deadline:
		await get_tree().process_frame
	assert_false(coordinator.is_busy(), "Animation did not finish within the timeout")


func _add_view() -> AssemblyView:
	var view: AssemblyView = AssemblyFixture.instantiate_view()
	add_child_autofree(view)
	assert_true(view.is_ready_for_parts(), view.last_error)
	return view


func _first_geometry(node: Node) -> GeometryInstance3D:
	if node is GeometryInstance3D:
		return node as GeometryInstance3D
	for child: Node in node.get_children():
		var found: GeometryInstance3D = _first_geometry(child)
		if found != null:
			return found
	return null
