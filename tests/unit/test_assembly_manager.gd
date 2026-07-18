extends GutTest

const Fixture := preload("res://tests/fixtures/assembly_manager_fixture.gd")

var _manager: AssemblyManager


func before_each() -> void:
	_manager = AssemblyManager.new()
	_configure_manager(_manager)


func test_install_check_exposes_every_typed_status() -> void:
	var allowed := InstallCheck.new(InstallCheck.Status.ALLOWED)
	var out_of_order := InstallCheck.new(InstallCheck.Status.OUT_OF_ORDER)
	var already_installed := InstallCheck.new(InstallCheck.Status.ALREADY_INSTALLED)
	var pending := InstallCheck.new(InstallCheck.Status.ANOTHER_INSTALL_PENDING)
	var unknown := InstallCheck.new(InstallCheck.Status.UNKNOWN_PART)
	var prerequisite := InstallCheck.new(InstallCheck.Status.PREREQUISITE_MISSING)

	assert_eq(typeof(allowed.status), TYPE_INT)
	assert_true(allowed.is_allowed())
	assert_eq(out_of_order.status, InstallCheck.Status.OUT_OF_ORDER)
	assert_eq(already_installed.status, InstallCheck.Status.ALREADY_INSTALLED)
	assert_eq(pending.status, InstallCheck.Status.ANOTHER_INSTALL_PENDING)
	assert_eq(unknown.status, InstallCheck.Status.UNKNOWN_PART)
	assert_eq(prerequisite.status, InstallCheck.Status.PREREQUISITE_MISSING)


func test_assembly_outcome_exposes_every_status_and_typed_fields() -> void:
	var committed := (
		AssemblyOutcome
		. new(
			AssemblyOutcome.Status.COMMITTED,
			"part",
			"component",
			true,
			"next",
		)
	)
	var no_pending := AssemblyOutcome.new(AssemblyOutcome.Status.NO_INSTALL_PENDING)
	var mismatch := AssemblyOutcome.new(AssemblyOutcome.Status.PENDING_PART_MISMATCH)
	var installed := AssemblyOutcome.new(AssemblyOutcome.Status.ALREADY_INSTALLED)

	assert_true(committed.is_committed())
	assert_eq(typeof(committed.status), TYPE_INT)
	assert_eq(typeof(committed.installed_part_id), TYPE_STRING)
	assert_eq(typeof(committed.completed_component_id), TYPE_STRING)
	assert_eq(typeof(committed.train_completed), TYPE_BOOL)
	assert_eq(typeof(committed.next_expected_part_id), TYPE_STRING)
	assert_eq(no_pending.status, AssemblyOutcome.Status.NO_INSTALL_PENDING)
	assert_eq(mismatch.status, AssemblyOutcome.Status.PENDING_PART_MISMATCH)
	assert_eq(installed.status, AssemblyOutcome.Status.ALREADY_INSTALLED)


func test_configure_and_reset_clear_pending_installed_and_completion_state() -> void:
	_install_next(Fixture.PART_IDS[0])
	assert_eq(
		_manager.begin_install(Fixture.PART_IDS[1]).status,
		InstallCheck.Status.ALLOWED,
	)

	_manager.configure(Fixture.parts(), Fixture.components(), Fixture.train_recipe())

	assert_eq(_manager.get_expected_part_id(), Fixture.PART_IDS[0])
	assert_eq(_manager.get_pending_part_id(), "")
	assert_eq(_manager.get_installed_part_ids(), [])
	assert_eq(_manager.get_completed_component_ids(), [])
	assert_false(_manager.is_train_completed())

	_install_all_parts()
	_manager.reset()

	assert_eq(_manager.get_expected_part_id(), Fixture.PART_IDS[0])
	assert_eq(_manager.get_installed_part_ids(), [])
	assert_eq(_manager.get_completed_component_ids(), [])
	assert_false(_manager.is_train_completed())


func test_expected_part_advances_and_is_empty_after_all_parts() -> void:
	assert_eq(_manager.get_expected_part_id(), Fixture.PART_IDS[0])
	for index: int in Fixture.PART_IDS.size():
		var outcome: AssemblyOutcome = _install_next(Fixture.PART_IDS[index])
		var expected: String = (
			"" if index == Fixture.PART_IDS.size() - 1 else Fixture.PART_IDS[index + 1]
		)
		assert_eq(outcome.next_expected_part_id, expected)
		assert_eq(_manager.get_expected_part_id(), expected)


