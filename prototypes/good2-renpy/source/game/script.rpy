# 游戏的脚本可置于此文件中。

# 声明此游戏使用的角色。颜色参数可使角色姓名着色。

define e = Character("艾琳")


# 游戏在此开始。

# ============================================
# 高铁探索收集系统 - 完整稳定版
# 使用 menu + 自定义界面
# ============================================

# ============================================
# 高铁探索收集系统 - 最终稳定版
# ============================================

label start:
    jump test_mode

default collected_parts = []
default q_score = 0
default current_q = None
default unity_mode = False
default current_trigger = None

init python:
    q_list = [
        {
            "id": "bogie",
            "question": "转向架在列车中承担什么核心功能？",
            "options": ["承载走行+转向", "提供电力", "控制空调"],
            "answer": 0,
            "model": "转向架",
            "knowledge": "转向架相当于列车的'脚'，保证列车平稳高速运行。",
            "scene_hint": "在列车底部发现了一个复杂的机械结构..."
        },
        {
            "id": "pantograph",
            "question": "受电弓从接触网获取的电压大约是多少？",
            "options": ["25kV交流电", "750V直流电", "380V交流电"],
            "answer": 0,
            "model": "受电弓",
            "knowledge": "中国高铁接触网标准电压为25kV单相交流电。",
            "scene_hint": "车顶上方，一个金属装置连接着高压线..."
        },
        {
            "id": "body_structure",
            "question": "和谐号车体主要采用什么材料制造？",
            "options": ["不锈钢", "铝合金", "碳钢"],
            "answer": 1,
            "model": "铝合金车体",
            "knowledge": "铝合金重量轻、强度高、耐腐蚀。",
            "scene_hint": "车身表面泛着银白色的金属光泽..."
        },
        {
            "id": "cab_atp",
            "question": "驾驶室ATP系统的中文全称是什么？",
            "options": ["自动列车防护", "自动列车运行", "自动列车调度"],
            "answer": 0,
            "model": "驾驶室ATP",
            "knowledge": "ATP是高铁的'安全大脑'，保障行车安全。",
            "scene_hint": "驾驶室内，屏幕显示着复杂的信号数据..."
        },
        {
            "id": "traction_motor",
            "question": "牵引电机安装在转向架的什么位置？",
            "options": ["轴端", "构架侧梁", "车体下方"],
            "answer": 1,
            "model": "牵引电机",
            "knowledge": "牵引电机是列车的'心脏'，驱动车轮转动。",
            "scene_hint": "转向架侧面，一个巨大的圆柱形装置..."
        },
        {
            "id": "power_distribution",
            "question": "和谐号动车组采用的是哪种动力形式？",
            "options": ["动力集中", "动力分散", "纯电力推挽"],
            "answer": 1,
            "model": "动力分散系统",
            "knowledge": "和谐号采用动力分散技术，每节车厢都有电机驱动。",
            "scene_hint": "每节车厢底部都有动力装置..."
        },
        {
            "id": "braking_system",
            "question": "和谐号最主要的制动方式是什么？",
            "options": ["盘式制动", "再生制动", "电磁制动"],
            "answer": 1,
            "model": "再生制动系统",
            "knowledge": "再生制动把动能转化为电能送回电网，节能环保。",
            "scene_hint": "车轮旁，有一套复杂的制动装置..."
        },
        {
            "id": "coupler",
            "question": "两节车厢之间用什么装置连接？",
            "options": ["密接式车钩", "缓冲器", "铰链"],
            "answer": 0,
            "model": "密接式车钩",
            "knowledge": "密接式车钩让车厢之间紧密连接，减少晃动。",
            "scene_hint": "两节车厢连接处，一个精密的机械结构..."
        },
        {
            "id": "hvac",
            "question": "空调机组通常安装在列车的什么位置？",
            "options": ["车顶", "车底", "车厢内部"],
            "answer": 0,
            "model": "车顶空调",
            "knowledge": "车顶空调不占用车内空间，保证温度舒适。",
            "scene_hint": "车顶上方，有通风口和散热装置..."
        },
        {
            "id": "aerodynamics",
            "question": "和谐号头部流线型设计的主要作用是什么？",
            "options": ["减小空气阻力", "美观装饰", "安装雷达"],
            "answer": 0,
            "model": "流线型头型",
            "knowledge": "流线型车头大幅降低空气阻力，让350km/h成为可能。",
            "scene_hint": "车头呈子弹头形状，极具流线感..."
        }
    ]
    
    def find_q(part_id):
        for q in q_list:
            if q["id"] == part_id:
                return q
        return None
    
    def save_progress():
        import json
        data = {
            "collected_parts": renpy.store.collected_parts,
            "total_parts": len(q_list),
            "score": renpy.store.q_score
        }
        try:
            with open("progress.json", "w") as f:
                json.dump(data, f)
        except:
            pass
    
    def save_unlock(part_id, part_name):
        import json
        try:
            with open("unlocked_part.json", "w") as f:
                json.dump({
                    "part_id": part_id,
                    "part_name": part_name,
                    "action": "unlock",
                    "total_collected": len(renpy.store.collected_parts)
                }, f)
        except:
            pass

