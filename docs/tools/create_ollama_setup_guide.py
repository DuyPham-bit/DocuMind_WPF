from pathlib import Path
from docx import Document
from docx.enum.table import WD_ALIGN_VERTICAL
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor


OUTPUT = Path("docs/Huong_dan_cai_Ollama_DocuMind.docx")


def set_font(run, size=None, bold=None, color=None):
    run.font.name = "Aptos"
    run._element.rPr.rFonts.set(qn("w:ascii"), "Aptos")
    run._element.rPr.rFonts.set(qn("w:hAnsi"), "Aptos")
    if size:
        run.font.size = Pt(size)
    if bold is not None:
        run.bold = bold
    if color:
        run.font.color.rgb = RGBColor(*color)


def shade(cell, fill):
    props = cell._tc.get_or_add_tcPr()
    element = OxmlElement("w:shd")
    element.set(qn("w:fill"), fill)
    props.append(element)


def set_cell_margins(cell, top=100, start=130, bottom=100, end=130):
    props = cell._tc.get_or_add_tcPr()
    margins = props.first_child_found_in("w:tcMar")
    if margins is None:
        margins = OxmlElement("w:tcMar")
        props.append(margins)
    for side, value in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = margins.find(qn(f"w:{side}"))
        if node is None:
            node = OxmlElement(f"w:{side}")
            margins.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_cell_border(cell, color="D9D9D9"):
    props = cell._tc.get_or_add_tcPr()
    borders = props.first_child_found_in("w:tcBorders")
    if borders is None:
        borders = OxmlElement("w:tcBorders")
        props.append(borders)
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        tag = qn(f"w:{edge}")
        node = borders.find(tag)
        if node is None:
            node = OxmlElement(f"w:{edge}")
            borders.append(node)
        node.set(qn("w:val"), "single")
        node.set(qn("w:sz"), "6")
        node.set(qn("w:space"), "0")
        node.set(qn("w:color"), color)


def add_text(paragraph, text, bold=False, size=11):
    run = paragraph.add_run(text)
    set_font(run, size=size, bold=bold, color=(0, 0, 0))
    return run


def add_body(doc, text):
    paragraph = doc.add_paragraph()
    paragraph.paragraph_format.space_after = Pt(7)
    paragraph.paragraph_format.line_spacing = 1.15
    add_text(paragraph, text)
    return paragraph


def add_heading(doc, text, level=1):
    paragraph = doc.add_paragraph(style=f"Heading {level}")
    paragraph.paragraph_format.space_before = Pt(14 if level == 1 else 9)
    paragraph.paragraph_format.space_after = Pt(6)
    run = paragraph.add_run(text)
    set_font(run, size=15 if level == 1 else 12, bold=True, color=(0, 0, 0))
    return paragraph


def add_bullet(doc, text):
    paragraph = doc.add_paragraph(style="List Bullet")
    paragraph.paragraph_format.space_after = Pt(3)
    paragraph.paragraph_format.line_spacing = 1.1
    add_text(paragraph, text)
    return paragraph


def add_number(doc, text):
    paragraph = doc.add_paragraph(style="List Number")
    paragraph.paragraph_format.space_after = Pt(4)
    paragraph.paragraph_format.line_spacing = 1.1
    add_text(paragraph, text)
    return paragraph


def add_table(doc, headers, rows, widths=None):
    table = doc.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    table.autofit = False
    header = table.rows[0]
    for index, text in enumerate(headers):
        cell = header.cells[index]
        if widths:
            cell.width = Inches(widths[index])
        shade(cell, "17365D")
        set_cell_margins(cell)
        set_cell_border(cell)
        cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
        paragraph = cell.paragraphs[0]
        paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
        add_text(paragraph, text, bold=True, size=10)
        for run in paragraph.runs:
            run.font.color.rgb = RGBColor(255, 255, 255)

    for row_index, row in enumerate(rows):
        cells = table.add_row().cells
        for index, text in enumerate(row):
            cell = cells[index]
            if widths:
                cell.width = Inches(widths[index])
            if row_index % 2 == 1:
                shade(cell, "F3F7FB")
            set_cell_margins(cell)
            set_cell_border(cell)
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            paragraph = cell.paragraphs[0]
            paragraph.paragraph_format.space_after = Pt(0)
            paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER if index == 0 and len(headers) > 2 else WD_ALIGN_PARAGRAPH.LEFT
            add_text(paragraph, text, size=10)
    for row in table.rows:
        row._tr.get_or_add_trPr().append(OxmlElement("w:cantSplit"))
    doc.add_paragraph().paragraph_format.space_after = Pt(3)
    return table


