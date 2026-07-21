extends GutTest

const Fixture := preload("res://tests/fixtures/quiz_manager_fixture.gd")

var _manager: QuizManager


func before_each() -> void:
	_manager = QuizManager.new()


func test_answer_result_constructor_preserves_every_status() -> void:
	var statuses: Array[AnswerResult.Status] = [
		AnswerResult.Status.WRONG,
		AnswerResult.Status.CORRECT_FIRST_TIME,
		AnswerResult.Status.ALREADY_SOLVED,
		AnswerResult.Status.INVALID_OPTION,
		AnswerResult.Status.NOT_STARTED,
	]

	for status: AnswerResult.Status in statuses:
		assert_eq(AnswerResult.new(status).status, status)


func test_configure_copies_container_and_reconfigure_clears_progress() -> void:
	var questions: Array[QuestionData] = Fixture.two_questions()
	_manager.configure(questions)
	questions.clear()

	assert_eq(_manager.start().question_id, "q01")
	assert_eq(_manager.submit_answer(2).status, AnswerResult.Status.CORRECT_FIRST_TIME)
	assert_true(_manager.advance_after_assembly())
	assert_eq(_manager.get_current_question().question_id, "q02")

	_manager.configure(Fixture.two_questions())

	assert_null(_manager.get_current_question())
	assert_eq(_manager.submit_answer(0).status, AnswerResult.Status.NOT_STARTED)
	assert_eq(_manager.start().question_id, "q01")


func test_empty_quiz_cannot_start_and_queries_are_pure() -> void:
	_manager.configure([])

	assert_null(_manager.start())
	assert_null(_manager.get_current_question())
	assert_false(_manager.has_next_question())
	assert_false(_manager.advance_after_assembly())
	assert_eq(_manager.submit_answer(0).status, AnswerResult.Status.NOT_STARTED)
	assert_null(_manager.get_current_question())


func test_start_is_idempotent_and_current_query_does_not_change_progress() -> void:
	_manager.configure(Fixture.two_questions())
	var first: QuestionData = _manager.start()

	assert_same(_manager.get_current_question(), first)
	assert_same(_manager.get_current_question(), first)
	assert_same(_manager.start(), first)
	assert_true(_manager.has_next_question())


func test_wrong_answers_stay_on_question_and_allow_retry() -> void:
	_manager.configure(Fixture.two_questions())
	var first: QuestionData = _manager.start()

	assert_eq(_manager.submit_answer(0).status, AnswerResult.Status.WRONG)
	assert_eq(_manager.submit_answer(1).status, AnswerResult.Status.WRONG)
	assert_same(_manager.get_current_question(), first)
	assert_false(_manager.advance_after_assembly())
	assert_eq(_manager.submit_answer(2).status, AnswerResult.Status.CORRECT_FIRST_TIME)


func test_first_correct_answer_locks_without_advancing() -> void:
	_manager.configure(Fixture.two_questions())
	var first: QuestionData = _manager.start()

	assert_eq(_manager.submit_answer(2).status, AnswerResult.Status.CORRECT_FIRST_TIME)
	assert_same(_manager.get_current_question(), first)
	assert_true(_manager.has_next_question())
	assert_eq(_manager.submit_answer(2).status, AnswerResult.Status.ALREADY_SOLVED)
	assert_eq(_manager.submit_answer(0).status, AnswerResult.Status.ALREADY_SOLVED)
	assert_same(_manager.get_current_question(), first)


func test_invalid_options_do_not_solve_or_advance() -> void:
	_manager.configure(Fixture.two_questions())
	var first: QuestionData = _manager.start()

	assert_eq(_manager.submit_answer(-1).status, AnswerResult.Status.INVALID_OPTION)
	assert_eq(_manager.submit_answer(4).status, AnswerResult.Status.INVALID_OPTION)
	assert_same(_manager.get_current_question(), first)
	assert_false(_manager.advance_after_assembly())
	assert_eq(_manager.submit_answer(2).status, AnswerResult.Status.CORRECT_FIRST_TIME)


func test_advance_requires_solution_then_moves_to_unsolved_next_question() -> void:
	_manager.configure(Fixture.two_questions())
	_manager.start()

	assert_false(_manager.advance_after_assembly())
	assert_eq(_manager.submit_answer(2).status, AnswerResult.Status.CORRECT_FIRST_TIME)
	assert_true(_manager.advance_after_assembly())
	assert_eq(_manager.get_current_question().question_id, "q02")
	assert_false(_manager.has_next_question())
	assert_false(_manager.advance_after_assembly())
	assert_eq(_manager.submit_answer(0).status, AnswerResult.Status.CORRECT_FIRST_TIME)


func test_last_question_has_no_next_and_failed_advance_preserves_lock() -> void:
	_manager.configure(Fixture.two_questions())
	_manager.start()
	_manager.submit_answer(2)
	_manager.advance_after_assembly()
	var last: QuestionData = _manager.get_current_question()

	assert_eq(_manager.submit_answer(0).status, AnswerResult.Status.CORRECT_FIRST_TIME)
	assert_false(_manager.has_next_question())
	assert_false(_manager.advance_after_assembly())
	assert_same(_manager.get_current_question(), last)
	assert_eq(_manager.submit_answer(1).status, AnswerResult.Status.ALREADY_SOLVED)


func test_reset_retains_questions_and_clears_solution_and_index() -> void:
	_manager.configure(Fixture.two_questions())
	_manager.start()
	_manager.submit_answer(2)

	_manager.reset()

	assert_null(_manager.get_current_question())
	assert_false(_manager.has_next_question())
	assert_false(_manager.advance_after_assembly())
	assert_eq(_manager.submit_answer(2).status, AnswerResult.Status.NOT_STARTED)
	assert_eq(_manager.start().question_id, "q01")
	assert_eq(_manager.submit_answer(0).status, AnswerResult.Status.WRONG)
