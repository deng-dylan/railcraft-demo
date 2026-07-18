extends GutTest

const Fixture := preload("res://tests/fixtures/assembly_view_fixture.gd")


func test_part_actor_contract_instantiates_and_clicks_only_when_enabled() -> void:
	var scene: PackedScene = load("res://scenes/assembly/part_actor.tscn") as PackedScene
	var actor: PartActor = scene.instantiate() as PartActor
	add_child_autofree(actor)
	actor.part_id = "fixture_part"
	watch_signals(actor)
	var event := InputEventMouseButton.new()
	event.button_index = MOUSE_BUTTON_LEFT
	event.pressed = true

	actor._on_click_area_input_event(null, event, Vector3.ZERO, Vector3.UP, 0)
	assert_signal_emit_count(actor, "part_clicked", 0)
	actor.set_interaction_enabled(true)
	actor._on_click_area_input_event(null, event, Vector3.ZERO, Vector3.UP, 0)
	assert_signal_emitted_with_parameters(actor, "part_clicked", ["fixture_part"])
	actor.set_interaction_enabled(false)
	actor._on_click_area_input_event(null, event, Vector3.ZERO, Vector3.UP, 0)
	assert_signal_emit_count(actor, "part_clicked", 1)
	assert_not_null(actor.get_node_or_null(^"VisualRoot"))
	assert_not_null(actor.get_node_or_null(^"ClickArea/CollisionShape3D"))


func test_train_root_matches_component_target_light_and_animation_contract() -> void:
	var root: Node3D = Fixture.instantiate_train_root()
	add_child_autofree(root)
	for path: NodePath in AssemblyView.COMPONENT_CONTAINER_PATHS.values():
		assert_true(root.get_node_or_null(path) is Node3D, String(path))
	for target_path: String in Fixture.TARGET_PATHS:
		assert_true(root.get_node_or_null(NodePath(target_path)) is Marker3D, target_path)
	assert_true(root.get_node_or_null(^"Headlights") is Node3D)
	assert_true(root.get_node_or_null(^"Headlights/LeftLight") is SpotLight3D)
	assert_true(root.get_node_or_null(^"Headlights/RightLight") is SpotLight3D)
	assert_true(root.get_node_or_null(^"FinalAnimationAnchors/HeadlightReveal") is Marker3D)
	assert_true(root.get_node_or_null(^"FinalAnimationAnchors/PantographLift") is Marker3D)
	assert_true(root.get_node_or_null(^"FinalAnimationAnchors/WheelRotation") is Marker3D)
	assert_true(root.get_node_or_null(^"KeyLight") is DirectionalLight3D)
	assert_true(root.get_node_or_null(^"FillLight") is OmniLight3D)


func test_view_reports_each_missing_required_node() -> void:
	var view := AssemblyView.new()
	add_child_autofree(view)
	assert_false(view.is_ready_for_parts())
	assert_true(view.last_error.contains("PartPreviewAnchor"))

	view.add_child(Node3D.new())
	view.get_child(0).name = "PartPreviewAnchor"
	assert_false(view.is_ready_for_parts())
	assert_true(view.last_error.contains("TrainAssemblyRoot"))

	var train_root: Node3D = Fixture.instantiate_train_root()
	view.add_child(train_root)
	assert_false(view.is_ready_for_parts())
	assert_true(view.last_error.contains("TargetHints"))

	var hints := Node3D.new()
	hints.name = "TargetHints"
	view.add_child(hints)
	assert_true(view.is_ready_for_parts(), view.last_error)


func test_prepare_applies_preview_transform_injects_id_and_is_idempotent() -> void:
	var view: AssemblyView = _add_view()
	var part: PartData = Fixture.transformed_body()
	assert_true(view.prepare_part(part), view.last_error)
	var actor: PartActor = view.get_part_actor(part.part_id)
	var first_instance_id: int = actor.get_instance_id()

	assert_eq(actor.part_id, part.part_id)
	assert_true(actor.transform.is_equal_approx(part.preview_transform.to_transform_3d()))
	assert_true(actor.is_interaction_enabled())
	assert_true(view.prepare_part(part), view.last_error)
	assert_eq(view.get_part_actor(part.part_id).get_instance_id(), first_instance_id)
	assert_eq(view.get_node(^"PartPreviewAnchor").get_child_count(), 1)


