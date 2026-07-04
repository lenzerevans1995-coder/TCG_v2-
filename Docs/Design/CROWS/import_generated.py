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

def main(apply=False):
    names = {}
    with open(MANIFEST, newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            names[int(row["line"])] = row["name"]

    plan, skipped = [], []
    for fn in sorted(os.listdir(GEN)):
        if not fn.lower().endswith(".png"):
            continue
        m = PAT.match(fn)
        if not m:
            skipped.append(fn)
            continue
        line, var = int(m.group(1)), int(m.group(2))
        if line not in names:
            skipped.append(fn + "  (line %d not in manifest)" % line)
            continue
        # manifest name already ends _v1/_v2; firefly's own var becomes a/b/c/d
        dest_name = "%s_%s.png" % (names[line], "abcd"[var - 1] if 1 <= var <= 4 else "x%d" % var)
        plan.append((fn, dest_name))

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
