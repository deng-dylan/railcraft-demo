extends GutTest

const EXPECTED_TRANSITIONS := {
	GameFlowManager.GameState.START: [GameFlowManager.GameState.QUIZ],
	GameFlowManager.GameState.QUIZ:
	[
		GameFlowManager.GameState.WRONG_FEEDBACK,
		GameFlowManager.GameState.CORRECT_FEEDBACK,
	],
	GameFlowManager.GameState.WRONG_FEEDBACK:
	[
		GameFlowManager.GameState.WRONG_FEEDBACK,
		GameFlowManager.GameState.CORRECT_FEEDBACK,
	],
	GameFlowManager.GameState.CORRECT_FEEDBACK: [GameFlowManager.GameState.ASSEMBLY],
	GameFlowManager.GameState.ASSEMBLY:
	[
		GameFlowManager.GameState.QUIZ,
		GameFlowManager.GameState.COMPONENT_COMPLETE,
	],
	GameFlowManager.GameState.COMPONENT_COMPLETE:
	[
		GameFlowManager.GameState.QUIZ,
		GameFlowManager.GameState.FINAL_ASSEMBLY,
	],
	GameFlowManager.GameState.FINAL_ASSEMBLY: [GameFlowManager.GameState.END],
	GameFlowManager.GameState.END: [],
}

var _flow: GameFlowManager
var _quiz: QuizManager
var _inventory: InventoryManager
var _assembly: AssemblyManager
var _recorder: GameFlowFixture.SignalRecorder


func before_each() -> void:
	_flow = GameFlowManager.new()
	_quiz = QuizManager.new()
	_inventory = InventoryManager.new()
	_assembly = AssemblyManager.new()
	_recorder = GameFlowFixture.SignalRecorder.new()
	add_child_autofree(_flow)
	_recorder.connect_to(_flow)
	assert_true(
		(
			_flow
			. inject_dependencies(
				_quiz,
				_inventory,
				_assembly,
				Callable(_recorder, "record_exit"),
			)
		)
	)
	assert_true(_flow.initialize(GameFlowFixture.catalog()))


func test_whitelist_contains_exactly_eight_states_and_only_documented_edges() -> void:
	assert_eq(GameFlowManager.GameState.size(), 8)
	for state_value: int in GameFlowManager.GameState.values():
		var state: GameFlowManager.GameState = state_value
		var actual: Array[GameFlowManager.GameState] = GameFlowManager.get_allowed_transitions(
			state
		)
		assert_eq(actual, EXPECTED_TRANSITIONS[state])
		for target_value: int in GameFlowManager.GameState.values():
			var target: GameFlowManager.GameState = target_value
			assert_eq(
				GameFlowManager.is_transition_allowed(state, target),
				target in EXPECTED_TRANSITIONS[state],
				(
					"%s -> %s"
					% [
						GameFlowManager.GameState.keys()[state],
						GameFlowManager.GameState.keys()[target]
					]
				),
			)

	var copied_start_edges: Array[GameFlowManager.GameState] = (
		GameFlowManager.get_allowed_transitions(GameFlowManager.GameState.START)
	)
	copied_start_edges.clear()
	assert_eq(
		GameFlowManager.get_allowed_transitions(GameFlowManager.GameState.START),
		[GameFlowManager.GameState.QUIZ],
	)


func test_every_nonterminal_whitelisted_state_has_a_path_to_end() -> void:
	for state_value: int in GameFlowManager.GameState.values():
		var state: GameFlowManager.GameState = state_value
		if state == GameFlowManager.GameState.END:
			continue
		assert_true(
			_can_reach_end(state),
			"%s 必须可达 END" % GameFlowManager.GameState.keys()[state],
		)


