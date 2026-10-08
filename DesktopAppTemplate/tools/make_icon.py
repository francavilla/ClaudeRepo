"""
Genera l'icona dell'applicazione (assets/app.ico).

Quadrato arrotondato sfumato negli stessi indaco del tema (#4F46E5 -> #312E81) con una finestra
stilizzata: menu laterale scuro e due righe di elenco con cerchi di spunta, come nelle due interfacce.
L'icona è disegnata a 1024 px e ridotta con LANCZOS a ogni misura del file .ico (16-256 px).

Uso:  python make_icon.py [percorso_ico] [percorso_png_1024]
Richiede: Pillow (pip install pillow)
"""
import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

MASTER = 1024
SIZES = [256, 128, 64, 48, 40, 32, 24, 20, 16]
TOP_LEFT = (0x4F, 0x46, 0xE5)
BOTTOM_RIGHT = (0x31, 0x2E, 0x81)
SIDEBAR = (0x1E, 0x1B, 0x4B)
ACCENT = (0x4F, 0x46, 0xE5)
LINE = (0xC7, 0xCB, 0xD6)


def lerp(a, b, t):
    return tuple(int(round(x + (y - x) * t)) for x, y in zip(a, b))


def tile_mask(size):
    margin = int(size * 0.04)
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [margin, margin, size - margin - 1, size - margin - 1], radius=int(size * 0.22), fill=255)
    return mask


def background(size):
    gradient = Image.new("RGB", (size, size))
    pixels = gradient.load()
    for y in range(size):
        for x in range(size):
            pixels[x, y] = lerp(TOP_LEFT, BOTTOM_RIGHT, (x + y) / (2.0 * (size - 1)))
    mask = tile_mask(size)
    tile = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    tile.paste(gradient, (0, 0), mask)
    return tile


def window_layer(size):
    """Finestra stilizzata (RGBA) da sovrapporre allo sfondo."""
    s = size / 1024.0
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    left, top, right, bottom = 190 * s, 250 * s, 834 * s, 774 * s
    radius = 64 * s

    # Finestra bianca; il menu laterale è ritagliato con la stessa forma per avere gli angoli giusti.
    window = Image.new("L", (size, size), 0)
    ImageDraw.Draw(window).rounded_rectangle([left, top, right, bottom], radius=radius, fill=255)
    d.bitmap((0, 0), window, fill=(255, 255, 255, 255))

    sidebar_w = 190 * s
    sidebar = Image.new("L", (size, size), 0)
    ImageDraw.Draw(sidebar).rounded_rectangle([left, top, left + sidebar_w + radius, bottom], radius=radius, fill=255)
    ImageDraw.Draw(sidebar).rectangle([left + sidebar_w, 0, size, size], fill=0)
    layer.paste(Image.new("RGBA", (size, size), SIDEBAR + (255,)), (0, 0), ImageChops.multiply(sidebar, window))

    # Voci del menu laterale.
    for i, y in enumerate((330, 410, 490)):
        w = 100 * s if i else 120 * s
        d.rounded_rectangle([left + 38 * s, y * s, left + 38 * s + w, (y + 28) * s], radius=14 * s,
                            fill=(255, 255, 255, 235 if i == 0 else 110))

    # Due righe di elenco: cerchio di spunta + riga di testo.
    content_left = left + sidebar_w + 56 * s
    for row, (y, done) in enumerate(((356, True), (540, False))):
        cy = y * s
        r = 44 * s
        cx = content_left + r
        if done:
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=ACCENT + (255,))
            d.line([(cx - 20 * s, cy + 2 * s), (cx - 6 * s, cy + 18 * s), (cx + 22 * s, cy - 16 * s)],
                   fill=(255, 255, 255, 255), width=int(13 * s), joint="curve")
        else:
            d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=(156, 163, 175, 255), width=int(10 * s))
        text_left = cx + r + 34 * s
        d.rounded_rectangle([text_left, cy - 20 * s, right - 56 * s, cy + 6 * s], radius=13 * s, fill=LINE + (255,))
        d.rounded_rectangle([text_left, cy + 24 * s, right - 150 * s, cy + 44 * s], radius=10 * s, fill=(229, 231, 235, 255))
    return layer


def master_icon():
    tile = background(MASTER)
    layer = window_layer(MASTER)

    # Ombra morbida sotto la finestra.
    shadow = layer.getchannel("A").filter(ImageFilter.GaussianBlur(MASTER * 0.02))
    shadow = shadow.point(lambda v: int(v * 0.4))
    shadow = ImageChops.offset(shadow, 0, int(MASTER * 0.015))
    tile = Image.composite(Image.new("RGBA", tile.size, (15, 12, 60, 255)), tile, shadow)

    tile.alpha_composite(layer)
    tile.putalpha(tile_mask(MASTER))
    return tile


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    ico_path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "assets", "app.ico")
    png_path = sys.argv[2] if len(sys.argv) > 2 else None

    master = master_icon()
    images = [master.resize((n, n), Image.LANCZOS) for n in SIZES]
    # Formato BMP (non PNG compresso): massima compatibilità con il decodificatore di icone di WPF.
    images[0].save(ico_path, format="ICO", sizes=[(n, n) for n in SIZES], append_images=images[1:], bitmap_format="bmp")
    if png_path:
        master.save(png_path)
    print("Icona scritta in", os.path.abspath(ico_path))


if __name__ == "__main__":
    main()
