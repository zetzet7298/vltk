// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — PlayerStateResponse (FS-02A contract)
//
// Pin từ backend contract (FS-02A, smoke step 9-10):
//   POST /v1/player  body = { roleId, level?, series? }  → PlayerStateResponse
//   GET  /v1/player/by-role/{roleId}                    → PlayerStateResponse
//
// Field camelCase khớp backend CamelCaseModel:
//   id          (int)   — primary key của player_states
//   roleId      (int)   — FK logic đến roles.id
//   level       (int)   — 1..200, default 1
//   exp         (int)   — kinh nghiệm hiện tại (long-term, default 0)
//   transLife   (int)   — số lần chuyển sinh (0..N), default 0
//   freePoint   (int)   — điểm tiềm năng còn dư (default 0)
//   magicPoint  (int)   — điểm nội công (default 0)
//   strength    (int)   — sức mạnh, base theo series (Kim=35, Mộc=25, …)
//   dexterity   (int)   — thân pháp
//   vitality    (int)   — nội lực tối đa
//   spirit      (int)   — tinh thần
//   series      (int)   — 0..4: Kim/Mộc/Thủy/Hỏa/Thổ, default 0
//   money       (int)   — bạc (long-term; bạc khóa tách ở DB khác)
//   repute      (int)   — danh vọng
//
// FS-02B chỉ cần GetPlayerStateAsync(roleId) → PlayerStateResponse. POST tạo
// player (PlayerStateCreate) sẽ port ở slice sau (FS-02B+) — task body chỉ yêu
// cầu GetPlayerStateAsync ở read-side.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Backend.Dto
{
    /// <summary>Trạng thái nhân vật (level, exp, chỉ số, tiền, danh vọng).</summary>
    [Serializable]
    public sealed class PlayerStateResponse
    {
        public int id;
        public int roleId;
        public int level;
        public int exp;
        public int transLife;
        public int freePoint;
        public int magicPoint;
        public int strength;
        public int dexterity;
        public int vitality;
        public int spirit;
        public int series;
        public int money;
        public int repute;
    }
}
