extends GutTest


func test_start_page_is_the_only_initial_page() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	var visible_pages: Array[StringName] = screen.get_visible_page_names()

	assert_eq(visible_pages.size(), 1)
	assert_has(visible_pages, ScreenCoordinator.PAGE_START)


func test_start_end_and_fatal_buttons_emit_only_exit_intents() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	watch_signals(screen)
	var start_page: Control = screen.get_node(NodePath(ScreenCoordinator.PAGE_START)) as Control
	var start_button: Button = start_page.find_child("StartButton", true, false) as Button
	var start_exit: Button = start_page.find_child("ExitButton", true, false) as Button

	start_button.pressed.emit()
	start_exit.pressed.emit()
	assert_signal_emit_count(screen, "start_requested", 1)
	assert_signal_emit_count(screen, "exit_requested", 1)

	screen.show_state(GameFlowManager.GameState.END)
	var end_page: Control = screen.get_node(NodePath(ScreenCoordinator.PAGE_END)) as Control
	var end_exit: Button = end_page.find_child("EndExitButton", true, false) as Button
	end_exit.pressed.emit()
	assert_signal_emit_count(screen, "exit_requested", 2)

	screen.show_fatal("TEST_FAILURE", "测试错误")
	var fatal_page: Control = screen.get_node(NodePath(ScreenCoordinator.PAGE_FATAL)) as Control
	var fatal_exit: Button = fatal_page.find_child("FatalExitButton", true, false) as Button
	fatal_exit.pressed.emit()
	assert_signal_emit_count(screen, "exit_requested", 3)


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


func test_correct_feedback_locks_options_and_displays_all_source_fields() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	var load_result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(load_result.is_success)
	var question: QuestionData = load_result.catalog.get_questions()[0]
	watch_signals(screen)

	screen.present_question(question, 1, 9)
	screen.show_correct_feedback(question)
	for button_index: int in 4:
		assert_true(screen.get_answer_button(button_index).disabled)
	var quiz_page: Control = screen.get_node(NodePath(ScreenCoordinator.PAGE_QUIZ)) as Control
	var feedback: RichTextLabel = (
		quiz_page.find_child("FeedbackLabel", true, false) as RichTextLabel
	)
	assert_true(feedback.text.contains(question.explanation))
	assert_true(feedback.text.contains(question.source.organization))
	assert_true(feedback.text.contains(question.source.title))
	assert_true(feedback.text.contains(question.source.url))
	assert_ne(feedback.autowrap_mode, TextServer.AUTOWRAP_OFF)
	var assembly_button: Button = quiz_page.find_child("AssemblyButton", true, false) as Button
	assembly_button.pressed.emit()
	assert_signal_emit_count(screen, "assembly_requested", 1)


func test_component_overlay_is_visible_and_contains_teaching_note() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	var load_result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(load_result.is_success)
	var components: Array[ComponentRecipe] = load_result.catalog.get_components()
	var component: ComponentRecipe = components[components.size() - 1]

	screen.show_state(GameFlowManager.GameState.COMPONENT_COMPLETE)
	screen.show_component_complete(component)
	assert_has(screen.get_visible_page_names(), ScreenCoordinator.PAGE_COMPONENT)
	var page: Control = screen.get_node(NodePath(ScreenCoordinator.PAGE_COMPONENT)) as Control
	var message_nodes: Array[Node] = page.find_children("*", "RichTextLabel", true, false)
	assert_eq(message_nodes.size(), 1)
	var message: RichTextLabel = message_nodes[0] as RichTextLabel
	assert_true(message.text.contains(component.teaching_note))


func test_all_eight_states_have_deterministic_page_mapping() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	var expected: Dictionary = {
		GameFlowManager.GameState.START: [ScreenCoordinator.PAGE_START],
		GameFlowManager.GameState.QUIZ: [ScreenCoordinator.PAGE_QUIZ],
		GameFlowManager.GameState.WRONG_FEEDBACK: [ScreenCoordinator.PAGE_QUIZ],
		GameFlowManager.GameState.CORRECT_FEEDBACK: [ScreenCoordinator.PAGE_QUIZ],
		GameFlowManager.GameState.ASSEMBLY: [ScreenCoordinator.PAGE_ASSEMBLY],
		GameFlowManager.GameState.COMPONENT_COMPLETE:
		[
			ScreenCoordinator.PAGE_ASSEMBLY,
			ScreenCoordinator.PAGE_COMPONENT,
		],
		GameFlowManager.GameState.FINAL_ASSEMBLY: [],
		GameFlowManager.GameState.END: [ScreenCoordinator.PAGE_END],
	}
	for state_value: Variant in expected:
		var state: int = state_value as int
		screen.show_state(state)
		var visible: Array[StringName] = screen.get_visible_page_names()
		var expected_pages: Array = expected[state]
		assert_eq(visible.size(), expected_pages.size(), GameFlowManager.GameState.keys()[state])
		for page_name_value: Variant in expected_pages:
			var page_name := StringName(page_name_value)
			assert_has(visible, page_name, GameFlowManager.GameState.keys()[state])


func test_assembly_hud_allows_mouse_input_to_reach_world() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	var assembly_page: Control = (
		screen.get_node(NodePath(ScreenCoordinator.PAGE_ASSEMBLY)) as Control
	)

	assert_eq(screen.mouse_filter, Control.MOUSE_FILTER_IGNORE)
	assert_eq(assembly_page.mouse_filter, Control.MOUSE_FILTER_IGNORE)


func test_quiz_controls_fit_minimum_and_default_viewports() -> void:
	var load_result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(load_result.is_success)
	var question: QuestionData = load_result.catalog.get_questions()[0]
	for viewport_size: Vector2i in [Vector2i(960, 540), Vector2i(1280, 720)]:
		var screen: ScreenCoordinator = await _add_screen(viewport_size)
		screen.present_question(question, 1, 9)
		screen.show_correct_feedback(question)
		await get_tree().process_frame
		var screen_rect: Rect2 = screen.get_global_rect()
		var quiz_page: Control = screen.get_node(NodePath(ScreenCoordinator.PAGE_QUIZ)) as Control
		for node: Node in quiz_page.find_children("*", "Control", true, false):
			var control: Control = node as Control
			if not control.visible:
				continue
			var control_rect: Rect2 = control.get_global_rect()
			assert_gte(control_rect.position.x, screen_rect.position.x - 1.0)
			assert_gte(control_rect.position.y, screen_rect.position.y - 1.0)
			assert_lte(control_rect.end.x, screen_rect.end.x + 1.0)
			assert_lte(control_rect.end.y, screen_rect.end.y + 1.0)


func test_fatal_page_replaces_all_other_pages() -> void:
	var screen: ScreenCoordinator = await _add_screen()
	screen.show_fatal("TEST_FAILURE", "测试错误")

	assert_true(screen.is_fatal())
	assert_eq(screen.get_visible_page_names(), [ScreenCoordinator.PAGE_FATAL])


func _add_screen(viewport_size: Vector2i = Vector2i(960, 540)) -> ScreenCoordinator:
	var scene: PackedScene = load("res://scenes/ui/screen_coordinator.tscn") as PackedScene
	var screen: ScreenCoordinator = scene.instantiate() as ScreenCoordinator
	var viewport := SubViewport.new()
	viewport.size = viewport_size
	add_child_autofree(viewport)
	viewport.add_child(screen)
	await get_tree().process_frame
	return screen
