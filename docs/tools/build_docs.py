"""Build the two DOCX manuals from Markdown and draw deterministic diagrams.

Requirements: python-docx, Pillow. No network, AI API, or database is accessed.
This intentionally supports only the Markdown constructs used by these manuals.
"""
from pathlib import Path
import json
import math
import re
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.opc.constants import RELATIONSHIP_TYPE as RT

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"
QA = ROOT / ".qa"
ASSETS.mkdir(exist_ok=True)
QA.mkdir(exist_ok=True)
FONT = Path("C:/Windows/Fonts/segoeui.ttf")
BOLD = Path("C:/Windows/Fonts/segoeuib.ttf")
INK = "#182C40"
BLUE = "#315D7D"
GRAY = "#CBD5DF"
FILL = "#F0F5F8"


def font(size=32, bold=False):
    return ImageFont.truetype(str(BOLD if bold else FONT), size)


def wrapped(draw, text, width, size=32):
    result = []
    for line in text.split("\n"):
        current = ""
        for word in line.split():
            candidate = (current + " " + word).strip()
            if draw.textlength(candidate, font=font(size)) > width and current:
                result.append(current)
                current = word
            else:
                current = candidate
        result.append(current)
    return result


def label(draw, xy, text, width=400, size=32, bold=False, fill=INK, center=False, opaque=False):
    x, y = xy
    lines = wrapped(draw, text, width, size)
    for line in lines:
        px = x - draw.textlength(line, font=font(size, bold)) / 2 if center else x
        if opaque:
            bounds = draw.textbbox((px, y), line, font=font(size, bold))
            draw.rectangle((bounds[0] - 4, bounds[1] - 4, bounds[2] + 4, bounds[3] + 4), fill="white")
        draw.text((px, y), line, font=font(size, bold), fill=fill)
        y += size + 12
    return y


def arrow(draw, start, end, color=BLUE):
    draw.line([start, end], fill=color, width=5)
    angle = math.atan2(end[1] - start[1], end[0] - start[0])
    pts = [end]
    for delta in [-0.48, 0.48]:
        pts.append((end[0] - 19 * math.cos(angle + delta), end[1] - 19 * math.sin(angle + delta)))
    draw.polygon(pts, fill=color)


def box(draw, rect, title, body):
    draw.rounded_rectangle(rect, radius=16, fill=FILL, outline=GRAY, width=3)
    x1, y1, x2, y2 = rect
    y = label(draw, (x1 + 24, y1 + 22), title, x2 - x1 - 48, 36, True)
    label(draw, (x1 + 24, y + 10), body, x2 - x1 - 48, 31)


def architecture():
    im = Image.new("RGB", (1600, 940), "white")
    d = ImageDraw.Draw(im)
    box(d, (50, 40, 680, 230), "WPF", "Views, ViewModels, App và DI")
    box(d, (50, 350, 680, 570), "Application", "RAG, vector helper, DTO và interface")
    box(d, (900, 350, 1550, 570), "Infrastructure", "EF SQLite, PDF, HTTP, DPAPI")
    box(d, (480, 730, 1130, 900), "Domain", "Workspace, Chunk, Message, Setting")
    arrow(d, (350, 230), (350, 350))
    arrow(d, (680, 170), (1220, 350))
    arrow(d, (900, 460), (680, 460))
    arrow(d, (350, 570), (630, 730))
    arrow(d, (1220, 570), (1000, 730))
    d.line([(50, 160), (18, 160), (18, 815), (450, 815)], fill=BLUE, width=4)
    arrow(d, (450, 815), (480, 815))
    label(d, (55, 640), "WPF tham chiếu Domain trực tiếp", 620, 27)
    label(d, (1150, 650), "Phụ thuộc project", 410, 28)
    im.save(ASSETS / "architecture.png")


