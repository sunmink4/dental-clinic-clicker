"""art/raw 그림을 한 장에 모아 art/sheet.png 로 저장한다. python tools/contact_sheet.py [이름 앞부분...]"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent.parent
files = sorted((root / "art" / "raw").glob("*.png"))
if sys.argv[1:]:
    files = [f for f in files if f.stem.startswith(tuple(sys.argv[1:]))]
cell, columns = 300, 4
rows = (len(files) + columns - 1) // columns
sheet = Image.new("RGB", (cell * columns, (cell + 20) * rows), "white")
draw = ImageDraw.Draw(sheet)
for i, file in enumerate(files):
    image = Image.open(file).convert("RGB")
    image.thumbnail((cell, cell))
    x, y = i % columns * cell, i // columns * (cell + 20)
    sheet.paste(image, (x, y + 20))
    draw.text((x + 4, y + 4), f"{file.stem} {Image.open(file).size}", fill="black")
sheet.save(root / "art" / "sheet.png")
print(len(files))
