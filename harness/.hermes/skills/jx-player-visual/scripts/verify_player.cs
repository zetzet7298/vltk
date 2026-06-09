// execute_code body — paste into unityMCP_execute_code (action=execute).
// Six ground-truth checks for any player avatar. Adjust the GameObject name if
// the avatar is not "MalePlayer". Returns a single string report.
//
// Requires Play mode + map loaded. Use after compiling a catalog/visual change.

var sb = new System.Text.StringBuilder();
var player = GameObject.Find("MalePlayer");           // <- change name if needed
if (player == null) return "player NOT FOUND (map still loading?)";

var srs = player.GetComponentsInChildren<SpriteRenderer>(true);
int withSprite = 0, minP = int.MaxValue, maxP = int.MinValue;
foreach (var sr in srs) {
    if (sr.sprite != null) withSprite++;
    if (sr.sortingOrder < minP) minP = sr.sortingOrder;
    if (sr.sortingOrder > maxP) maxP = sr.sortingOrder;
}
sb.AppendLine($"CHECK 1 parts: {withSprite}/{srs.Length} with sprite (0 => Bug 2 static-cache)");
sb.AppendLine($"  pos={player.transform.position} sortingOrder=[{minP}..{maxP}]");

// CHECK 2: sorting model (flat 5000 + camera CustomAxis world-Y sort).
// The OLD "player must exceed map max" test is wrong now — map art reaches ~100000
// and depth is resolved by the camera's transparencySortMode=CustomAxis, NOT by the
// order value. Just confirm the base order is MapRenderer.PlayerSortingOrder (5000).
int expectedBase = VLTK.Sandbox.MapRenderer.PlayerSortingOrder; // 5000
sb.AppendLine($"CHECK 2 sorting: player order=[{minP}..{maxP}] base expected {expectedBase} -> {(minP >= expectedBase ? "ok (CustomAxis handles depth)" : "BELOW base => check PlayerBaseSortingOrder")}");

// CHECK 3: A/B occlusion diff
var cam = GameObject.FindObjectsOfType<Camera>()[0];
cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -100f);
cam.orthographic = true; cam.orthographicSize = 120f;
int W = 256, H = 256;
System.Func<Texture2D> grab = () => {
    var rt = new RenderTexture(W, H, 24); var prev = cam.targetTexture;
    cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
    var t = new Texture2D(W, H, TextureFormat.RGB24, false);
    t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply();
    RenderTexture.active = null; cam.targetTexture = prev;
    UnityEngine.Object.DestroyImmediate(rt); return t;
};
foreach (var sr in srs) sr.enabled = true;  var a = grab();
foreach (var sr in srs) sr.enabled = false; var b = grab();
foreach (var sr in srs) sr.enabled = true;
int diff = 0;
for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) {
    var ca = a.GetPixel(x, y); var cb = b.GetPixel(x, y);
    if (Mathf.Abs(ca.r - cb.r) + Mathf.Abs(ca.g - cb.g) + Mathf.Abs(ca.b - cb.b) > 0.08f) diff++;
}
UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
sb.AppendLine($"CHECK 3 visible (A/B diff): {diff} px on top -> {(diff > 50 ? "VISIBLE ok" : "OCCLUDED/empty")}");

// CHECK 4: 8-way move
var ctrl = player.GetComponent<VLTK.Sandbox.SandboxPlayerController>();
var vis = player.GetComponentInChildren<VLTK.Sandbox.MalePlayerVisual>(true);
if (ctrl != null && vis != null) {
    var cases = new (Vector2 v, int dir)[] {
        (new Vector2(1,0),6),(new Vector2(1,1),5),(new Vector2(0,1),4),(new Vector2(-1,1),3),
        (new Vector2(-1,0),2),(new Vector2(-1,-1),1),(new Vector2(0,-1),0),(new Vector2(1,-1),7) };
    int pass = 0;
    foreach (var c in cases) {
        var before = player.transform.position;
        ctrl.SetMoveInput(c.v);
        for (int i = 0; i < 10; i++) ctrl.SimulateMove(1f/60f);
        bool ok = vis.direction == c.dir
            && vis.currentAction == VLTK.Sandbox.PlayerVisualAction.Move
            && (player.transform.position - before).magnitude > 0.5f;
        if (ok) pass++;
    }
    ctrl.SetMoveInput(Vector2.zero);
    sb.AppendLine($"CHECK 4 8-way move: {pass}/8 PASS");
} else {
    sb.AppendLine("CHECK 4 skipped (controller/visual type mismatch)");
}

// CHECK 5: mounted direction-lock (Bug 3 — horse "spins" while idle).
// While mounted + idle, direction must stay constant and every horse-part clip must be
// directionCount=8. A horse clip at directionCount=1 = the SPR-header-lies bug.
if (ctrl != null && vis != null && ctrl.Mount != null) {
    if (!ctrl.Mount.IsMounted && ctrl.defaultHorseId > 0) {
        ctrl.Mount.Mount(ctrl.defaultHorseId);
        for (int i = 0; i < 8; i++) ctrl.SimulateMove(0.1f); // past 0.5s transition
    }
    ctrl.SetMoveInput(Vector2.zero);
    int d0 = vis.GetCurrentDirection(); bool locked = true;
    for (int i = 0; i < 8; i++) { ctrl.SimulateMove(0.12f); if (vis.GetCurrentDirection() != d0) locked = false; }
    int horseParts = 0, oneDir = 0;
    var fParts = typeof(VLTK.Sandbox.MalePlayerVisual).GetField("_parts",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    if (fParts != null) foreach (var kv in (System.Collections.IEnumerable)fParts.GetValue(vis)) {
        var val = kv.GetType().GetProperty("Value").GetValue(kv);
        var spec = val.GetType().GetField("spec").GetValue(val);
        var clip = val.GetType().GetField("clip").GetValue(val);
        string nm = spec.GetType().GetField("name").GetValue(spec) as string;
        if (clip == null || nm == null || !nm.Contains("Horse")) continue;
        horseParts++;
        int dc = (int)clip.GetType().GetField("directionCount").GetValue(clip);
        if (dc <= 1) oneDir++;
    }
    sb.AppendLine($"CHECK 5 mounted lock: dir {(locked ? "constant ok" : "DRIFTS")}, horseParts={horseParts} dir1Count={oneDir} -> {(oneDir == 0 ? "8-dir ok" : "Bug 3 (pass expectedDirections=8)")}");
} else {
    sb.AppendLine("CHECK 5 skipped (no mount service)");
}

// CHECK 6: joystick touch not eaten by overlapping UI.
// Top raycast hit at the joystick center must BE the joystick, not a panel backdrop.
var js = GameObject.FindObjectOfType<VLTK.Sandbox.MobileJoystick>();
var es = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
if (js != null && es != null) {
    var jrt = js.background as RectTransform; var corners = new Vector3[4]; jrt.GetWorldCorners(corners);
    Vector2 jc = new Vector2((corners[0].x + corners[2].x) / 2f, (corners[0].y + corners[2].y) / 2f);
    var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = jc };
    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    UnityEngine.EventSystems.EventSystem.current.RaycastAll(ped, hits);
    string top = hits.Count > 0 ? hits[0].gameObject.name : "NONE";
    sb.AppendLine($"CHECK 6 joystick raycast: top hit '{top}' -> {(top.Contains("Joystick") ? "ok" : "BLOCKED by UI (set backdrop raycastTarget=false)")}");
} else {
    sb.AppendLine("CHECK 6 skipped (no joystick/EventSystem)");
}
return sb.ToString();
