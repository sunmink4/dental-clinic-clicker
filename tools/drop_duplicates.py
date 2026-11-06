"""art/raw 에서 내용이 같은 파일 중 나중 것을 지운다 — 다음 묶음에서 다시 받는다."""
import hashlib
from pathlib import Path

raw = Path(__file__).resolve().parent.parent / "art" / "raw"
files = sorted(raw.glob("*.png"), key=lambda p: p.stat().st_mtime)
seen = {}
for file in files:
    digest = hashlib.md5(file.read_bytes()).hexdigest()
    if digest in seen:
        print("삭제", file.stem, "=", seen[digest])
        file.unlink()
    else:
        seen[digest] = file.stem
