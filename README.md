# 迷失孤岛 Lost Island

丧尸塔防生存游戏 - 完整项目

## 项目结构

```
lost-island/
├── code/                          # 游戏源代码（Unity项目）
│   └── LostIsland/
│       └── Assets/
│           ├── Scripts/
│           │   ├── Core/          # 核心单例、事件总线、GameManager
│           │   ├── Infrastructure/ # 对象池等基础设施
│           │   ├── Utils/         # 工具类（计时器、数学、扩展方法）
│           │   ├── Logic/         # 业务逻辑层（各Manager）
│           │   ├── Data/          # 数据层（配置、运行时数据、存档）
│           │   ├── Entity/        # 实体组件系统
│           │   └── View/          # 表现层（UI、战斗表现）
│           ├── Scenes/            # 场景文件
│           ├── ScriptableObjects/ # 配置文件
│           └── Prefabs/           # 预制体
│
├── docs/                          # 设计文档
│   ├── gdd/                       # 游戏设计文档（GDD）
│   ├── ddd/                       # 详细设计说明书（DDD）
│   ├── execution-plan/            # 代码生成执行计划书
│   └── analysis/                  # 数值分析报告
```

## 文档索引

| 文档 | 路径 | 说明 |
|------|------|------|
| 游戏设计文档 | `docs/gdd/lost-island-gdd.html` | 完整GDD，13个章节 |
| 详细设计说明书 | `docs/ddd/lost-island-ddd.html` | 技术DDD，12个章节 |
| 代码生成执行计划书 | `docs/execution-plan/code-execution-plan.html` | 10阶段52步执行计划 |
| 0氪玩家数值分析 | `docs/analysis/zero-spend-analysis.html` | 30天数值模拟报告 |

## 代码进度

- **P1 阶段** ✅ 已完成 — 项目骨架与基础设施（11个C#文件）
- **P2 阶段** ✅ 已完成 — 数据层与属性系统（12个C#文件）
- **P3 阶段** ✅ 已完成 — 战斗核心系统（12个C#文件）
- **P4 阶段** ✅ 已完成 — 波次与丧尸系统（10个C#文件）
- **P5 阶段** ✅ 已完成 — 塔内建造与经营系统（12个C#文件）
- **P6 阶段** ✅ 已完成 — 技能卡系统（6个C#文件）
- **P7 阶段** ⏳ 待开始 — 符文系统
- ...（后续阶段按执行计划书推进）

## P2 阶段文件清单

### 数据层（Data/Config）
| 文件 | 说明 |
|------|------|
| `BaseConfigSO.cs` | ScriptableObject 配置基类（id/name/description/icon） |
| `ConfigDatabase.cs` | 配置数据库，统一管理所有配置表，支持按ID查找 |
| `ConfigLoader.cs` | 配置加载器，启动时从 Resources 加载所有配置 |

### 实体组件系统（Entity）
| 文件 | 说明 |
|------|------|
| `AttrEnums.cs` | 属性类型枚举（10种）+ 加成层级枚举（15层） |
| `AttributeComponent.cs` | 核心属性计算组件，脏标记+懒计算，多层叠加 |
| `HealthComponent.cs` | 生命组件，受伤/治疗/死亡/无敌帧 |
| `EntityComponent.cs` | 组件基类，所有组件的父类 |
| `EntityBase.cs` | 实体基类，Entity-Component 模式核心 |
| `MoveComponent.cs` | 移动组件，位置/方向/速度管理 |
| `AttackComponent.cs` | 攻击组件，冷却/范围/伤害计算 |

### 测试
| 文件 | 说明 |
|------|------|
| `AttributeSystemTests.cs` | 属性系统单元测试（20+测试用例） |

### P2 核心特性
- ✅ 15层加成叠加（乘法关系，同层内多源独立相乘）
- ✅ 脏标记懒计算模式，性能优化
- ✅ 属性上限裁剪（暴击率75%、攻速3.0、伤害减免90%）
- ✅ 属性变化事件通知
- ✅ Entity-Component 组件系统
- ✅ 完整单元测试覆盖

## P3 阶段文件清单

