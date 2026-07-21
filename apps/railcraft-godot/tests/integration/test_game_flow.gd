extends GutTest

var _flow: GameFlowManager
var _quiz: QuizManager
var _inventory: InventoryManager
var _assembly: AssemblyManager
var _recorder: GameFlowFixture.SignalRecorder


func before_each() -> void:
	_build_flow(GameFlowFixture.catalog(), InventoryManager.new())


func test_initialize_requires_all_dependencies_and_locks_successful_injection() -> void:
	var missing_flow := GameFlowManager.new()
	var missing_recorder := GameFlowFixture.SignalRecorder.new()
	add_child_autofree(missing_flow)
	missing_recorder.connect_to(missing_flow)

	assert_false(missing_flow.initialize(GameFlowFixture.catalog()))
	assert_eq(missing_recorder.error_codes, ["MISSING_DEPENDENCY"])
	assert_false(missing_flow.is_initialized())
	assert_eq(missing_flow.get_state(), GameFlowManager.GameState.START)

	var uninjected_quiz := QuizManager.new()
	var uninjected_inventory := InventoryManager.new()
	assert_false(missing_flow.inject_dependencies(uninjected_quiz, uninjected_inventory, null))
	assert_eq(missing_recorder.error_codes[-1], "MISSING_DEPENDENCY")

	var before: Dictionary[String, Variant] = _snapshot()
	assert_false(
		_flow.inject_dependencies(QuizManager.new(), InventoryManager.new(), AssemblyManager.new())
	)
	assert_false(_flow.initialize(GameFlowFixture.catalog()))
	assert_eq(_snapshot(), before)
	assert_eq(_recorder.error_codes.slice(-2), ["DEPENDENCIES_LOCKED", "ALREADY_INITIALIZED"])


func test_start_and_exit_are_state_guarded_and_publish_first_question_in_order() -> void:
	assert_eq(_flow.get_state(), GameFlowManager.GameState.START)

	_flow.request_exit()
	assert_eq(_recorder.exit_call_count, 1)

	_recorder.events.clear()
	_flow.request_start()

	assert_eq(_flow.get_state(), GameFlowManager.GameState.QUIZ)
	assert_eq(
		_recorder.events,
		[
			"state:START>QUIZ",
			"question:q01:1/9",
		],
	)
	assert_eq(_recorder.question_totals, [9])

	var before: Dictionary[String, Variant] = _snapshot()
	_flow.request_start()
	_flow.request_exit()
	assert_eq(_snapshot(), before)
	assert_eq(_recorder.exit_call_count, 1)
	assert_eq(_recorder.error_codes.slice(-2), ["INVALID_EVENT", "INVALID_EVENT"])


func test_wrong_answer_never_grants_and_first_correct_grants_exactly_once() -> void:
	var recording_inventory := GameFlowFixture.RecordingInventoryManager.new()
	_build_flow(GameFlowFixture.catalog(), recording_inventory)
	_flow.request_start()
	var question: QuestionData = _quiz.get_current_question()

	_flow.select_answer(GameFlowFixture.wrong_option(question))

	assert_eq(_flow.get_state(), GameFlowManager.GameState.WRONG_FEEDBACK)
	assert_eq(recording_inventory.grant_call_count, 0)
	assert_eq(_inventory.get_owned_part_ids(), [])
	assert_eq(_recorder.wrong_messages, [GameFlowManager.WRONG_ANSWER_MESSAGE])

	_flow.select_answer(question.correct_option_index)

	assert_eq(_flow.get_state(), GameFlowManager.GameState.CORRECT_FEEDBACK)
	assert_eq(recording_inventory.grant_call_count, 1)
	assert_eq(_inventory.get_owned_part_ids(), [question.reward_part_id])
	assert_eq(_recorder.correct_question_ids, [question.question_id])

	var before: Dictionary[String, Variant] = _snapshot()
	_flow.select_answer(question.correct_option_index)
	assert_eq(_snapshot(), before)
	assert_eq(recording_inventory.grant_call_count, 1)
	assert_eq(_recorder.correct_question_ids, [question.question_id])


