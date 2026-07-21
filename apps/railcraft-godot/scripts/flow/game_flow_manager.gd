class_name GameFlowManager
extends Node

signal state_changed(previous: GameState, current: GameState)
signal question_presented(question: QuestionData, current_number: int, total: int)
signal wrong_feedback_requested(message: String)
signal correct_feedback_requested(question: QuestionData)
signal assembly_preparation_requested(part: PartData)
signal snap_animation_requested(part_id: String)
signal component_animation_requested(component: ComponentRecipe)
signal final_animation_requested(train_recipe: TrainRecipe)
signal end_view_requested(train_name: String)
signal recoverable_error_occurred(code: String, message: String)

enum GameState {
	START,
	QUIZ,
	WRONG_FEEDBACK,
	CORRECT_FEEDBACK,
	ASSEMBLY,
	COMPONENT_COMPLETE,
	FINAL_ASSEMBLY,
	END,
}

const WRONG_ANSWER_MESSAGE: String = "回答错误，请再试一次"
const _ALLOWED_TRANSITIONS := {
	GameState.START: [GameState.QUIZ],
	GameState.QUIZ: [GameState.WRONG_FEEDBACK, GameState.CORRECT_FEEDBACK],
	GameState.WRONG_FEEDBACK: [GameState.WRONG_FEEDBACK, GameState.CORRECT_FEEDBACK],
	GameState.CORRECT_FEEDBACK: [GameState.ASSEMBLY],
	GameState.ASSEMBLY: [GameState.QUIZ, GameState.COMPONENT_COMPLETE],
	GameState.COMPONENT_COMPLETE: [GameState.QUIZ, GameState.FINAL_ASSEMBLY],
	GameState.FINAL_ASSEMBLY: [GameState.END],
	GameState.END: [],
}

var _state: GameState = GameState.START
var _initialized: bool = false
var _catalog: ContentCatalog = null
var _quiz_manager: QuizManager = null
var _inventory_manager: InventoryManager = null
var _assembly_manager: AssemblyManager = null
var _exit_handler: Callable = Callable()
var _rewarded_part_id: String = ""
var _pending_snap_part_id: String = ""
var _pending_component_id: String = ""
var _final_animation_pending: bool = false


## Injects the concrete domain managers atomically before initialize().
func inject_dependencies(
	quiz_manager: QuizManager,
	inventory_manager: InventoryManager,
	assembly_manager: AssemblyManager,
	exit_handler: Callable = Callable(),
) -> bool:
	if _initialized:
		_emit_error("DEPENDENCIES_LOCKED", "流程已初始化，不能替换依赖")
		return false
	if quiz_manager == null:
		_emit_error("MISSING_DEPENDENCY", "缺少 QuizManager")
		return false
	if inventory_manager == null:
		_emit_error("MISSING_DEPENDENCY", "缺少 InventoryManager")
		return false
	if assembly_manager == null:
		_emit_error("MISSING_DEPENDENCY", "缺少 AssemblyManager")
		return false

	_quiz_manager = quiz_manager
	_inventory_manager = inventory_manager
	_assembly_manager = assembly_manager
	_exit_handler = exit_handler
	return true


## Configures all domain managers from one catalog and enters the START state.
func initialize(catalog: ContentCatalog) -> bool:
	if _initialized:
		_emit_error("ALREADY_INITIALIZED", "流程已经初始化")
		return false
	if _quiz_manager == null or _inventory_manager == null or _assembly_manager == null:
		_emit_error("MISSING_DEPENDENCY", "初始化前必须注入三个领域管理器")
		return false
	if catalog == null:
		_emit_error("MISSING_CATALOG", "初始化缺少 ContentCatalog")
		return false

	var questions: Array[QuestionData] = catalog.get_questions()
	var parts: Array[PartData] = catalog.get_parts()
	var components: Array[ComponentRecipe] = catalog.get_components()
	var train_recipe: TrainRecipe = catalog.get_train_recipe()
	if questions.is_empty() or parts.is_empty() or components.is_empty() or train_recipe == null:
		_emit_error("INVALID_CATALOG", "流程内容不完整")
		return false

	_catalog = catalog
	_quiz_manager.configure(questions)
	_inventory_manager.configure(parts)
	_assembly_manager.configure(parts, components, train_recipe)
	_rewarded_part_id = ""
	_pending_snap_part_id = ""
	_pending_component_id = ""
	_final_animation_pending = false
	_state = GameState.START
	_initialized = true
	return true


func get_state() -> GameState:
	return _state


func is_initialized() -> bool:
	return _initialized


func get_pending_snap_part_id() -> String:
	return _pending_snap_part_id


func get_pending_component_id() -> String:
	return _pending_component_id


func is_final_animation_pending() -> bool:
	return _final_animation_pending


## Returns a copy so reachability tests cannot mutate the transition whitelist.
static func get_allowed_transitions(from_state: GameState) -> Array[GameState]:
	var result: Array[GameState] = []
	var raw_transitions: Array = _ALLOWED_TRANSITIONS.get(from_state, [])
	for target: GameState in raw_transitions:
		result.append(target)
	return result


