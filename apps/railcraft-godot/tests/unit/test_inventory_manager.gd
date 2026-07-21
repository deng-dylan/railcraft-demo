extends GutTest

const Fixture := preload("res://tests/fixtures/inventory_manager_fixture.gd")

var _manager: InventoryManager


func before_each() -> void:
	_manager = InventoryManager.new()
	_manager.configure(Fixture.minimal_parts())


func after_each() -> void:
	_manager = null


func test_grant_result_defines_all_inventory_outcomes() -> void:
	var granted := GrantResult.new(GrantResult.Status.GRANTED)
	var already_owned := GrantResult.new(GrantResult.Status.ALREADY_OWNED)
	var unknown := GrantResult.new(GrantResult.Status.UNKNOWN_PART)

	assert_eq(granted.status, GrantResult.Status.GRANTED)
	assert_eq(already_owned.status, GrantResult.Status.ALREADY_OWNED)
	assert_eq(unknown.status, GrantResult.Status.UNKNOWN_PART)
	assert_ne(granted.status, already_owned.status)
	assert_ne(already_owned.status, unknown.status)


func test_configure_accepts_empty_parts_and_clears_owned_inventory() -> void:
	assert_eq(_manager.grant_part("part_one").status, GrantResult.Status.GRANTED)

	var empty_parts: Array[PartData] = []
	_manager.configure(empty_parts)

	assert_true(_manager.get_owned_part_ids().is_empty())
	assert_false(_manager.has_part("part_one"))
	assert_eq(_manager.grant_part("part_one").status, GrantResult.Status.UNKNOWN_PART)


func test_reconfigure_replaces_known_parts_and_clears_owned_inventory() -> void:
	assert_eq(_manager.grant_part("part_one").status, GrantResult.Status.GRANTED)
	var replacement_parts: Array[PartData] = [Fixture.first_release_parts()[0]]

	_manager.configure(replacement_parts)

	assert_true(_manager.get_owned_part_ids().is_empty())
	assert_eq(_manager.grant_part("part_one").status, GrantResult.Status.UNKNOWN_PART)
	assert_eq(_manager.grant_part("body_shell").status, GrantResult.Status.GRANTED)


func test_first_known_grant_adds_exactly_one_owned_part() -> void:
	var result: GrantResult = _manager.grant_part("part_one")

	assert_eq(result.status, GrantResult.Status.GRANTED)
	assert_true(_manager.has_part("part_one"))
	assert_eq(_manager.get_owned_part_ids(), ["part_one"])


func test_repeated_grant_is_idempotent_and_keeps_owned_count() -> void:
	var first: GrantResult = _manager.grant_part("part_one")
	var second: GrantResult = _manager.grant_part("part_one")
	var third: GrantResult = _manager.grant_part("part_one")

	assert_eq(first.status, GrantResult.Status.GRANTED)
	assert_eq(second.status, GrantResult.Status.ALREADY_OWNED)
	assert_eq(third.status, GrantResult.Status.ALREADY_OWNED)
	assert_eq(_manager.get_owned_part_ids().size(), 1)


func test_unknown_and_empty_ids_do_not_change_inventory() -> void:
	var unknown: GrantResult = _manager.grant_part("missing_part")
	var empty: GrantResult = _manager.grant_part("")

	assert_eq(unknown.status, GrantResult.Status.UNKNOWN_PART)
	assert_eq(empty.status, GrantResult.Status.UNKNOWN_PART)
	assert_true(_manager.get_owned_part_ids().is_empty())
	assert_false(_manager.has_part("missing_part"))
	assert_false(_manager.has_part(""))


func test_owned_part_query_returns_an_isolated_array_copy() -> void:
	_manager.grant_part("part_one")
	var returned_ids: Array[String] = _manager.get_owned_part_ids()

	returned_ids.clear()
	returned_ids.append("part_two")

	assert_eq(_manager.get_owned_part_ids(), ["part_one"])
	assert_true(_manager.has_part("part_one"))
	assert_false(_manager.has_part("part_two"))


func test_reset_clears_progress_and_keeps_known_parts_regrantable() -> void:
	_manager.grant_part("part_one")
	_manager.grant_part("part_two")

	_manager.reset()

	assert_true(_manager.get_owned_part_ids().is_empty())
	assert_false(_manager.has_part("part_one"))
	assert_false(_manager.has_part("part_two"))
	assert_eq(_manager.grant_part("part_one").status, GrantResult.Status.GRANTED)


func test_first_release_nine_parts_preserve_order_and_are_each_granted_once() -> void:
	_manager.configure(Fixture.first_release_parts())

	for part_id: String in Fixture.FIRST_RELEASE_PART_IDS:
		assert_eq(_manager.grant_part(part_id).status, GrantResult.Status.GRANTED)

	assert_eq(_manager.get_owned_part_ids(), Fixture.FIRST_RELEASE_PART_IDS)
	assert_eq(_manager.get_owned_part_ids().size(), 9)

	for part_id: String in Fixture.FIRST_RELEASE_PART_IDS:
		assert_eq(_manager.grant_part(part_id).status, GrantResult.Status.ALREADY_OWNED)

	assert_eq(_manager.get_owned_part_ids(), Fixture.FIRST_RELEASE_PART_IDS)
	assert_eq(_manager.get_owned_part_ids().size(), 9)

	_manager.reset()
	assert_true(_manager.get_owned_part_ids().is_empty())
	for part_id: String in Fixture.FIRST_RELEASE_PART_IDS:
		assert_eq(_manager.grant_part(part_id).status, GrantResult.Status.GRANTED)
	assert_eq(_manager.get_owned_part_ids(), Fixture.FIRST_RELEASE_PART_IDS)