### 战斗逻辑（Logic/Battle）
| 文件 | 说明 |
|------|------|
| `BattleManager.cs` | 战斗管理器，状态机、波次计时、胜负判定、腐肉管理 |
| `DamageCalculator.cs` | 9步伤害计算流水线，纯函数实现，便于测试 |
| `PlayerController.cs` | 玩家控制器，移动/攻击/狂暴模式/受击反馈/复活 |
| `TowerController.cs` | 灯塔控制器，HP管理/破损等级/修复/天数成长 |
| `FleshUpgradeSystem.cs` | 腐肉升级系统，6种升级，消耗递增，战斗内临时加成 |

### 战斗UI（View/UI）
| 文件 | 说明 |
|------|------|
| `BattleHUD.cs` | 战斗HUD，玩家血条/狂暴条/灯塔血条/波次信息/技能槽 |
| `SkillSlot.cs` | 技能槽位UI，冷却显示/等级/解锁状态 |
| `DamagePopUpManager.cs` | 伤害飘字管理器，对象池/暴击/治疗/真实伤害 |
| `FleshUpgradePanel.cs` | 腐肉升级面板，等级/消耗/效果显示 |

### 测试
| 文件 | 说明 |
|------|------|
| `BattleSystemTests.cs` | 伤害计算单元测试（10+测试用例） |
| `BattleIntegrationTests.cs` | 战斗集成测试（6个场景，玩家vs丧尸/狂暴/腐肉/胜负） |

### P3 核心特性
- ✅ 9步伤害计算流水线（基础伤害→防御减伤→暴击→伤害减免→最终伤害→吸血→反伤）
- ✅ 战斗状态机（Prepare/PreNight/WaveStart/WaveActive/Victory/Defeat）
- ✅ 玩家控制器（移动/自动攻击/狂暴模式/受击无敌帧/自动复活）
- ✅ 灯塔系统（天数成长/破损等级/被摧毁=失败）
- ✅ 腐肉升级系统（6种升级，消耗递增20%，Pct_Flesh层生效）
- ✅ 战斗HUD（血条/狂暴条/波次/技能槽/伤害飘字）
- ✅ 完整单元测试 + 集成测试覆盖

## P4 阶段文件清单

### 波次系统（Logic/Wave）
| 文件 | 说明 |
|------|------|
| `WaveConfigSO.cs` | 波次配置ScriptableObject，间隔/成长/出怪表 |
| `WaveManager.cs` | 波次管理器，间隔计算/属性成长/BOSS判定 |
| `ZombieConfigSO.cs` | 丧尸配置ScriptableObject，16种丧尸属性 |
| `ZombieConfigFactory.cs` | 丧尸配置工厂，默认配置 + 配置数据库 |
| `ZombieController.cs` | 丧尸控制器，AI状态机/目标/护盾/死亡回收 |
| `ZombieStates.cs` | 丧尸AI状态：Spawn/Chase/Attack/Stun/Dead |
| `BossController.cs` | BOSS控制器，阶段机制/技能系统/狂暴 |
| `BossSkills.cs` | BOSS技能基类 + 冲锋/召唤/毒雾/护盾技能 |
| `ZombieSpawner.cs` | 丧尸生成器，分层对象池/出生点/波次生成 |

### 测试
| 文件 | 说明 |
|------|------|
| `WaveSystemTests.cs` | 波次系统单元测试（20+测试用例，含300波抽样） |

### P4 核心特性
- ✅ 300波波次系统，上凸曲线间隔（30s→15s，前期慢后期快）
- ✅ 属性双重成长：波次成长（HP×1.02/ATK×1.015）+ 天数成长（HP×1.10）
- ✅ 16种丧尸：4种普通 + 6种精英 + 6个BOSS
- ✅ 丧尸AI状态机：Spawn→Chase→Attack→Dead，轻量级实现
- ✅ BOSS阶段机制：4阶段（100%/70%/40%/20% HP），技能解锁，狂暴模式
- ✅ BOSS技能系统：冲锋/召唤/毒雾/护盾，可扩展
- ✅ 分层对象池：每种丧尸独立池，战斗中零GC
- ✅ 8个出生点均匀分布，每波随机选择3-5个
- ✅ 每20波BOSS，第100/200/300波里程碑BOSS
- ✅ 完整单元测试覆盖

