const fs = require("fs");
function facKey(p) {
  const seg = (p || "").split("\\").join("/").split("/")[3] || "";
  return seg.replace(/\.lua$/i, "");
}
const s = fs.readFileSync("Assets/StreamingAssets/Reference/PcSkills.txt").toString("utf8");
const sl = s.split("\n").filter((l, i) => i > 0 && l.trim());
const fac = {};
const pc10 = [];
const ten = new Set(["shaolin", "saolin", "tangmen", "tangmeng", "cuiyan", "emei", "tianwang", "kunlun", "wudu", "wudang", "tianren", "gaibang"]);
for (const l of sl) {
  const c = l.split("\t");
  const k = facKey(c[70]);
  fac[k] = (fac[k] || 0) + 1;
  if (ten.has(k)) pc10.push(c[2]);
}
const ks = Object.keys(fac).sort((a, b) => fac[b] - fac[a]);
console.log("PcSkills faction dist:", sl.length);
for (const k of ks) console.log(" ", JSON.stringify(k), fac[k]);
console.log("10-faction pool:", pc10.length);

// display file same
const raw = fs.readFileSync("Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt");
const dl = raw.toString("latin1").split("\n").filter((l, i) => i > 0 && l.trim());
const facD = {}; const idsD = [];
for (const l of dl) {
  const c = l.split("\t");
  const k = facKey(c[71]);
  facD[k] = (facD[k] || 0) + 1;
  idsD.push(c[2]);
}
console.log("display dist:"); for (const k of Object.keys(facD).sort((a, b) => facD[b] - facD[a])) console.log(" ", JSON.stringify(k), facD[k]);
const set10 = new Set(pc10);
let inter = 0; for (const id of idsD) if (set10.has(id)) inter++;
console.log("display ids ∩ 10-faction pool:", inter, "/", idsD.length);
let missD = 0; for (const id of pc10) if (!idsD.includes(id)) missD++;
console.log("10-faction ids missing from display:", missD);
