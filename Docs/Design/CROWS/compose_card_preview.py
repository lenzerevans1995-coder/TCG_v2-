from PIL import Image, ImageDraw, ImageFont
import colorsys

ART = r"C:\UnityProjects\TradingCardGame\My project\Assets\CrowsTCG\Art"
FONTS = r"C:\UnityProjects\TradingCardGame\My project\Assets\CrowsTCG\Fonts"
CAT = r"C:\UnityProjects\TradingCardGame\My project\Docs\Design\CROWS\catalog"

W, H = 1800, 2520
WIN = (130, 133, 1668, 1417)      # card frame art window
COST_C = (267, 266)               # measured solid-disk centers (OCCULT, assumed shared)
GEM_C = (232, 522)

ultra = lambda s: ImageFont.truetype(FONTS + r"\Ultra-Regular.ttf", s)
hand = lambda s: ImageFont.truetype(FONTS + r"\PatrickHand-Regular.ttf", s)

def outlined(d, pos, text, font, fill=(255, 255, 255, 255), outline=(10, 8, 12, 255), w=7, anchor="mm"):
    x, y = pos
    for dx in range(-w, w + 1, 3):
        for dy in range(-w, w + 1, 3):
            if dx or dy:
                d.text((x + dx, y + dy), text, font=font, fill=outline, anchor=anchor)
    d.text(pos, text, font=font, fill=fill, anchor=anchor)

def icon(path, size):
    im = Image.open(path).convert("RGBA")
    im.thumbnail((size, size), Image.LANCZOS)
    return im

def paste_center(card, im, cx, cy):
    card.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))

def cover(art, ww, wh, top_bias=0.25):
    s = max(ww / art.width, wh / art.height)
    art = art.resize((int(art.width * s), int(art.height * s)), Image.LANCZOS)
    ax = (art.width - ww) // 2
    ay = int((art.height - wh) * top_bias)
    return art.crop((ax, ay, ax + ww, ay + wh))

