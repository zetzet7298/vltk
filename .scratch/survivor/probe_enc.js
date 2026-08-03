const fs = require("fs");

function isAscii(s) { for (let i = 0; i < s.length; i++) if (s.charCodeAt(i) > 126) return false; return true; }
function isMoji(s) {
  const moji = "³Ô±ứẻùÀớạƠằữếêôơăâđư";
  for (const ch of s) if (moji.includes(ch)) return true;
  return false;
}

const s = fs.readFileSync("Assets/StreamingAssets/Reference/PcSkills.txt").toString("utf8");
const lines = s.split("\n").filter((l, i) => i > 0 && l.trim());
const stats = { c6_proper: 0, c6_moji: 0, c6_empty: 0, c70_ascii: 0, c70_moji: 0, c70_chinese: 0, c70_empty: 0 };
for (const l of lines) {
  const c = l.split("\t");
  const p6 = c[6] || "";
  if (!p6) stats.c6_empty++; else if (isMoji(p6)) stats.c6_moji++; else stats.c6_proper++;
  const p70 = c[70] || "";
  if (!p70) stats.c70_empty++;
  else if (isAscii(p70)) stats.c70_ascii++;
  else if (isMoji(p70)) stats.c70_moji++;
  else stats.c70_chinese++;
}
console.log("col6:", JSON.stringify(stats), "col70:", JSON.stringify({ ascii: stats.c70_ascii, moji: stats.c70_moji, chinese: stats.c70_chinese, empty: stats.c70_empty }));

const fac = {};
for (const l of lines) {
  const p = (l.split("\t")[70] || "").replace(/\\/g, "/");
  const seg = p.split("/")[1] || "(empty)";
  fac[seg] = (fac[seg] || 0) + 1;
}
const ks = Object.keys(fac).sort((a, b) => fac[b] - fac[a]);
console.log("faction dist total", lines.length);
for (const k of ks) console.log(" ", JSON.stringify(k), fac[k]);
