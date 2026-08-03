const fs = require("fs");
const table = [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,160,258,194,202,212,416,431,272,259,226,234,244,417,432,273,175,176,177,178,179,180,224,7843,227,225,7841,186,7857,7859,7861,7855,191,192,193,194,195,196,197,7863,7847,7849,7851,7845,7853,232,205,7867,7869,233,7865,7873,7875,7877,7871,7879,236,7881,217,218,219,297,237,7883,242,224,7887,245,243,7885,7891,7893,7895,7889,7897,7901,7903,7905,7899,7907,249,240,7911,361,250,7909,7915,7917,7919,7913,7921,7923,7927,7929,253,7925,255];
const rev = new Map();
for (let i = 0; i < table.length; i++) if (!rev.has(table[i])) rev.set(table[i], i);
function revTcvn3(s) { const out = []; for (const ch of s) { const cp = ch.codePointAt(0); if (cp < 128) { out.push(cp); continue; } const b = rev.get(cp); if (b === undefined) return null; out.push(b); } return Buffer.from(out); }
function isMoji(s) { const m = "³Ô±ứẻùÀớạƠằữếêôơăâđư"; for (const ch of s) if (m.includes(ch)) return true; return false; }

const s = fs.readFileSync("Assets/StreamingAssets/Reference/PcSkills.txt").toString("utf8");
const pcRows = s.split("\n").filter((l, i) => i > 0 && l.trim()).map(l => l.split("\t"));
const disp = fs.readFileSync("Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt").toString("latin1");
const dispRows = disp.split("\n").filter((l, i) => i > 0 && l.trim()).map(l => l.split("\t"));
const dispBytes = new Map();
for (const c of dispRows) if (c[6] && c[6].trim()) dispBytes.set(c[2], Buffer.from(c[6], "latin1"));

let mojiCovered = 0, mojiNotCovered = 0, properCovered = 0, properNotCovered = 0;
for (const c of pcRows) {
  const p = c[6] || "";
  if (!p) continue;
  const id = c[2];
  const isM = isMoji(p);
  if (dispBytes.has(id)) { isM ? mojiCovered++ : properCovered++; }
  else { isM ? mojiNotCovered++ : properNotCovered++; }
}
console.log("precast rows:", mojiCovered + mojiNotCovered + properCovered + properNotCovered,
  "| moji: covered-by-disp", mojiCovered, "not", mojiNotCovered,
  "| proper-chinese: covered-by-disp", properCovered, "not", properNotCovered);

// hex của id15 mojibake reverse bytes
for (const c of pcRows) {
  if (c[2] === "15") {
    const b = revTcvn3(c[6]);
    console.log("id15 reverse bytes hex:", b.toString("hex"));
    break;
  }
}
// id4 precast: moji hay proper? và display có không
const row4 = pcRows.find(r => r[2] === "4");
console.log("id4 col6 moji:", isMoji(row4[6] || ""), "disp bytes:", dispBytes.has("4"), JSON.stringify(row4[6]).slice(0, 60));