## P5 阶段文件清单

### 塔内系统（Logic/Tower）
| 文件 | 说明 |
|------|------|
| `BuildingTypes.cs` | 建筑类型枚举、分类、索敌策略、攻击方式 |
| `TowerFloorData.cs` | 楼层数据 + 房间槽位数据结构 |
| `TowerManager.cs` | 塔内管理器，7层楼管理、建造/升级/拆除 |
| `PowerSystem.cs` | 电力系统，产出/消耗/建造检查/明细 |
| `BuildingConfigSO.cs` | 建筑配置ScriptableObject，13种建筑数值 |
| `BuildingConfigFactory.cs` | 建筑配置工厂，默认配置数据库 |
| `BuildingBase.cs` | 建筑基类，生命周期、加成管理 |
| `BuildingFactory.cs` | 建筑工厂，按类型创建实例 |
| `DefenseTowerBase.cs` | 防御塔基类，索敌+攻击系统 |
| `DefenseTowers.cs` | 4种防御塔实现（机枪/炮塔/箭塔/电击） |
| `BuildingImplementations.cs` | 资源类+功能类建筑实现（9种） |
| `BuildingBonusManager.cs` | 建筑加成管理器，汇总+事件通知 |

### 测试
| 文件 | 说明 |
|------|------|
| `TowerSystemTests.cs` | 塔内系统单元测试（20+测试用例） |

### P5 核心特性
- ✅ 7层灯塔结构（B1F~5F+塔顶），共19个房间槽位
- ✅ 楼层按天数解锁（1/2/4/6/8天渐进解锁）
- ✅ 房间槽位消耗废料解锁，首个免费
- ✅ 电力系统：基础20电力 + 发电机产出，建造前电力检查
- ✅ 13种建筑：4防御 + 3资源 + 6功能
- ✅ 防御塔战斗：4种攻击方式（单体/AOE/穿透/链式）
- ✅ 5种索敌策略（最近/最远/最低HP/最高HP/最前）
- ✅ 建筑等级系统（最高5级），升级消耗递增20-30%
- ✅ 建筑属性加成通过 Pct_Tower 层生效
- ✅ 事件驱动架构，建筑/电力/加成变化全部事件化
- ✅ 完整单元测试覆盖

## P6 阶段文件清单

### 技能卡系统（Logic/Skill）
| 文件 | 说明 |
|------|------|
| `SkillEnums.cs` | 技能类型/稀有度/效果类型/目标类型枚举 |
| `SkillCardData.cs` | 技能卡配置ScriptableObject，数值与升级成长 |
| `SkillEffects.cs` | 9种效果组件（伤害/AOE/燃烧/冰冻/眩晕/减速/击退/治疗/护盾） |
| `SkillCardSlot.cs` | 技能卡槽，装配/等级/冷却管理 |
| `SkillCardManager.cs` | 技能卡管理器，4主动+3被动槽，释放与冷却 |
| `SkillCardFactory.cs` | 技能卡配置工厂，24张卡（14主动+8被动） |

### 测试
| 文件 | 说明 |
|------|------|
| `SkillSystemTests.cs` | 技能卡系统单元测试（20+测试用例） |

### P6 核心特性
- ✅ 24张技能卡：14张主动 + 8张被动 + 2张特殊
- ✅ 4种稀有度：白/蓝/紫/金
- ✅ 9种效果组件，可组合实现丰富技能
- ✅ 效果组件模式：每个技能可挂载多个效果
- ✅ 4个主动技能槽 + 3个被动技能槽
- ✅ 技能等级成长：伤害+15%/级，冷却-5%/级，最高5级
- ✅ 被动技能属性加成通过 Pct_SkillCard 层生效
- ✅ 事件驱动：技能释放/冷却变化/装配变化事件
- ✅ 完整单元测试覆盖

## 开发环境

- 引擎：Unity 2022.3 LTS 或更高 / Cocos Creator
- 目标平台：iOS / Android / 微信小游戏
- 语言：C#
- 架构：MVC + 事件驱动 + Entity-Component

---

*迷失孤岛 · Lost Island — 丧尸塔防生存游戏*
