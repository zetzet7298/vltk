// -----------------------------------------------------------------------------
// VLTK.Backend.Mock — MockGameBackend
// Implementation mock (offline) của IGameBackend. KHÔNG import
// UnityEngine.Networking, KHÔNG gọi network. Mọi call trả về canned response
// với IsSuccess=true và data hợp lệ. Dùng cho:
//   - Runtime offline (SandboxManager không cần thay đổi khi useMock=true)
//   - EditMode test không cần server thật
//
// Mock auth flow: trả về LoginResponse với accName echo, một role seeded
// (`Vo_Si_Test`), và PlayerStateResponse với stat Kim mặc định (35/25/25/15).
// Mock map flow: EnterMap/GetMapPosition trả về SceneResponse với mapId=1
// (Phượng Tường), posX/posY=1500/1500. ListItems trả về 2 vật phẩm cứng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VLTK.Backend.Dto;

namespace VLTK.Backend.Mock
{
    /// <summary>
    /// Mock backend với dữ liệu cứng cho smoke test. Mọi Task trả về trạng thái
    /// thành công, code="200", message="Mock" để phân biệt với response thật.
    /// </summary>
    public sealed class MockGameBackend : IGameBackend
    {
        public BackendConfig Config { get; }
        public bool IsConfigured => Config != null;

