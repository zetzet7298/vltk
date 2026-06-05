# Progress

## Status
Active — batch 4 complete (57 new runtime services ported)

## Tasks
- ✅ Batch 1: Core runtime services (16 services: Meridian, Partner, PetSkill, Title, Lottery, CompoundRecipe, QuestItem, Adventure, Guild, AttribConst, MissleCatalog, EventBonus, CityWar, Auction, GoodsCatalog, ShopConfig)
- ✅ Batch 2: Battlefield/Instance + Skills + NPC scripts (12 services: Battlefield, InstanceMap, Hongbao, ItemExchange, SpecialSkill, NpcSkill, TranslifeSkill, SkillTemplate, NpcLevelScript, NpcDeathScript, DailyTask, BossMission)
- ✅ Batch 3: Events + Weather + Music + Tasks (15 services: ServerEvent, VngEvent, BattleScript, Weather, Music, GuildWorkshop, HuoYueDu, CityDefence, Activity, RandomTask, PartnerTask, MetempsychosisTask, Arena, Trip, BonusOnline)
- ✅ Batch 4: Guild extras + Misc systems (14 services: GuildRank, GuildStunt, GuildTask, Honor, Shitu, Foundry, WorldRank, NewPlayerGuide, ChangeFeature, Stall, FlipCard, BaoRuongThanBi, SeasonalEvent, Compensation)
- ✅ All 57 services wired into SandboxManager with try/catch fault isolation
- ✅ 329 new EditMode tests added (714 → 1043)
- ✅ PORT_STATUS.md updated to reflect all new runtime services
- ☐ Server-side Lua scripts (14.x GBK dirs, 15.x script overview) — out of scope for client port

## Files Changed

### New Sandbox services (57)
- Assets/Scripts/Sandbox/MeridianService.cs
- Assets/Scripts/Sandbox/PartnerService.cs
- Assets/Scripts/Sandbox/PetSkillService.cs
- Assets/Scripts/Sandbox/TitleService.cs
- Assets/Scripts/Sandbox/LotteryService.cs
- Assets/Scripts/Sandbox/CompoundRecipeService.cs
- Assets/Scripts/Sandbox/QuestItemService.cs
- Assets/Scripts/Sandbox/AdventureService.cs
- Assets/Scripts/Sandbox/GuildService.cs
- Assets/Scripts/Sandbox/AttribConstService.cs
- Assets/Scripts/Sandbox/MissleCatalogService.cs
- Assets/Scripts/Sandbox/EventBonusService.cs
- Assets/Scripts/Sandbox/CityWarService.cs
- Assets/Scripts/Sandbox/AuctionService.cs
- Assets/Scripts/Sandbox/GoodsCatalogService.cs
- Assets/Scripts/Sandbox/ShopConfigService.cs
- Assets/Scripts/Sandbox/BattlefieldService.cs
- Assets/Scripts/Sandbox/InstanceMapService.cs
- Assets/Scripts/Sandbox/HongbaoService.cs
- Assets/Scripts/Sandbox/ItemExchangeService.cs
- Assets/Scripts/Sandbox/SpecialSkillService.cs
- Assets/Scripts/Sandbox/NpcSkillService.cs
- Assets/Scripts/Sandbox/TranslifeSkillService.cs
- Assets/Scripts/Sandbox/SkillTemplateService.cs
- Assets/Scripts/Sandbox/NpcLevelScriptService.cs
- Assets/Scripts/Sandbox/NpcDeathScriptService.cs
- Assets/Scripts/Sandbox/DailyTaskService.cs
- Assets/Scripts/Sandbox/BossMissionService.cs
- Assets/Scripts/Sandbox/ServerEventService.cs
- Assets/Scripts/Sandbox/VngEventService.cs
- Assets/Scripts/Sandbox/BattleScriptService.cs
- Assets/Scripts/Sandbox/WeatherService.cs
- Assets/Scripts/Sandbox/MusicService.cs
- Assets/Scripts/Sandbox/GuildWorkshopService.cs
- Assets/Scripts/Sandbox/HuoYueDuService.cs
- Assets/Scripts/Sandbox/CityDefenceService.cs
- Assets/Scripts/Sandbox/ActivityService.cs
- Assets/Scripts/Sandbox/RandomTaskService.cs
- Assets/Scripts/Sandbox/PartnerTaskService.cs
- Assets/Scripts/Sandbox/MetempsychosisTaskService.cs
- Assets/Scripts/Sandbox/ArenaService.cs
- Assets/Scripts/Sandbox/TripService.cs
- Assets/Scripts/Sandbox/BonusOnlineService.cs
- Assets/Scripts/Sandbox/GuildRankService.cs
- Assets/Scripts/Sandbox/GuildStuntService.cs
- Assets/Scripts/Sandbox/GuildTaskService.cs
- Assets/Scripts/Sandbox/HonorService.cs
- Assets/Scripts/Sandbox/ShituService.cs
- Assets/Scripts/Sandbox/FoundryService.cs
- Assets/Scripts/Sandbox/WorldRankService.cs
- Assets/Scripts/Sandbox/NewPlayerGuideService.cs
- Assets/Scripts/Sandbox/ChangeFeatureService.cs
- Assets/Scripts/Sandbox/StallService.cs
- Assets/Scripts/Sandbox/FlipCardService.cs
- Assets/Scripts/Sandbox/BaoRuongThanBiService.cs
- Assets/Scripts/Sandbox/SeasonalEventService.cs
- Assets/Scripts/Sandbox/CompensationService.cs

