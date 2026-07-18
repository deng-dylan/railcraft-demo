class_name AnswerResult
extends RefCounted

## Stable outcomes returned by QuizManager without exposing answer details.
enum Status {
	WRONG,
	CORRECT_FIRST_TIME,
	ALREADY_SOLVED,
	INVALID_OPTION,
	NOT_STARTED,
}

var status: Status


func _init(answer_status: Status) -> void:
	status = answer_status
