"""Rename Firefly batch downloads against the prompt manifest and stage them for Unity.

Firefly names: firefly_YYYYMMDD_HHMM_<line>_var<V>.png  (line = 1-based prompt line)
Manifest:      03_Card_Art_Prompts_manifest.csv  (line,name,full_prompt)

Usage: python import_generated.py [--apply]
Without --apply it only prints the plan. With --apply it moves files to
Assets/CrowsTCG/Art/Generated/<name>_<fireflyvar>.png (Unity imports on focus).
Unmatched files are left in place and reported.
"""
import csv, os, re, shutil, sys

ROOT = r"C:\UnityProjects\TradingCardGame\My project"
GEN = os.path.join(ROOT, "Generated")
MANIFEST = os.path.join(ROOT, r"Docs\Design\CROWS\03_Card_Art_Prompts_manifest.csv")
DEST = os.path.join(ROOT, r"Assets\CrowsTCG\Art\Generated")

PAT = re.compile(r"^firefly_\d{8}_\d{4}_(\d+)_var(\d+)\.png$", re.I)
# alt schemes: "Firefly_Gemini Flash_<prompt snippet> <digits> <suffix>.png"
# and zip-export bare "<prompt snippet> <digits> <suffix>.png"
PAT_SNIPPET = re.compile(r"^(?:firefly_(?:gemini[^_]*_)?)?(.+?)\s+\d+(?:\s+\S+)?\.png$", re.I)

def norm(s):
    return re.sub(r"[^a-z0-9]+", " ", s.lower()).strip()

def main(apply=False):
    names = {}
    prompts = []  # (line, name, normalized prompt)
    with open(MANIFEST, newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            names[int(row["line"])] = row["name"]
            prompts.append((int(row["line"]), row["name"], norm(row["full_prompt"])))

    existing = set()
    if os.path.isdir(DEST):
        existing = {fn[:-4] for fn in os.listdir(DEST) if fn.endswith(".png")}

    plan, skipped = [], []
    for fn in sorted(os.listdir(GEN)):
        if not fn.lower().endswith(".png"):
            continue
        m = PAT.match(fn)
        if m:
            line, var = int(m.group(1)), int(m.group(2))
            if line not in names:
                skipped.append(fn + "  (line %d not in manifest)" % line)
                continue
            dest_name = "%s_%s.png" % (names[line], "abcd"[var - 1] if 1 <= var <= 4 else "x%d" % var)
            plan.append((fn, dest_name))
            continue

        ms = PAT_SNIPPET.match(fn)
        if not ms:
            skipped.append(fn)
            continue
        snippet = norm(ms.group(1))
        # prompt snippets are truncated filenames: match manifest prompts by prefix
        hits = [(ln, nm) for ln, nm, p in prompts if p.startswith(snippet)]
        if not hits:
            skipped.append(fn + "  (no prompt match)")
            continue
        # v1/v2 share the prompt prefix: prefer the variant not yet imported
        chosen = None
        for ln, nm in sorted(hits):
            if nm + "_a" not in existing and not any(d == nm + "_a.png" for _, d in plan):
                chosen = nm
                break
        if chosen is None:
            chosen = hits[0][1]  # both present: extra render, suffix picks next letter
        suffix = "a"
        while chosen + "_" + suffix in existing or any(d == chosen + "_" + suffix + ".png" for _, d in plan):
            suffix = chr(ord(suffix) + 1)
        plan.append((fn, chosen + "_" + suffix + ".png"))

    print("plan: %d files, skipped: %d" % (len(plan), len(skipped)))
    for src, dst in plan:
        print("  %s -> %s" % (src, dst))
    for s in skipped:
        print("  SKIP %s" % s)

    if apply and plan:
        os.makedirs(DEST, exist_ok=True)
        for src, dst in plan:
            shutil.move(os.path.join(GEN, src), os.path.join(DEST, dst))
        print("moved %d files to %s" % (len(plan), DEST))

if __name__ == "__main__":
    main(apply="--apply" in sys.argv)
