extends GutTest


func test_main_scene_loads_enters_start_and_connects_each_signal_once() -> void:
	var app: AppRoot = await _add_app()

	assert_true(app.is_initialized())
	assert_not_null(app.get_catalog())
	assert_eq(app.get_window().min_size, AppRoot.MINIMUM_WINDOW_SIZE)
	assert_eq(app.get_flow_manager().get_state(), GameFlowManager.GameState.START)
	assert_has(
		app.get_screen_coordinator().get_visible_page_names(),
		ScreenCoordinator.PAGE_START,
	)
	assert_eq(app.get_node(^"DomainServices").get_child_count(), 1)
	assert_eq(app.get_node(^"PresentationServices").get_child_count(), 1)
	var screen: ScreenCoordinator = app.get_screen_coordinator()
	var assembly_view: AssemblyView = app.get_node(^"WorldRoot/AssemblyView") as AssemblyView
	assert_eq(screen.start_requested.get_connections().size(), 1)
	assert_eq(screen.answer_selected.get_connections().size(), 1)
	assert_eq(screen.assembly_requested.get_connections().size(), 1)
	assert_eq(screen.exit_requested.get_connections().size(), 1)
	assert_eq(assembly_view.part_clicked.get_connections().size(), 1)


func test_start_exit_cleans_up_once_without_quitting_test_tree() -> void:
	var app: AppRoot = await _add_app()
	watch_signals(app)

	app.get_screen_coordinator().exit_requested.emit()
	app.get_screen_coordinator().exit_requested.emit()

	assert_true(app.is_shutdown_started())
	assert_signal_emit_count(app, "shutdown_requested", 1)


func test_fatal_exit_cleans_up_once_without_domain_transition() -> void:
	var app: AppRoot = await _add_app("res://tests/fixtures/does-not-exist.json")
	watch_signals(app)

	assert_push_error("CONTENT_LOAD_FAILED")
	assert_false(app.is_initialized())
	assert_true(app.get_screen_coordinator().is_fatal())
	assert_eq(
		app.get_screen_coordinator().get_visible_page_names(),
		[ScreenCoordinator.PAGE_FATAL],
	)

	app.get_screen_coordinator().exit_requested.emit()

	assert_true(app.is_shutdown_started())
	assert_signal_emit_count(app, "shutdown_requested", 1)


func test_main_scene_completes_wrong_then_correct_nine_question_path() -> void:
	var app: AppRoot = await _add_app()
	assert_true(app.is_initialized())
	if not app.is_initialized():
		return
	watch_signals(app)
	var flow: GameFlowManager = app.get_flow_manager()
	var catalog: ContentCatalog = app.get_catalog()
	var animation: AnimationCoordinator = (
		app.get_node(^"PresentationServices/AnimationCoordinator") as AnimationCoordinator
	)
	var assembly_view: AssemblyView = app.get_node(^"WorldRoot/AssemblyView") as AssemblyView
	animation.snap_lift_duration = 0.005
	animation.snap_move_duration = 0.005
	animation.snap_settle_duration = 0.005
	animation.component_hold_duration = 0.005
	animation.teaching_hold_duration = 0.005
	animation.final_step_duration = 0.005

	flow.request_start()
	var questions: Array[QuestionData] = catalog.get_questions()
	var parts: Array[PartData] = catalog.get_parts()
	for index: int in questions.size():
		var question: QuestionData = questions[index]
		var wrong_index: int = (question.correct_option_index + 1) % question.options.size()
		flow.select_answer(wrong_index)
		assert_eq(flow.get_state(), GameFlowManager.GameState.WRONG_FEEDBACK)
		flow.select_answer(question.correct_option_index)
		assert_eq(flow.get_state(), GameFlowManager.GameState.CORRECT_FEEDBACK)
		flow.request_assembly()
		assert_eq(flow.get_state(), GameFlowManager.GameState.ASSEMBLY)
		assembly_view.part_clicked.emit(parts[index].part_id)
		await wait_seconds(0.08)

	assert_eq(flow.get_state(), GameFlowManager.GameState.END)
	assert_eq(assembly_view.get_installed_part_ids().size(), 9)
	assert_has(
		app.get_screen_coordinator().get_visible_page_names(),
		ScreenCoordinator.PAGE_END,
	)
	app.get_screen_coordinator().exit_requested.emit()
	assert_true(app.is_shutdown_started())
	assert_signal_emit_count(app, "shutdown_requested", 1)


func _add_app(questions_path: String = ContentRepository.QUESTIONS_PATH) -> AppRoot:
	var scene: PackedScene = load("res://scenes/main/main.tscn") as PackedScene
	assert_not_null(scene)
	if scene == null:
		return null
	var app: AppRoot = scene.instantiate() as AppRoot
	assert_not_null(app)
	if app == null:
		return null
	app.quit_on_shutdown = false
	app.questions_path = questions_path
	add_child_autofree(app)
	await get_tree().process_frame
	return app
