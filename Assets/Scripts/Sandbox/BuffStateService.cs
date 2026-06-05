// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.3 Buff State Service
// Buff/Debuff effect state catalog matching PC StateSpecialId.
// Source: PC skill states, mobile haptic feedback mapping.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [Serializable]
    public class BuffInstance
    {
        public int skillId;
        public int level;
        public float durationRemaining;
        public float tickInterval = 1.0f;
        public float tickTimer;
        public List<SkillMagicAttribute> attributes = new();
        public bool isHaptic; // Rung màn hình khi nhận/kết thúc buff này (mobile)
    }

    /// <summary>
    /// Service quản lý trạng thái hiệu ứng (Buff/Debuff) của nhân vật.
    /// PC source: StateSpecialId, KNpc::AddState / m_StateSpecial.
    /// </summary>
    public class BuffStateService
    {
        private readonly Dictionary<int, Dictionary<int, BuffInstance>> _actorBuffs = new(); // actorId -> skillId -> Buff

        /// <summary>Event kích hoạt khi có buff mới được thêm.</summary>
        public event Action<int, BuffInstance> OnBuffAdded; // (actorId, buff)

        /// <summary>Event kích hoạt khi buff kết thúc.</summary>
        public event Action<int, int> OnBuffRemoved; // (actorId, skillId)

        /// <summary>
        /// Áp dụng buff lên đối tượng.
        /// </summary>
        public void ApplyBuff(int actorId, SkillDefinition skill, int skillLevel, float durationSeconds)
        {
            if (skill == null || durationSeconds <= 0) return;

            if (!_actorBuffs.TryGetValue(actorId, out var buffs))
            {
                buffs = new Dictionary<int, BuffInstance>();
                _actorBuffs[actorId] = buffs;
            }

            // Mobile Haptic isolation: Chỉ rung thiết bị đối với các buff/debuff quan trọng (ví dụ: bị choáng, bốc cháy)
            bool isHapticEffect = skill.stateSpecialId == 22 || skill.skillId == 20; // 22 = Choáng, 20 = Sư Tử Hống

            if (buffs.TryGetValue(skill.skillId, out var existing))
            {
                // Làm mới thời gian buff
                existing.durationRemaining = Mathf.Max(existing.durationRemaining, durationSeconds);
                existing.level = skillLevel;
            }
            else
            {
                var levelData = skill.GetPcLevelData(skillLevel);
                var attrs = new List<SkillMagicAttribute>();
                if (levelData != null)
                {
                    attrs.AddRange(levelData.state);
                }

                var instance = new BuffInstance
                {
                    skillId = skill.skillId,
                    level = skillLevel,
                    durationRemaining = durationSeconds,
                    attributes = attrs,
                    isHaptic = isHapticEffect,
                    tickTimer = 0f
                };

                buffs[skill.skillId] = instance;

                if (isHapticEffect)
                {
                    TriggerHapticFeedback();
                }

                OnBuffAdded?.Invoke(actorId, instance);
                SubsystemLog.Info("BuffState", $"Applied buff {skill.DisplayName} on actor {actorId} (duration={durationSeconds}s)");
            }
        }

        /// <summary>
        /// Xóa bỏ một buff khỏi đối tượng.
        /// </summary>
        public void RemoveBuff(int actorId, int skillId)
        {
            if (_actorBuffs.TryGetValue(actorId, out var buffs) && buffs.TryGetValue(skillId, out var instance))
            {
                buffs.Remove(skillId);
                OnBuffRemoved?.Invoke(actorId, skillId);
                SubsystemLog.Info("BuffState", $"Removed buff {skillId} from actor {actorId}");
            }
        }

        /// <summary>
        /// Cập nhật thời gian giảm dần của buff mỗi frame.
        /// </summary>
        public void Tick(float deltaTime)
        {
            foreach (var kvp in _actorBuffs)
            {
                int actorId = kvp.Key;
                var buffs = kvp.Value;
                var expired = new List<int>();

                // Thu thập các buff đã hết thời gian
                foreach (var bKvp in buffs)
                {
                    var instance = bKvp.Value;
                    instance.durationRemaining -= deltaTime;

                    if (instance.durationRemaining <= 0f)
                    {
                        expired.Add(bKvp.Key);
                    }
                }

                // Xóa buff đã hết hạn
                foreach (var skillId in expired)
                {
                    RemoveBuff(actorId, skillId);
                }
            }
        }

        /// <summary>
        /// Cộng dồn chỉ số tăng thêm của buff cho thuộc tính nhân vật.
        /// </summary>
        public int GetBuffModifier(int actorId, MagicAttributeKind kind)
        {
            if (!_actorBuffs.TryGetValue(actorId, out var buffs))
                return 0;

            int sum = 0;
            foreach (var instance in buffs.Values)
            {
                foreach (var attr in instance.attributes)
                {
                    if (attr.kind == kind)
                    {
                        sum += attr.value1;
                    }
                }
            }
            return sum;
        }

        /// <summary>Kiểm tra đối tượng có đang chịu trạng thái buff không.</summary>
        public bool HasBuff(int actorId, int skillId)
        {
            return _actorBuffs.TryGetValue(actorId, out var buffs) && buffs.ContainsKey(skillId);
        }

        // ── Helper ─────────────────────────────────────────────────────────

        private void TriggerHapticFeedback()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Rung phản hồi trên thiết bị di động
            Handheld.Vibrate();
#endif
            SubsystemLog.Info("Haptic", "Haptic vibration triggered on mobile client.");
        }
    }
}
