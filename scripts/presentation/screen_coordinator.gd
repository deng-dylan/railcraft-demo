class_name ScreenCoordinator
extends Control

signal start_requested
signal answer_selected(option_index: int)
signal assembly_requested
signal exit_requested

const PAGE_START: StringName = &"StartView"
const PAGE_QUIZ: StringName = &"QuizView"
const PAGE_ASSEMBLY: StringName = &"AssemblyHUD"
const PAGE_COMPONENT: StringName = &"ComponentCompleteOverlay"
const PAGE_END: StringName = &"EndView"
const PAGE_FATAL: StringName = &"FatalErrorView"

var _pages: Dictionary[StringName, Control] = {}
var _answer_buttons: Array[Button] = []
var _progress_label: Label
var _question_label: Label
var _feedback_label: RichTextLabel
var _assembly_button: Button
var _assembly_progress_label: Label
var _assembly_part_label: Label
var _assembly_hint_label: Label
var _component_title_label: Label
var _component_message_label: RichTextLabel
var _end_title_label: Label
var _fatal_code_label: Label
var _fatal_message_label: RichTextLabel
var _fatal_active: bool = false


func _ready() -> void:
	_build_ui()
	show_state(GameFlowManager.GameState.START)


func show_state(state: int) -> void:
	if _fatal_active:
		return
	_set_page_visible(PAGE_START, state == GameFlowManager.GameState.START)
	_set_page_visible(
		PAGE_QUIZ,
		(
			state
			in [
				GameFlowManager.GameState.QUIZ,
				GameFlowManager.GameState.WRONG_FEEDBACK,
				GameFlowManager.GameState.CORRECT_FEEDBACK,
			]
		),
	)
	_set_page_visible(
		PAGE_ASSEMBLY,
		(
			state
			in [
				GameFlowManager.GameState.ASSEMBLY,
				GameFlowManager.GameState.COMPONENT_COMPLETE,
			]
		),
	)
	_set_page_visible(
		PAGE_COMPONENT,
		state == GameFlowManager.GameState.COMPONENT_COMPLETE,
	)
	_set_page_visible(PAGE_END, state == GameFlowManager.GameState.END)
	_set_page_visible(PAGE_FATAL, false)


func present_question(question: QuestionData, current_number: int, total: int) -> void:
	if question == null:
		return
	_progress_label.text = "问题 %d / %d" % [current_number, total]
	_question_label.text = question.prompt
	for index: int in _answer_buttons.size():
		var button: Button = _answer_buttons[index]
		button.text = question.options[index] if index < question.options.size() else ""
		button.disabled = index >= question.options.size()
	_feedback_label.visible = false
	_feedback_label.text = ""
	_assembly_button.visible = false
	show_state(GameFlowManager.GameState.QUIZ)


func show_wrong_feedback(message: String) -> void:
	_feedback_label.text = "[color=#ff8c8c]%s[/color]" % message
	_feedback_label.visible = true
	for button: Button in _answer_buttons:
		button.disabled = false


func show_correct_feedback(question: QuestionData) -> void:
	if question == null:
		return
	for button: Button in _answer_buttons:
		button.disabled = true
	var source_text: String = "来源机构：未提供\n资料标题：未提供\n网页地址：未提供"
	if question.source != null:
		source_text = (
			"来源机构：%s\n资料标题：%s\n网页地址：%s"
			% [
				question.source.organization,
				question.source.title,
				question.source.url,
			]
		)
	_feedback_label.text = (
		"[color=#8ff0a4]回答正确[/color]\n\n%s\n\n%s" % [question.explanation, source_text]
	)
	_feedback_label.visible = true
	_assembly_button.visible = true


func prepare_assembly(part: PartData, current_number: int, total: int) -> void:
	if part == null:
		return
	_assembly_progress_label.text = "装配 %d / %d" % [current_number, total]
	_assembly_part_label.text = "新零件：%s" % part.display_name
	_assembly_hint_label.text = "点击零件，将其吸附到发光安装位置"
	show_state(GameFlowManager.GameState.ASSEMBLY)


func set_assembly_busy(busy: bool) -> void:
	_assembly_hint_label.text = ("正在安装，请稍候……" if busy else "点击零件，将其吸附到发光安装位置")