func test_assembly_entry_requires_owned_reward_and_expected_part_match() -> void:
	_flow.request_start()
	var question: QuestionData = _quiz.get_current_question()
	_flow.select_answer(question.correct_option_index)
	_inventory.reset()
	var missing_reward_snapshot: Dictionary[String, Variant] = _snapshot()

	_flow.request_assembly()

	assert_eq(_snapshot(), missing_reward_snapshot)
	assert_eq(_flow.get_state(), GameFlowManager.GameState.CORRECT_FEEDBACK)
	assert_eq(_recorder.assembly_part_ids, [])
	assert_eq(_recorder.error_codes[-1], "REWARD_NOT_OWNED")

	assert_eq(
		_inventory.grant_part(question.reward_part_id).status,
		GrantResult.Status.GRANTED,
	)
	_flow.request_assembly()
	assert_eq(_flow.get_state(), GameFlowManager.GameState.ASSEMBLY)
	assert_eq(_recorder.assembly_part_ids, [question.reward_part_id])

	_build_flow(
		GameFlowFixture.catalog(GameFlowFixture.PART_IDS[1]),
		InventoryManager.new(),
	)
	_flow.request_start()
	question = _quiz.get_current_question()
	_flow.select_answer(question.correct_option_index)
	var mismatch_snapshot: Dictionary[String, Variant] = _snapshot()

	_flow.request_assembly()

	assert_eq(_snapshot(), mismatch_snapshot)
	assert_eq(_flow.get_state(), GameFlowManager.GameState.CORRECT_FEEDBACK)
	assert_eq(_recorder.assembly_part_ids, [])
	assert_eq(_recorder.error_codes[-1], "EXPECTED_PART_MISMATCH")


func test_snap_failure_unlocks_matching_transaction_and_stale_ids_are_atomic() -> void:
	_start_and_prepare_first_part()
	var part_id: String = GameFlowFixture.PART_IDS[0]
	var stale_part_id: String = GameFlowFixture.PART_IDS[1]

	_flow.click_part(part_id)
	assert_eq(_recorder.snap_part_ids, [part_id])
	assert_eq(_flow.get_pending_snap_part_id(), part_id)
	assert_eq(_assembly.get_pending_part_id(), part_id)

	var pending_snapshot: Dictionary[String, Variant] = _snapshot()
	_flow.click_part(part_id)
	_flow.notify_snap_finished(stale_part_id)
	_flow.notify_snap_failed(stale_part_id, "旧动画失败")
	assert_eq(_snapshot(), pending_snapshot)
	assert_eq(_recorder.snap_part_ids, [part_id])

	_flow.notify_snap_failed(part_id, "测试动画无法启动")
	assert_eq(_flow.get_state(), GameFlowManager.GameState.ASSEMBLY)
	assert_eq(_flow.get_pending_snap_part_id(), "")
	assert_eq(_assembly.get_pending_part_id(), "")
	assert_eq(_recorder.error_codes[-1], "SNAP_ANIMATION_FAILED")
	assert_string_contains(_recorder.error_messages[-1], "测试动画无法启动")

	_flow.click_part(part_id)
	assert_eq(_recorder.snap_part_ids, [part_id, part_id])
	_flow.notify_snap_finished(part_id)
	assert_eq(_flow.get_state(), GameFlowManager.GameState.QUIZ)
	assert_eq(_quiz.get_current_question().question_id, "q02")
	assert_eq(_assembly.get_installed_part_ids(), [part_id])

	var advanced_snapshot: Dictionary[String, Variant] = _snapshot()
	_flow.notify_snap_finished(part_id)
	assert_eq(_snapshot(), advanced_snapshot)


