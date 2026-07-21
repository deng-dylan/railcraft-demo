class_name QuizManagerFixture
extends RefCounted


static func two_questions() -> Array[QuestionData]:
	return [
		_question("q01", 1, 2, "part_one"),
		_question("q02", 2, 0, "part_two"),
	]


static func _question(
	question_id: String,
	order: int,
	correct_option_index: int,
	reward_part_id: String,
) -> QuestionData:
	return (
		QuestionData
		. new(
			question_id,
			order,
			"测试题目 %d" % order,
			["选项甲", "选项乙", "选项丙", "选项丁"],
			correct_option_index,
			"测试解析 %d" % order,
			SourceData.new("测试机构", "测试资料", "https://example.invalid/source"),
			reward_part_id,
		)
	)
