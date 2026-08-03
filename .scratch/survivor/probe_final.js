const fs = require("fs");
const path = require("path");
const table = [
  0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
  32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
  64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
  96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
  128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,
  160,258,194,202,212,416,431,272,259,226,234,244,417,432,273,175,
  176,177,178,179,180,224,7843,227,225,7841,186,7857,7859,7861,7855,191,
  192,193,194,195,196,197,7863,7847,7849,7851,7845,7853,232,205,7867,7869,
  233,7865,7873,7875,7877,7871,7879,236,7881,217,218,219,297,237,
  7883,242,224,7887,245,243,7885,7891,7893,7895,7889,7897,7901,7903,
  7905,7899,7907,249,240,7911,361,250,7909,7915,7917,7919,7913,7921,
  7923,7927,7929,253,7925,255,
];
const rev = new Map();
for (let i = 0; i < table.length; i++) if (!rev.has(table[i])) rev.set(table[i], i);
function revTcvn3(s) {
  const out = [];
  for (const ch of s) {
    const cp = ch.codePointAt(0);
    if (cp < 128) { out.push(cp); continue; }
    const b = rev.get(cp);
    if (b === undefined) return null;
    out.push(b);
  }
  return Buffer.from(out);
}
function hashBytes(bytes) {
  let value = 0n;
  for (let i = 0; i < bytes.length; i++) {
    let byte = bytes[i];
    if (byte >= 65 && byte <= 90) byte += 32;
    const c = byte >= 128 ? byte - 256 : byte;
    value = ((value + BigInt(i + 1) * BigInt(c)) % 0x8000000Bn) * 0xFFFFFFEFn;
    value &= 0xFFFFFFFFn;
  }
  return Number((value ^ 0x12345678n) & 0xFFFFFFFFn);
}
const s = fs.readFileSync("Assets/StreamingAssets/Reference/PcSkills.txt").toString("utf8");
const lines = s.split("\n").filter((l, i) => i > 0 && l.trim());
const root = "SpritesRuntime";
let hit = 0, miss = 0, unrec = 0, empty = 0;
const fails = [];
for (const l of lines) {
  const c = l.split("\t");
  const p = c[6] || "";
  if (!p) { empty++; continue; }
  const bytes = revTcvn3(p);
  if (!bytes) { unrec++; continue; }
  const h = hashBytes(bytes).toString(16).padStart(8, "0");
  if (fs.existsSync(path.join(root, h + ".spr"))) hit++; else { miss++; if (fails.length < 6) fails.push({ id: c[2], h }); }
}
console.log("PcSkills col6 precast: hit", hit, "miss", miss, "empty", empty, "unresolvable", unrec);
console.log("miss sample:", fails);

// LvlData sample: cols 71..110 of a few rows (PcSkills layout: 71=LvlSetting1,72=LvlData1,...)
for (const i of [1, 2, 100]) {
  const c = lines[i].split("\t");
  console.log("row", i + 1, "id", c[2], "lvl71-76:", JSON.stringify(c.slice(71, 77)), "fan58/60:", c[58], c[60], "req/max:", c[52], c[53], "form:", c[19], "child:", c[20], "melee:", c[26], "aura:", c[11], "byMissle:", c[41]);
}
// supply check: count skills with LvlSetting1 in heal/bomb scripts + special/bomb.lua
let heal = 0, bomb = 0, aura = 0;
for (const l of lines) {
  const c = l.split("\t");
  const s1 = c[71] || "";
  if (s1 === "lifereplenish_v" || s1 === "lifemax_v") heal++;
  if (s1 === "physicsdamage_v" || (c[70] || "").includes("bomb.lua")) bomb++;
  if ((c[11] || "") === "1") aura++;
}
console.log("heal(lifereplenish_v/lifemax_v):", heal, "bomb(physicsdamage_v or bomb.lua):", bomb, "aura:", aura);
// fan spread skills sample (Param1/2 non-zero)
let fan = 0; for (const l of lines) { const c = l.split("\t"); if ((c[58] || "") !== "0" && (c[58] || "") !== "") fan++; }
console.log("skills with Param1 != 0:", fan);