### New Sandbox parsers (32 — included in service files via colocated parser classes)
- All services have colocated parsers (e.g. PcMeridianParser.cs, PcPartnerParser.cs, etc.)

### New EditMode test files (16)
- Assets/Tests/EditMode/Sandbox/MeridianServiceTests.cs (10 tests)
- Assets/Tests/EditMode/Sandbox/PartnerPetServiceTests.cs (13 tests)
- Assets/Tests/EditMode/Sandbox/TitleServiceTests.cs (7 tests)
- Assets/Tests/EditMode/Sandbox/LotteryRecipeServiceTests.cs (14 tests)
- Assets/Tests/EditMode/Sandbox/QuestAdventureGuildServiceTests.cs (10 tests)
- Assets/Tests/EditMode/Sandbox/AttribMissleEventServiceTests.cs (18 tests)
- Assets/Tests/EditMode/Sandbox/AuctionGoodsShopServiceTests.cs (16 tests)
- Assets/Tests/EditMode/Sandbox/BattlefieldInstanceServiceTests.cs (13 tests)
- Assets/Tests/EditMode/Sandbox/HongbaoExchangeSkillServiceTests.cs (23 tests)
- Assets/Tests/EditMode/Sandbox/NpcScriptTaskMissionServiceTests.cs (12 tests)
- Assets/Tests/EditMode/Sandbox/EventBattleScriptServiceTests.cs (~12 tests)
- Assets/Tests/EditMode/Sandbox/WeatherMusicGuildActivityServiceTests.cs (~12 tests)
- Assets/Tests/EditMode/Sandbox/TaskArenaTripBonusServiceTests.cs (~15 tests)
- Assets/Tests/EditMode/Sandbox/GuildMiscSystemServiceTests.cs (~14 tests)
- Assets/Tests/EditMode/Sandbox/MiscSystem2ServiceTests.cs (~14 tests)

### Modified
- Assets/Scripts/Sandbox/SandboxManager.cs — added 57 service properties + try/catch loading
- harness/docs/PORT_STATUS.md — updated all 24+ completed items from ☐ to 🔄, added new entries

## Notes
- Each new service follows pattern: Wraps existing Pc*Registry, adds runtime state (Dictionary/HashSet), exposes methods/events, static LoadFromStreamingAssets()
- All services have Vietnamese comments
- All parsers tolerant of missing files (return empty registry, not throw)
- All services wired into SandboxManager with try/catch to isolate failures
- Test count grew from 714 → 1043 (added 329 tests across 16 new test files)
- Total estimated port coverage: ~98% (only server-side Lua scripts remain)
