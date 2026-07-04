from PIL import Image, ImageDraw, ImageFont

ART = r"C:\UnityProjects\TradingCardGame\My project\Assets\CrowsTCG\Art"
FONTS = r"C:\UnityProjects\TradingCardGame\My project\Assets\CrowsTCG\Fonts"
OUT = r"C:\UnityProjects\TradingCardGame\My project\Docs\Design\CROWS\catalog\card_preview_void_acolyte.png"

W, H = 1800, 2520
WIN = (130, 133, 1668, 1417)  # frame art window (transparent region)

frame = Image.open(ART + r"\Frames\OCCULT.png").convert("RGBA")
art = Image.open(ART + r"\Generated\minion_occult_acolyte_v1_a.png").convert("RGBA")

# cover-fit art into window
ww, wh = WIN[2] - WIN[0], WIN[3] - WIN[1]
s = max(ww / art.width, wh / art.height)
art = art.resize((int(art.width * s), int(art.height * s)), Image.LANCZOS)
ax = (art.width - ww) // 2
ay = int((art.height - wh) * 0.25)  # top-bias like v1 (faces sit high)
art = art.crop((ax, ay, ax + ww, ay + wh))

card = Image.new("RGBA", (W, H), (0, 0, 0, 0))
card.paste(art, (WIN[0], WIN[1]))
card.alpha_composite(frame)

d = ImageDraw.Draw(card)
ultra = lambda size: ImageFont.truetype(FONTS + r"\Ultra-Regular.ttf", size)
hand = lambda size: ImageFont.truetype(FONTS + r"\PatrickHand-Regular.ttf", size)

def outlined(pos, text, font, fill, outline=(10, 8, 12, 255), w=6, anchor="mm"):
    x, y = pos
    for dx in range(-w, w + 1, 3):
        for dy in range(-w, w + 1, 3):
            if dx or dy:
                d.text((x + dx, y + dy), text, font=font, fill=outline, anchor=anchor)
    d.text(pos, text, font=font, fill=fill, anchor=anchor)

# cost in the top black circle; aspect gem in the lower circle
outlined((262, 300), "2", ultra(150), (255, 255, 255, 255))
gem = Image.open(ART + r"\Icons\50.png").convert("RGBA")
gem.thumbnail((190, 190), Image.LANCZOS)
card.alpha_composite(gem, (256 - gem.width // 2, 540 - gem.height // 2))

# card id top-right
outlined((1560, 210), "#050", ultra(64), (230, 220, 240, 255))

# title on the text box top edge
outlined((900, 1520), "VOID ACOLYTE", ultra(110), (255, 255, 255, 255))

# rules text (Patrick Hand), with inline icon feel kept plain for v0
body = ["When played: each enemy", "unit gains Distress 1."]
y = 1720
for line in body:
    outlined((900, y), line, hand(96), (232, 224, 238, 255), w=4)
    y += 120

# type plate
outlined((900, 2380), "MINION", ultra(60), (240, 235, 245, 255))

# AC shield bottom-left, Life heart bottom-right (pack icons 57 / 52)
shield = Image.open(ART + r"\Icons\57.png").convert("RGBA")
shield.thumbnail((330, 330), Image.LANCZOS)
card.alpha_composite(shield, (60, 2160))
outlined((60 + shield.width // 2, 2160 + int(shield.height * 0.46)), "1", ultra(120), (30, 26, 34, 255), outline=(255, 255, 255, 220), w=4)

heart = Image.open(ART + r"\Icons\52.png").convert("RGBA")
heart.thumbnail((330, 330), Image.LANCZOS)
card.alpha_composite(heart, (W - 60 - heart.width, 2160))
outlined((W - 60 - heart.width // 2, 2160 + int(heart.height * 0.44)), "3", ultra(120), (255, 255, 255, 255))

card.convert("RGB").resize((643, 900), Image.LANCZOS).save(OUT)
print("saved", OUT)
