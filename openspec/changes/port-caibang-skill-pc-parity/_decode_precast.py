import io, re

src = io.open(r"Assets/Scripts/Sandbox/PcModSkillParser.cs", encoding="utf-8").read()
start = src.index("private static readonly Dictionary<char, char> Tcvn3ToUnicode")
body = src[start:start + 4000]
inv = {}
pat = re.compile(r"\[\s*'(.)'\s*\]\s*=\s*'(.)'")
for line in body.split("\n"):
    mm = pat.search(line)
    if mm:
        inv[mm.group(2)] = ord(mm.group(1))
print("map entries:", len(inv))


def reverse_mojibake(s):
    out = bytearray()
    for ch in s:
        if ch in inv:
            out.append(inv[ch])
        elif ord(ch) < 0x80:
            out.append(ord(ch))
        else:
            return None
    try:
        return out.decode("gbk", errors="replace")
    except Exception:
        return None


raw = open(r"Assets/StreamingAssets/Reference/PcSkills.txt", "rb").read()
lines = raw.replace(b"\r\n", b"\n").split(b"\n")
for t in (102, 136, 148, 174, 392, 393):
    for line in lines:
        c = line.split(b"\t")
        if len(c) > 30 and c[2] == str(t).encode():
            s = c[6].decode("utf-8", errors="replace")
            print("%d -> %s" % (t, reverse_mojibake(s)))
            break