func test_can_begin_distinguishes_statuses_and_never_mutates_state() -> void:
	assert_eq(
		_manager.can_begin_install("unknown").status,
		InstallCheck.Status.UNKNOWN_PART,
	)
	assert_eq(
		_manager.can_begin_install(Fixture.PART_IDS[1]).status,
		InstallCheck.Status.OUT_OF_ORDER,
	)
	assert_eq(
		_manager.can_begin_install(Fixture.PART_IDS[0]).status,
		InstallCheck.Status.ALLOWED,
	)
	assert_eq(
		_manager.can_begin_install(Fixture.PART_IDS[0]).status,
		InstallCheck.Status.ALLOWED,
	)
	assert_eq(_manager.get_pending_part_id(), "")
	assert_eq(_manager.get_installed_part_ids(), [])

	_install_next(Fixture.PART_IDS[0])
	assert_eq(
		_manager.can_begin_install(Fixture.PART_IDS[0]).status,
		InstallCheck.Status.ALREADY_INSTALLED,
	)

	var missing_prerequisite_manager := AssemblyManager.new()
	(
		missing_prerequisite_manager
		. configure(
			Fixture.parts_with_first_prerequisite("missing_dependency"),
			Fixture.components(),
			Fixture.train_recipe(),
		)
	)
	assert_eq(
		missing_prerequisite_manager.can_begin_install(Fixture.PART_IDS[0]).status,
		InstallCheck.Status.PREREQUISITE_MISSING,
	)
	assert_eq(missing_prerequisite_manager.get_pending_part_id(), "")
	assert_eq(missing_prerequisite_manager.get_installed_part_ids(), [])


func test_begin_opens_only_one_pending_transaction_without_installing() -> void:
	assert_eq(
		_manager.begin_install("unknown").status,
		InstallCheck.Status.UNKNOWN_PART,
	)
	assert_eq(
		_manager.begin_install(Fixture.PART_IDS[1]).status,
		InstallCheck.Status.OUT_OF_ORDER,
	)
	assert_eq(_manager.get_pending_part_id(), "")
	assert_eq(_manager.get_installed_part_ids(), [])

	var first: InstallCheck = _manager.begin_install(Fixture.PART_IDS[0])
	var concurrent: InstallCheck = _manager.begin_install(Fixture.PART_IDS[1])
	var queried: InstallCheck = _manager.can_begin_install(Fixture.PART_IDS[0])

	assert_eq(first.status, InstallCheck.Status.ALLOWED)
	assert_eq(concurrent.status, InstallCheck.Status.ANOTHER_INSTALL_PENDING)
	assert_eq(queried.status, InstallCheck.Status.ANOTHER_INSTALL_PENDING)
	assert_eq(_manager.get_pending_part_id(), Fixture.PART_IDS[0])
	assert_false(_manager.is_part_installed(Fixture.PART_IDS[0]))
	assert_eq(_manager.get_expected_part_id(), Fixture.PART_IDS[0])


func test_abort_requires_matching_id_and_allows_retry() -> void:
	_manager.begin_install(Fixture.PART_IDS[0])
	_manager.abort_pending_install(Fixture.PART_IDS[1])
	assert_eq(_manager.get_pending_part_id(), Fixture.PART_IDS[0])

	_manager.abort_pending_install(Fixture.PART_IDS[0])
	assert_eq(_manager.get_pending_part_id(), "")
	assert_eq(
		_manager.begin_install(Fixture.PART_IDS[0]).status,
		InstallCheck.Status.ALLOWED,
	)


func test_commit_requires_matching_pending_id_and_failures_have_no_partial_commit() -> void:
	var no_pending: AssemblyOutcome = _manager.commit_install(Fixture.PART_IDS[0])
	assert_eq(no_pending.status, AssemblyOutcome.Status.NO_INSTALL_PENDING)
	assert_eq(_manager.get_installed_part_ids(), [])

	_manager.begin_install(Fixture.PART_IDS[0])
	var mismatch: AssemblyOutcome = _manager.commit_install(Fixture.PART_IDS[1])
	assert_eq(mismatch.status, AssemblyOutcome.Status.PENDING_PART_MISMATCH)
	assert_eq(mismatch.installed_part_id, "")
	assert_eq(_manager.get_pending_part_id(), Fixture.PART_IDS[0])
	assert_eq(_manager.get_installed_part_ids(), [])
	assert_eq(_manager.get_expected_part_id(), Fixture.PART_IDS[0])

	var committed: AssemblyOutcome = _manager.commit_install(Fixture.PART_IDS[0])
	assert_eq(committed.status, AssemblyOutcome.Status.COMMITTED)
	assert_eq(committed.installed_part_id, Fixture.PART_IDS[0])
	assert_eq(_manager.get_pending_part_id(), "")
	assert_true(_manager.is_part_installed(Fixture.PART_IDS[0]))