func test_three_six_nine_components_and_final_sequence_are_emitted_once() -> void:
	_flow.request_start()
	for index: int in GameFlowFixture.PART_IDS.size():
		_progress_current_question_to_post_snap()
		if (index + 1) % 3 == 0:
			assert_eq(_flow.get_state(), GameFlowManager.GameState.COMPONENT_COMPLETE)
			assert_eq(_flow.get_pending_component_id(), GameFlowFixture.COMPONENT_IDS[index / 3])
			assert_eq(_recorder.component_ids[-1], GameFlowFixture.COMPONENT_IDS[index / 3])
			assert_eq(_recorder.component_ids.count(GameFlowFixture.COMPONENT_IDS[index / 3]), 1)
			var component_snapshot: Dictionary[String, Variant] = _snapshot()
			_flow.notify_component_animation_finished("stale_component")
			assert_eq(_snapshot(), component_snapshot)
			_flow.notify_component_animation_finished(GameFlowFixture.COMPONENT_IDS[index / 3])
			if index < GameFlowFixture.PART_IDS.size() - 1:
				assert_eq(_flow.get_state(), GameFlowManager.GameState.QUIZ)
			else:
				assert_eq(_flow.get_state(), GameFlowManager.GameState.FINAL_ASSEMBLY)
				assert_true(_flow.is_final_animation_pending())
		else:
			assert_eq(_flow.get_state(), GameFlowManager.GameState.QUIZ)

	assert_eq(_recorder.component_ids, GameFlowFixture.COMPONENT_IDS)
	assert_eq(_recorder.final_train_ids, [GameFlowFixture.TRAIN_ID])
	assert_eq(_recorder.end_train_names, [])
	assert_true(_assembly.is_train_completed())

	_flow.notify_final_animation_finished()

	assert_eq(_flow.get_state(), GameFlowManager.GameState.END)
	assert_false(_flow.is_final_animation_pending())
	assert_eq(_recorder.end_train_names, [GameFlowFixture.TRAIN_NAME])
	assert_eq(
		_recorder.events.slice(-7),
		[
			"state:ASSEMBLY>COMPONENT_COMPLETE",
			"component:traction_power",
			"error:STALE_COMPONENT_CALLBACK",
			"state:COMPONENT_COMPLETE>FINAL_ASSEMBLY",
			"final:railcraft_train",
			"state:FINAL_ASSEMBLY>END",
			"end:轨道匠心号",
		],
	)

	var end_snapshot: Dictionary[String, Variant] = _snapshot()
	_flow.notify_final_animation_finished()
	_flow.notify_component_animation_finished(GameFlowFixture.COMPONENT_IDS[-1])
	assert_eq(_snapshot(), end_snapshot)
	assert_eq(_recorder.final_train_ids, [GameFlowFixture.TRAIN_ID])
	assert_eq(_recorder.end_train_names, [GameFlowFixture.TRAIN_NAME])

	_flow.request_exit()
	assert_eq(_recorder.exit_call_count, 1)