static func is_transition_allowed(from_state: GameState, to_state: GameState) -> bool:
	return to_state in get_allowed_transitions(from_state)


func request_start() -> void:
	if not _require_initialized("request_start"):
		return
	if not _require_event_state("request_start", [GameState.START]):
		return

	var first_question: QuestionData = _quiz_manager.start()
	if first_question == null:
		_emit_error("QUIZ_START_FAILED", "题库无法开始")
		return
	if not _transition_to(GameState.QUIZ):
		return
	_emit_question(first_question)


func select_answer(option_index: int) -> void:
	if not _require_initialized("select_answer"):
		return
	if not _require_event_state("select_answer", [GameState.QUIZ, GameState.WRONG_FEEDBACK]):
		return

	var question: QuestionData = _quiz_manager.get_current_question()
	if question == null:
		_emit_error("QUESTION_UNAVAILABLE", "当前题目不可用")
		return
	var result: AnswerResult = _quiz_manager.submit_answer(option_index)
	match result.status:
		AnswerResult.Status.WRONG:
			if _transition_to(GameState.WRONG_FEEDBACK):
				wrong_feedback_requested.emit(WRONG_ANSWER_MESSAGE)
		AnswerResult.Status.CORRECT_FIRST_TIME:
			_handle_first_correct_answer(question)
		AnswerResult.Status.ALREADY_SOLVED:
			_emit_error("ANSWER_ALREADY_SOLVED", "当前题目已完成")
		AnswerResult.Status.INVALID_OPTION:
			_emit_error("INVALID_OPTION", "答案索引无效")
		AnswerResult.Status.NOT_STARTED:
			_emit_error("QUIZ_NOT_STARTED", "答题尚未开始")


func request_assembly() -> void:
	if not _require_initialized("request_assembly"):
		return
	if not _require_event_state("request_assembly", [GameState.CORRECT_FEEDBACK]):
		return

	var part: PartData = _get_current_assembly_part()
	if part == null:
		return
	if not _transition_to(GameState.ASSEMBLY):
		return
	assembly_preparation_requested.emit(part)


func click_part(part_id: String) -> void:
	if not _require_initialized("click_part"):
		return
	if not _require_event_state("click_part", [GameState.ASSEMBLY]):
		return
	if not _pending_snap_part_id.is_empty():
		_emit_error("SNAP_ALREADY_PENDING", "零件吸附动画正在进行")
		return
	if not _inventory_manager.has_part(part_id):
		_emit_error("PART_NOT_OWNED", "尚未拥有所选零件")
		return

	var check: InstallCheck = _assembly_manager.begin_install(part_id)
	if not check.is_allowed():
		_emit_error("INSTALL_REJECTED", "零件安装请求被拒绝：%d" % check.status)
		return
	_pending_snap_part_id = part_id
	snap_animation_requested.emit(part_id)


func notify_snap_finished(part_id: String) -> void:
	if not _require_initialized("notify_snap_finished"):
		return
	if not _require_event_state("notify_snap_finished", [GameState.ASSEMBLY]):
		return
	if _pending_snap_part_id.is_empty() or part_id != _pending_snap_part_id:
		_emit_error("STALE_SNAP_CALLBACK", "吸附完成回调与当前零件不匹配")
		return

	var outcome: AssemblyOutcome = _assembly_manager.commit_install(part_id)
	if not outcome.is_committed():
		_emit_error("INSTALL_COMMIT_FAILED", "零件安装提交失败：%d" % outcome.status)
		return
	_pending_snap_part_id = ""

	if outcome.completed_component_id.is_empty():
		_advance_to_next_question()
		return
	_begin_component_completion(outcome.completed_component_id)


func notify_snap_failed(part_id: String, reason: String) -> void:
	if not _require_initialized("notify_snap_failed"):
		return
	if not _require_event_state("notify_snap_failed", [GameState.ASSEMBLY]):
		return
	if _pending_snap_part_id.is_empty() or part_id != _pending_snap_part_id:
		_emit_error("STALE_SNAP_CALLBACK", "吸附失败回调与当前零件不匹配")
		return

	_assembly_manager.abort_pending_install(part_id)
	_pending_snap_part_id = ""
	_emit_error("SNAP_ANIMATION_FAILED", "零件吸附失败，可重试：%s" % reason)


func notify_component_animation_finished(component_id: String) -> void:
	if not _require_initialized("notify_component_animation_finished"):
		return
	if not _require_event_state(
		"notify_component_animation_finished", [GameState.COMPONENT_COMPLETE]
	):
		return
	if _pending_component_id.is_empty() or component_id != _pending_component_id:
		_emit_error("STALE_COMPONENT_CALLBACK", "组件动画回调与当前组件不匹配")
		return

	if _assembly_manager.is_train_completed():
		var train_recipe: TrainRecipe = _catalog.get_train_recipe()
		if train_recipe == null:
			_emit_error("TRAIN_RECIPE_UNAVAILABLE", "整车配方不可用")
			return
		_pending_component_id = ""
		_final_animation_pending = true
		if not _transition_to(GameState.FINAL_ASSEMBLY):
			_final_animation_pending = false
			return
		final_animation_requested.emit(train_recipe)
		return

	_pending_component_id = ""
	_advance_to_next_question()


