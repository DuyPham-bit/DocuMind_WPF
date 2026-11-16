"""Build the current Vietnamese project specification from its Markdown source.

Requires python-docx. Render and visually verify the result before publishing.
The historical build_docs.py remains dedicated to the original manuals.
"""
from pathlib import Path
import re
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "07_Dac_ta_du_an.md"
OUTPUT = SOURCE.with_suffix(".docx")


def inline(paragraph, value):
    for part in re.split(r"(`[^`]+`|\*\*[^*]+\*\*)", value):
        run = paragraph.add_run(part.strip("`") if part.startswith("`") else part.strip("*") if part.startswith("**") else part)
        if part.startswith("`"):
            run.font.name = "Consolas"
            run.font.size = Pt(9)
        elif part.startswith("**"):
            run.bold = True


def make_table(document, lines):
    records = [[cell.strip() for cell in line.strip().strip("|").split("|")] for line in lines]
    records = [row for row in records if not all(re.fullmatch(r"[-: ]+", cell) for cell in row)]
    count = len(records[0])
    # Key columns remain narrower than explanations and source paths.
    weights = [0.15, 0.31, 0.54] if count == 3 else [0.13, 0.25, 0.32, 0.30] if count == 4 else [0.30, 0.70]
    if records[0][0] == "Project":
        weights = [0.27, 0.45, 0.28]
    elif records[0][0] == "Thành phần":
        weights = [0.29, 0.35, 0.36]
    table = document.add_table(rows=0, cols=count)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    for col, weight in zip(table.columns, weights):
        col.width = Cm(17 * weight)
    for index, values in enumerate(records):
        cells = table.add_row().cells
        for cell, value, weight in zip(cells, values, weights):
            cell.width = Cm(17 * weight)
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            props = cell._tc.get_or_add_tcPr()
            shading = OxmlElement("w:shd")
            shading.set(qn("w:fill"), "24445C" if index == 0 else "F3F6F8" if index % 2 == 0 else "FFFFFF")
            props.append(shading)
            margins = OxmlElement("w:tcMar")
            for edge in ("top", "left", "bottom", "right"):
                item = OxmlElement("w:" + edge)
                item.set(qn("w:w"), "85")
                item.set(qn("w:type"), "dxa")
                margins.append(item)
            props.append(margins)
            paragraph = cell.paragraphs[0]
            paragraph.paragraph_format.space_after = Pt(2)
            paragraph.paragraph_format.space_before = Pt(2)
            paragraph.paragraph_format.line_spacing = 1.04
            inline(paragraph, value)
            for run in paragraph.runs:
                run.font.size = Pt(9.5)
                run.bold = index == 0
                run.font.color.rgb = RGBColor.from_string("FFFFFF" if index == 0 else "000000")
        row_props = table.rows[-1]._tr.get_or_add_trPr()
        row_props.append(OxmlElement("w:cantSplit"))
        if index == 0:
            row_props.append(OxmlElement("w:tblHeader"))
    borders = OxmlElement("w:tblBorders")
    for side in ("top", "left", "bottom", "right", "insideH", "insideV"):
        edge = OxmlElement("w:" + side)
        for key, value in (("val", "single"), ("sz", "4"), ("color", "D9D9D9")):
            edge.set(qn("w:" + key), value)
        borders.append(edge)
    table._tbl.tblPr.append(borders)
    document.add_paragraph().paragraph_format.space_after = Pt(2)


def build():
    document = Document()
    section = document.sections[0]
    section.page_width, section.page_height = Cm(21), Cm(29.7)
    section.top_margin, section.bottom_margin = Cm(1.9), Cm(1.8)
    section.left_margin = section.right_margin = Cm(2)
    section.footer_distance = Cm(0.8)
    # The bundled Word template may contain a blue paragraph rule on Title.
    for style in document.styles:
        for border in style.element.xpath(".//w:pBdr"):
            border.getparent().remove(border)
    for name in ("Normal", "Title", "Heading 1", "Heading 2", "Heading 3"):
        style = document.styles[name]
        style.font.name = "Calibri"
        style.font.color.rgb = RGBColor(0, 0, 0)
        style.font.size = Pt({"Normal": 11, "Title": 24, "Heading 1": 15, "Heading 2": 12.5, "Heading 3": 11}[name])
        style.paragraph_format.space_after = Pt(6)
        style.paragraph_format.line_spacing = 1.1
    for name in ("Heading 1", "Heading 2", "Heading 3"):
        document.styles[name].paragraph_format.space_before = Pt(12)
        document.styles[name].paragraph_format.keep_with_next = True
    footer = section.footer.paragraphs[0]
    footer.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    footer.add_run("DocuMind   |   ").font.size = Pt(9)
    field = OxmlElement("w:fldSimple")
    field.set(qn("w:instr"), "PAGE")
    footer._p.append(field)
    document.core_properties.author = "BossTom0912"
    document.core_properties.title = "Đặc tả đầy đủ dự án DocuMind WPF"
    document.core_properties.subject = "Đặc tả chức năng thiết kế kỹ thuật vận hành và nghiệm thu"
    lines = SOURCE.read_text(encoding="utf-8").splitlines()
    index = 0
    in_code = False
    toc_added = False
    while index < len(lines):
        line = lines[index]
        if line.startswith("## ") and not toc_added:
            document.add_heading("Nội dung", level=1)
            for item in lines:
                if item.startswith("## "):
                    paragraph = document.add_paragraph(item[3:])
                    paragraph.paragraph_format.space_after = Pt(1)
                    paragraph.paragraph_format.line_spacing = 1
                    for run in paragraph.runs:
                        run.font.size = Pt(10)
            document.add_page_break()
            toc_added = True
        if line.startswith("```"):
            in_code = not in_code
        elif in_code:
            paragraph = document.add_paragraph(line)
            paragraph.paragraph_format.space_after = Pt(3)
            paragraph.paragraph_format.line_spacing = 1
            paragraph.paragraph_format.keep_with_next = index + 1 < len(lines) and not lines[index + 1].startswith("```")
            for run in paragraph.runs:
                run.font.name = "Consolas"
                run.font.size = Pt(8.5)
        elif line.startswith("|"):
            rows = []
            while index < len(lines) and lines[index].startswith("|"):
                rows.append(lines[index])
                index += 1
            make_table(document, rows)
            continue
        elif line.startswith("# "):
            document.add_paragraph(line[2:], style="Title")
        elif line.startswith("### "):
            document.add_heading(line[4:], level=2)
        elif line.startswith("## "):
            document.add_heading(line[3:], level=1)
        elif line.strip():
            paragraph = document.add_paragraph()
            inline(paragraph, line)
            if re.match(r"\d+\. ", line):
                paragraph.paragraph_format.left_indent = Cm(0.45)
                paragraph.paragraph_format.first_line_indent = Cm(-0.45)
            paragraph.paragraph_format.widow_control = True
        index += 1
    document.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    build()
