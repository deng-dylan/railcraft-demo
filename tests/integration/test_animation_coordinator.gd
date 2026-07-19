extends GutTest

const AssemblyFixture := preload("res://tests/fixtures/assembly_view_fixture.gd")


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
	await wait_seconds(0.08)

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
	await wait_seconds(0.07)

	assert_signal_emitted_with_parameters(
		coordinator,
		"component_animation_finished",
		[component.component_id],
	)
	assert_false(coordinator.is_busy())
	assert_true(component_node.position.is_equal_approx(start_position))


func test_teaching_component_uses_longer_hold_duration() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.component_hold_duration = 0.01
	coordinator.teaching_hold_duration = 0.08
	var view: AssemblyView = _add_view()
	assert_true(coordinator.inject_view(view))
	var component := (
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

	coordinator.play_component_complete(component)
	await wait_seconds(0.03)
	assert_true(coordinator.is_busy())
	await wait_seconds(0.08)
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
	var wheel_start_x: float = wheel_root.rotation.x

	coordinator.play_final_assembly(load_result.catalog.get_train_recipe())
	await wait_seconds(0.08)

	assert_signal_emitted(coordinator, "final_animation_finished")
	assert_true((train_root.get_node(^"Headlights/LeftLight") as Light3D).visible)
	assert_true((train_root.get_node(^"Headlights/RightLight") as Light3D).visible)
	assert_gt(pantograph_root.position.y, pantograph_start_y)
	assert_gt(wheel_root.rotation.x, wheel_start_x)
	assert_false(coordinator.is_busy())


func test_cancel_releases_busy_lock_and_disables_pending_interaction() -> void:
	var coordinator := AnimationCoordinator.new()
	add_child_autofree(coordinator)
	coordinator.snap_lift_duration = 1.0
	coordinator.snap_move_duration = 1.0
	coordinator.snap_settle_duration = 1.0
	var view: AssemblyView = _add_view()
	var part: PartData = AssemblyFixture.part(0)
	assert_true(view.prepare_part(part), view.last_error)
	assert_true(coordinator.inject_view(view))

	coordinator.play_part_snap(part.part_id)
	assert_true(coordinator.is_busy())
	coordinator.cancel_all_for_shutdown()

	assert_false(coordinator.is_busy())
	assert_false(view.get_part_actor(part.part_id).is_interaction_enabled())


func _add_view() -> AssemblyView:
	var view: AssemblyView = AssemblyFixture.instantiate_view()
	add_child_autofree(view)
	assert_true(view.is_ready_for_parts(), view.last_error)
	return view
