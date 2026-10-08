"""把 Compress-Archive 产出的载荷规范化为标准 zip（正斜杠分隔），再重新打包。

顺带把载荷内容汇到 _staging2，便于安装程序内嵌。
"""
import os
import shutil
import sys
import zipfile

# ROOT 从脚本自身位置推 —— 写死的话发布副本里带本机路径，
# 而把路径换成占位符又跑不了。脚本在 installer/ 下，上一级就是仓库根。
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
INST = os.path.join(ROOT, "installer")
OLD = os.path.join(INST, "payload.zip")
TMP = r"E:\DaShaoHuo\cache\tmp\payload_unz"
NEW = os.path.join(INST, "payload.zip")

if os.path.isdir(TMP):
    shutil.rmtree(TMP)
os.makedirs(TMP, exist_ok=True)

BACKSLASH = chr(92)
FORWARD = "/"

with zipfile.ZipFile(OLD) as z:
    entries = z.infolist()
    print("旧 zip 条目数:", len(entries))
    print("样例:", [e.filename for e in entries[:3]])
    for e in entries:
        if e.is_dir():
            continue
        name = e.filename.replace(BACKSLASH, FORWARD)
        dest = os.path.join(TMP, *name.split(FORWARD))
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        with z.open(e) as src, open(dest, "wb") as out:
            shutil.copyfileobj(src, out)

# 重新打包，条目名一律正斜杠
if os.path.exists(NEW):
    os.remove(NEW)
count = 0
with zipfile.ZipFile(NEW, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for base, _dirs, files in os.walk(TMP):
        for f in files:
            full = os.path.join(base, f)
            rel = os.path.relpath(full, TMP).replace(os.sep, FORWARD)
            z.write(full, rel)
            count += 1

with zipfile.ZipFile(NEW) as z:
    names = z.namelist()

size_mb = os.path.getsize(NEW) / 1024 / 1024
print(f"新 payload.zip: {count} 个条目, {size_mb:.2f} MB")
print("样例:", names[:4])
bad = [n for n in names if BACKSLASH in n]
print("仍含反斜杠的条目:", len(bad))