func show_component_complete(component: ComponentRecipe) -> void:
	if component == null:
		return
	_component_title_label.text = "%s已完成" % component.display_name
	var details: Array[String] = []
	if not component.completion_message.is_empty():
		details.append(component.completion_message)
	if not component.teaching_note.is_empty():
		details.append(component.teaching_note)
	_component_message_label.text = "\n\n".join(details)
	_set_page_visible(PAGE_COMPONENT, true)


func show_end(train_name: String) -> void:
	_end_title_label.text = "装配完成：%s" % train_name
	show_state(GameFlowManager.GameState.END)


func show_runtime_message(code: String, message: String) -> void:
	if _fatal_active:
		return
	_assembly_hint_label.text = "%s：%s" % [code, message]


func show_fatal(code: String, message: String) -> void:
	_fatal_active = true
	for page_name: StringName in _pages:
		_set_page_visible(page_name, false)
	_fatal_code_label.text = "错误编号：%s" % code
	_fatal_message_label.text = message
	_set_page_visible(PAGE_FATAL, true)


func is_fatal() -> bool:
	return _fatal_active


func get_visible_page_names() -> Array[StringName]:
	var result: Array[StringName] = []
	for page_name: StringName in _pages:
		if _pages[page_name].visible:
			result.append(page_name)
	return result


func get_answer_button(index: int) -> Button:
	if index < 0 or index >= _answer_buttons.size():
		return null
	return _answer_buttons[index]


func _build_ui() -> void:
	if not get_children().is_empty():
		return
	var font: Font = load("res://assets/fonts/NotoSansSC-wght.ttf") as Font
	var app_theme := Theme.new()
	app_theme.default_font = font
	app_theme.default_font_size = 20
	theme = app_theme

	var background := ColorRect.new()
	background.name = "Background"
	background.color = Color(0.025, 0.055, 0.09, 1.0)
	background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	background.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(background)

	_build_start_page()
	_build_quiz_page()
	_build_assembly_hud()
	_build_component_overlay()
	_build_end_page()
	_build_fatal_page()


func _build_start_page() -> void:
	var page: Control = _make_full_page(PAGE_START)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	page.add_child(center)
	var panel: PanelContainer = _make_panel(Vector2(650, 430))
	center.add_child(panel)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 22)
	panel.add_child(column)
	column.add_child(_make_title("RailCraft Demo", 40))
	var description := Label.new()
	description.text = ("回答 9 道铁路知识题，获得零件并按顺序完成高速动车组装配。\n" + "答错可以继续尝试；答对后会显示解析和资料来源。")
	description.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	description.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	column.add_child(description)
	var start_button := Button.new()
	start_button.name = "StartButton"
	start_button.text = "开始体验"
	start_button.custom_minimum_size = Vector2(0, 58)
	start_button.pressed.connect(start_requested.emit)
	column.add_child(start_button)
	var exit_button := Button.new()
	exit_button.name = "ExitButton"
	exit_button.text = "退出"
	exit_button.pressed.connect(exit_requested.emit)
	column.add_child(exit_button)


func _build_quiz_page() -> void:
	var page: Control = _make_full_page(PAGE_QUIZ)
	var margin := MarginContainer.new()
	margin.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	margin.add_theme_constant_override("margin_left", 36)
	margin.add_theme_constant_override("margin_top", 24)
	margin.add_theme_constant_override("margin_right", 36)
	margin.add_theme_constant_override("margin_bottom", 24)
	page.add_child(margin)
	var panel: PanelContainer = _make_panel(Vector2.ZERO)
	margin.add_child(panel)
	var body := HBoxContainer.new()
	body.add_theme_constant_override("separation", 20)
	panel.add_child(body)
	var question_column := VBoxContainer.new()
	question_column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	question_column.add_theme_constant_override("separation", 8)
	body.add_child(question_column)
	var feedback_column := VBoxContainer.new()
	feedback_column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	feedback_column.add_theme_constant_override("separation", 10)
	body.add_child(feedback_column)

	_progress_label = Label.new()
	_progress_label.name = "ProgressLabel"
	question_column.add_child(_progress_label)
	_question_label = _make_title("", 27)
	_question_label.name = "QuestionLabel"
	_question_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_question_label.custom_minimum_size = Vector2(0, 76)
	question_column.add_child(_question_label)
	for index: int in 4:
		var button := Button.new()
		button.name = "Option%d" % (index + 1)
		button.custom_minimum_size = Vector2(0, 44)
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.pressed.connect(_on_answer_pressed.bind(index))
		question_column.add_child(button)
		_answer_buttons.append(button)

	var feedback_title := Label.new()
	feedback_title.text = "知识解析与资料来源"
	feedback_title.add_theme_font_size_override("font_size", 23)
	feedback_column.add_child(feedback_title)
	_feedback_label = RichTextLabel.new()
	_feedback_label.name = "FeedbackLabel"
	_feedback_label.bbcode_enabled = true
	_feedback_label.fit_content = false
	_feedback_label.scroll_active = true
	_feedback_label.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_feedback_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	feedback_column.add_child(_feedback_label)
	_assembly_button = Button.new()
	_assembly_button.name = "AssemblyButton"
	_assembly_button.text = "进入装配"
	_assembly_button.custom_minimum_size = Vector2(0, 48)
	_assembly_button.pressed.connect(assembly_requested.emit)
	feedback_column.add_child(_assembly_button)


