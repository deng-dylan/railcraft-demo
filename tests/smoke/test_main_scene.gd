extends GutTest


func test_main_scene_loads_and_enters_start_state() -> void:
	var scene: PackedScene = load("res://scenes/main/main.tscn") as PackedScene
	assert_not_null(scene)
	if scene == null:
		return
	var app: AppRoot = scene.instantiate() as AppRoot
	assert_not_null(app)
	if app == null:
		return
	add_child_autofree(app)
	await get_tree().process_frame

	assert_true(app.is_initialized())
	assert_not_null(app.get_catalog())
	assert_eq(app.get_flow_manager().get_state(), GameFlowManager.GameState.START)
	assert_has(
		app.get_screen_coordinator().get_visible_page_names(),
		ScreenCoordinator.PAGE_START,
	)