def erd():
    im = Image.new("RGB", (1600, 1060), "white")
    d = ImageDraw.Draw(im)
    box(d, (50, 50, 710, 370), "Workspaces", "PK Id Guid\nName, SystemPrompt, CreatedAt")
    box(d, (920, 50, 1550, 370), "ProviderSettings", "PK Id int\nProviderType unique\nIsActive, key, model, URL")
    box(d, (50, 660, 710, 1000), "DocumentChunks", "PK Id Guid; FK WorkspaceId\nFileName, PageNumber, Content\nEmbeddingJson, CreatedAt")
    box(d, (920, 660, 1550, 1000), "ChatMessages", "PK Id Guid; FK WorkspaceId\nRole, Content, SourcesJson\nTimestamp")
    arrow(d, (350, 370), (350, 660))
    label(d, (390, 460), "1 đến nhiều\nON DELETE CASCADE", 460, 29)
    arrow(d, (710, 310), (1150, 660))
    label(d, (1160, 440), "1 đến nhiều\nCASCADE", 370, 29)
    im.save(ASSETS / "erd.png")


def sequence(name, actors, steps):
    width = 1600
    row = 118
    height = 200 + row * len(steps)
    im = Image.new("RGB", (width, height), "white")
    d = ImageDraw.Draw(im)
    xs = [160 + i * (1280 / (len(actors) - 1)) for i in range(len(actors))]
    for x, actor in zip(xs, actors):
        d.rounded_rectangle((x - 145, 20, x + 145, 123), radius=12, fill=FILL, outline=GRAY, width=3)
        label(d, (x, 40), actor, 265, 29, True, center=True)
        for y in range(135, height - 25, 25):
            d.line((x, y, x, y + 11), fill=GRAY, width=3)
    for i, (a, b, text) in enumerate(steps):
        y = 206 + row * i
        xa, xb = xs[a], xs[b]
        if a == b:
            # A local operation uses a short self-call arrow.
            right = xa < width / 2
            xe = xa + (150 if right else -150)
            d.line([(xa, y), (xe, y), (xe, y + 22)], fill=BLUE, width=4)
            arrow(d, (xe, y + 22), (xa, y + 22))
            center = xa + (210 if right else -210)
            label(d, (center, y - 68), f"{i + 1} {text}", 520, 29, center=True, opaque=True)
        else:
            center = (xa + xb) / 2
            label(d, (center, y - 70), f"{i + 1} {text}", max(390, abs(xb - xa) - 20), 29, center=True, opaque=True)
            arrow(d, (xa, y + 18), (xb, y + 18))
    im.save(ASSETS / f"{name}.png")


def diagrams():
    architecture()
    erd()
    sequence("startup", ["App", "DI và DB", "Window", "ViewModel"], [
        (0, 1, "Đăng ký dependency"), (0, 1, "Scope và EnsureCreated"),
        (1, 1, "Schema mới và seed"), (0, 2, "Resolve và Show"),
        (2, 3, "Loaded gọi InitializeAsync"), (3, 1, "Đọc workspace và provider"),
        (3, 2, "Binding và lời chào")])
    sequence("settings", ["Dialog", "Settings VM", "Factory DB", "AI API"], [
        (0, 1, "Loaded và đọc cấu hình"), (1, 2, "Đọc active và key"),
        (0, 1, "Save hoặc Test"), (1, 2, "SwitchProviderAsync"),
        (2, 2, "SaveChanges và active"), (1, 3, "Nếu Test thì gửi Ping"),
        (3, 1, "true hoặc false"), (1, 0, "Status hoặc đóng khi Save")])
    sequence("ingest", ["UI và VM", "Orchestrator", "Parser DB", "AI API"], [
        (0, 1, "workspaceId và filePath"), (1, 2, "ParseFileAsync"),
        (1, 2, "Xóa chunks cũ và commit"), (1, 1, "Chunk 180 từ overlap 30"),
        (1, 3, "Embedding từng chunk"), (3, 1, "Vector float array"),
        (1, 2, "AddRange và SaveChanges"), (1, 0, "Refresh file và báo xong")])
    sequence("direct", ["UI và VM", "Orchestrator", "Factory DB", "AI API"], [
        (0, 0, "Guard và user bubble"), (0, 1, "ChatExternalAsync"),
        (1, 2, "Lấy active provider"), (1, 3, "System prompt và câu hỏi"),
        (3, 1, "Text câu trả lời"), (1, 0, "Assistant bubble")])
    sequence("rag", ["UI và VM", "Orchestrator", "Chunk DB", "AI API"], [
        (0, 1, "ChatWithRagAsync"), (1, 2, "Đọc toàn bộ chunks"),
        (1, 3, "Embedding câu hỏi"), (3, 1, "Query vector"),
        (1, 1, "Cosine và lọc trên 0.05"), (1, 1, "Top 4 hoặc fallback"),
        (1, 1, "Context và Sources"), (1, 3, "Augmented prompt"),
        (3, 1, "Answer"), (1, 0, "Answer cùng Sources")])
    sequence("manage", ["Người dùng", "ViewModel", "Service", "Chunk DB"], [
        (0, 1, "Chọn workspace"), (1, 2, "Load tên file theo ID"),
        (2, 3, "Distinct FileName"), (0, 1, "Xóa file và xác nhận"),
        (1, 2, "DeleteDocumentAsync"), (2, 3, "Xóa theo ID và fileName"),
        (1, 1, "Bỏ tên file và báo xong")])
    sequence("export", ["Người dùng", "ViewModel", "Collection", "Ổ đĩa"], [
        (0, 1, "Xóa chat"), (1, 2, "Clear và thêm thông báo"),
        (0, 1, "Export và chọn đường dẫn"), (1, 2, "Đọc từng message và nguồn"),
        (1, 1, "Dựng Markdown"), (1, 3, "WriteAllText UTF-8"),
        (1, 0, "Báo đường dẫn đã xuất")])


