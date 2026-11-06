"""art/raw 의 생성 그림을 게임용으로 다듬어 assets/images 에 넣는다.

    python tools/process_art.py            art/raw 전부
    python tools/process_art.py chair_idle 이름 하나만

마젠타 배경은 투명하게 지우고, 파일 이름 앞부분으로 크기를 정한다. 배경 그림(bg_ · title_ · app_)은 투명 처리를 하지 않는다.
"""
import colorsys
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
RAW, OUT = ROOT / "art" / "raw", ROOT / "assets" / "images"
STORY = "story_"
SIZES = {"chair": 512, "tap": 768, "face": 512, "tx": 256, "eq": 256, "up": 256, "sk": 256, "mn": 256, "hero": 512, "hyg": 512, "it": 256, "ic": 128, "ev": 768, "app": 1024}
OPAQUE = ("bg_", "wait_", "title_", "app_")
BACKGROUND_HUE = 300 / 360


def magenta_amount(r, g, b):
    """0 = 그림, 1 = 완전한 배경. 가장자리는 그 사이 값이라 반투명이 된다."""
    h, s, _ = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    distance = min(abs(h - BACKGROUND_HUE), 1 - abs(h - BACKGROUND_HUE))
    if distance > 0.06 or s < 0.15:
        return 0.0
    return min(1.0, (s - 0.15) / 0.35)


def cut_background(image):
    image = image.convert("RGBA")
    pixels = image.load()
    # 오른쪽 아래 구석의 생성 도구 표식은 피사체가 아니므로 오려 낼 때 함께 뺀다
    for y in range(int(image.height * 0.9), image.height):
        for x in range(int(image.width * 0.9), image.width):
            pixels[x, y] = (0, 0, 0, 0)
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            amount = magenta_amount(r, g, b)
            if amount <= 0:
                continue
            keep = 1 - amount
            # 분홍 테두리가 남지 않게 배경색 몫을 뺀다
            pixels[x, y] = (min(r, g + 40), g, min(b, g + 40), int(a * keep))
    box = image.getbbox()
    return image.crop(box) if box else image


def fit_square(image, size):
    scale = min(size / image.width, size / image.height)
    resized = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(resized, ((size - resized.width) // 2, (size - resized.height) // 2), resized)
    return canvas


def cover(image, width, height):
    scale = max(width / image.width, height / image.height)
    resized = image.resize((round(image.width * scale), round(image.height * scale)), Image.LANCZOS)
    left, top = (resized.width - width) // 2, (resized.height - height) // 2
    return resized.crop((left, top, left + width, top + height)).convert("RGB")


def process(path):
    name = path.stem
    image = Image.open(path)
    if name.startswith("bg_"):
        result = cover(image, 1080, 700)   # 진료실 칸 비율
    elif name.startswith("wait_"):
        result = cover(image, 1116, 264)   # 대기실 칸 비율
    elif name.startswith("title_"):
        result = cover(image, 1080, 1920)
    elif name.startswith(STORY):
        # 오른쪽 아래 생성기 표식(반짝이)을 잘라 낸다
        w, h = image.size
        result = cover(image.crop((0, 0, round(w * 0.92), round(h * 0.92))), 1080, 1080)
    elif name.startswith(OPAQUE):
        result = cover(image, 1024, 1024)
    else:
        result = fit_square(cut_background(image), SIZES[name.split("_")[0]])
    OUT.mkdir(parents=True, exist_ok=True)
    result.save(OUT / (name + ".png"), optimize=True)
    print(name, result.size)


if __name__ == "__main__":
    names = sys.argv[1:]
    for file in sorted(RAW.glob("*.png")):
        if not names or file.stem in names:
            process(file)
