// execute_code body — run with the live execute_code tool.
// Four ground-truth checks for any player avatar. Adjust the GameObject name if
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

// CHECK 2: above map ceiling
var mr = GameObject.Find("MapRenderer");
int mapMax = int.MinValue;
if (mr != null) foreach (var sr in mr.GetComponentsInChildren<SpriteRenderer>(true))
    if (sr.sortingOrder > mapMax) mapMax = sr.sortingOrder;
sb.AppendLine($"CHECK 2 vs map: player min {minP} vs map max {mapMax} -> {(minP > mapMax ? "ABOVE ok" : "UNDER => Bug 1 sorting")}");

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
return sb.ToString();