def plain(text):
    parts = []
    for token in re.split(r"(\[[^\]]+\]\([^)]+\)|\*\*.+?\*\*|`[^`]+`)", text):
        if token.startswith("`"):
            parts.append(token[1:-1])
        elif token.startswith("**"):
            parts.append(token[2:-2])
        elif token.startswith("[") and "](" in token:
            parts.append(re.fullmatch(r"\[([^\]]+)\]\(([^)]+)\)", token)[1])
        else:
            parts.append(token)
    return "".join(parts)


def inline(p, text):
    pattern = r"(\[[^\]]+\]\([^)]+\)|\*\*.+?\*\*|`[^`]+`)"
    for token in re.split(pattern, text):
        if not token:
            continue
        if token.startswith("[") and "](" in token:
            match = re.fullmatch(r"\[([^\]]+)\]\(([^)]+)\)", token)
            title, target = match.groups()
            rel = p.part.relate_to(target, RT.HYPERLINK, is_external=True)
            link = OxmlElement("w:hyperlink")
            link.set(qn("r:id"), rel)
            run = OxmlElement("w:r")
            props = OxmlElement("w:rPr")
            color = OxmlElement("w:color")
            color.set(qn("w:val"), "245575")
            props.append(color)
            run.append(props)
            node = OxmlElement("w:t")
            node.text = title
            run.append(node)
            link.append(run)
            p._p.append(link)
        else:
            run = p.add_run(token[2:-2] if token.startswith("**") else token[1:-1] if token.startswith("`") else token)
            if token.startswith("**"):
                run.bold = True
            elif token.startswith("`"):
                run.font.name = "Consolas"
                run.font.size = Pt(10)


def cell_format(cell, fill=None):
    cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
    tcpr = cell._tc.get_or_add_tcPr()
    margins = OxmlElement("w:tcMar")
    for name in ["top", "left", "bottom", "right"]:
        node = OxmlElement("w:" + name)
        node.set(qn("w:w"), "95" if name in ["top", "bottom"] else "115")
        node.set(qn("w:type"), "dxa")
        margins.append(node)
    tcpr.append(margins)
    if fill:
        node = OxmlElement("w:shd")
        node.set(qn("w:fill"), fill)
        tcpr.append(node)