# ============================================
# 核心答题系统 - 使用最简单的方式
# ============================================

label quiz_trigger(part_id):
    $ current_q = find_q(part_id)
    
    if current_q is None:
        return
    
    if part_id in collected_parts:
        scene black
        show text "{size=40}🔧 已收集{/size}\n\n{size=25}这个部件你已经获得了！{/size}\n\n{size=20}继续探索其他部件吧 🚀{/size}" with dissolve
        pause 2
        hide text with dissolve
        return
    
    # 显示场景提示
    scene black
    show text "{size=40}🔍 发现新部件！{/size}\n\n{size=25}[current_q['scene_hint']]{/size}" with dissolve
    pause 2
    hide text with dissolve
    
    # 使用 menu 显示题目 - 最简单的方式
    label quiz_loop:
        menu:
            "【[current_q['model']]】[current_q['question']]"
            "已收集 [len(collected_parts)] / [len(q_list)] 个部件"
            
            "[current_q['options'][0]]":
                if 0 == current_q["answer"]:
                    jump quiz_correct
                else:
                    jump quiz_wrong
            
            "[current_q['options'][1]]":
                if 1 == current_q["answer"]:
                    jump quiz_correct
                else:
                    jump quiz_wrong
            
            "[current_q['options'][2]]":
                if 2 == current_q["answer"]:
                    jump quiz_correct
                else:
                    jump quiz_wrong
    
    label quiz_correct:
        $ part_name = current_q["model"]
        $ part_id = current_q["id"]
        
        if part_id not in collected_parts:
            $ collected_parts.append(part_id)
            $ q_score += 10
        
        # 显示成功信息
        scene black
        show text "{size=50}🎉 获得部件！{/size}\n\n{size=30}✅ 你成功获得了 [part_name]！{/size}\n\n{size=24}[current_q['knowledge']]{/size}" with dissolve
        pause 3
        hide text with dissolve
        
        # 保存进度
        python:
            save_progress()
            save_unlock(part_id, part_name)
        
        # 检查是否收集完成
        if len(collected_parts) >= len(q_list):
            scene black
            show text "{size=50}🎊 恭喜完成！{/size}\n\n{size=30}你已集齐所有 [len(q_list)] 个部件！{/size}\n\n{size=28}🏆 总分：[q_score] 分{/size}\n\n{size=24}你已成为高铁专家！{/size}" with dissolve
            pause 4
            hide text with dissolve
            
            if unity_mode:
                python:
                    import json
                    try:
                        with open("game_complete.json", "w") as f:
                            json.dump({
                                "status": "complete", 
                                "score": renpy.store.q_score,
                                "collected": renpy.store.collected_parts
                            }, f)
                    except:
                        pass
        
        return
    
    label quiz_wrong:
        $ correct_answer = current_q["options"][current_q["answer"]]
        
        scene black
        show text "{size=50}❌ 再想想哦~{/size}\n\n{size=30}正确答案：[correct_answer]{/size}\n\n{size=24}💡 仔细观察部件特征！{/size}" with dissolve
        pause 2
        hide text with dissolve
        
        jump quiz_loop

