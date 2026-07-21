class_name QuizManager
extends RefCounted

var _questions: Array[QuestionData] = []
var _current_index: int = -1
var _current_question_solved: bool = false
var _started: bool = false


## Replaces the question sequence with a shallow container copy and clears all progress.
func configure(questions: Array[QuestionData]) -> void:
	_questions = questions.duplicate()
	reset()


## Starts a configured quiz. An empty sequence leaves the manager unstarted.
func start() -> QuestionData:
	if _questions.is_empty():
		return null
	if _started:
		return get_current_question()

	_current_index = 0
	_current_question_solved = false
	_started = true
	return _questions[_current_index]


## Returns the current question without changing progress, or null outside an active quiz.
func get_current_question() -> QuestionData:
	if not _has_valid_current_question():
		return null
	return _questions[_current_index]


## Evaluates one option while keeping question advancement separate from answer submission.
func submit_answer(option_index: int) -> AnswerResult:
	var current_question: QuestionData = get_current_question()
	if current_question == null:
		return AnswerResult.new(AnswerResult.Status.NOT_STARTED)
	if option_index < 0 or option_index >= current_question.options.size():
		return AnswerResult.new(AnswerResult.Status.INVALID_OPTION)
	if _current_question_solved:
		return AnswerResult.new(AnswerResult.Status.ALREADY_SOLVED)
	if option_index != current_question.correct_option_index:
		return AnswerResult.new(AnswerResult.Status.WRONG)

	_current_question_solved = true
	return AnswerResult.new(AnswerResult.Status.CORRECT_FIRST_TIME)


## Advances exactly once after the current solved question has been assembled.
func advance_after_assembly() -> bool:
	if not _has_valid_current_question() or not _current_question_solved:
		return false
	if not has_next_question():
		return false

	_current_index += 1
	_current_question_solved = false
	return true


func has_next_question() -> bool:
	if not _has_valid_current_question():
		return false
	return _current_index + 1 < _questions.size()


## Clears runtime progress while retaining the configured question sequence.
func reset() -> void:
	_current_index = -1
	_current_question_solved = false
	_started = false


func _has_valid_current_question() -> bool:
	return _started and _current_index >= 0 and _current_index < _questions.size()
