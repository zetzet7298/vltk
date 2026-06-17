# Spawn Fix Summary - Training Center at MPS (6665, 6509)

## Problem
Player và training NPCs cần luôn spawn tại tọa độ `MPS (6665, 6509)` (tâm ngũ giác training) mỗi khi bắt đầu play mode trong Unity, bất kể vị trí player trong editor scene.

## Solution

### 1. Runtime Spawn (✅ Đã có)
**File:** `Assets/Scripts/Sandbox/SandboxManager.cs`

- `PlacePlayerOnActiveMap()` đã được cập nhật để luôn spawn player tại MPS (6665, 6509)
- `SpawnTrainingNpcs()` gọi `TrainingNpcSpawner.Spawn()` để tạo 5 training NPCs quanh tọa độ này
- Code:
  ```csharp
  // Always spawn at training NPC pentagon center: MPS (6665, 6509)
  Vector2 spawn = BaLangEnemyDatabase.MpsToWorld(6665, 6509);
  PlayerController.PlaceAt(spawn, snapCamera: false);
  ```

### 2. Training NPCs Spawner (✅ Đã có)
**File:** `Assets/Scripts/Sandbox/TrainingNpcSpawner.cs`

- Spawn 5 training NPCs (Bao cát, Cọc gỗ, Mộc nhân) theo hình ngũ giác
- Center: MPS (6665, 6509)
- Radius: 300 world units
- Template IDs từ PC: 413 (Cọc gỗ), 414 (Mộc nhân), 415 (Bao cát)
- Max HP: 9999

### 3. Editor Utility (✅ Mới tạo)
**File:** `Assets/Editor/SandboxDefaultSpawnEditor.cs`

Cung cấp Unity Editor menu items:
- **VLTK/Spawn/Set Player to Training Center (Edit Mode)**: Di chuyển player GameObject trong editor về tọa độ training center
- **VLTK/Spawn/Show Training Center Coordinates**: Hiển thị tọa độ MPS và World trong Console

## Coordinate Conversion

### PC MPS → Unity World
```csharp
// From BaLangEnemyDatabase.MpsToWorld(int mpsX, int mpsY)
int regionRow = mpsY / 1024;
float worldX = mpsX;
float worldY = -(mpsY - regionRow * 512);
```

### Training Center
- **MPS**: (6665, 6509)
- **Region Row**: 6509 / 1024 = 6
- **World X**: 6665
- **World Y**: -(6509 - 6 * 512) = -(6509 - 3072) = -3437
- **Unity Position**: (6665.00, -3437.00, 0)

## How It Works

### Play Mode Flow:
1. Unity enters play mode
2. `SandboxManager.Awake()` → `InitializeSubsystems()`
3. `MapManager.LoadMap()` triggers `OnMapLoaded` event
4. `PlacePlayerOnActiveMap()` sets player to MPS (6665, 6509)
5. `SpawnTrainingNpcs()` creates 5 NPCs around the same center
6. Player camera snaps to follow player at training center

### Edit Mode Reset:
- Khi stop play mode, Unity tự động restore scene về editor state
- Nếu muốn set player position trong editor, dùng menu: **VLTK/Spawn/Set Player to Training Center (Edit Mode)**

## Files Modified

### New Files:
- `Assets/Editor/SandboxDefaultSpawnEditor.cs` - Editor utility for spawn position
- `Assets/Editor/SandboxDefaultSpawnEditor.cs.meta` - Unity metadata
- `Assets/Scripts/Sandbox/TrainingNpcSpawner.cs` - Training NPCs spawner (nếu mới)
- `Assets/Scripts/Sandbox/TrainingNpcSpawner.cs.meta` - Unity metadata (nếu mới)

### Modified Files:
- `Assets/Scripts/Sandbox/SandboxManager.cs`:
  - Added `TrainingSpawner` property
  - Updated `PlacePlayerOnActiveMap()` to always use MPS (6665, 6509)
  - Added `SpawnTrainingNpcs()` call in map load flow
  - Added `TrainingSpawner` component in `EnsureEnemyRuntime()`

## Testing

### In Unity Editor:
1. **Open scene**: `Assets/Scenes/Sandbox.unity`
2. **Check spawn coordinates**:
   - Menu: VLTK → Spawn → Show Training Center Coordinates
   - Console sẽ hiển thị: `MPS: (6665, 6509)` và `World: (6665.00, -3437.00)`
3. **Set player in editor** (optional):
   - Menu: VLTK → Spawn → Set Player to Training Center (Edit Mode)
4. **Enter play mode**:
   - Player sẽ spawn tại training center
   - 5 training NPCs sẽ xuất hiện quanh ngũ giác
5. **Exit play mode**:
   - Player position trong editor được preserve (hoặc về vị trí đã set)

### Verify Coordinates:
```csharp
// Runtime check trong Play Mode
var spawn = BaLangEnemyDatabase.MpsToWorld(6665, 6509);
Debug.Log($"Spawn position: {spawn}"); // Output: (6665.00, -3437.00)
```

## PC Source Reference
Tọa độ này được derive từ PC JX Online source tại:
- **PC Source**: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/`
- **Training NPCs**: Template IDs 413, 414, 415 trong NpcS.txt
- **Map**: Ba Lăng Huyện (Map 79)
- **Region Data**: Region_S.dat có chứa spawn coordinates

## Notes
- ✅ Player luôn spawn tại training center khi enter play mode
- ✅ Training NPCs spawn tự động quanh player
- ✅ Editor utility cho phép set position trong edit mode
- ✅ Sử dụng công thức chuyển đổi MPS→World chính xác từ PC
- ✅ Vietnamese localization cho training NPC names
