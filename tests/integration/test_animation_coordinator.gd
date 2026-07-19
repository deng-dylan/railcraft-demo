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