func _build_assembly_hud() -> void:
	var page: Control = _make_full_page(PAGE_ASSEMBLY)
	page.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var panel: PanelContainer = _make_panel(Vector2(430, 145))
	panel.position = Vector2(28, 28)
	page.add_child(panel)
	var column := VBoxContainer.new()
	panel.add_child(column)
	_assembly_progress_label = _make_title("装配 1 / 9", 24)
	column.add_child(_assembly_progress_label)
	_assembly_part_label = Label.new()
	column.add_child(_assembly_part_label)
	_assembly_hint_label = Label.new()
	_assembly_hint_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	column.add_child(_assembly_hint_label)


func _build_component_overlay() -> void:
	var page: Control = _make_full_page(PAGE_COMPONENT)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	page.add_child(center)
	var panel: PanelContainer = _make_panel(Vector2(720, 300))
	center.add_child(panel)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 18)
	panel.add_child(column)
	_component_title_label = _make_title("组件已完成", 34)
	column.add_child(_component_title_label)
	_component_message_label = RichTextLabel.new()
	_component_message_label.fit_content = false
	_component_message_label.custom_minimum_size = Vector2(0, 170)
	_component_message_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	column.add_child(_component_message_label)


func _build_end_page() -> void:
	var page: Control = _make_full_page(PAGE_END)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	page.add_child(center)
	var panel: PanelContainer = _make_panel(Vector2(650, 260))
	center.add_child(panel)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 24)
	panel.add_child(column)
	_end_title_label = _make_title("装配完成", 36)
	column.add_child(_end_title_label)
	var message := Label.new()
	message.text = "你已完成全部铁路知识题和列车装配。"
	message.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	column.add_child(message)
	var exit_button := Button.new()
	exit_button.name = "EndExitButton"
	exit_button.text = "退出"
	exit_button.custom_minimum_size = Vector2(0, 56)
	exit_button.pressed.connect(exit_requested.emit)
	column.add_child(exit_button)


func _build_fatal_page() -> void:
	var page: Control = _make_full_page(PAGE_FATAL)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	page.add_child(center)
	var panel: PanelContainer = _make_panel(Vector2(720, 340))
	center.add_child(panel)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 18)
	panel.add_child(column)
	column.add_child(_make_title("工程内容无法启动", 32))
	_fatal_code_label = Label.new()
	column.add_child(_fatal_code_label)
	_fatal_message_label = RichTextLabel.new()
	_fatal_message_label.fit_content = false
	_fatal_message_label.custom_minimum_size = Vector2(0, 150)
	_fatal_message_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	column.add_child(_fatal_message_label)
	var exit_button := Button.new()
	exit_button.name = "FatalExitButton"
	exit_button.text = "退出"
	exit_button.pressed.connect(exit_requested.emit)
	column.add_child(exit_button)


func _make_full_page(page_name: StringName) -> Control:
	var page := Control.new()
	page.name = page_name
	page.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(page)
	_pages[page_name] = page
	return page


func _make_panel(minimum_size: Vector2) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.custom_minimum_size = minimum_size
	panel.add_theme_constant_override("margin_left", 28)
	panel.add_theme_constant_override("margin_top", 24)
	panel.add_theme_constant_override("margin_right", 28)
	panel.add_theme_constant_override("margin_bottom", 24)
	return panel


func _make_title(text_value: String, font_size: int) -> Label:
	var label := Label.new()
	label.text = text_value
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", font_size)
	return label


func _set_page_visible(page_name: StringName, visible: bool) -> void:
	if _pages.has(page_name):
		_pages[page_name].visible = visible


func _on_answer_pressed(option_index: int) -> void:
	answer_selected.emit(option_index)
