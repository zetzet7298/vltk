# `addskilldamage` Engine Semantics Evidence

## Question
Does casting a Cai Bang skill that has `addskilldamageN` (e.g. Thiên Hạ Vô Cẩu 359, Bổng Đả
Ác Cẩu 125) spawn the listed sub-skill's missiles/visual, or only modify damage?

## Source of truth
PC engine C++ at `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/Core/Src`.

### 1. Parse: `addskilldamage1..9` stored as magic attribs
`KSkills.cpp:2544`:
```
if (i >= magic_addskilldamage1 && i <= magic_addskilldamage9)
{
    m_AddSkillDamage[m_nAddSkillDamageNum].nAttribType = i;
    m_AddSkillDamage[m_nAddSkillDamageNum].nValue[0] = nValue1; // target skillId
    m_AddSkillDamage[m_nAddSkillDamageNum].nValue[1] = nValue2;
    m_AddSkillDamage[m_nAddSkillDamageNum].nValue[2] = nValue3; // percent (slot[3])
    m_nAddSkillDamageNum++;
}
```

### 2. Resolve: sum percents from LEARNED skills targeting the cast skill
`KSkillList.cpp:895` — `KSkillList::GetAddSkillDamage(int nSkillID)`:
```
int nAddP = 0;
for (int i = 1; i < MAX_NPCSKILL; i++) {
    if (m_Skills[i].SkillLevel) {
        KSkill* pSkill = g_SkillManager.GetSkill(m_Skills[i].SkillId, m_Skills[i].CurrentSkillLevel);
        if (pSkill) {
            KMagicAttrib* pMagicData = pSkill->GetAddSkillDamage();
            for (int j = 0; j < MAX_ADDSKILLDAMAGE; j++) {
                if (!pMagicData[j].nAttribType) continue;
                if (pMagicData[j].nValue[0] == nSkillID)   // entry targets the cast skill
                    nAddP += pMagicData[j].nValue[2];      // add its percent
            }
        }
    }
}
return nAddP;
```
- Iterates ALL learned skills (`m_Skills`).
- For each `addskilldamage` entry whose `nValue[0]` equals the skill being cast (`nSkillID`),
  adds `nValue[2]` (the percent) to `nAddP`.
- **No RNG / no chance roll. No sub-skill cast. No missile spawn.**

### 3. Apply: flat %-damage amplifier on the cast skill's own damage
`KNpc.cpp:3017` — `KNpc::AppendSkillEffect(int nSkillID, ...)`:
```
int nAddDamageP = m_SkillList.GetAddSkillDamage(nSkillID) + m_CurrentSkillEnhancePercent;
...
// magic_magicdamage_v (3045):
pDes->nValue[0] = pTemp->nValue[0] + (pTemp->nValue[0] * nAddDamageP / MAX_PERCENT);
pDes->nValue[2] = pTemp->nValue[2] + (pTemp->nValue[2] * nAddDamageP / MAX_PERCENT);
// magic_physicsenhance_p → physicsdamage_v (3119):
pDes->nValue[0] = nMinDamage * (MAX_PERCENT + (pTemp->nValue[0] + (pTemp->nValue[0]*nAddDamageP/MAX_PERCENT))) / MAX_PERCENT;
```
`GameDataDef.h:47`: `#define MAX_PERCENT 100`.

So `addskilldamage` behaves identically to skill-enhance: it scales the cast skill's own
magic/physics damage by `(100 + nAddP) / 100`.

## Direction (important)
The `addskilldamage` table lives on the GRANTING skill and points at the skill it BUFFS:
- `bangda_egou` (125): `addskilldamage1 → 359` (+60% L20), `addskilldamage2 → 1074` (+50%).
- Meaning: if the player has LEARNED 125, then casting **359** gets +60% and casting **1074**
  gets +50%. Casting **125 itself** is NOT buffed by these entries (nothing targets 125), and
  does NOT launch 359/1074.

## Mobile bug fixed
`CombatRuntimeService.TryFireAddSkillDamageChain/Slot` previously rolled a chance and then
`ApplyDamage + SpawnProjectiles(subSkill)` — spawning the sub-skill's missiles and applying its
damage. That is wrong on three counts: (1) it spawned extra missiles/visual, (2) it rolled RNG,
(3) it reversed the direction (casting 125 triggered 359/1074).

Fix: `ComputeAddSkillDamagePercent(caster, castSkillId)` sums the percents from learned grant
skills whose `addskilldamageN[1]` targets the cast skill, and `ApplyDamage` scales the cast
skill's damage components by `(MAX_PERCENT + addP) / MAX_PERCENT`. No spawn, no RNG.

## Test impact
- `CaiBang_359...`: spawns only its own 3 missiles (now correct; previously failed expecting 3 vs 8).
- `CaiBang_Cast_...125`: spawns 0 own sub-skill missiles, `addSkillDamagePercent == 0` (nothing targets 125).
- New `CaiBang_AddSkillDamage_IsPassiveDamageAmp_NotChainSpawn`: casting 359 with 119+125 learned
  yields `addSkillDamagePercent == 100` (40 + 60) and exactly 3 own missiles.