func test_target_lookup_combines_marker_and_content_adjustment() -> void:
	var view: AssemblyView = _add_view()
	var part: PartData = Fixture.transformed_body()
	assert_true(view.prepare_part(part), view.last_error)
	var target: Marker3D = view.get_snap_target(part.part_id)
	var expected: Transform3D = target.global_transform * part.target_transform.to_transform_3d()

	assert_not_null(target)
	assert_true(view.get_snap_transform(part.part_id).is_equal_approx(expected))


func test_hint_is_visual_only_emissive_and_uses_install_transform() -> void:
	var view: AssemblyView = _add_view()
	var part: PartData = Fixture.transformed_body()
	assert_true(view.prepare_part(part), view.last_error)
	var hint: Node3D = view.get_target_hint()

	assert_not_null(hint)
	assert_true(hint.global_transform.is_equal_approx(view.get_snap_transform(part.part_id)))
	assert_eq(_count_collision_objects(hint), 0)
	var geometry: GeometryInstance3D = _first_geometry(hint)
	assert_not_null(geometry)
	assert_true(geometry.material_override is StandardMaterial3D)
	var material: StandardMaterial3D = geometry.material_override as StandardMaterial3D
	assert_true(material.emission_enabled)
	assert_eq(material.transparency, BaseMaterial3D.TRANSPARENCY_ALPHA)


func test_finalize_places_disables_and_groups_visual_actor() -> void:
	var view: AssemblyView = _add_view()
	var part: PartData = Fixture.transformed_body()
	assert_true(view.prepare_part(part), view.last_error)
	var expected: Transform3D = view.get_snap_transform(part.part_id)
	view.finalize_visual_install(part.part_id)
	var actor: PartActor = view.get_part_actor(part.part_id)

	assert_not_null(actor)
	assert_false(actor.is_interaction_enabled())
	assert_eq(actor.get_parent().name, "CarbodyConnection")
	assert_true(actor.global_transform.is_equal_approx(expected))
	assert_eq(view.get_installed_part_ids(), [part.part_id])
	assert_null(view.get_target_hint())
	assert_eq(view.get_node(^"PartPreviewAnchor").get_child_count(), 0)


func test_component_highlight_is_scoped_and_restores_overrides() -> void:
	var view: AssemblyView = _add_view()
	_install(view, Fixture.part(0))
	_install(view, Fixture.part(3))
	var carbody_mesh: GeometryInstance3D = _first_geometry(view.get_part_actor("body_shell"))
	var running_mesh: GeometryInstance3D = _first_geometry(view.get_part_actor("bogie_frame"))
	var carbody_original: Material = carbody_mesh.material_override
	var running_original: Material = running_mesh.material_override

	view.show_component_highlight("carbody_connection")
	assert_ne(carbody_mesh.material_override, carbody_original)
	assert_eq(running_mesh.material_override, running_original)
	view.clear_component_highlight("carbody_connection")
	assert_eq(carbody_mesh.material_override, carbody_original)
	assert_eq(running_mesh.material_override, running_original)


func test_interaction_master_switch_gates_forwarded_clicks() -> void:
	var view: AssemblyView = _add_view()
	assert_true(view.prepare_part(Fixture.part(0)), view.last_error)
	watch_signals(view)
	var actor: PartActor = view.get_part_actor("body_shell")

	view.set_part_interaction_enabled(false)
	actor.part_clicked.emit(actor.part_id)
	assert_signal_emit_count(view, "part_clicked", 0)
	view.set_part_interaction_enabled(true)
	actor.part_clicked.emit(actor.part_id)
	assert_signal_emitted_with_parameters(view, "part_clicked", [actor.part_id])


func test_failed_preparations_cleanup_and_do_not_poison_next_part() -> void:
	var view: AssemblyView = _add_view()
	var missing_scene: PartData = Fixture.part(0)
	missing_scene.model_scene_path = "res://scenes/train/parts/missing.tscn"
	assert_false(view.prepare_part(missing_scene))
	assert_null(view.get_part_actor(missing_scene.part_id))
	assert_null(view.get_target_hint())

	var missing_target: PartData = Fixture.body_with_target("SnapTargets/MissingTarget")
	assert_false(view.prepare_part(missing_target))
	assert_null(view.get_part_actor(missing_target.part_id))
	assert_null(view.get_target_hint())
	assert_eq(view.get_node(^"PartPreviewAnchor").get_child_count(), 0)

	assert_true(view.prepare_part(Fixture.part(0)), view.last_error)
	assert_not_null(view.get_part_actor("body_shell"))
	assert_not_null(view.get_target_hint())