def create_document():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    doc = Document()
    section = doc.sections[0]
    section.top_margin = Inches(0.72)
    section.bottom_margin = Inches(0.7)
    section.left_margin = Inches(0.78)
    section.right_margin = Inches(0.78)

    normal = doc.styles["Normal"]
    normal.font.name = "Aptos"
    normal._element.rPr.rFonts.set(qn("w:ascii"), "Aptos")
    normal._element.rPr.rFonts.set(qn("w:hAnsi"), "Aptos")
    normal.font.size = Pt(11)

    title_style = doc.styles["Title"]
    title_style.base_style = normal
    title_style.font.name = "Aptos"
    title_style._element.rPr.rFonts.set(qn("w:ascii"), "Aptos")
    title_style._element.rPr.rFonts.set(qn("w:hAnsi"), "Aptos")
    title_style.font.color.rgb = RGBColor(0, 0, 0)
    title_style_properties = title_style._element.get_or_add_pPr()
    for border in title_style_properties.findall(qn("w:pBdr")):
        title_style_properties.remove(border)

    title = doc.add_paragraph(style="Title")
    title.alignment = WD_ALIGN_PARAGRAPH.LEFT
    title.paragraph_format.space_after = Pt(9)
    title_run = title.add_run("Hướng Dẫn Cài Ollama Local Cho DocuMind")
    set_font(title_run, size=22, bold=True, color=(0, 0, 0))

    intro = doc.add_paragraph()
    intro.paragraph_format.space_after = Pt(12)
    intro.paragraph_format.line_spacing = 1.15
    add_text(intro, "Tài liệu này hướng dẫn từng thành viên chạy file cài đặt tự động, sau đó kết nối DocuMind với Ollama trên chính máy của mình. Ollama local không dùng API key trả phí: trong DocuMind, ô API Key phải để trống.", bold=False, size=11)

    add_heading(doc, "Kết Quả Sau Khi Hoàn Thành")
    for item in (
        "Ollama bản ổn định mới nhất đã được cài trên máy Windows.",
        "EXE đã tự chọn và tải model theo RAM, GPU và ổ đĩa còn trống.",
        "DocuMind chat và tạo embedding bằng API local http://localhost:11434.",
        "Không cần nạp tiền OpenRouter và không cần dùng API key cho provider OllamaLocal.",
    ):
        add_bullet(doc, item)

    add_heading(doc, "Chuẩn Bị")
    add_body(doc, "Cần Windows 10 trở lên, Internet ổn định trong lúc tải model và tối thiểu 8 GB RAM. Khuyến nghị cắm sạc laptop, tắt game hoặc ứng dụng nặng và để trống ít nhất 16 GB trên SSD đối với cấu hình khuyến nghị. File EXE chỉ nên chạy khi được nhận từ kho phát hành hoặc trưởng nhóm.")

    add_heading(doc, "Chạy File Cài Đặt")
    add_number(doc, "Tải DocuMind-Ollama-Setup.exe về một thư mục có quyền ghi, ví dụ Downloads hoặc Desktop.")
    add_number(doc, "Nhấp đúp file EXE. Cửa sổ dòng lệnh sẽ hiển thị RAM, GPU, VRAM, các ổ đĩa và model được chọn.")
    add_number(doc, "Chờ tiện ích cài Ollama và tải hai model. Không đóng cửa sổ trong khi đang tải. Thời gian phụ thuộc mạng và dung lượng model.")
    add_number(doc, "Khi thấy HOÀN TẤT, mở file DocuMind-Ollama-Settings.txt nằm cạnh EXE. File này chứa đúng tên chat model và embedding model mà máy bạn đã tải.")
    add_body(doc, "Tiện ích ưu tiên ổ trong máy thay vì ổ USB nếu cả hai đều đủ dung lượng. Nó đặt model vào thư mục DocuMind OllamaModels và chỉ cấu hình biến OLLAMA_MODELS cho tài khoản Windows hiện tại. API vẫn chỉ nghe tại localhost; không mở cổng mạng cho máy khác.")

    add_heading(doc, "Cấu Hình DocuMind")
    add_body(doc, "Mở DocuMind, vào Cài đặt API Key và Nền tảng. Chọn đúng các giá trị bên dưới. Hai trường tên model phải lấy từ file DocuMind-Ollama-Settings.txt vì EXE có thể chọn mức khác trên từng máy.")
    add_table(doc,
              ["Trường", "Giá trị cần nhập"],
              [
                  ("Provider", "OllamaLocal"),
                  ("API Key", "Để trống"),
                  ("Chat Model", "Sao chép dòng Chat Model trong file tóm tắt"),
                  ("Embedding Model", "Sao chép dòng Embedding Model trong file tóm tắt"),
                  ("Base URL", "http://localhost:11434/v1"),
                  ("Temperature", "0.3"),
                  ("Dùng provider này cho embedding RAG", "Bật"),
              ],
              [2.05, 4.62])
    add_body(doc, "Sau khi điền xong, bấm Kiểm tra kết nối, rồi Kiểm tra embedding. Cả hai đều thành công mới bấm Lưu cấu hình. Nếu tên model không hiện trong phần kiểm tra, mở lại file tóm tắt cạnh EXE để đối chiếu.")

    add_heading(doc, "Nạp Lại Tài Liệu Và Kiểm Tra")
    add_number(doc, "Chọn workspace cần dùng và nạp lại một tài liệu TXT hoặc MD nhỏ.")
    add_number(doc, "Bật RAG, hỏi một dữ kiện có trong tài liệu và kiểm tra nguồn trích dẫn.")
    add_number(doc, "Sau khi kiểm tra tốt mới nạp PDF lớn hoặc toàn bộ kho tài liệu. Khi thay embedding model, cần re-index tài liệu để kết quả tìm kiếm nhất quán.")

    add_heading(doc, "Cách EXE Chọn Model")
    add_table(doc,
              ["Điều kiện", "Chat", "Embedding", "Ghi chú"],
              [
                  ("GPU NVIDIA >= 16 GB và RAM >= 32 GB", "qwen3:14b", "bge-m3", "Chất lượng cao, cần nhiều VRAM"),
                  ("GPU NVIDIA >= 10 GB và RAM >= 24 GB", "qwen3:8b", "bge-m3", "Cân bằng cao"),
                  ("GPU NVIDIA >= 6 GB và RAM >= 16 GB", "qwen3:4b-instruct", "bge-m3", "Mức khuyến nghị cho DocuMind"),
                  ("Không đủ GPU nhưng RAM >= 24 GB", "qwen3:4b-instruct", "bge-m3", "Chạy CPU, chậm hơn"),
                  ("RAM 8 đến dưới 24 GB", "qwen3:1.7b", "nomic-embed-text", "Dùng thử, RAG tiếng Việt có thể kém hơn"),
              ],
              [1.53, 1.35, 1.25, 2.54])

    add_heading(doc, "Khắc Phục Sự Cố")
    add_table(doc,
              ["Hiện tượng", "Cách xử lý"],
              [
                  ("EXE không cài được Ollama", "Kiểm tra Internet, chạy lại EXE. Nếu vẫn lỗi, cài Ollama từ ollama.com/download/windows rồi chạy lại EXE để tải model."),
                  ("DocuMind báo không kết nối", "Mở Ollama từ Start Menu, chờ vài giây rồi kiểm tra lại Base URL là http://localhost:11434/v1."),
                  ("Model not found", "Mở file DocuMind-Ollama-Settings.txt và chép chính xác tên model đã tải vào hai trường model."),
                  ("Máy chậm hoặc thiếu VRAM", "Đóng game và ứng dụng dùng GPU. Chạy lại EXE nếu cần chọn lại mức nhẹ hơn, hoặc dùng máy có GPU/RAM mạnh hơn."),
                  ("Đã đổi embedding nhưng RAG cũ", "Nạp lại file nguồn để DocuMind tạo index bằng embedding model mới."),
              ],
              [2.05, 4.62])

    for paragraph in doc.paragraphs:
        for run in paragraph.runs:
            if run.font.color.rgb is None:
                run.font.color.rgb = RGBColor(0, 0, 0)

    doc.core_properties.title = "Hướng Dẫn Cài Ollama Local Cho DocuMind"
    doc.core_properties.subject = "Thiết lập Ollama local và cấu hình DocuMind"
    doc.core_properties.author = "DocuMind Team"
    doc.save(OUTPUT)
    print(OUTPUT.resolve())


if __name__ == "__main__":
    create_document()
