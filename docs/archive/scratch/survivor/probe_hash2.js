// Verify hash staging with missles.txt GBK col32 paths + display-file GBK col6
const fs = require("fs");
const path = require("path");

function hashBytes(bytes) {
  let value = 0;
  for (let i = 0; i < bytes.length; i++) {
    let byte = bytes[i];
    if (byte >= 65 && byte <= 90) byte += 32;
    let c = byte >= 128 ? byte - 256 : byte;
    value = ((value + (i + 1) * c) % 0x8000000B) * 0xFFFFFFEF;
    value >>>= 0;
  }
  return (value ^ 0x12345678) >>> 0;
}
const norm = (p) => { let s = p.trim().replace(/\0+$/,"").replace(/\//g,"\\"); if (!s.startsWith("\\")) s = "\\" + s; return s; };
function gbkBytes(s) {
  // encode JS string to GBK bytes using TextDecoder trick: build via binary string of GBK decode
  // Node has no GBK encoder; use latin1 capture: decode original bytes already available in missles case.
  return null;
}

// missles.txt: raw bytes, col32 = GBK path
const raw = fs.readFileSync("Assets/StreamingAssets/Reference/PcAttrib/missles.txt");
const lines = raw.toString("latin1").split("\n").filter((l, i) => i > 0 && l.trim());
const root = "SpritesRuntime";
let hit = 0, miss = 0;
const samples = [];
for (const l of lines) {
  const c = l.split("\t");
  const p = c[32] || "";
  if (!p) continue;
  const bytes = Buffer.from(p, "latin1"); // raw GBK bytes preserved via latin1
  const h = hashBytes(bytes).toString(16).padStart(8, "0");
  const staged = fs.existsSync(path.join(root, h + ".spr"));
  staged ? hit++ : miss++;
  if (samples.length < 5) samples.push({ id: c[0], h, staged });
}
console.log("missles col32: hit", hit, "miss", miss, "total", hit + miss);
console.log(samples);

// display file col6 GBK raw
const rawD = fs.readFileSync("Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt");
const linesD = rawD.toString("latin1").split("\n").filter((l, i) => i > 0 && l.trim());
let hitD = 0, missD = 0, emptyD = 0;
for (const l of linesD) {
  const c = l.split("\t");
  const p = c[6] || "";
  if (!p) { emptyD++; continue; }
  const bytes = Buffer.from(p, "latin1");
  const h = hashBytes(bytes).toString(16).padStart(8, "0");
  fs.existsSync(path.join(root, h + ".spr")) ? hitD++ : missD++;
}
console.log("display col6: hit", hitD, "miss", missD, "empty", emptyD);

// sanity: known staged 00002d56.spr -> what path? find any file mapping? skip.
// PcSkills proper-chinese col6 (13) — hash via UTF-8->GBK encode requires encoder; emulate: those paths decode from GBK bytes; use display/missles evidence instead.