# ============================================
# Unity 集成模式
# ============================================

label unity_wait_mode:
    scene black
    show text "{size=45}🚄 高铁探索之旅{/size}\n{size=25}在列车场景中发现部件吧！{/size}" with dissolve
    
    python:
        import os
        import json
        
        trigger_file = "unity_trigger.json"
        
        while True:
            if os.path.exists(trigger_file):
                try:
                    with open(trigger_file, "r") as f:
                        trigger_data = json.load(f)
                    
                    if "part_id" in trigger_data:
                        part_id = trigger_data["part_id"]
                        if part_id not in renpy.store.collected_parts:
                            renpy.store.current_trigger = part_id
                            os.remove(trigger_file)
                            renpy.jump("on_unity_trigger")
                            break
                        else:
                            os.remove(trigger_file)
                except:
                    pass
            
            renpy.pause(0.1, hard=True)
    
    return

label on_unity_trigger:
    $ part_id = current_trigger
    call quiz_trigger(part_id) from _call_quiz_trigger
    jump unity_wait_mode

# ============================================
# 测试模式
# ============================================

label test_mode:
    scene black
    show text "{size=50}🚄 探索收集测试模式{/size}\n{size=30}模拟从Unity触发答题{/size}" with dissolve
    pause 2
    hide text with dissolve
    
    # 测试所有10个部件 - 注意：答错的会重试直到答对
    call test_trigger("bogie") from _call_test_trigger
    call test_trigger("pantograph") from _call_test_trigger_1
    call test_trigger("body_structure") from _call_test_trigger_2
    call test_trigger("cab_atp") from _call_test_trigger_3
    call test_trigger("traction_motor") from _call_test_trigger_4
    call test_trigger("power_distribution") from _call_test_trigger_5
    call test_trigger("braking_system") from _call_test_trigger_6
    call test_trigger("coupler") from _call_test_trigger_7
    call test_trigger("hvac") from _call_test_trigger_8
    call test_trigger("aerodynamics") from _call_test_trigger_9
    
    # 显示最终结果
    scene black
    show text "{size=50}🎉 全部测试完成！{/size}\n\n{size=30}已收集：[len(collected_parts)] / [len(q_list)] 个部件{/size}\n\n{size=28}🏆 总分：[q_score] 分{/size}" with dissolve
    pause 4
    
    return

label test_trigger(part):
    "🔍 你发现了一个新部件：[part]"
    call quiz_trigger(part) from _call_quiz_trigger_1
    return

# ============================================
# 额外功能
# ============================================

label reset_progress:
    $ collected_parts = []
    $ q_score = 0
    $ current_q = None
    scene black
    show text "{size=40}🔄 进度已重置{/size}" with dissolve
    pause 2
    hide text with dissolve
    return

label show_progress:
    scene black
    $ progress_text = "📊 当前进度\n\n已收集：[len(collected_parts)] / [len(q_list)] 个部件\n总分：[q_score] 分"
    
    if collected_parts:
        $ progress_text += "\n\n已收集的部件："
        python:
            for part_id in renpy.store.collected_parts:
                q = find_q(part_id)
                if q:
                    progress_text += "\n  ✅ " + q["model"]
    else:
        $ progress_text += "\n\n还没有收集任何部件，快去探索吧！"
    
    show text "[progress_text]" with dissolve
    pause 3
    hide text with dissolve
    return