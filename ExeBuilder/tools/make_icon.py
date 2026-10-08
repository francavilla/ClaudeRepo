"""
Genera l'icona di ExeBuilder (src/ExeBuilder/Assets/ExeBuilder.ico).

Quadrato con angoli arrotondati, sfumato negli stessi blu dell'intestazione dell'app
(#1E3A8A -> #2563EB), con una chiave inglese bianca. L'icona viene disegnata a 1024 px
e ridotta con LANCZOS a ogni misura del file .ico (16-256 px), così resta nitida ovunque.

Uso:  python make_icon.py [percorso_ico] [percorso_png_1024]
Richiede: Pillow (pip install pillow)
"""
import math
import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

MASTER = 1024
SIZES = [256, 128, 64, 48, 40, 32, 24, 20, 16]
TOP_LEFT = (0x1E, 0x3A, 0x8A)
BOTTOM_RIGHT = (0x25, 0x63, 0xEB)


def lerp(a, b, t):
    return tuple(int(round(x + (y - x) * t)) for x, y in zip(a, b))


def background(size):
    # Sfumatura diagonale dall'angolo in alto a sinistra a quello in basso a destra.
    gradient = Image.new("RGB", (size, size))
    pixels = gradient.load()
    for y in range(size):
        for x in range(size):
            pixels[x, y] = lerp(TOP_LEFT, BOTTOM_RIGHT, (x + y) / (2.0 * (size - 1)))

    margin = int(size * 0.04)
    radius = int(size * 0.22)
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle([margin, margin, size - margin - 1, size - margin - 1], radius=radius, fill=255)

    tile = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    tile.paste(gradient, (0, 0), mask)

    # Luce morbida dall'alto (sfuma a zero verso il centro) per dare profondità.
    fade = int(size * 0.6)
    column = Image.new("L", (1, size), 0)
    for y in range(size):
        column.putpixel((0, y), max(0, int(42 * (1 - y / float(fade)))))
    highlight = ImageChops.multiply(column.resize((size, size)), mask)
    white = Image.new("RGBA", (size, size), (255, 255, 255, 255))
    tile = Image.composite(white, tile, highlight).copy()
    tile.putalpha(mask)
    return tile


def wrench_mask(size):
    """Chiave inglese a bocca aperta, inclinata a 45 gradi, come maschera L (255 = bianco)."""
    s = size / 1024.0
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)

    head = (655 * s, 365 * s)          # centro della testa
    head_r = 205 * s                   # raggio della testa
    handle_end = (318 * s, 702 * s)    # estremità del manico
    handle_w = 150 * s                 # larghezza del manico
    jaw_w = 150 * s                    # apertura della bocca

    # Manico con estremità arrotondata.
    draw.line([head, handle_end], fill=255, width=int(handle_w))
    r = handle_w / 2
    draw.ellipse([handle_end[0] - r, handle_end[1] - r, handle_end[0] + r, handle_end[1] + r], fill=255)

    # Testa.
    draw.ellipse([head[0] - head_r, head[1] - head_r, head[0] + head_r, head[1] + head_r], fill=255)

    # Bocca: canale dal centro della testa verso l'alto a destra, con fondo arrotondato.
    ux, uy = 1 / math.sqrt(2), -1 / math.sqrt(2)   # direzione verso l'alto a destra
    px, py = -uy, ux                               # perpendicolare
    half = jaw_w / 2
    far = head_r * 1.6
    jaw = [
        (head[0] + px * half, head[1] + py * half),
        (head[0] + px * half + ux * far, head[1] + py * half + uy * far),
        (head[0] - px * half + ux * far, head[1] - py * half + uy * far),
        (head[0] - px * half, head[1] - py * half),
    ]
    draw.polygon(jaw, fill=0)
    draw.ellipse([head[0] - half, head[1] - half, head[0] + half, head[1] + half], fill=0)

    # Foro sull'estremità del manico.
    hole = 34 * s
    draw.ellipse([handle_end[0] - hole, handle_end[1] - hole, handle_end[0] + hole, handle_end[1] + hole], fill=0)
    return mask


def master_icon():
    tile = background(MASTER)
    mask = wrench_mask(MASTER)

    # Ombra morbida sotto la chiave.
    shadow = mask.filter(ImageFilter.GaussianBlur(MASTER * 0.018))
    shadow = shadow.point(lambda v: int(v * 0.35))
    shadow = ImageChops.offset(shadow, 0, int(MASTER * 0.012))
    tile = Image.composite(Image.new("RGBA", tile.size, (8, 20, 60, 255)), tile, shadow)

    tile = Image.composite(Image.new("RGBA", tile.size, (255, 255, 255, 255)), tile, mask)
    # Ripristina la trasparenza fuori dal quadrato arrotondato.
    tile.putalpha(background(MASTER).getchannel("A"))
    return tile


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    ico_path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "src", "ExeBuilder", "Assets", "ExeBuilder.ico")
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