func notify_final_animation_finished() -> void:
	if not _require_initialized("notify_final_animation_finished"):
		return
	if not _require_event_state("notify_final_animation_finished", [GameState.FINAL_ASSEMBLY]):
		return
	if not _final_animation_pending or not _assembly_manager.is_train_completed():
		_emit_error("FINAL_ANIMATION_NOT_READY", "整车完成条件尚未满足")
		return

	var train_recipe: TrainRecipe = _catalog.get_train_recipe()
	if train_recipe == null:
		_emit_error("TRAIN_RECIPE_UNAVAILABLE", "整车配方不可用")
		return
	_final_animation_pending = false
	if not _transition_to(GameState.END):
		return
	end_view_requested.emit(train_recipe.display_name)


func request_exit() -> void:
	if not _require_initialized("request_exit"):
		return
	if not _require_event_state("request_exit", [GameState.START, GameState.END]):
		return
	if _exit_handler.is_valid():
		_exit_handler.call()
		return
	if is_inside_tree():
		get_tree().quit()
		return
	_emit_error("EXIT_UNAVAILABLE", "当前无法提交退出请求")


func _handle_first_correct_answer(question: QuestionData) -> void:
	var grant_result: GrantResult = _inventory_manager.grant_part(question.reward_part_id)
	if grant_result.status != GrantResult.Status.GRANTED:
		_emit_error("REWARD_GRANT_FAILED", "奖励零件发放失败：%d" % grant_result.status)
		return
	_rewarded_part_id = question.reward_part_id
	if _transition_to(GameState.CORRECT_FEEDBACK):
		correct_feedback_requested.emit(question)


func _get_current_assembly_part() -> PartData:
	var question: QuestionData = _quiz_manager.get_current_question()
	if question == null:
		_emit_error("QUESTION_UNAVAILABLE", "当前题目不可用")
		return null
	if _rewarded_part_id != question.reward_part_id:
		_emit_error("REWARD_MISMATCH", "当前奖励与题目不一致")
		return null
	if _assembly_manager.get_expected_part_id() != _rewarded_part_id:
		_emit_error("EXPECTED_PART_MISMATCH", "当前奖励与待安装零件不一致")
		return null
	if not _inventory_manager.has_part(_rewarded_part_id):
		_emit_error("REWARD_NOT_OWNED", "尚未拥有当前奖励零件")
		return null
	var part: PartData = _catalog.get_part(_rewarded_part_id)
	if part == null:
		_emit_error("PART_NOT_FOUND", "找不到当前奖励零件")
	return part


func _begin_component_completion(component_id: String) -> void:
	var component: ComponentRecipe = _catalog.get_component(component_id)
	if component == null:
		_emit_error("COMPONENT_NOT_FOUND", "找不到已完成组件")
		return
	_pending_component_id = component_id
	if not _transition_to(GameState.COMPONENT_COMPLETE):
		_pending_component_id = ""
		return
	component_animation_requested.emit(component)


func _advance_to_next_question() -> void:
	if not _quiz_manager.advance_after_assembly():
		_emit_error("QUESTION_ADVANCE_FAILED", "装配完成后无法推进题目")
		return
	var question: QuestionData = _quiz_manager.get_current_question()
	if question == null:
		_emit_error("QUESTION_UNAVAILABLE", "推进后的题目不可用")
		return
	_rewarded_part_id = ""
	if _transition_to(GameState.QUIZ):
		_emit_question(question)


func _emit_question(question: QuestionData) -> void:
	var questions: Array[QuestionData] = _catalog.get_questions()
	var current_number: int = 0
	for index: int in questions.size():
		if questions[index].question_id == question.question_id:
			current_number = index + 1
			break
	if current_number == 0:
		_emit_error("QUESTION_NOT_IN_CATALOG", "当前题目不在内容目录中")
		return
	question_presented.emit(question, current_number, questions.size())


func _transition_to(next_state: GameState) -> bool:
	if not is_transition_allowed(_state, next_state):
		_emit_error(
			"INVALID_TRANSITION",
			"禁止状态转换：%s -> %s" % [GameState.keys()[_state], GameState.keys()[next_state]],
		)
		return false
	if next_state == _state:
		return true
	var previous: GameState = _state
	_state = next_state
	state_changed.emit(previous, _state)
	return true


func _require_initialized(event_name: String) -> bool:
	if _initialized:
		return true
	_emit_error("NOT_INITIALIZED", "流程未初始化，拒绝事件：%s" % event_name)
	return false


func _require_event_state(event_name: String, allowed_states: Array[int]) -> bool:
	if _state in allowed_states:
		return true
	_emit_error(
		"INVALID_EVENT",
		"状态 %s 不接受事件 %s" % [GameState.keys()[_state], event_name],
	)
	return false


func _emit_error(code: String, message: String) -> void:
	recoverable_error_occurred.emit(code, message)