def make_card(frame_png, art_png, title, body_lines, cost, gem_png, atk, ac, life, cardtype, cid, out):
    frame = Image.open(ART + "\\Frames\\" + frame_png).convert("RGBA")
    art = cover(Image.open(art_png).convert("RGBA"), WIN[2] - WIN[0], WIN[3] - WIN[1])
    card = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    card.paste(art, (WIN[0], WIN[1]))
    card.alpha_composite(frame)
    d = ImageDraw.Draw(card)

    if cost is not None:
        outlined(d, COST_C, str(cost), ultra(150))
    if gem_png:
        paste_center(card, icon(ART + "\\Icons\\" + gem_png, 175), GEM_C[0], GEM_C[1])

    outlined(d, (1560, 210), cid, ultra(64), fill=(230, 220, 240, 255))
    outlined(d, (900, 1520), title, ultra(104))
    y = 1712
    for line in body_lines:
        outlined(d, (900, y), line, hand(94), fill=(235, 228, 240, 255), w=4)
        y += 116
    outlined(d, (900, 2380), cardtype, ultra(58), fill=(240, 235, 245, 255))

    # bottom badges: shield=AC (BL), dagger=ATK (right of shield), heart=LIFE (BR)
    sh = icon(ART + "\\Icons\\57.png", 330)
    card.alpha_composite(sh, (55, 2160))
    outlined(d, (55 + sh.width // 2, 2160 + int(sh.height * 0.46)), str(ac), ultra(124))
    if atk is not None:
        dg = icon(ART + "\\Icons\\24.png", 300)
        card.alpha_composite(dg, (385, 2180))
        outlined(d, (385 + dg.width // 2 + 8, 2180 + int(dg.height * 0.52)), str(atk), ultra(124))
    hp = icon(ART + "\\Icons\\52.png", 330)
    card.alpha_composite(hp, (W - 55 - hp.width, 2160))
    outlined(d, (W - 55 - hp.width // 2, 2160 + int(hp.height * 0.44)), str(life), ultra(124))

    card.convert("RGB").resize((643, 900), Image.LANCZOS).save(out)

# ---------- board token: square frame, art window, 4 corner icons ----------
TOK_BBOX = (111, 478, 1688, 2041)
TOK_WIN = (111 + 117, 478 + 82, 111 + 1460, 478 + 1416)

def tint_frame(frame, hexcolor):
    # recolor the grey stone to the aspect hue; keep the green vines
    tr, tg, tb = tuple(int(hexcolor[i:i+2], 16) for i in (1, 3, 5))
    th, ts, tv = colorsys.rgb_to_hsv(tr / 255, tg / 255, tb / 255)
    px = frame.load()
    for yy in range(frame.height):
        for xx in range(frame.width):
            r, g, b, a = px[xx, yy]
            if a == 0:
                continue
            hh, ss, vv = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if ss < 0.25 and vv > 0.08:  # grey stone only
                nr, ng, nb = colorsys.hsv_to_rgb(th, ts * 0.75, vv)
                px[xx, yy] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return frame

def make_token(art_png, atk, ac, life, gem_png, out, tint=None):
    frame = Image.open(ART + r"\Frames\UNIVERSAL_token.png").convert("RGBA").crop(TOK_BBOX)
    if tint:
        frame = tint_frame(frame, tint)
    fw, fh = frame.size
    win = (TOK_WIN[0] - TOK_BBOX[0], TOK_WIN[1] - TOK_BBOX[1], TOK_WIN[2] - TOK_BBOX[0], TOK_WIN[3] - TOK_BBOX[1])
    art = cover(Image.open(art_png).convert("RGBA"), win[2] - win[0], win[3] - win[1], top_bias=0.2)
    tok = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    tok.paste(art, (win[0], win[1]))
    tok.alpha_composite(frame)
    d = ImageDraw.Draw(tok)

    dg = icon(ART + r"\Icons\24.png", 300)   # TL attack
    tok.alpha_composite(dg, (30, 30))
    outlined(d, (30 + dg.width // 2 + 10, 30 + int(dg.height * 0.55)), str(atk), ultra(130))
    gm = icon(ART + "\\Icons\\" + gem_png, 240)  # TR aspect
    tok.alpha_composite(gm, (fw - 30 - gm.width, 36))
    sh = icon(ART + r"\Icons\57.png", 300)   # BL armor
    tok.alpha_composite(sh, (30, fh - 30 - sh.height))
    outlined(d, (30 + sh.width // 2, fh - 30 - sh.height + int(sh.height * 0.46)), str(ac), ultra(130))
    hp = icon(ART + r"\Icons\52.png", 300)   # BR life
    tok.alpha_composite(hp, (fw - 30 - hp.width, fh - 30 - hp.height))
    outlined(d, (fw - 30 - hp.width // 2, fh - 30 - hp.height + int(hp.height * 0.44)), str(life), ultra(130))

    tok.convert("RGB").resize((700, 694), Image.LANCZOS).save(out)

# card with GENERATED art, corrected centers + attack dagger
make_card("OCCULT.png", ART + r"\Generated\minion_occult_acolyte_v1_a.png",
          "VOID ACOLYTE", ["When played: each enemy", "unit gains Distress 1."],
          2, "50.png", 2, 1, 3, "MINION", "#050", CAT + r"\card_preview_void_acolyte.png")

# card with ORIGINAL pack artwork (M-series, MIGHT frame)
make_card("MIGHT.png", ART + r"\Artwork\M01.png",
          "IRONBLOOD SOLDIER", ["Strike: gains Enraged 1", "until end of turn."],
          1, "47.png", 3, 2, 2, "MINION", "#021", CAT + r"\card_preview_original_art.png")

# board tokens: universal + occult-tinted variant
make_token(ART + r"\Generated\minion_occult_acolyte_v1_a.png", 2, 1, 3, "50.png", CAT + r"\token_preview_universal.png")
make_token(ART + r"\Generated\minion_occult_acolyte_v1_a.png", 2, 1, 3, "50.png", CAT + r"\token_preview_occult.png", tint="#ad01bc")
print("done")
