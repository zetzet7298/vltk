// -----------------------------------------------------------------------------
// VLTK.Backend.Mock — MockGameBackend
// Implementation mock (offline) của IGameBackend. KHÔNG import
// UnityEngine.Networking, KHÔNG gọi network. Mọi call trả về canned response
// với IsSuccess=true và data hợp lệ. Dùng cho:
//   - Runtime offline (SandboxManager không cần thay đổi khi useMock=true)
//   - EditMode test không cần server thật
//
// Slice FS-03C (combat — server-authoritative):
//   - POST /v1/combat/damage/calc  (mock dùng simple subtract + flat 50% damage)
//   - POST /v1/combat/status/tick  (mock decrement poison/freeze state time)
//   - POST /v1/combat/pk/check     (mock City = safe, Battlefield = OK)
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
    }
}
