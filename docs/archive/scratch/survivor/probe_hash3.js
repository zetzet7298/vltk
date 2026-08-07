// Verify hash with BigInt math
const fs = require("fs");
const path = require("path");

function hashBytes(bytes) {
  let value = 0n;
  const M = 0x8000000Bn, F = 0xFFFFFFEFn;
  for (let i = 0; i < bytes.length; i++) {
    let byte = bytes[i];
    if (byte >= 65 && byte <= 90) byte += 32;
    let c = byte >= 128 ? byte - 256 : byte;
    value = ((value + BigInt(i + 1) * BigInt(c)) % M) * F;
    value &= 0xFFFFFFFFn;
  }
  return Number((value ^ 0x12345678n) & 0xFFFFFFFFn);
}
const norm = (p) => { let s = p.trim().replace(/\0+$/,"").replace(/\//g,"\\"); if (!s.startsWith("\\")) s = "\\" + s; return s; };

const raw = fs.readFileSync("Assets/StreamingAssets/Reference/PcAttrib/missles.txt");
const lines = raw.toString("latin1").split("\n").filter((l, i) => i > 0 && l.trim());
const root = "SpritesRuntime";
let hit = 0, miss = 0;
const samples = [];
for (const l of lines) {
  const c = l.split("\t");
  const p = c[32] || "";
  if (!p) continue;
  const bytes = Buffer.from(p, "latin1");
  const h = hashBytes(bytes).toString(16).padStart(8, "0");
  const staged = fs.existsSync(path.join(root, h + ".spr"));
  staged ? hit++ : miss++;
  if (samples.length < 6) samples.push({ id: c[0], h, staged });
}
console.log("missles col32: hit", hit, "miss", miss, "total", hit + miss);
console.log(samples);

const rawD = fs.readFileSync("Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt");
const linesD = rawD.toString("latin1").split("\n").filter((l, i) => i > 0 && l.trim());
let hitD = 0, missD = 0, emptyD = 0;
const samD = [];
for (const l of linesD) {
  const c = l.split("\t");
  const p = c[6] || "";
  if (!p) { emptyD++; continue; }
  const bytes = Buffer.from(p, "latin1");
  const h = hashBytes(bytes).toString(16).padStart(8, "0");
  const st = fs.existsSync(path.join(root, h + ".spr"));
  st ? hitD++ : missD++;
  if (samD.length < 4) samD.push({ id: c[2], h, staged: st });
}
console.log("display col6: hit", hitD, "miss", missD, "empty", emptyD);
console.log(samD);