func test_every_state_rejects_disallowed_events_without_partial_domain_changes() -> void:
	_assert_calls_atomic(
		[
			Callable(_flow, "select_answer").bind(0),
			Callable(_flow, "request_assembly"),
			Callable(_flow, "click_part").bind(GameFlowFixture.PART_IDS[0]),
			Callable(_flow, "notify_snap_finished").bind(GameFlowFixture.PART_IDS[0]),
			Callable(_flow, "notify_snap_failed").bind(GameFlowFixture.PART_IDS[0], "早到"),
			Callable(_flow, "notify_component_animation_finished").bind(
				GameFlowFixture.COMPONENT_IDS[0]
			),
			Callable(_flow, "notify_final_animation_finished"),
		]
	)

	_flow.request_start()
	_assert_calls_atomic(_illegal_calls_outside_quiz_answer())
	_assert_call_atomic(Callable(_flow, "select_answer").bind(-1))

	var first_question: QuestionData = _quiz.get_current_question()
	_flow.select_answer(GameFlowFixture.wrong_option(first_question))
	assert_eq(_flow.get_state(), GameFlowManager.GameState.WRONG_FEEDBACK)
	_assert_calls_atomic(_illegal_calls_outside_quiz_answer())

	_flow.select_answer(first_question.correct_option_index)
	assert_eq(_flow.get_state(), GameFlowManager.GameState.CORRECT_FEEDBACK)
	_assert_calls_atomic(
		[
			Callable(_flow, "request_start"),
			Callable(_flow, "select_answer").bind(first_question.correct_option_index),
			Callable(_flow, "click_part").bind(first_question.reward_part_id),
			Callable(_flow, "notify_snap_finished").bind(first_question.reward_part_id),
			Callable(_flow, "notify_snap_failed").bind(first_question.reward_part_id, "早到"),
			Callable(_flow, "notify_component_animation_finished").bind(
				GameFlowFixture.COMPONENT_IDS[0]
			),
			Callable(_flow, "notify_final_animation_finished"),
			Callable(_flow, "request_exit"),
		]
	)

	_flow.request_assembly()
	assert_eq(_flow.get_state(), GameFlowManager.GameState.ASSEMBLY)
	_assert_calls_atomic(
		[
			Callable(_flow, "request_start"),
			Callable(_flow, "select_answer").bind(first_question.correct_option_index),
			Callable(_flow, "request_assembly"),
			Callable(_flow, "notify_snap_finished").bind(first_question.reward_part_id),
			Callable(_flow, "notify_snap_failed").bind(first_question.reward_part_id, "早到"),
			Callable(_flow, "notify_component_animation_finished").bind(
				GameFlowFixture.COMPONENT_IDS[0]
			),
			Callable(_flow, "notify_final_animation_finished"),
			Callable(_flow, "request_exit"),
		]
	)
	_flow.click_part(first_question.reward_part_id)
	_assert_calls_atomic(
		[
			Callable(_flow, "click_part").bind(first_question.reward_part_id),
			Callable(_flow, "notify_snap_finished").bind(GameFlowFixture.PART_IDS[1]),
			Callable(_flow, "notify_snap_failed").bind(GameFlowFixture.PART_IDS[1], "旧回调"),
		]
	)
	_flow.notify_snap_finished(first_question.reward_part_id)

	_progress_current_question_to_post_snap()
	_progress_current_question_to_post_snap()
	assert_eq(_flow.get_state(), GameFlowManager.GameState.COMPONENT_COMPLETE)
	_assert_calls_atomic(
		[
			Callable(_flow, "request_start"),
			Callable(_flow, "select_answer").bind(0),
			Callable(_flow, "request_assembly"),
			Callable(_flow, "click_part").bind(GameFlowFixture.PART_IDS[2]),
			Callable(_flow, "notify_snap_finished").bind(GameFlowFixture.PART_IDS[2]),
			Callable(_flow, "notify_snap_failed").bind(GameFlowFixture.PART_IDS[2], "迟到"),
			Callable(_flow, "notify_component_animation_finished").bind("old_component"),
			Callable(_flow, "notify_final_animation_finished"),
			Callable(_flow, "request_exit"),
		]
	)
	_flow.notify_component_animation_finished(GameFlowFixture.COMPONENT_IDS[0])

	for index: int in range(3, GameFlowFixture.PART_IDS.size()):
		_progress_current_question_to_post_snap()
		if (index + 1) % 3 == 0:
			_flow.notify_component_animation_finished(GameFlowFixture.COMPONENT_IDS[index / 3])
	assert_eq(_flow.get_state(), GameFlowManager.GameState.FINAL_ASSEMBLY)
	_assert_calls_atomic(
		[
			Callable(_flow, "request_start"),
			Callable(_flow, "select_answer").bind(0),
			Callable(_flow, "request_assembly"),
			Callable(_flow, "click_part").bind(GameFlowFixture.PART_IDS[-1]),
			Callable(_flow, "notify_snap_finished").bind(GameFlowFixture.PART_IDS[-1]),
			Callable(_flow, "notify_snap_failed").bind(GameFlowFixture.PART_IDS[-1], "迟到"),
			Callable(_flow, "notify_component_animation_finished").bind(
				GameFlowFixture.COMPONENT_IDS[-1]
			),
			Callable(_flow, "request_exit"),
		]
	)
	_flow.notify_final_animation_finished()
	assert_eq(_flow.get_state(), GameFlowManager.GameState.END)
	_assert_calls_atomic(
		[
			Callable(_flow, "request_start"),
			Callable(_flow, "select_answer").bind(0),
			Callable(_flow, "request_assembly"),
			Callable(_flow, "click_part").bind(GameFlowFixture.PART_IDS[-1]),
			Callable(_flow, "notify_snap_finished").bind(GameFlowFixture.PART_IDS[-1]),
			Callable(_flow, "notify_snap_failed").bind(GameFlowFixture.PART_IDS[-1], "重复"),
			Callable(_flow, "notify_component_animation_finished").bind(
				GameFlowFixture.COMPONENT_IDS[-1]
			),
			Callable(_flow, "notify_final_animation_finished"),
		]
	)


