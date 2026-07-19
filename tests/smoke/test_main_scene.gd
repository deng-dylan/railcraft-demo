extends GutTest


func test_main_scene_loads_and_enters_start_state() -> void:
	var app: AppRoot = await _add_app()

	assert_true(app.is_initialized())
	assert_not_null(app.get_catalog())
	assert_eq(app.get_flow_manager().get_state(), GameFlowManager.GameState.START)
	assert_has(
		app.get_screen_coordinator().get_visible_page_names(),
		ScreenCoordinator.PAGE_START,
	)


func test_main_scene_completes_wrong_then_correct_nine_question_path() -> void:
	var app: AppRoot = await _add_app()
	assert_true(app.is_initialized())
	if not app.is_initialized():
		return
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


func _add_app() -> AppRoot:
	var scene: PackedScene = load("res://scenes/main/main.tscn") as PackedScene
	assert_not_null(scene)
	if scene == null:
		return null
	var app: AppRoot = scene.instantiate() as AppRoot
	assert_not_null(app)
	if app == null:
		return null
	add_child_autofree(app)
	await get_tree().process_frame
	return app