func test_duplicate_and_stale_events_are_idempotent() -> void:
	_install_next(Fixture.PART_IDS[0])
	var before_installed: Array[String] = _manager.get_installed_part_ids()
	var before_expected: String = _manager.get_expected_part_id()

	assert_eq(
		_manager.begin_install(Fixture.PART_IDS[0]).status,
		InstallCheck.Status.ALREADY_INSTALLED,
	)
	var duplicate: AssemblyOutcome = _manager.commit_install(Fixture.PART_IDS[0])
	assert_eq(duplicate.status, AssemblyOutcome.Status.ALREADY_INSTALLED)
	assert_eq(_manager.get_installed_part_ids(), before_installed)
	assert_eq(_manager.get_expected_part_id(), before_expected)

	_manager.begin_install(Fixture.PART_IDS[1])
	var stale: AssemblyOutcome = _manager.commit_install(Fixture.PART_IDS[0])
	assert_eq(stale.status, AssemblyOutcome.Status.PENDING_PART_MISMATCH)
	assert_eq(_manager.get_pending_part_id(), Fixture.PART_IDS[1])
	assert_eq(_manager.get_installed_part_ids(), before_installed)
	assert_eq(_manager.get_expected_part_id(), before_expected)


func test_three_six_nine_complete_components_once_and_ninth_completes_train_once() -> void:
	var completed_events: Array[String] = []
	var train_completion_events: int = 0
	for index: int in Fixture.PART_IDS.size():
		var outcome: AssemblyOutcome = _install_next(Fixture.PART_IDS[index])
		var expected_component_id: String = (
			Fixture.COMPONENT_IDS[index / 3] if (index + 1) % 3 == 0 else ""
		)
		assert_eq(outcome.completed_component_id, expected_component_id)
		if not outcome.completed_component_id.is_empty():
			completed_events.append(outcome.completed_component_id)
		if outcome.train_completed:
			train_completion_events += 1
		if index < Fixture.PART_IDS.size() - 1:
			assert_false(outcome.train_completed)

	assert_eq(completed_events, Fixture.COMPONENT_IDS)
	assert_eq(_manager.get_completed_component_ids(), Fixture.COMPONENT_IDS)
	assert_eq(train_completion_events, 1)
	assert_true(_manager.is_train_completed())
	assert_eq(_manager.get_expected_part_id(), "")

	assert_eq(
		_manager.begin_install(Fixture.PART_IDS[8]).status,
		InstallCheck.Status.ALREADY_INSTALLED,
	)
	var duplicate: AssemblyOutcome = _manager.commit_install(Fixture.PART_IDS[8])
	assert_eq(duplicate.status, AssemblyOutcome.Status.ALREADY_INSTALLED)
	assert_eq(duplicate.completed_component_id, "")
	assert_false(duplicate.train_completed)
	assert_eq(_manager.get_completed_component_ids(), Fixture.COMPONENT_IDS)
	assert_true(_manager.is_train_completed())


func test_read_only_snapshots_do_not_expose_internal_collections() -> void:
	_install_next(Fixture.PART_IDS[0])
	_install_next(Fixture.PART_IDS[1])
	_install_next(Fixture.PART_IDS[2])
	var installed_snapshot: Array[String] = _manager.get_installed_part_ids()
	var completed_snapshot: Array[String] = _manager.get_completed_component_ids()

	installed_snapshot.clear()
	completed_snapshot.append("injected")

	assert_eq(
		_manager.get_installed_part_ids(),
		Fixture.PART_IDS.slice(0, 3),
	)
	assert_eq(_manager.get_completed_component_ids(), [Fixture.COMPONENT_IDS[0]])


func _configure_manager(manager: AssemblyManager) -> void:
	manager.configure(Fixture.parts(), Fixture.components(), Fixture.train_recipe())


func _install_next(part_id: String) -> AssemblyOutcome:
	var check: InstallCheck = _manager.begin_install(part_id)
	assert_eq(check.status, InstallCheck.Status.ALLOWED)
	var outcome: AssemblyOutcome = _manager.commit_install(part_id)
	assert_eq(outcome.status, AssemblyOutcome.Status.COMMITTED)
	return outcome


func _install_all_parts() -> void:
	for part_id: String in Fixture.PART_IDS:
		_install_next(part_id)
