"""art/raw 에 이미 받은 이름을 gemini_batch.js 에 넣어 art/batch.js 를 만든다. 브라우저 쪽 코드는 파일을 읽지 못한다."""
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
names = sorted(p.stem for p in (root / "art" / "raw").glob("*.png"))
code = (root / "tools" / "gemini_batch.js").read_text(encoding="utf-8")
code = code.replace("const EXISTING = [];", "const EXISTING = " + json.dumps(names) + ";")
(root / "art" / "batch.js").write_text(code, encoding="utf-8")
print(len(names))