func test_all_nine_production_scenes_have_distinct_visual_and_animation_contracts() -> void:
	for index: int in Fixture.PART_IDS.size():
		var scene: PackedScene = load(Fixture.SCENE_PATHS[index]) as PackedScene
		var actor: PartActor = scene.instantiate() as PartActor
		assert_not_null(actor, Fixture.SCENE_PATHS[index])
		assert_true(actor.get_node_or_null(^"VisualRoot") is Node3D, Fixture.PART_IDS[index])
		assert_true(actor.get_node_or_null(^"ClickArea") is Area3D, Fixture.PART_IDS[index])
		assert_true(
			actor.get_node_or_null(^"ClickArea/CollisionShape3D") is CollisionShape3D,
			Fixture.PART_IDS[index],
		)
		assert_true(actor.get_node_or_null(^"AnimationNodes") is Node3D, Fixture.PART_IDS[index])
		assert_gt(_count_geometry(actor.get_node(^"VisualRoot")), 0, Fixture.PART_IDS[index])
		actor.free()

	var wheel_scene: PackedScene = load(Fixture.SCENE_PATHS[4]) as PackedScene
	var wheel: PartActor = wheel_scene.instantiate() as PartActor
	assert_true(wheel.get_node_or_null(^"VisualRoot/WheelRotationRoot") is Node3D)
	assert_true(wheel.get_node_or_null(^"AnimationNodes/WheelRotationAnchor") is Marker3D)
	wheel.free()
	var pantograph_scene: PackedScene = load(Fixture.SCENE_PATHS[6]) as PackedScene
	var pantograph: PartActor = pantograph_scene.instantiate() as PartActor
	assert_true(pantograph.get_node_or_null(^"VisualRoot/PantographLiftRoot") is Node3D)
	assert_true(pantograph.get_node_or_null(^"AnimationNodes/PantographLiftAnchor") is Marker3D)
	pantograph.free()


func test_real_catalog_installs_all_nine_in_data_order_without_residue() -> void:
	var result: ContentLoadResult = ContentRepository.new().load_catalog()
	assert_true(result.is_success)
	var view: AssemblyView = _add_view()
	var expected_ids: Array[String] = []
	for part: PartData in result.catalog.get_parts():
		expected_ids.append(part.part_id)
		assert_true(view.prepare_part(part), "%s: %s" % [part.part_id, view.last_error])
		assert_not_null(view.get_target_hint(), part.part_id)
		view.finalize_visual_install(part.part_id)
		assert_eq(
			view.get_part_actor(part.part_id).get_parent().name, _container_name(part.component_id)
		)

	assert_eq(expected_ids, Fixture.PART_IDS)
	assert_eq(view.get_installed_part_ids(), expected_ids)
	assert_null(view.get_target_hint())
	assert_eq(view.get_node(^"PartPreviewAnchor").get_child_count(), 0)


func _add_view() -> AssemblyView:
	var view: AssemblyView = Fixture.instantiate_view()
	add_child_autofree(view)
	return view


func _install(view: AssemblyView, part: PartData) -> void:
	assert_true(view.prepare_part(part), view.last_error)
	view.finalize_visual_install(part.part_id)
	assert_true(view.last_error.is_empty(), view.last_error)


func _first_geometry(node: Node) -> GeometryInstance3D:
	if node is GeometryInstance3D:
		return node as GeometryInstance3D
	for child: Node in node.get_children():
		var found: GeometryInstance3D = _first_geometry(child)
		if found != null:
			return found
	return null


func _count_geometry(node: Node) -> int:
	var result: int = 1 if node is GeometryInstance3D else 0
	for child: Node in node.get_children():
		result += _count_geometry(child)
	return result


func _count_collision_objects(node: Node) -> int:
	var result: int = 1 if node is CollisionObject3D else 0
	for child: Node in node.get_children():
		result += _count_collision_objects(child)
	return result


func _container_name(component_id: String) -> String:
	match component_id:
		"carbody_connection":
			return "CarbodyConnection"
		"running_braking":
			return "RunningBraking"
		"traction_power":
			return "TractionPower"
	return ""