func _build_flow(catalog: ContentCatalog, inventory: InventoryManager) -> void:
	_flow = GameFlowManager.new()
	_quiz = QuizManager.new()
	_inventory = inventory
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
	assert_true(_flow.initialize(catalog))


func _start_and_prepare_first_part() -> void:
	_flow.request_start()
	var question: QuestionData = _quiz.get_current_question()
	_flow.select_answer(question.correct_option_index)
	_flow.request_assembly()
	assert_eq(_flow.get_state(), GameFlowManager.GameState.ASSEMBLY)


func _progress_current_question_to_post_snap() -> void:
	var question: QuestionData = _quiz.get_current_question()
	_flow.select_answer(question.correct_option_index)
	_flow.request_assembly()
	_flow.click_part(question.reward_part_id)
	_flow.notify_snap_finished(question.reward_part_id)


func _illegal_calls_outside_quiz_answer() -> Array[Callable]:
	return [
		Callable(_flow, "request_start"),
		Callable(_flow, "request_assembly"),
		Callable(_flow, "click_part").bind(GameFlowFixture.PART_IDS[0]),
		Callable(_flow, "notify_snap_finished").bind(GameFlowFixture.PART_IDS[0]),
		Callable(_flow, "notify_snap_failed").bind(GameFlowFixture.PART_IDS[0], "早到"),
		Callable(_flow, "notify_component_animation_finished").bind(
			GameFlowFixture.COMPONENT_IDS[0]
		),
		Callable(_flow, "notify_final_animation_finished"),
		Callable(_flow, "request_exit"),
	]


func _assert_calls_atomic(calls: Array[Callable]) -> void:
	for event_call: Callable in calls:
		_assert_call_atomic(event_call)


func _assert_call_atomic(event_call: Callable) -> void:
	var before: Dictionary[String, Variant] = _snapshot()
	event_call.call()
	assert_eq(_snapshot(), before)


func _snapshot() -> Dictionary[String, Variant]:
	var question: QuestionData = _quiz.get_current_question()
	var question_id: String = "" if question == null else question.question_id
	return {
		"state": _flow.get_state(),
		"question_id": question_id,
		"owned": _inventory.get_owned_part_ids(),
		"expected": _assembly.get_expected_part_id(),
		"assembly_pending": _assembly.get_pending_part_id(),
		"flow_snap_pending": _flow.get_pending_snap_part_id(),
		"flow_component_pending": _flow.get_pending_component_id(),
		"final_pending": _flow.is_final_animation_pending(),
		"installed": _assembly.get_installed_part_ids(),
		"completed": _assembly.get_completed_component_ids(),
		"train_completed": _assembly.is_train_completed(),
	}
