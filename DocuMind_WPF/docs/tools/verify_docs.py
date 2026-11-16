"""Check local links, DOCX navigation/style and final render inventory."""
from pathlib import Path
import re
from zipfile import ZipFile
from xml.etree import ElementTree as ET
from pypdf import PdfReader

root = Path(__file__).resolve().parents[1]
ns = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
checked = 0
for source in root.glob("*.md"):
    for match in re.finditer(r"\[[^\]]+\]\(([^)]+)\)", source.read_text(encoding="utf-8")):
        url = match[1]
        if "://" not in url and not url.startswith("#"):
            path = (source.parent / url.split("#")[0]).resolve()
            assert path.exists(), (source.name, url)
            checked += 1
for stem, render in [("01_Setup_va_Code", "code_review"), ("02_Luong_hoat_dong", "flow_review")]:
    with ZipFile(root / (stem + ".docx")) as z:
        body = ET.fromstring(z.read("word/document.xml"))
        styles = ET.fromstring(z.read("word/styles.xml"))
        assert not styles.findall(".//w:pBdr", ns), "Inherited paragraph border"
        assert not body.findall(".//w:pBdr", ns), "Direct paragraph border"
        key = "{" + ns["w"] + "}"
        anchors = {b.get(key + "name") for b in body.findall(".//w:bookmarkStart", ns)}
        links = [h.get(key + "anchor") for h in body.findall(".//w:hyperlink", ns) if h.get(key + "anchor")]
        assert all(target in anchors for target in links), "Broken TOC target"
        assert len(links) == len(re.findall(r"^## ", (root / (stem + ".md")).read_text(encoding="utf-8"), re.M))
    pdf = root / ".qa" / render / (stem + ".pdf")
    if pdf.exists():
        pages = PdfReader(pdf).pages
        for i, page in enumerate(pages, 1):
            assert (pdf.parent / f"page-{i}.png").exists(), "Missing page PNG"
            assert len(page.extract_text()) > 100, "Unexpected nearly empty page"
        print(f"{stem}: {len(pages)} pages, navigation/style/render inventory PASS")
print(f"{checked} local document/image/source links PASS")
