"""
Genera los PDF de la documentacion a partir de los .md de docs/.

Uso:
    python docs/generar-pdf.py

Requiere:  pip install xhtml2pdf markdown
Salida:    docs/pdf/*.pdf
"""
import os
import sys
from datetime import date

import markdown
from xhtml2pdf import pisa

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, "pdf")

DOCS = [
    ("PUESTA_EN_MARCHA.md", "Puesta en marcha - ERP Galeria ACATRAS"),
    ("MANUAL_DE_USO.md", "Manual de uso - ERP Galeria ACATRAS"),
]

CSS = """
@page {
    size: a4;
    margin: 2.2cm 2cm 2cm 2cm;
    @frame footer {
        -pdf-frame-content: footerContent;
        bottom: 1cm; margin-left: 2cm; margin-right: 2cm; height: 1cm;
    }
}
body { font-family: "Helvetica", sans-serif; font-size: 10.5pt; line-height: 1.45; color: #1a1a1a; }
h1 { font-size: 20pt; color: #17324d; border-bottom: 2px solid #17324d; padding-bottom: 6pt; margin-top: 0; }
h2 { font-size: 14pt; color: #17324d; margin-top: 20pt; border-bottom: 1px solid #c9d4de; padding-bottom: 3pt; }
h3 { font-size: 11.5pt; color: #2b4a66; margin-top: 14pt; }
p, li { text-align: left; }
code { font-family: "Courier", monospace; font-size: 9pt; background: #eef2f5; }
pre { font-family: "Courier", monospace; font-size: 8.5pt; background: #f4f6f8;
      border: 1px solid #d6dde3; padding: 6pt; }
pre code { background: transparent; }
table { -pdf-keep-with-next: true; border: 0.5pt solid #c9d4de; margin: 8pt 0; width: 100%; }
th { background: #17324d; color: #ffffff; padding: 4pt 6pt; text-align: left; font-size: 9.5pt; }
td { padding: 4pt 6pt; border-bottom: 0.5pt solid #d6dde3; font-size: 9.5pt; vertical-align: top; }
blockquote { color: #4a4a4a; border-left: 3pt solid #b7c3cd; padding-left: 8pt; margin-left: 0; }
hr { border: 0; border-top: 0.5pt solid #c9d4de; }
"""


def convertir(md_name: str, titulo: str) -> None:
    ruta_md = os.path.join(AQUI, md_name)
    with open(ruta_md, encoding="utf-8") as f:
        texto = f.read()

    cuerpo = markdown.markdown(
        texto, extensions=["tables", "fenced_code", "sane_lists", "toc"]
    )
    hoy = date.today().strftime("%d/%m/%Y")
    html = f"""<!DOCTYPE html><html><head><meta charset="utf-8">
<style>{CSS}</style></head><body>
<div id="footerContent" style="font-size:8pt; color:#7a7a7a; text-align:center;">
  {titulo} &nbsp;&middot;&nbsp; {hoy} &nbsp;&middot;&nbsp; pag. <pdf:pagenumber> / <pdf:pagecount>
</div>
{cuerpo}
</body></html>"""

    os.makedirs(SALIDA, exist_ok=True)
    ruta_pdf = os.path.join(SALIDA, md_name.replace(".md", ".pdf"))
    with open(ruta_pdf, "wb") as f:
        resultado = pisa.CreatePDF(html, dest=f, encoding="utf-8")
    if resultado.err:
        print(f"  ERROR generando {ruta_pdf}", file=sys.stderr)
        sys.exit(1)
    print(f"  OK  {ruta_pdf}")


if __name__ == "__main__":
    print("Generando PDF en docs/pdf/ ...")
    for nombre, titulo in DOCS:
        convertir(nombre, titulo)
    print("Listo.")