def table(doc, rows):
    n = len(rows[0])
    t = doc.add_table(rows=0, cols=n)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    ratios = [0.26, 0.37, 0.37] if n == 3 else [0.29, 0.13, 0.58] if n == 4 else [1 / n] * n
    if n != len(ratios):
        ratios = [1 / n] * n
    for col, ratio in zip(t.columns, ratios):
        col.width = Inches(6.8 * ratio)
    props = t._tbl.tblPr
    borders = OxmlElement("w:tblBorders")
    for side in ["top", "left", "bottom", "right", "insideH", "insideV"]:
        node = OxmlElement("w:" + side)
        for attr, val in [("val", "single"), ("sz", "5"), ("color", "D9D9D9")]:
            node.set(qn("w:" + attr), val)
        borders.append(node)
    props.append(borders)
    for i, row in enumerate(rows):
        cells = t.add_row().cells
        for j, (cell, text) in enumerate(zip(cells, row)):
            cell.width = Inches(6.8 * ratios[j])
            cell_format(cell, "DDE8F0" if i == 0 else "F5F8FA" if i % 2 == 0 else "FFFFFF")
            p = cell.paragraphs[0]
            p.paragraph_format.space_after = Pt(1)
            p.paragraph_format.space_before = Pt(1)
            p.paragraph_format.line_spacing = 1.08
            inline(p, text)
            for r in p.runs:
                r.font.size = Pt(9.5)
                r.bold = i == 0 or r.bold
            if i == 0:
                p.paragraph_format.keep_with_next = True
        if i == 0:
            repeat = OxmlElement("w:tblHeader")
            t.rows[i]._tr.get_or_add_trPr().append(repeat)
        avoid = OxmlElement("w:cantSplit")
        t.rows[i]._tr.get_or_add_trPr().append(avoid)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)


