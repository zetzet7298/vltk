const fs = require("fs");
const t = fs.readFileSync("Assets/Tests/EditMode/Survivor/SurvivorSkillCatalogTests.cs").toString("utf8");
const m = t.match(/const string moji = "([^"]+)"/);
const mk = m[1];
let esc = "";
for (const ch of mk) { const cp = ch.codePointAt(0); esc += cp > 126 ? "\\u" + cp.toString(16).padStart(4, "0") : ch; }
console.log("test literal esc:", esc);
const s = fs.readFileSync("Assets/StreamingAssets/Reference/PcSkills.txt").toString("utf8");
for (const l of s.split("\n")) {
  const c = l.split("\t");
  if (c[2] === "15") { console.log("real :", JSON.stringify(c[6])); console.log("match:", c[6] === mk); break; }
}
