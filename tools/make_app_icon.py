"""art/raw/app_icon.png (마스크 쓴 여자 치과의사 흉상) 의 마젠타 배경을 지우고 청록 판에 얹어 앱 아이콘을 만든다.
흉상은 판 아래 끝까지 이어지게 아래에 붙이고, 판 모양 밖은 잘라 낸다.

    python tools/make_app_icon.py

assets/images/app_icon.png, src/Dc.Desktop/app.ico, 그리고
src/Dc.Android/Resources/mipmap-*/ 의 안드로이드 런처 아이콘(일반 · 둥근 · 적응형)을 쓴다.
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from process_art import ROOT, RAW, cut_background

SIZE = 1024
TEAL = (15, 118, 110, 255)
TEAL_HEX = "#0F766E"
BLUE = (22, 86, 214, 255)
TITLE = ("치과", "키우기")
FONTS = [Path.home() / "AppData/Local/Microsoft/Windows/Fonts/Pretendard-Black.otf", Path("C:/Windows/Fonts/malgunbd.ttf")]
ANDROID = ROOT / "src" / "Dc.Android" / "Resources"
DENSITIES = {"mdpi": 1, "hdpi": 1.5, "xhdpi": 2, "xxhdpi": 3, "xxxhdpi": 4}


def plate_mask(size, shape):
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    if shape == "circle":
        draw.ellipse((0, 0, size - 1, size - 1), fill=255)
    elif shape == "rounded":
        draw.rounded_rectangle((0, 0, size - 1, size - 1), size // 6, fill=255)
    else:
        draw.rectangle((0, 0, size - 1, size - 1), fill=255)
    return mask


def on_plate(subject, size, shape, height=0.9, plate=True):
    """흉상을 판 아래 가운데에 붙인다. plate=False 면 배경 없이 그림만 (적응형 전경)."""
    image = Image.new("RGBA", (size, size), TEAL if plate else (0, 0, 0, 0))
    h = round(size * height)
    w = round(subject.width * h / subject.height)
    image.alpha_composite(subject.resize((w, h), Image.LANCZOS), ((size - w) // 2, size - h))
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(image, (0, 0), plate_mask(size, shape))
    return out


def add_title(image, left, bottom, height):
    """왼쪽 아래에 '치과 / 키우기' 를 파란 글씨, 흰 테두리로 두 줄 쓴다. 값은 판 크기에 대한 비율."""
    size = image.width
    font_path = next(str(f) for f in FONTS if f.exists())
    line = round(size * height / 2)
    font = ImageFont.truetype(font_path, line)
    draw = ImageDraw.Draw(image)
    stroke = max(1, round(line * 0.14))
    x, y = round(size * left), round(size * bottom) - line * 2
    for i, text in enumerate(TITLE):
        draw.text((x, y + i * line), text, font=font, fill=BLUE, stroke_width=stroke, stroke_fill="white", anchor="lt")
    return image


raw = Image.open(RAW / "app_icon.png").convert("RGBA")
raw = raw.crop((0, 0, raw.width, round(raw.height * 0.915)))  # 오른쪽 아래 생성기 표식과 함께 잘린 아랫단을 버린다
subject = cut_background(raw)
subject = subject.crop(subject.getbbox())

icon = add_title(on_plate(subject, SIZE, "rounded"), 0.07, 0.95, 0.30)
icon.save(ROOT / "assets" / "images" / "app_icon.png")
icon.save(ROOT / "src" / "Dc.Desktop" / "app.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
round_icon = add_title(on_plate(subject, SIZE, "circle"), 0.12, 0.85, 0.25)
# 적응형 전경 108dp 중 런처가 보여 주는 것은 가운데 72dp 뿐이다. 둥근 아이콘과 같은 구도를 그 안에 넣는다.
visible = add_title(on_plate(subject, SIZE, "square", plate=False), 0.12, 0.85, 0.25)
foreground = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
foreground.alpha_composite(visible.resize((SIZE * 2 // 3,) * 2, Image.LANCZOS), (SIZE // 6, SIZE // 6))  # 얼굴이 가운데 66dp 안전 영역 안에 들게

# 안드로이드 — 옛 런처용 48dp 아이콘, 둥근 아이콘, 8.0+ 적응형 아이콘의 108dp 전경.
for name, scale in DENSITIES.items():
    folder = ANDROID / f"mipmap-{name}"
    folder.mkdir(parents=True, exist_ok=True)
    legacy = round(48 * scale)
    icon.resize((legacy, legacy), Image.LANCZOS).save(folder / "ic_launcher.png")
    round_icon.resize((legacy, legacy), Image.LANCZOS).save(folder / "ic_launcher_round.png")
    full = round(108 * scale)
    foreground.resize((full, full), Image.LANCZOS).save(folder / "ic_launcher_foreground.png")

adaptive = ANDROID / "mipmap-anydpi-v26"
adaptive.mkdir(parents=True, exist_ok=True)
xml = """<?xml version="1.0" encoding="utf-8"?>
<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
    <background android:drawable="@color/ic_launcher_background" />
    <foreground android:drawable="@mipmap/ic_launcher_foreground" />
</adaptive-icon>
"""
for name in ("ic_launcher", "ic_launcher_round"):
    (adaptive / f"{name}.xml").write_text(xml, encoding="utf-8")
values = ANDROID / "values"
values.mkdir(parents=True, exist_ok=True)
(values / "ic_launcher_background.xml").write_text(
    f'<?xml version="1.0" encoding="utf-8"?>\n<resources>\n    <color name="ic_launcher_background">{TEAL_HEX}</color>\n</resources>\n',
    encoding="utf-8")
