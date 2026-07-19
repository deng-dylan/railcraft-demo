extends GutTest


func test_start_page_is_the_only_initial_page() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	var visible_pages: Array[StringName] = screen.get_visible_page_names()

	assert_eq(visible_pages.size(), 1)
	assert_has(visible_pages, ScreenCoordinator.PAGE_START)


func test_question_binding_and_answer_signal_keep_wrong_options_available() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	var load_result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(load_result.is_success)
	var question: QuestionData = load_result.catalog.get_questions()[0]
	watch_signals(screen)

	screen.present_question(question, 1, 9)
	assert_has(screen.get_visible_page_names(), ScreenCoordinator.PAGE_QUIZ)
	for index: int in question.options.size():
		assert_eq(screen.get_answer_button(index).text, question.options[index])

	screen.get_answer_button(1).pressed.emit()
	assert_signal_emitted_with_parameters(screen, "answer_selected", [1])
	screen.show_wrong_feedback(GameFlowManager.WRONG_ANSWER_MESSAGE)
	for button_index: int in 4:
		assert_false(screen.get_answer_button(button_index).disabled)


func test_correct_feedback_locks_options_and_component_overlay_is_visible() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	var load_result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(load_result.is_success)
	var question: QuestionData = load_result.catalog.get_questions()[0]
	var component: ComponentRecipe = load_result.catalog.get_components()[0]

	screen.present_question(question, 1, 9)
	screen.show_correct_feedback(question)
	for button_index: int in 4:
		assert_true(screen.get_answer_button(button_index).disabled)

	screen.show_state(GameFlowManager.GameState.COMPONENT_COMPLETE)
	screen.show_component_complete(component)
	assert_has(
		screen.get_visible_page_names(),
		ScreenCoordinator.PAGE_COMPONENT,
	)


func test_fatal_page_replaces_all_other_pages() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	screen.show_fatal("TEST_FAILURE", "测试错误")

	assert_true(screen.is_fatal())
	assert_eq(screen.get_visible_page_names(), [ScreenCoordinator.PAGE_FATAL])


func _add_screen() -> ScreenCoordinator:
	var scene: PackedScene = load("res://scenes/ui/screen_coordinator.tscn") as PackedScene
	var screen: ScreenCoordinator = scene.instantiate() as ScreenCoordinator
	add_child_autofree(screen)
	await get_tree().process_frame
	return screen
