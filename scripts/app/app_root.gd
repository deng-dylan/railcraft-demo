class_name AppRoot
extends Node

signal shutdown_requested

@export var quit_on_shutdown: bool = true

var _catalog: ContentCatalog
var _initialized: bool = false
var _fatal_active: bool = false
var _shutdown_started: bool = false

@onready var _flow: GameFlowManager = $DomainServices/GameFlowManager
@onready var _animation: AnimationCoordinator = (
	$PresentationServices/AnimationCoordinator as AnimationCoordinator
)
@onready var _world_root: Node3D = $WorldRoot
@onready var _assembly_view: AssemblyView = $WorldRoot/AssemblyView
@onready var _screen: ScreenCoordinator = $UILayer/ScreenCoordinator


func _ready() -> void:
	_connect_signals()
	_start_application()


func is_initialized() -> bool:
	return _initialized


func is_shutdown_started() -> bool:
	return _shutdown_started


func get_flow_manager() -> GameFlowManager:
	return _flow


func get_screen_coordinator() -> ScreenCoordinator:
	return _screen


func get_catalog() -> ContentCatalog:
	return _catalog


func _start_application() -> void:
	if not _animation.inject_view(_assembly_view):
		_show_fatal("ANIMATION_SETUP_FAILED", "动画协调器无法连接装配视图。")
		return

	var load_result: ContentLoadResult = ContentRepository.new().load_catalog()
	if not load_result.is_success or load_result.catalog == null:
		_show_issues("CONTENT_LOAD_FAILED", load_result.issues)
		return
	_catalog = load_result.catalog

	var train_root: Node = _assembly_view.get_node_or_null(_assembly_view.train_assembly_root_path)
	var asset_issues: Array[ValidationIssue] = AssemblyAssetValidator.new().validate(
		_catalog, train_root
	)
	if not asset_issues.is_empty():
		_show_issues("ASSET_VALIDATION_FAILED", asset_issues)
		return

	var dependencies_ready: bool = (
		_flow
		. inject_dependencies(
			QuizManager.new(),
			InventoryManager.new(),
			AssemblyManager.new(),
			Callable(self, "_shutdown"),
		)
	)
	if not dependencies_ready:
		_show_fatal("FLOW_SETUP_FAILED", "核心流程依赖注入失败。")
		return
	if not _flow.initialize(_catalog):
		_show_fatal("FLOW_INITIALIZE_FAILED", "核心流程初始化失败。")
		return

	_initialized = true
	_screen.show_state(GameFlowManager.GameState.START)
	_apply_world_visibility(GameFlowManager.GameState.START)


func _connect_signals() -> void:
	_screen.start_requested.connect(_flow.request_start)
	_screen.answer_selected.connect(_flow.select_answer)
	_screen.assembly_requested.connect(_flow.request_assembly)
	_screen.exit_requested.connect(_on_exit_requested)
	_assembly_view.part_clicked.connect(_flow.click_part)

	_flow.state_changed.connect(_on_state_changed)
	_flow.question_presented.connect(_screen.present_question)
	_flow.wrong_feedback_requested.connect(_screen.show_wrong_feedback)
	_flow.correct_feedback_requested.connect(_screen.show_correct_feedback)
	_flow.assembly_preparation_requested.connect(_on_assembly_preparation_requested)
	_flow.snap_animation_requested.connect(_on_snap_animation_requested)
	_flow.component_animation_requested.connect(_on_component_animation_requested)
	_flow.final_animation_requested.connect(_on_final_animation_requested)
	_flow.end_view_requested.connect(_screen.show_end)
	_flow.recoverable_error_occurred.connect(_on_recoverable_error)

	_animation.part_snap_finished.connect(_on_part_snap_finished)
	_animation.part_snap_failed.connect(_on_part_snap_failed)
	_animation.component_animation_finished.connect(_flow.notify_component_animation_finished)
	_animation.final_animation_finished.connect(_flow.notify_final_animation_finished)


func _on_state_changed(_previous: int, current: int) -> void:
	_screen.show_state(current)
	_apply_world_visibility(current)


func _on_assembly_preparation_requested(part: PartData) -> void:
	if not _assembly_view.prepare_part(part):
		_show_fatal(
			"ASSEMBLY_PREPARE_FAILED",
			"无法准备零件 %s：%s" % [part.part_id, _assembly_view.last_error],
		)
		return
	var total: int = _catalog.get_parts().size() if _catalog != null else 9
	_screen.prepare_assembly(part, part.order, total)
	_screen.set_assembly_busy(false)


func _on_snap_animation_requested(part_id: String) -> void:
	_screen.set_assembly_busy(true)
	_animation.play_part_snap(part_id)


func _on_component_animation_requested(component: ComponentRecipe) -> void:
	_screen.show_component_complete(component)
	_animation.play_component_complete(component)


func _on_final_animation_requested(train_recipe: TrainRecipe) -> void:
	_animation.play_final_assembly(train_recipe)


func _on_part_snap_finished(part_id: String) -> void:
	_screen.set_assembly_busy(false)
	_flow.notify_snap_finished(part_id)


func _on_part_snap_failed(part_id: String, reason: String) -> void:
	_screen.set_assembly_busy(false)
	_flow.notify_snap_failed(part_id, reason)


func _on_recoverable_error(code: String, message: String) -> void:
	push_warning("%s: %s" % [code, message])
	_screen.show_runtime_message(code, message)


func _on_exit_requested() -> void:
	if _fatal_active or not _initialized:
		_shutdown()
		return
	_flow.request_exit()


func _apply_world_visibility(state: int) -> void:
	var world_visible: bool = (
		state
		in [
			GameFlowManager.GameState.ASSEMBLY,
			GameFlowManager.GameState.COMPONENT_COMPLETE,
			GameFlowManager.GameState.FINAL_ASSEMBLY,
			GameFlowManager.GameState.END,
		]
	)
	_world_root.visible = world_visible
	var background: CanvasItem = _screen.get_node_or_null(^"Background") as CanvasItem
	if background != null:
		background.visible = not world_visible


func _show_issues(code: String, issues: Array[ValidationIssue]) -> void:
	var lines: Array[String] = []
	for issue: ValidationIssue in issues:
		lines.append("%s · %s · %s" % [issue.code, issue.json_path, issue.message])
	var message: String = "\n".join(lines)
	if message.is_empty():
		message = "没有可用的详细错误信息。"
	_show_fatal(code, message)


func _show_fatal(code: String, message: String) -> void:
	_fatal_active = true
	_world_root.visible = false
	var background: CanvasItem = _screen.get_node_or_null(^"Background") as CanvasItem
	if background != null:
		background.visible = true
	push_error("%s: %s" % [code, message])
	_screen.show_fatal(code, message)


func _shutdown() -> void:
	if _shutdown_started:
		return
	_shutdown_started = true
	_animation.cancel_all_for_shutdown()
	_assembly_view.cleanup_pending_part()
	shutdown_requested.emit()
	if quit_on_shutdown:
		get_tree().quit()