        public MockGameBackend(BackendConfig config)
        {
            Config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        // ---- FS-01D ----

        public Task<BackendResponse<HealthResponse>> GetHealthAsync(CancellationToken ct = default)
        {
            var data = new HealthResponse
            {
                status = "ok",
                service = "mock",
                version = "0.0.0-mock",
                timestamp = DateTime.UtcNow.ToString("o"),
            };
            return Task.FromResult(new BackendResponse<HealthResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<MapListResponse>> ListMapsAsync(
            string mapType = null, CancellationToken ct = default)
        {
            var maps = new List<MapResponse>
            {
                new MapResponse
                {
                    mapId = 1,
                    name = "Phượng Tường Cổ Thành",
                    mapType = "city",
                    mapTypeName = "Thành phố",
                    posX = 1500,
                    posY = 1500,
                    newWorldScript = "NewWorld",
                    newWorldParam = "1 1500 1500",
                },
                new MapResponse
                {
                    mapId = 2,
                    name = "Tân Thủ Thôn",
                    mapType = "village",
                    mapTypeName = "Làng",
                    posX = 1800,
                    posY = 1200,
                    newWorldScript = "NewWorld",
                    newWorldParam = "2 1800 1200",
                },
            };
            if (!string.IsNullOrEmpty(mapType))
            {
                maps.RemoveAll(m => !string.Equals(m.mapType, mapType, StringComparison.OrdinalIgnoreCase));
            }
            var data = new MapListResponse
            {
                total = maps.Count,
                maps = maps,
            };
            return Task.FromResult(new BackendResponse<MapListResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        // ---- FS-02B ----

        public Task<BackendResponse<AccountResponse>> CreateAccountAsync(
            AccountCreateRequest request, CancellationToken ct = default)
        {
            if (request == null || string.IsNullOrEmpty(request.accName) ||
                string.IsNullOrEmpty(request.password))
            {
                return Task.FromResult(BackendResponse<AccountResponse>.Failure(
                    "validation_error", "account request không hợp lệ"));
            }
            return Task.FromResult(new BackendResponse<AccountResponse>
            {
                code = "200",
                message = "Mock",
                data = new AccountResponse
                {
                    id = 1,
                    accName = request.accName,
                    serviceFlag = request.serviceFlag,
                },
            });
        }

        public Task<BackendResponse<LoginResponse>> LoginAsync(
            string accName,
            string password,
            string otp = null,
            string clientIp = null,
            CancellationToken ct = default)
        {
            // Mock: trả về LoginResponse với accName echo, không validate
            // password (mock chỉ cần trả về đúng shape cho caller sử dụng).
            if (string.IsNullOrEmpty(accName))
            {
                return Task.FromResult(BackendResponse<LoginResponse>.Failure(
                    "validation_error", "accName không được rỗng"));
            }
            if (string.IsNullOrEmpty(password))
            {
                return Task.FromResult(BackendResponse<LoginResponse>.Failure(
                    "validation_error", "password không được rỗng"));
            }
            var data = new LoginResponse
            {
                accName = accName,
                serviceFlag = 0,
                extPoint = 0,
            };
            return Task.FromResult(new BackendResponse<LoginResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<RoleListResponse>> ListRolesAsync(
            string accName, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(accName))
            {
                return Task.FromResult(BackendResponse<RoleListResponse>.Failure(
                    "validation_error", "accName không được rỗng"));
            }
            // Mock: trả về 1 role seeded (id=1, Kim) cho account bất kỳ.
            // Trong thực tế, account chưa tạo role sẽ trả về roles=[]; mock giữ
            // 1 role để caller test thấy đủ luồng List → GetPlayer.
            var role = new RoleResponse
            {
                id = 1,
                roleName = "Vo_Si_Mock",
                account = accName,
                faction = 0,
                factionName = "Thiếu Lâm",
                level = 1,
            };
            var data = new RoleListResponse
            {
                account = accName,
                roles = new List<RoleResponse> { role },
            };
            return Task.FromResult(new BackendResponse<RoleListResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<RoleResponse>> CreateRoleAsync(
            RoleCreateRequest request, CancellationToken ct = default)
        {
            if (request == null || string.IsNullOrEmpty(request.account) ||
                string.IsNullOrEmpty(request.roleName))
            {
                return Task.FromResult(BackendResponse<RoleResponse>.Failure(
                    "validation_error", "role request không hợp lệ"));
            }
            return Task.FromResult(new BackendResponse<RoleResponse>
            {
                code = "200",
                message = "Mock",
                data = new RoleResponse
                {
                    id = 1,
                    roleName = request.roleName,
                    account = request.account,
                    faction = request.faction,
                    factionName = request.faction >= 0 ? "Mock faction" : null,
                    level = 1,
                },
            });
        }

        public Task<BackendResponse<PlayerStateResponse>> GetPlayerStateAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerStateResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            // Stat Kim mặc định (35/25/25/15) — parity với backend task_head.lua:79-82.
            var data = new PlayerStateResponse
            {
                id = 1,
                roleId = roleId,
                level = 1,
                exp = 0,
                transLife = 0,
                freePoint = 0,
                magicPoint = 0,
                strength = 35,
                dexterity = 25,
                vitality = 25,
                spirit = 15,
                series = 0,
                money = 0,
                repute = 0,
            };
            return Task.FromResult(new BackendResponse<PlayerStateResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        // ---- FS-02C ----

        public Task<BackendResponse<SceneResponse>> EnterMapAsync(
            EnterMapRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", "EnterMapRequest is null"));
            }
            // Mock: trả về SceneResponse với id giả lập (hash từ roleId+mapId để
            // ổn định giữa các call). posX/posY echo từ request để caller thấy
            // dữ liệu đã được server "xác nhận".
            int fakeId = unchecked(request.roleId * 1000 + request.mapId);
            var data = new SceneResponse
            {
                id = fakeId,
                roleId = request.roleId,
                mapId = request.mapId,
                posX = request.posX,
                posY = request.posY,
            };
            return Task.FromResult(new BackendResponse<SceneResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<SceneResponse>> GetMapPositionAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", $"roleId phải > 0; got {roleId}"));
            }
            // Mock: vị trí mặc định thành Phượng Tường Cổ Thành (mapId=1).
            var data = new SceneResponse
            {
                id = roleId, // scene id tạm bằng roleId cho đơn giản
                roleId = roleId,
                mapId = 1,
                posX = 1500,
                posY = 1500,
            };
            return Task.FromResult(new BackendResponse<SceneResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<ItemListResponse>> ListItemsAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<ItemListResponse>.Failure(
                    "invalid_arg", $"roleId phải > 0; got {roleId}"));
            }
            // Mock: 2 vật phẩm cứng trong túi — 1 hồi máu (genre=2, detail=1)
            // và 1 vũ khí chưa trang bị (genre=0).
            var items = new List<ItemResponse>
            {
                new ItemResponse
                {
                    id = roleId * 100 + 1,
                    roleId = roleId,
                    genre = 2,
                    detail = 1,
                    particular = 1,
                    level = 1,
                    amount = 5,
                    slot = 0,
                    equipSlot = -1,
                    name = "Hồi Huyết Đan (nhỏ)",
                },
                new ItemResponse
                {
                    id = roleId * 100 + 2,
                    roleId = roleId,
                    genre = 0,
                    detail = 1,
                    particular = 12,
                    level = 10,
                    amount = 1,
                    slot = 1,
                    equipSlot = -1,
                    name = "Kiếm Phổ Thông",
                },
            };
            var data = new ItemListResponse
            {
                roleId = roleId,
                items = items,
            };
            return Task.FromResult(new BackendResponse<ItemListResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        // ---- FS-04B ----

        public Task<BackendResponse<SceneResponse>> MoveAsync(
            MoveRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", "MoveRequest is null"));
            }
            if (request.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", $"roleId phải > 0; got {request.roleId}"));
            }
            if (request.posX < 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", $"posX phải >= 0; got {request.posX}"));
            }
            if (request.posY < 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", $"posY phải >= 0; got {request.posY}"));
            }
            // Mock: trả về SceneResponse với posX/posY echo từ request để
            // caller thấy dữ liệu đã được "server" xác nhận. mapId giữ
            // mặc định 1 (Phượng Tường, parity với GetMapPosition mock) vì
            // mock không truy được scene thật của role; rest mới parity với
            // server thật (giữ mapId cũ).
            var data = new SceneResponse
            {
                id = request.roleId, // scene id tạm bằng roleId cho đơn giản
                roleId = request.roleId,
                mapId = 1,
                posX = request.posX,
                posY = request.posY,
            };
            return Task.FromResult(new BackendResponse<SceneResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }


// ---- FS-03B (skill read + cast — mock) ----

        public Task<BackendResponse<PlayerSkillListResponse>> ListSkillsAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillListResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            // Mock: trả 1 skill seeded (id=1, Kim Ba, parity với FS-03A
            // evidence 06_skill_by_role.json) để caller test đủ luồng
            // List → Cast.
            var skill = new PlayerSkillResponse
            {
                id = 1,
                roleId = roleId,
                skillId = 22,
                level = 1,
                isActive = true,
                skillName = "Kim Ba",
                maxLevel = 20,
            };
            var data = new PlayerSkillListResponse
            {
                roleId = roleId,
                skills = new List<PlayerSkillResponse> { skill },
            };
            return Task.FromResult(new BackendResponse<PlayerSkillListResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<PlayerSkillResponse>> LearnSkillAsync(
            SkillLearnRequest req, CancellationToken ct = default)
        {
            if (req == null)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "req không được null"));
            }
            if (req.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (req.skillId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "skillId phải > 0"));
            }
            if (req.charLevel <= 0 || req.charLevel > 200)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "charLevel phải trong khoảng 1..200"));
            }
            if (req.faction < -1 || req.faction > 9)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "faction phải trong khoảng -1..9 (0..9 = môn phái, -1 = chưa nhập)"));
            }
            // Mock: trả về PlayerSkillResponse với level=1, parity với FS-03A
            // evidence 05_skill_learn.json.
            var data = new PlayerSkillResponse
            {
                id = 1,
                roleId = req.roleId,
                skillId = req.skillId,
                level = 1,
                isActive = true,
                skillName = "Skill " + req.skillId,
                maxLevel = 20,
            };
            return Task.FromResult(new BackendResponse<PlayerSkillResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<PlayerSkillResponse>> LevelUpSkillAsync(
            int roleId, int skillId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (skillId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "skillId phải > 0"));
            }
            // Mock: trả về PlayerSkillResponse với level=2, parity với FS-03A
            // evidence 07_skill_level_up.json.
            var data = new PlayerSkillResponse
            {
                id = 1,
                roleId = roleId,
                skillId = skillId,
                level = 2,
                isActive = true,
                skillName = "Skill " + skillId,
                maxLevel = 20,
            };
            return Task.FromResult(new BackendResponse<PlayerSkillResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<SkillCastCheckResponse>> CastSkillCheckAsync(
            SkillCastCheckRequest req, CancellationToken ct = default)
        {
            if (req == null)
            {
                return Task.FromResult(BackendResponse<SkillCastCheckResponse>.Failure(
                    "validation_error", "req không được null"));
            }
            if (req.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SkillCastCheckResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (req.skillId <= 0)
            {
                return Task.FromResult(BackendResponse<SkillCastCheckResponse>.Failure(
                    "validation_error", "skillId phải > 0"));
            }
            if (req.nowMs < 1)
            {
                return Task.FromResult(BackendResponse<SkillCastCheckResponse>.Failure(
                    "validation_error", "nowMs phải >= 1"));
            }
            // Mock: skill 210 → costType=1 (mana), costValue=50, delay=0;
            // parity với FS-03A evidence 09_skill_cast_check.json.
            int costType = 1, costValue = 50, delayPerCast = 0;
            if (req.skillId == 22)
            {
                costType = 0; // Kim Ba: costType=0, costValue=0
                costValue = 0;
            }
            var data = new SkillCastCheckResponse
            {
                skillId = req.skillId,
                canCast = true,
                reason = null,
                costType = costType,
                costValue = costValue,
                delayPerCast = delayPerCast,
                nextCastTime = 0, // stateless check: input.lastCastMs nếu có
            };
            return Task.FromResult(new BackendResponse<SkillCastCheckResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<SkillCastResponse>> CastSkillAsync(
            SkillCastRequest req, CancellationToken ct = default)
        {
            if (req == null)
            {
                return Task.FromResult(BackendResponse<SkillCastResponse>.Failure(
                    "validation_error", "req không được null"));
            }
            if (req.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SkillCastResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (req.skillId <= 0)
            {
                return Task.FromResult(BackendResponse<SkillCastResponse>.Failure(
                    "validation_error", "skillId phải > 0"));
            }
            if (req.nowMs < 1)
            {
                return Task.FromResult(BackendResponse<SkillCastResponse>.Failure(
                    "validation_error", "nowMs phải >= 1"));
            }
            // Mock: trả cast=true, costType=1 mana=50, currentLife=1000,
            // currentMana=250 (300-50 server-authoritative), parity với
            // FS-03A evidence 10_skill_cast.json.
            var data = new SkillCastResponse
            {
                skillId = req.skillId,
                cast = true,
                costType = 1,
                costPaid = 50,
                currentLife = 1000,
                currentMana = 250,
                currentStamina = 0,
                nextCastTime = req.nowMs,
                effects = new List<SkillCastEffect>(),
            };
            return Task.FromResult(new BackendResponse<SkillCastResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

// ----------------------------------------------------------------
        // FS-03C — Combat (server-authoritative mock)
        // ----------------------------------------------------------------

        public Task<BackendResponse<DamageCalcResponse>> CalcDamageAsync(
            DamageCalcRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<DamageCalcResponse>.Failure(
                    "invalid_arg", "DamageCalcRequest is null"));
            }
            if (request.target == null)
            {
                return Task.FromResult(BackendResponse<DamageCalcResponse>.Failure(
                    "invalid_arg", "DamageCalcRequest.target is null"));
            }
            // Mock damage: dùng atkMax (đơn giản hóa) - armor (nếu có) - resist
            // % rồi cap ở 0. KHÔNG phải parity KNpc.cpp thật (chỉ Rest mới parity);
            // mock chỉ để test happy path + cấu trúc response.
            int raw = request.atkMax > 0 ? request.atkMax : request.atkMin;
            int armor;
            switch (request.damageKind)
            {
                case 1: armor = request.target.coldArmor; break;
                case 2: armor = request.target.fireArmor; break;
                case 3: armor = request.target.lightArmor; break;
                case 4: armor = request.target.poisonArmor; break;
                default: armor = request.target.physicsArmor; break;
            }
            int resist;
            switch (request.damageKind)
            {
                case 1: resist = request.target.coldResist; break;
                case 2: resist = request.target.fireResist; break;
                case 3: resist = request.target.lightResist; break;
                case 4: resist = request.target.poisonResist; break;
                default: resist = request.target.physicsResist; break;
            }
            int armorLeft = System.Math.Max(0, armor - raw);
            int damageAfterArmor = System.Math.Max(0, raw - armor);
            int damageAfterResist = damageAfterArmor * (100 - resist) / 100;
            // Mutate target: trừ armor theo kind, trừ life. KHÔNG tự trừ mana
            // shield (mock chỉ để test field round-trip).
            switch (request.damageKind)
            {
                case 0: request.target.physicsArmor = armorLeft; break;
                case 1: request.target.coldArmor = armorLeft; break;
                case 2: request.target.fireArmor = armorLeft; break;
                case 3: request.target.lightArmor = armorLeft; break;
                case 4: request.target.poisonArmor = armorLeft; break;
            }
            request.target.life = System.Math.Max(0, request.target.life - damageAfterResist);
            var data = new DamageCalcResponse
            {
                damage = damageAfterResist,
                manaAbsorbed = 0,
                armorAbsorbed = System.Math.Min(armor, raw),
                manaShieldBroke = false,
                targetDied = request.target.life <= 0,
                reflectToAttacker = 0,
                reflectKind = 5, // magic default
                target = request.target,
            };
            return Task.FromResult(new BackendResponse<DamageCalcResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<StatusTickResponse>> StatusTickAsync(
            StatusTickRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<StatusTickResponse>.Failure(
                    "invalid_arg", "StatusTickRequest is null"));
            }
            if (request.target == null || request.status == null)
            {
                return Task.FromResult(BackendResponse<StatusTickResponse>.Failure(
                    "invalid_arg", "StatusTickRequest.target/status is null"));
            }
            // Mock: poison time-- + burn time--; controlled=true nếu freeze/stun
            // còn time>0. KHÔNG tính DoT thật — đó là việc của server (Rest mới
            // parity KNpc.cpp:783-816).
            var dotResults = new List<DotResult>();
            if (request.status.poisonState != null && request.status.poisonState.time > 0)
            {
                request.status.poisonState.time -= 1;
                // Mock DoT: value0 dmg (poisonSource nếu có)
                int dotDmg = request.status.poisonState.value0;
                request.target.life = System.Math.Max(0, request.target.life - dotDmg);
                dotResults.Add(new DotResult
                {
                    damage = dotDmg,
                    manaAbsorbed = 0,
                    armorAbsorbed = 0,
                    manaShieldBroke = false,
                    targetDied = request.target.life <= 0,
                    reflectToAttacker = 0,
                    reflectKind = 4, // poison
                });
            }
            if (request.status.freezeState != null && request.status.freezeState.time > 0)
            {
                request.status.freezeState.time -= 1;
            }
            if (request.status.burnState != null && request.status.burnState.time > 0)
            {
                request.status.burnState.time -= 1;
            }
            if (request.status.confuseState != null && request.status.confuseState.time > 0)
            {
                request.status.confuseState.time -= 1;
            }
            if (request.status.stunState != null && request.status.stunState.time > 0)
            {
                request.status.stunState.time -= 1;
            }
            if (request.status.lifeState != null && request.status.lifeState.time > 0)
            {
                request.status.lifeState.time -= 1;
            }
            if (request.status.manaState != null && request.status.manaState.time > 0)
            {
                request.status.manaState.time -= 1;
            }
            if (request.status.drunkState != null && request.status.drunkState.time > 0)
            {
                request.status.drunkState.time -= 1;
            }
            bool controlled = (request.status.freezeState != null && request.status.freezeState.time > 0)
                || (request.status.stunState != null && request.status.stunState.time > 0);
            bool confuseEnded = request.status.confuseState != null
                && request.status.confuseState.time == 0;
            var data = new StatusTickResponse
            {
                controlled = controlled,
                confuseEnded = confuseEnded,
                dotResults = dotResults,
                auraCastSkillId = request.activeAuraId,
                auraCastLevel = request.activeAuraLevel,
                target = request.target,
                status = request.status,
            };
            return Task.FromResult(new BackendResponse<StatusTickResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<PkCheckResponse>> CheckPkAsync(
            PkCheckRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<PkCheckResponse>.Failure(
                    "invalid_arg", "PkCheckRequest is null"));
            }
            if (string.IsNullOrEmpty(request.mapType))
            {
                return Task.FromResult(BackendResponse<PkCheckResponse>.Failure(
                    "invalid_arg", "PkCheckRequest.mapType is null/empty"));
            }
            // Mock policy: City/Capital = safe zone, Battlefield = OK, Field/other
            // = cảnh báo nhưng vẫn cho đánh. KHÔNG parity với server (chỉ Rest
            // mới parity combat_resolve.pk_allowed_check).
            string mt = request.mapType;
            bool isCity = mt == "City" || mt == "Capital";
            bool isBattlefield = mt == "Battlefield";
            bool isSafeZone = isCity;
            bool mapPkAllowed = !isCity;
            bool canAttack = mapPkAllowed && !isSafeZone && (request.attackerCamp != request.targetCamp);
            string reason = null;
            if (isSafeZone)
            {
                reason = "Vùng an toàn — cấm PK";
            }
            else if (request.attackerCamp == request.targetCamp)
            {
                canAttack = false;
                reason = "Cùng phe — không thể PK";
            }
            var data = new PkCheckResponse
            {
                canAttack = canAttack,
                mapPkAllowed = mapPkAllowed,
                isSafeZone = isSafeZone,
                reason = reason,
            };
            return Task.FromResult(new BackendResponse<PkCheckResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        // ----------------------------------------------------------------
        // FS-04C — Movement (mock parity)
        // ----------------------------------------------------------------

        public Task<BackendResponse<SceneResponse>> UpdatePositionAsync(
            UpdatePositionRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", "UpdatePositionRequest is null"));
            }
            if (request.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (request.posX < 0 || request.posY < 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "validation_error",
                    $"posX/posY phải >= 0; got ({request.posX},{request.posY})"));
            }
            // Mock: echo posX/posY từ request, mapId giả lập = 1 (Phượng Tường
            // — default city để MovementSyncService.EditMode test có mapId cố
            // định để assert). Parity với KNpc::SetPos (chỉ set nX, nY) — mock
            // KHÔNG parity validation ngoài grid bounds (Rest mới parity).
            var data = new SceneResponse
            {
                id = unchecked(request.roleId * 1000 + 1),
                roleId = request.roleId,
                mapId = 1,
                posX = request.posX,
                posY = request.posY,
            };
            return Task.FromResult(new BackendResponse<SceneResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }
    }
}
