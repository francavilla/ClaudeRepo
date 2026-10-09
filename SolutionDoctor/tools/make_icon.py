"""
Genera l'icona di SolutionDoctor (src/SolutionDoctor.App/Assets/SolutionDoctor.ico).

Quadrato con angoli arrotondati, sfumato negli stessi verdi acqua dell'intestazione dell'app
(#134E4A -> #0D9488), con una croce medica bianca e una linea di battito che l'attraversa.
L'icona viene disegnata a 1024 px e ridotta con LANCZOS a ogni misura del file .ico (16-256 px),
così resta nitida ovunque.

Uso:  python make_icon.py [percorso_ico] [percorso_png_1024]
Richiede: Pillow (pip install pillow)
"""
import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

MASTER = 1024
SIZES = [256, 128, 64, 48, 40, 32, 24, 20, 16]
TOP_LEFT = (0x13, 0x4E, 0x4A)
BOTTOM_RIGHT = (0x0D, 0x94, 0x88)


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


def symbol_mask(size):
    """Croce medica con angoli arrotondati; nel braccio orizzontale un battito, scavato nella croce."""
    s = size / 1024.0
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)

    centre = 512 * s
    arm = 150 * s          # metà larghezza di un braccio
    length = 300 * s       # distanza dal centro all'estremità del braccio
    radius = 60 * s
    draw.rounded_rectangle([centre - arm, centre - length, centre + arm, centre + length], radius=radius, fill=255)
    draw.rounded_rectangle([centre - length, centre - arm, centre + length, centre + arm], radius=radius, fill=255)

    # Linea di battito (ECG) scavata nella croce: tratto piatto, picco in su, picco in giù, tratto piatto.
    points = [(-length + 40, 0), (-110, 0), (-60, -120), (0, 110), (50, -40), (95, 0), (length - 40, 0)]
    path = [(centre + x * s, centre + y * s) for x, y in points]
    draw.line(path, fill=0, width=max(2, int(34 * s)), joint="curve")
    return mask


def master_icon():
    tile = background(MASTER)
    mask = symbol_mask(MASTER)

    # Ombra morbida sotto il simbolo.
    shadow = mask.filter(ImageFilter.GaussianBlur(MASTER * 0.018))
    shadow = shadow.point(lambda v: int(v * 0.35))
    shadow = ImageChops.offset(shadow, 0, int(MASTER * 0.012))
    tile = Image.composite(Image.new("RGBA", tile.size, (4, 40, 36, 255)), tile, shadow)

    tile = Image.composite(Image.new("RGBA", tile.size, (255, 255, 255, 255)), tile, mask)
    # Ripristina la trasparenza fuori dal quadrato arrotondato.
    tile.putalpha(background(MASTER).getchannel("A"))
    return tile


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    ico_path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "src", "SolutionDoctor.App", "Assets", "SolutionDoctor.ico")
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