def build(source, flow=False):
    text = source.read_text(encoding="utf-8")
    lines = text.splitlines()
    doc = Document()
    # The bundled blank template contains a blue border in its Title style.
    # Remove paragraph borders from all styles so none are inherited.
    for border in list(doc.styles.element.iter(qn("w:pBdr"))):
        border.getparent().remove(border)
    sec = doc.sections[0]
    sec.page_width, sec.page_height = Inches(8.5), Inches(11)
    sec.top_margin, sec.bottom_margin = Inches(0.7), Inches(0.65)
    sec.left_margin, sec.right_margin = Inches(0.85), Inches(0.85)
    for name in ["Normal", "Title", "Subtitle", "Heading 1", "Heading 2", "Heading 3"]:
        style = doc.styles[name]
        style.font.name = "Calibri"
        style.font.color.rgb = RGBColor(0, 0, 0)
        style.paragraph_format.space_after = Pt(7)
        if name.startswith("Heading"):
            style.paragraph_format.keep_with_next = True
            style.paragraph_format.space_before = Pt(13)
    doc.styles["Normal"].font.size = Pt(11)
    doc.styles["Normal"].paragraph_format.line_spacing = 1.14
    doc.styles["Title"].font.size = Pt(27)
    doc.styles["Title"].paragraph_format.space_after = Pt(18)
    doc.styles["Heading 1"].font.size = Pt(16)
    doc.styles["Heading 2"].font.size = Pt(12.5)
    doc.styles["Heading 3"].font.size = Pt(11.5)
    doc.styles["Caption"].font.color.rgb = RGBColor(0, 0, 0)
    doc.styles["Caption"].font.size = Pt(9.5)
    ref = doc.styles.add_style("Reference", 1)
    ref.base_style = doc.styles["Normal"]
    ref.font.size = Pt(10)
    ref.paragraph_format.space_after = Pt(3)
    ref.paragraph_format.line_spacing = 1.05
    footer = sec.footer.paragraphs[0]
    footer.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    r = footer.add_run("DocuMind  |  ")
    r.font.size = Pt(9)
    field = OxmlElement("w:fldSimple")
    field.set(qn("w:instr"), "PAGE")
    footer._p.append(field)
    doc.core_properties.title = plain(lines[0][2:])
    doc.core_properties.author = "Nhóm DocuMind"
    doc.core_properties.subject = "Tài liệu kỹ thuật baseline 355df3d"
    toc_done = False
    chapter = 0
    i = 0
    while i < len(lines):
        line = lines[i]
        if not line.strip():
            i += 1
            continue
        if line.startswith("```"):
            i += 1
            while i < len(lines) and not lines[i].startswith("```"):
                p = doc.add_paragraph()
                p.paragraph_format.space_after = Pt(0)
                p.paragraph_format.line_spacing = 1.05
                p.paragraph_format.left_indent = Inches(0.12)
                r = p.add_run(lines[i] or " ")
                r.font.name = "Consolas"
                r.font.size = Pt(9.2)
                node = OxmlElement("w:shd")
                node.set(qn("w:fill"), "F3F5F7")
                p._p.get_or_add_pPr().append(node)
                i += 1
            i += 1
            doc.add_paragraph().paragraph_format.space_after = Pt(1)
            continue
        if line.startswith("| "):
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                parts = [x.strip() for x in lines[i].strip().strip("|").split("|")]
                if not all(re.fullmatch(r":?-+:?", x.replace(" ", "")) for x in parts):
                    rows.append(parts)
                i += 1
            table(doc, rows)
            continue
        image = re.fullmatch(r"!\[([^\]]+)\]\(([^)]+)\)", line)
        if image:
            p = doc.add_paragraph()
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p.add_run().add_picture(str(ROOT / image.group(2)), width=Inches(6.65))
            p.paragraph_format.keep_with_next = True
            caption = doc.add_paragraph(image.group(1), "Caption")
            caption.paragraph_format.space_after = Pt(10)
            caption.alignment = WD_ALIGN_PARAGRAPH.CENTER
            i += 1
            continue
        heading = re.match(r"^(#{1,4}) (.+)$", line)
        if heading:
            depth = len(heading[1])
            if depth == 2 and not toc_done:
                doc.add_heading("Mục lục", level=1)
                for number, item in enumerate(re.findall(r"^## (.+)$", text, re.M), 1):
                    p = doc.add_paragraph()
                    link = OxmlElement("w:hyperlink")
                    link.set(qn("w:anchor"), f"chapter_{number}")
                    run = OxmlElement("w:r")
                    props = OxmlElement("w:rPr")
                    size = OxmlElement("w:sz")
                    size.set(qn("w:val"), "21")
                    props.append(size)
                    run.append(props)
                    content = OxmlElement("w:t")
                    content.text = item
                    run.append(content)
                    link.append(run)
                    p._p.append(link)
                    p.paragraph_format.space_after = Pt(3)
                toc_done = True
                doc.add_page_break()
            p = doc.add_paragraph(style="Title" if depth == 1 else f"Heading {depth - 1}")
            if depth == 2:
                chapter += 1
                start = OxmlElement("w:bookmarkStart")
                start.set(qn("w:id"), str(chapter))
                start.set(qn("w:name"), f"chapter_{chapter}")
                p._p.append(start)
            inline(p, heading[2])
            if depth == 2:
                end = OxmlElement("w:bookmarkEnd")
                end.set(qn("w:id"), str(chapter))
                p._p.append(end)
            i += 1
            continue
        numbered = re.match(r"^(\d+)\. (.*)$", line)
        bullet = line.startswith("- ")
        if numbered or bullet:
            p = doc.add_paragraph(style="Reference" if line.startswith("- [S") else "Normal")
            p.paragraph_format.left_indent = Inches(0.17)
            p.paragraph_format.first_line_indent = Inches(-0.17)
            inline(p, (numbered[1] + ". " + numbered[2]) if numbered else "• " + line[2:])
            i += 1
            continue
        para = [line]
        i += 1
        while i < len(lines) and lines[i].strip() and not re.match(r"^(#|\||```|!\[|- |\d+\. )", lines[i]):
            para.append(lines[i])
            i += 1
        p = doc.add_paragraph()
        inline(p, " ".join(para))
    out = source.with_suffix(".docx")
    doc.save(out)
    # Compare every source paragraph/cell/code line against OOXML text in order.
    from zipfile import ZipFile
    import xml.etree.ElementTree as ET
    with ZipFile(out) as z:
        tree = ET.fromstring(z.read("word/document.xml"))
    full = "".join(node.text or "" for node in tree.iter(qn("w:t")))
    normalized = re.sub(r"\s+", "", full)
    missing = []
    for line in lines:
        if not line.strip() or line.startswith(("```", "![", "| ---")):
            continue
        parts = [x.strip() for x in line.strip("|").split("|")] if line.startswith("|") else [re.sub(r"^#{1,4} |^- ", "", line)]
        for part in parts:
            expected = re.sub(r"\s+", "", plain(part))
            if expected and expected not in normalized:
                missing.append(expected)
    if missing:
        raise AssertionError("DOCX missing source content: " + repr(missing[:5]))
    print(f"Built {out.name}; source content check PASS; {len(doc.inline_shapes)} diagrams")


if __name__ == "__main__":
    diagrams()
    build(ROOT / "01_Setup_va_Code.md")
    build(ROOT / "02_Luong_hoat_dong.md", flow=True)