func test_nine_questions_all_correct_once_reach_end_with_exact_domain_totals() -> void:
	var visited_states: Array[int] = _run_full_flow(false)

	assert_eq(_flow.get_state(), GameFlowManager.GameState.END)
	assert_eq(_recorder.question_ids.size(), 9)
	assert_eq(_recorder.question_numbers, [1, 2, 3, 4, 5, 6, 7, 8, 9])
	assert_eq(_recorder.wrong_messages, [])
	assert_eq(_recorder.correct_question_ids.size(), 9)
	assert_eq(_recorder.assembly_part_ids, GameFlowFixture.PART_IDS)
	assert_eq(_recorder.snap_part_ids, GameFlowFixture.PART_IDS)
	assert_eq(_recorder.component_ids, GameFlowFixture.COMPONENT_IDS)
	assert_eq(_recorder.final_train_ids, [GameFlowFixture.TRAIN_ID])
	assert_eq(_recorder.end_train_names, [GameFlowFixture.TRAIN_NAME])
	assert_eq(_recorder.error_codes, [])
	assert_eq(_inventory.get_owned_part_ids(), GameFlowFixture.PART_IDS)
	assert_eq(_assembly.get_installed_part_ids(), GameFlowFixture.PART_IDS)
	assert_eq(_assembly.get_completed_component_ids(), GameFlowFixture.COMPONENT_IDS)
	assert_true(_assembly.is_train_completed())
	assert_true(GameFlowManager.GameState.END in visited_states)


func test_each_question_wrong_then_correct_reaches_end_and_visits_all_states() -> void:
	var visited_states: Array[int] = _run_full_flow(true)

	assert_eq(_flow.get_state(), GameFlowManager.GameState.END)
	assert_eq(_recorder.question_ids.size(), 9)
	assert_eq(_recorder.wrong_messages.size(), 9)
	for message: String in _recorder.wrong_messages:
		assert_eq(message, GameFlowManager.WRONG_ANSWER_MESSAGE)
	assert_eq(_recorder.correct_question_ids.size(), 9)
	assert_eq(_recorder.snap_part_ids.size(), 9)
	assert_eq(_recorder.component_ids, GameFlowFixture.COMPONENT_IDS)
	assert_eq(_recorder.final_train_ids.size(), 1)
	assert_eq(_recorder.end_train_names, [GameFlowFixture.TRAIN_NAME])
	assert_eq(_recorder.error_codes, [])
	assert_eq(_inventory.get_owned_part_ids(), GameFlowFixture.PART_IDS)
	assert_eq(_assembly.get_installed_part_ids(), GameFlowFixture.PART_IDS)
	assert_eq(_assembly.get_completed_component_ids(), GameFlowFixture.COMPONENT_IDS)
	assert_true(_assembly.is_train_completed())
	for state_value: int in GameFlowManager.GameState.values():
		assert_true(
			state_value in visited_states,
			"完整错后对路径应访问 %s" % GameFlowManager.GameState.keys()[state_value],
		)


func _can_reach_end(start_state: GameFlowManager.GameState) -> bool:
	var frontier: Array[GameFlowManager.GameState] = [start_state]
	var visited: Dictionary[int, bool] = {}
	while not frontier.is_empty():
		var current: GameFlowManager.GameState = frontier.pop_front()
		if current == GameFlowManager.GameState.END:
			return true
		if visited.has(current):
			continue
		visited[current] = true
		for target: GameFlowManager.GameState in GameFlowManager.get_allowed_transitions(current):
			if not visited.has(target):
				frontier.append(target)
	return false


func _run_full_flow(answer_wrong_first: bool) -> Array[int]:
	var visited_states: Array[int] = [GameFlowManager.GameState.START]
	_flow.state_changed.connect(
		func(_previous: GameFlowManager.GameState, current: GameFlowManager.GameState) -> void:
			visited_states.append(current)
	)
	_flow.request_start()

	for index: int in GameFlowFixture.PART_IDS.size():
		var question: QuestionData = _quiz.get_current_question()
		if answer_wrong_first:
			_flow.select_answer(GameFlowFixture.wrong_option(question))
		_flow.select_answer(question.correct_option_index)
		_flow.request_assembly()
		_flow.click_part(question.reward_part_id)
		_flow.notify_snap_finished(question.reward_part_id)
		if (index + 1) % 3 == 0:
			_flow.notify_component_animation_finished(GameFlowFixture.COMPONENT_IDS[index / 3])

	assert_eq(_flow.get_state(), GameFlowManager.GameState.FINAL_ASSEMBLY)
	_flow.notify_final_animation_finished()
	return visited_states
