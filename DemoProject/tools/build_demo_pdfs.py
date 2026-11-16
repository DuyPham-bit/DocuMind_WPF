"""Rebuild the four original Vietnamese PRN212 demo PDFs with ReportLab.

Requires reportlab. All examples labelled ShopLab are synthetic test data.
"""
from pathlib import Path
from html import escape
from reportlab.pdfgen import canvas
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, Preformatted
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

ROOT = Path(__file__).resolve().parents[1]
FONTS = Path('C:/Windows/Fonts')
for face, filename in [('Arial', 'arial.ttf'), ('Arial-Bold', 'arialbd.ttf'), ('Arial-Italic', 'ariali.ttf')]:
    pdfmetrics.registerFont(TTFont(face, str(FONTS / filename)))
pdfmetrics.registerFontFamily('Arial', normal='Arial', bold='Arial-Bold', italic='Arial-Italic', boldItalic='Arial-Bold')
INK = colors.HexColor('#172B3A')
ORANGE = colors.HexColor('#B95114')
TEAL = colors.HexColor('#176B70')
MUTED = colors.HexColor('#526473')
styles = {
    'title': ParagraphStyle('title', fontName='Arial-Bold', fontSize=23, leading=29, textColor=INK, spaceAfter=12),
    'deck': ParagraphStyle('deck', fontName='Arial', fontSize=11, leading=17, textColor=MUTED, spaceAfter=16),
    'heading': ParagraphStyle('heading', fontName='Arial-Bold', fontSize=13, leading=19, textColor=TEAL, spaceBefore=10, spaceAfter=7, keepWithNext=True),
    'body': ParagraphStyle('body', fontName='Arial', fontSize=10.5, leading=16.5, textColor=INK, spaceAfter=9),
    'small': ParagraphStyle('small', fontName='Arial', fontSize=8, leading=12, textColor=MUTED, spaceAfter=4),
    'cell': ParagraphStyle('cell', fontName='Arial', fontSize=9.2, leading=14, textColor=INK),
    'headcell': ParagraphStyle('headcell', fontName='Arial-Bold', fontSize=9.2, leading=14, textColor=colors.white),
    'code': ParagraphStyle('code', fontName='Courier', fontSize=8.2, leading=12, textColor=INK, backColor=colors.HexColor('#F1F5F5'), borderPadding=10, spaceAfter=12),
}


def para(text, style='body'):
    return Paragraph(escape(text), styles[style])


def page(title, lead, sections, source):
    return dict(title=title, lead=lead, sections=sections, source=source)


DOCS = [
    dict(file='01_CSharp_OOP_PRN212.pdf', label='C# & OOP', subtitle='Nền tảng ngôn ngữ, đối tượng và hợp đồng', pages=[
        page('C# nền tảng cho PRN212',
             '01 / Từ bài HelloCS và BMI đến mô hình đối tượng có kiểm tra dữ liệu.', [
            ('text', 'Phạm vi: tài liệu tự biên soạn phục vụ kiểm thử DocuMind, dựa trên các ví dụ trong thư mục học tập. Đây không phải đề thi hay giáo trình chính thức. Các số liệu ShopLab trong bộ tài liệu là dữ liệu giả lập được cố định để đối chiếu đáp án.'),
            ('h', 'Kiểu dữ liệu và tham số'),
            ('text', 'int, double và struct là kiểu giá trị; biến kiểu giá trị thông thường giữ giá trị của chính nó. class là kiểu tham chiếu; biến giữ tham chiếu đến đối tượng. Gán hai biến tham chiếu không tự tạo bản sao đối tượng. C# mặc định truyền tham số theo giá trị, kể cả khi giá trị được truyền là một tham chiếu. ref, out và in có ý nghĩa riêng, không suy ra chỉ từ class hay struct.'),
            ('h', 'Ví dụ BMI dùng để kiểm tra phép tính'),
            ('text', 'BmiCalculator.GetBmi trong bài học dùng công thức cân nặng chia bình phương chiều cao. Bộ test giả lập quy định cân nặng 72 kg, chiều cao 1,8 m; kết quả toán học là khoảng 22,22. Ví dụ chỉ kiểm tra số học, không phân loại sức khỏe. Đầu vào chiều cao bằng 0 phải bị kiểm tra trước khi chia.'),
            ('code', 'double Bmi(double kg, double meters)\n{\n    if (kg <= 0 || meters <= 0)\n        throw new ArgumentOutOfRangeException();\n    return kg / (meters * meters);\n}'),
            ('text', 'Từ khóa ôn tập: value type, reference type, parameter passing, validation. Câu hỏi mẫu: Với đầu vào trên, BMI tính theo công thức trong tài liệu bằng bao nhiêu?')
        ], 'Session01_Hello/HelloCS/Program.cs; Session02_Language/HealthV2/BossTom.BmiTester/BmiCalculator.cs'),
        page('Đóng gói và tính hợp lệ', '02 / Mô hình Fruit của bài ShopLab minh họa.', [
            ('h', 'Đóng gói - encapsulation'),
            ('text', 'Đóng gói gom trạng thái và hành vi liên quan vào một lớp, đồng thời kiểm soát đường thay đổi trạng thái. Property có public setter không tự bảo đảm dữ liệu hợp lệ. Trong ví dụ ShopLab, Price và Stock không được âm; constructor và phương thức cập nhật phải cùng giữ các bất biến này. Quy tắc nghiệp vụ cần tồn tại ngoài TextBox vì dữ liệu cũng có thể đến từ test hoặc import.'),
            ('code', 'public sealed class Fruit\n{\n    public decimal Price { get; private set; }\n    public int Stock { get; private set; }\n    public Fruit(decimal price, int stock)\n    {\n        if (price < 0 || stock < 0)\n            throw new ArgumentOutOfRangeException();\n        Price = price;\n        Stock = stock;\n    }\n}'),
            ('h', 'Một ví dụ, hai thiết kế'),
            ('text', 'Model Fruit trong bài database của repo dùng double cho Price. Ví dụ ShopLab ở đây chủ động chọn decimal để diễn đạt tiền trong bài minh họa. Không được diễn giải rằng source hiện tại đã đổi kiểu Price thành decimal. Khi trả lời câu hỏi về source, phải phân biệt mã gốc và thiết kế đề xuất.'),
            ('text', 'Bộ test giả lập SHOP-DEMO-212 sẽ dùng cùng bốn mặt hàng trong tài liệu 02 và 04. Tên Fruit không có nghĩa mọi lớp tên Fruit trong các bài học phải có cùng cấu trúc. Namespace giúp phân biệt kiểu ở các project khác nhau.'),
            ('text', 'Câu hỏi mẫu: Vì sao chỉ kiểm tra giá âm trong giao diện chưa đủ? Đáp án cần nhắc đến những đường nhập dữ liệu khác và bất biến của đối tượng.')
        ], 'Session06-Database/Session06-Database/ShopSetting/Monday.ShopDieuDao/Models/Fruit.cs; ví dụ ShopLab tự biên soạn'),
        page('Kế thừa, đa hình, interface', '03 / Đối chiếu nhóm ShapeManager trong thư mục OOP.', [
            ('h', 'Khi nào dùng lớp trừu tượng?'),
            ('text', 'Trong Goldie.ShapeManager.V4, Shape là abstract class có Name, Color và PrintDetail trừu tượng. Lớp cơ sở giữ trạng thái chung, lớp con cung cấp cách in chi tiết. Không thể new trực tiếp Shape. Với virtual hoặc abstract member được override, phương thức thực thi được chọn theo đối tượng thực tế ở runtime, kể cả khi biến được khai báo bằng kiểu cơ sở.'),
            ('h', 'Khi nào dùng interface?'),
            ('text', 'IMeasurable quy định GetArea và GetPerimeter cho những đối tượng có ý nghĩa diện tích và chu vi. Line vẫn có thể là Shape nhưng không nên bị ép triển khai phép tính diện tích không có ý nghĩa của bài. Interface mô tả khả năng; abstract class có thể chia sẻ trạng thái và cách cài đặt. Một lớp có thể triển khai nhiều interface, nhưng chỉ kế thừa trực tiếp một lớp cơ sở.'),
            ('table', ([['Nhu cầu', 'Lựa chọn trong ví dụ'], ['Name và Color dùng chung', 'abstract class Shape'], ['Có diện tích và chu vi', 'interface IMeasurable'], ['Duyệt nhiều loại hình', 'List<Shape> và override PrintDetail']], [190, 297])),
            ('text', 'Ví dụ hình học giả lập: Rectangle rộng 3, cao 5 có diện tích 15 và chu vi 16. Disk bán kính 2 có diện tích 4*pi và chu vi 4*pi. Không làm tròn giữa chừng; làm tròn chỉ khi hiển thị.'),
            ('text', 'Câu hỏi so sánh: Vì sao không đặt GetArea bắt buộc vào mọi Shape? Trả lời bằng ví dụ Line và hợp đồng IMeasurable, thay vì chỉ liệt kê định nghĩa OOP.')
        ], 'Session03_OOP/Geometry/Goldie.ShapeManager.V4/Goldie.ShapeManager.V4/{Shape,IMeasurable,Rectangle,Disk,Line}.cs'),
        page('Bài tập truy xuất có căn cứ', '04 / Dữ kiện kiểm thử và giới hạn kiến thức của tài liệu.', [
            ('h', 'Mã neo nội dung'),
            ('text', 'Mã neo OOP-212: đối tượng phải giữ được trạng thái hợp lệ sau mọi thao tác công khai. Trong ví dụ Fruit, giá -100 và tồn kho -1 đều không hợp lệ; giá 0 và tồn kho 0 được chấp nhận theo quy ước riêng của ShopLab. Không suy rộng quy ước miễn phí này cho mọi cửa hàng thực tế.'),
            ('h', 'Ba câu ôn tập có đáp án'),
            ('text', 'Câu 1: Biến Shape chứa một Rectangle thì PrintDetail override được chọn theo điều gì? Đáp án: đối tượng thực tế ở runtime. Câu 2: IMeasurable có vai trò gì? Đáp án: hợp đồng cho diện tích và chu vi. Câu 3: Gán biến tham chiếu có sao chép toàn bộ đối tượng không? Đáp án: không; hai biến có thể cùng trỏ đến một đối tượng.'),
            ('h', 'Liên kết sang các tài liệu khác'),
            ('text', 'Tài liệu 02 dùng mô hình Fruit để lọc bằng LINQ và tính tổng tồn kho. Tài liệu 03 dùng cùng quy tắc giá không âm để quyết định CanExecute của lệnh lưu. Tài liệu 04 đặt kiểm tra nghiệp vụ trong service và kiểm tra lại ở ranh giới ghi dữ liệu. Ba cách trình bày cùng nhắc một quy tắc, nhưng có trách nhiệm khác nhau.'),
            ('text', 'Không có trong bộ này: lịch thi chính thức, điểm thi thật, học phí, tài khoản sinh viên hoặc mật khẩu database. Nếu được hỏi các thông tin đó, hệ thống cần nói thiếu căn cứ. Không dùng kiến thức nền của model để bịa một con số có vẻ hợp lý.'),
            ('text', 'Mục tiêu test: định nghĩa, tính toán, so sánh interface với abstract class, và trả lời đúng khi dữ kiện không tồn tại. Yêu cầu câu trả lời ngắn kèm tên file và trang để kiểm tra trích dẫn.')
        ], 'Tổng hợp từ các trang 1-3 của tài liệu này; dữ liệu kiểm thử ShopLab tự biên soạn')
    ]),
    dict(file='02_Delegate_LINQ_Async_PRN212.pdf', label='Delegate / LINQ / Async', subtitle='Hàm như dữ liệu, truy vấn và thao tác bất đồng bộ', pages=[
        page('Delegate và lambda', '01 / Từ ví dụ hai tham số đến các hàm có thể truyền vào.', [
            ('text', 'Tài liệu tự biên soạn theo bài Delegate trong repo PRN212. Các ví dụ được rút gọn để học và kiểm thử hỏi đáp, không thay thế toàn bộ source. Delegate là kiểu biểu diễn tham chiếu đến phương thức có chữ ký phù hợp; lambda là cú pháp biểu diễn hàm ẩn danh trong ngữ cảnh có kiểu đích phù hợp.'),
            ('h', 'Chữ ký quyết định phép gán'),
            ('text', 'Bài BossTom.DelegateBasic.Returned khai báo FTwoInputsOneput nhận hai double và trả một double. GetRectangleArea và ComputePower đều có thể gán vào kiểu delegate đó vì phù hợp chữ ký. Điều này không có nghĩa hai hàm có cùng mục đích nghiệp vụ: một hàm tính tích, một hàm tính lũy thừa.'),
            ('code', 'Func<double, double, double> area = (w, h) => w * h;\nFunc<double, double, double> power = Math.Pow;\ndouble a = area(3, 5);  // 15\ndouble b = power(2, 3); // 8\nAction<string> log = Console.WriteLine;\nPredicate<int> validStock = stock => stock >= 0;'),
            ('h', 'Func, Action, Predicate'),
            ('text', 'Func<T, TResult> nhận T và trả TResult; Func có nhiều biến thể số tham số. Action<T> nhận T và không trả kết quả. Predicate<T> nhận T và trả bool. Các dạng này giảm nhu cầu tự khai báo delegate cho chữ ký phổ biến. Tên biến và tên hàm vẫn cần diễn đạt ý nghĩa thay vì chỉ thỏa chữ ký.'),
            ('text', 'Mã neo DEL-212: area(3,5) = 15; power(2,3) = 8. Đây là dữ kiện cố định để thử truy xuất chính xác.')
        ], 'Session04-Delegate/DelegatesInsideOut/DelegatesInsideOut/BossTom.DelegateBasic.Returned/Program.cs; các bài BuiltIn.Action và BuiltIn.Func'),
        page('Bộ dữ liệu SHOP-DEMO-212', '02 / Dữ liệu giả lập dùng chung cho LINQ, WPF và database.', [
            ('text', 'ShopLab là cửa hàng giả lập phục vụ kiểm thử, không phải dữ liệu đang có trong FruitShopDB của máy. Giá có đơn vị VND cho một sản phẩm; Stock là số sản phẩm tồn kho. CategoryId 1 là Nội địa; CategoryId 2 là Nhập khẩu. Mã bộ dữ liệu: SHOP-DEMO-212, phiên bản D1.'),
            ('table', ([['Id', 'Tên', 'CategoryId', 'Price (VND)', 'Stock'], ['101', 'Táo xanh', '2', '45.000', '12'], ['102', 'Chuối', '1', '20.000', '30'], ['103', 'Cam', '1', '35.000', '0'], ['104', 'Nho tím', '2', '80.000', '5']], [34, 130, 80, 135, 108])),
            ('h', 'Kết quả đối chiếu chuẩn'),
            ('text', 'Có 4 mặt hàng, tổng tồn kho 47 sản phẩm. Cam có tồn kho bằng 0. Nhóm Nhập khẩu gồm Táo xanh và Nho tím; tổng tồn của nhóm là 17. Những mặt hàng còn hàng và có giá ít nhất 40.000 VND là Táo xanh và Nho tím. Sắp theo giá tăng dần: Chuối, Cam, Táo xanh, Nho tím.'),
            ('text', 'Tổng giá trị tồn kho được tính bằng tổng Price nhân Stock cho từng mặt hàng: 45.000*12 + 20.000*30 + 35.000*0 + 80.000*5 = 1.540.000 VND. Tổng giá bốn mặt hàng là 180.000 VND nhưng không phải giá trị tồn kho. Đây là hai đại lượng khác nhau để kiểm tra khả năng đọc đúng câu hỏi.'),
            ('h', 'Câu hỏi nhiều điều kiện'),
            ('text', 'Hỏi: Mặt hàng nhập khẩu nào có giá trên 50.000 VND và vẫn còn hàng? Đáp án duy nhất: Nho tím, Id 104, giá 80.000 VND, Stock 5. Câu trả lời phải trích trang 2 tài liệu này; không đoán theo giá trái cây ngoài thị trường.'),
            ('text', 'Nguồn số liệu: tự tạo cho demo. Khi so sánh với tài liệu 04, sử dụng cùng mã SHOP-DEMO-212 để tránh trộn ví dụ ở hai bộ dữ liệu khác nhau.')
        ], 'Bộ dữ liệu tổng hợp SHOP-DEMO-212 D1 tự biên soạn; không đọc dữ liệu thật từ database'),
        page('LINQ và thời điểm thực thi', '03 / Lọc, sắp xếp, chiếu và tổng hợp dữ liệu.', [
            ('h', 'Chuỗi truy vấn có ý nghĩa'),
            ('code', 'var matches = fruits\n    .Where(f => f.Stock > 0 && f.Price >= 40000m)\n    .OrderBy(f => f.Price)\n    .Select(f => f.Name)\n    .ToList();\n// ShopLab: Tao xanh, Nho tim\nvar value = fruits.Sum(f => f.Price * f.Stock);'),
            ('text', 'Where lọc phần tử theo điều kiện; OrderBy sắp xếp; Select chiếu sang dữ liệu cần dùng; Sum tổng hợp. ToList tạo danh sách kết quả tại thời điểm gọi. Nhiều toán tử trên IEnumerable dùng thực thi trì hoãn: khai báo truy vấn chưa nhất thiết duyệt dữ liệu. Thay đổi nguồn trước khi enumerate có thể thay đổi kết quả. Một List đã tạo không tự chạy lại truy vấn khi nguồn thay đổi.'),
            ('h', 'IEnumerable và IQueryable'),
            ('text', 'LINQ trên List thường thực thi trong bộ nhớ. Truy vấn IQueryable do provider EF Core hỗ trợ có thể được dịch thành SQL cho database; không phải mọi biểu thức C# đều dịch được. ToList quá sớm có thể kéo nhiều hàng về bộ nhớ trước khi lọc. Không được nói mọi truy vấn LINQ đều chạy ở database.'),
            ('text', 'Với truy vấn chỉ đọc trong EF Core, AsNoTracking có thể giảm chi phí theo dõi entity. Điều này không phải cơ chế phân quyền và không cấm mọi cập nhật khác. Kết quả tìm kiếm trên UI vẫn cần được nạp hoặc cập nhật phù hợp sau thao tác lưu.'),
            ('text', 'Bài tập: Với SHOP-DEMO-212, đổi điều kiện thành Stock == 0. Kết quả chỉ có Cam. Nếu lọc CategoryId == 1 và tính tổng Stock, kết quả là 30. Câu hỏi này yêu cầu kết hợp hiểu LINQ với dữ liệu trang 2.')
        ], 'Session03_OOP và Session06-Database: ví dụ collection, truy vấn; thuật toán demo tự biên soạn'),
        page('Event và async/await', '04 / Thông báo thay đổi và giữ giao diện phản hồi.', [
            ('h', 'Event giới hạn quyền phát thông báo'),
            ('text', 'event thường dựa trên delegate nhưng giới hạn mã bên ngoài ở việc đăng ký hoặc hủy đăng ký handler. Publisher quyết định lúc phát event. Với EventHandler, subscriber nhận sender và EventArgs theo quy ước. Khi subscriber sống ngắn hơn publisher, cần quản lý đăng ký để tránh giữ tham chiếu ngoài ý muốn. Delegate thông thường và event không phải hai từ khóa thay thế tùy ý.'),
            ('h', 'Async không tự tạo thread mới'),
            ('text', 'async/await cho phép chờ tác vụ mà không chặn thread trong lúc I/O bất đồng bộ phù hợp đang chờ. Từ khóa async không bảo đảm toàn bộ hàm chạy trên thread khác. Trên WPF, dùng .Result hoặc .Wait trên tác vụ liên quan UI có thể làm treo giao diện hoặc gây deadlock. Với việc nặng CPU, cân nhắc Task.Run và đưa cập nhật UI về Dispatcher.'),
            ('code', 'using var timeout = new CancellationTokenSource(\n    TimeSpan.FromMinutes(2));\ntry\n{\n    var rows = await service.LoadAsync(timeout.Token);\n}\ncatch (OperationCanceledException)\n{\n    // Show a cancellation or timeout message.\n}'),
            ('text', 'CancellationToken là yêu cầu hủy hợp tác, không tự dừng mọi công việc nếu các tầng bỏ qua token. Timeout là giới hạn thời gian chờ, khác giới hạn context theo token của AI. Một request có thể vừa trong giới hạn token nhưng vẫn timeout vì sinh câu trả lời chậm.'),
            ('text', 'Câu hỏi ôn tập: Vì sao await truy vấn database phù hợp hơn .Result trên UI? Vì sao event phù hợp để báo thay đổi? Trả lời từng ý trong tối đa ba câu trước khi yêu cầu quiz dài.')
        ], 'Session04-Delegate/GiaoLang.DieuDao; FinalProject/DocuMind_WPF/DocuMind_WPF/DocuMind.WPF/ViewModels/MainViewModel.cs')
    ]),
    dict(file='03_WPF_MVVM_PRN212.pdf', label='WPF & MVVM', subtitle='Binding, command, trạng thái và trải nghiệm desktop', pages=[
        page('WPF và phân chia MVVM', '01 / Chuyển từ bài giao diện Shop sang ứng dụng có cấu trúc.', [
            ('text', 'Tài liệu tự biên soạn dựa trên Session05-GUI và ứng dụng DocuMind. WPF dùng XAML để mô tả cây giao diện, layout, style và binding trên Windows. Code-behind vẫn có vai trò cho hành vi thuần giao diện; MVVM giúp phần xử lý trạng thái và nghiệp vụ dễ kiểm thử hơn.'),
            ('table', ([['Thành phần', 'Trách nhiệm của ví dụ ShopLab'], ['Model', 'Fruit, Category, dữ liệu và quy tắc nghiệp vụ'], ['View', 'TextBox, DataGrid, Button và cách trình bày'], ['ViewModel', 'Danh sách, lựa chọn, bộ lọc, lệnh và trạng thái']], [108, 379])),
            ('h', 'DataContext là đầu mối binding'),
            ('text', 'Binding không chỉ dựa vào tên control. Một binding thông thường tìm property theo DataContext của phần tử, trừ khi đã chỉ định Source, RelativeSource hoặc ElementName. DataContext có thể được kế thừa từ phần tử cha. Gán sai ViewModel hoặc viết sai tên property có thể khiến UI rỗng dù dữ liệu trong service vẫn có.'),
            ('code', '<Window.DataContext>\n    <vm:ShopViewModel />\n</Window.DataContext>\n<TextBox Text="{Binding SearchText,\n    UpdateSourceTrigger=PropertyChanged}" />'),
            ('text', 'Ví dụ trên chỉ minh họa khi ShopViewModel có constructor phù hợp. Với DI, cửa sổ có thể nhận ViewModel qua constructor rồi gán DataContext trong code-behind. Không đồng thời tạo hai ViewModel khác nhau nếu cần chia sẻ cùng trạng thái.'),
            ('text', 'Mã neo WPF-212: View trình bày, ViewModel giữ trạng thái và command, service thực hiện công việc được giao. Câu hỏi mẫu: Sai DataContext sẽ làm lỗi ở tầng nào dễ thấy nhất?')
        ], 'Session05-GUI/Session05-GUI/Monday.ShopDieuDao/MainWindow.xaml.cs; DocuMind.WPF/MainWindow.xaml.cs'),
        page('Thông báo thay đổi dữ liệu', '02 / PropertyChanged và CollectionChanged giải quyết hai việc khác nhau.', [
            ('h', 'Property của một đối tượng'),
            ('text', 'INotifyPropertyChanged báo cho binding rằng giá trị property của đối tượng đã thay đổi. Trong ViewModel, khi SearchText hoặc IsBusy đổi, setter cần phát thông báo thích hợp. CommunityToolkit.Mvvm có thể sinh property và notification qua ObservableProperty, nhưng lớp và cấu hình generator vẫn phải đúng.'),
            ('h', 'Các phần tử trong danh sách'),
            ('text', 'ObservableCollection<Fruit> thông báo khi thêm, xóa hoặc sắp lại phần tử collection. Collection không tự thay thế trách nhiệm INotifyPropertyChanged của từng Fruit. Nếu sửa Price trên phần tử mà muốn ô DataGrid đổi ngay theo notification, phần tử cần cơ chế thông báo thay đổi property phù hợp. List<Fruit> không tự thông báo việc Add cho WPF binding.'),
            ('table', ([['Thay đổi', 'Cơ chế cần kiểm tra'], ['SearchText từ rỗng thành Cam', 'PropertyChanged của ViewModel'], ['Thêm Fruit mới vào danh sách', 'CollectionChanged của collection'], ['Price của Fruit đang có thay đổi', 'PropertyChanged của phần tử'], ['Thay toàn bộ instance danh sách', 'PropertyChanged của property danh sách']], [235, 252])),
            ('h', 'Ví dụ kiểm tra trực quan'),
            ('text', 'Nạp SHOP-DEMO-212 sẽ có 4 dòng. Lọc Cam chỉ còn 1 dòng có Stock 0. Xóa bộ lọc phải thấy lại 4 dòng. Thêm hàng mới phải cập nhật số dòng sau khi lưu thành công. Đây là kỳ vọng cho ứng dụng ShopLab minh họa, không phải tính năng quản lý trái cây trong DocuMind.'),
            ('text', 'Câu hỏi đối chiếu: Vì sao dùng ObservableCollection vẫn chưa đủ khi sửa giá của một Fruit? Đáp án phải phân biệt thay đổi collection với thay đổi property của item.')
        ], 'Ví dụ MVVM tự biên soạn; DocuMind.WPF/ViewModels/MainViewModel.cs sử dụng ObservableCollection và ObservableProperty'),
        page('Command, validation và IsBusy', '03 / Thao tác lưu cần đúng dữ liệu và đúng thời điểm.', [
            ('h', 'ICommand và điều kiện thực thi'),
            ('text', 'Command biểu diễn thao tác từ ViewModel để View binding vào Button. CanExecute quyết định khi nào thao tác được cho phép, Execute thực hiện thao tác. RelayCommand và AsyncRelayCommand giảm code lặp. Khi dữ liệu quyết định CanExecute thay đổi, command cần được thông báo đánh giá lại; không chỉ đổi màu nút bằng style.'),
            ('text', 'Trong ShopLab, Save chỉ được bật khi Name có nội dung, Price >= 0, Stock >= 0 và IsBusy == false. Cam có Stock 0 vẫn hợp lệ để lưu, vì hết hàng khác với dữ liệu sai. Cùng quy tắc giá và tồn không âm đã được nêu ở tài liệu 01.'),
            ('h', 'Luồng lưu được đề xuất'),
            ('text', 'Bước 1 kiểm tra đầu vào. Bước 2 đặt IsBusy true và hiển thị trạng thái. Bước 3 await service lưu dữ liệu. Bước 4 cập nhật danh sách khi thành công. Bước 5 trả IsBusy false trong finally. Khi lỗi, giữ thông báo đủ lâu để người dùng đọc và giữ đầu vào cần thiết cho lần thử tiếp theo.'),
            ('code', 'try\n{\n    IsBusy = true;\n    await service.SaveAsync(draft, cancellationToken);\n    Status = "Saved";\n}\nfinally\n{\n    IsBusy = false;\n}'),
            ('text', 'Trong ví dụ rút gọn, bắt lỗi và kiểm tra nghiệp vụ được đặt ở tầng gọi hoặc service thích hợp. Không bật một vùng báo lỗi chỉ khi IsBusy là true: sau finally, lỗi sẽ biến mất đúng lúc người dùng cần đọc. DocuMind có vùng trạng thái còn hiển thị khi idle để tránh lỗi đó.')
        ], 'DocuMind.WPF/ViewModels/MainViewModel.cs; DocuMind.WPF/MainWindow.xaml; docs/08_Review_cuoi.md'),
        page('Workspace và kiểm thử giao diện', '04 / Liên hệ với dự án DocuMind đang chạy.', [
            ('h', 'Những thao tác cần thử'),
            ('text', 'Trong DocuMind, tạo workspace mới, nạp từng PDF, bật Search documents và hỏi có căn cứ. New chat bắt đầu hội thoại mới; không đồng nghĩa xóa các PDF đã lập chỉ mục. Workspace khác phải có tập tài liệu và lịch sử riêng. Export xuất phần hội thoại mà ứng dụng đang hỗ trợ; cần kiểm tra nội dung file sau xuất thay vì chỉ thấy thông báo thành công.'),
            ('h', 'Trạng thái tài liệu và lỗi'),
            ('text', 'Pending hoặc Re-index nhắc rằng tài liệu chưa có index dùng được với cấu hình hiện tại. Lỗi kết nối provider, timeout và vượt context là các tình huống khác nhau. Một file PDF có chữ chọn được phù hợp parser văn bản; PDF chỉ là ảnh scan cần OCR bên ngoài trong phiên bản hiện tại.'),
            ('h', 'Kịch bản UI ngắn'),
            ('text', 'Tạo workspace PRN212 Demo; nạp riêng PDF 01; hỏi về IMeasurable và kiểm tra nguồn. Sau đó nạp PDF 02, hỏi mặt hàng nào hết hàng. Chuyển workspace rỗng, hỏi lại dữ kiện tồn kho: app cần báo thiếu căn cứ. Trở về workspace ban đầu, dữ liệu phải còn. Không đánh giá RAG bằng câu trả lời từ Direct Chat khi Search documents đã tắt.'),
            ('text', 'Với Study quiz, chọn chủ đề Đóng gói, LINQ hoặc MVVM, bắt đầu bằng 3 câu. Nộp bài để kiểm tra chấm điểm, giải thích, nguồn và lịch sử. App chấm từ đáp án trong bộ câu đã tạo, không hỏi lại model cho từng lựa chọn.'),
            ('text', 'Giới hạn của test: những bước này là kế hoạch nghiệm thu, không khẳng định đã thao tác UI tự động. Cần đối chiếu kết quả AI thực tế với các trang có dữ kiện trong bộ PDF.')
        ], 'DocuMind.WPF/ViewModels/{MainViewModel,QuizViewModel}.cs; DocuMind.Application/Services/QuizService.cs; USER_GUIDE.md')
    ]),
    dict(file='04_EFCore_DI_DocuMind_PRN212.pdf', label='EF Core / DI / DocuMind', subtitle='Dữ liệu, vòng đời dịch vụ và kiến trúc dự án', pages=[
        page('EF Core và quan hệ dữ liệu', '01 / Từ FruitShop SQL Server đến DocuMind SQLite.', [
            ('text', 'Tài liệu tự biên soạn phục vụ demo PRN212. Bài Session06-Database dùng FruitShopDbContext với provider SQL Server. Dự án DocuMind hiện dùng EF Core với SQLite. Không suy ra hai ứng dụng đang dùng cùng database chỉ vì cùng dùng DbContext.'),
            ('h', 'Entity, DbSet và DbContext'),
            ('text', 'Entity mô hình hóa dữ liệu được ánh xạ. DbSet cho phép xây dựng truy vấn và thao tác trên nhóm entity. DbContext theo dõi thay đổi trong một đơn vị công việc và SaveChanges ghi những thay đổi được theo dõi xuống database. DbContext không dành cho sử dụng đồng thời từ nhiều thao tác bất đồng bộ trên cùng instance.'),
            ('h', 'Khóa ngoại và navigation'),
            ('text', 'Category có nhiều Fruit; Fruit có CategoryId và navigation Category. Khóa ngoại thể hiện liên kết lưu trữ, navigation hỗ trợ truy cập mô hình đối tượng. Việc khai báo navigation không bảo đảm dữ liệu liên quan đã được nạp: cần chọn truy vấn, Include hoặc projection phù hợp. Migration mô tả thay đổi schema; không tự sửa mọi dữ liệu nghiệp vụ sai.'),
            ('table', ([['Ứng dụng trong repo', 'Provider database'], ['ShopSetting / FruitShopDbContext', 'SQL Server'], ['ASM1 / ProductManagerMvc', 'SQL Server theo cấu hình project'], ['DocuMind / AppDbContext', 'SQLite cục bộ']], [260, 227])),
            ('text', 'Với ShopLab giả lập, Category 1 là Nội địa, Category 2 là Nhập khẩu; Fruit 104 là Nho tím thuộc Category 2. Những mã này thuộc SHOP-DEMO-212, không khẳng định là hàng hiện có trong database thật.')
        ], 'Session06-Database/.../Models/FruitShopDbContext.cs; FinalProject/DocuMind_WPF/ASM1; DocuMind.WPF/App.xaml.cs'),
        page('CRUD và ranh giới giao dịch', '02 / Đề xuất luồng lưu cho bộ dữ liệu ShopLab.', [
            ('h', 'Truy vấn chỉ lấy phần cần dùng'),
            ('code', 'var rows = await db.Fruits\n    .AsNoTracking()\n    .Where(f => f.Stock > 0)\n    .Select(f => new { f.Id, f.Name, f.Price })\n    .ToListAsync(ct);'),
            ('text', 'Đoạn code là ví dụ ShopLab, nơi Fruit được bổ sung Stock; không chép nguyên vào model FruitShop của bài học nếu model đó chưa có property Stock. Projection chọn cột cần dùng. AsNoTracking phù hợp truy vấn chỉ đọc; SaveChanges không tự theo dõi thay đổi của một DTO vừa chiếu ra.'),
            ('h', 'Một thao tác nghiệp vụ có nhiều thay đổi'),
            ('text', 'Ví dụ ShopLab bán 3 Táo xanh từ tồn 12 thì tồn mới là 9. Tổng tiền theo giá giả lập 45.000 VND là 135.000 VND. Nếu yêu cầu vừa lưu đơn hàng vừa trừ tồn, hai thay đổi cần tính nguyên tử theo thiết kế giao dịch. Cũng phải xử lý cạnh tranh khi nhiều người mua cùng lúc; chỉ kiểm tra tồn trong UI không đủ.'),
            ('h', 'Luồng index thật của DocuMind'),
            ('text', 'DocuMind tạo DocumentVersion Staging, đọc snapshot file nguồn và gọi embedding ngoài giao dịch ghi cuối. Khi mọi đoạn đã hợp lệ, ActivateAsync ghi phiên bản Ready và các chunk trong một giao dịch ngắn. Nếu bước xử lý thất bại, phiên bản Ready cũ được giữ, phiên bản đang xử lý được đánh dấu Failed. Không giữ transaction database mở trong suốt các request AI lâu.'),
            ('text', 'Câu hỏi liên tài liệu: ShopLab cấm tồn âm ở tầng nào? Tài liệu 01 nêu bất biến đối tượng; tài liệu 03 kiểm tra trước khi bật Save; trang này bổ sung kiểm tra nghiệp vụ, giao dịch và cạnh tranh tại tầng lưu. Đây là bảo vệ nhiều ranh giới, không phải ba quy tắc khác nhau.')
        ], 'Ví dụ ShopLab tự biên soạn; DocuMind.Application/Services/RagOrchestratorService.cs; Infrastructure/Persistence/EfDocumentStore.cs'),
        page('Dependency Injection và lifetime', '03 / Liên hệ ASM2 với cách khởi tạo DocuMind.', [
            ('h', 'Constructor injection'),
            ('text', 'Một lớp khai báo dependency qua constructor và nhận implementation từ bên ngoài. Ví dụ ProductService nhận IProductRepository nên test có thể truyền fake repository. Composition root đăng ký interface với implementation. DI không có nghĩa mọi lớp đều bắt buộc phải có interface, và container không thay thế thiết kế trách nhiệm rõ ràng.'),
            ('table', ([['Lifetime', 'Ý nghĩa'], ['Transient', 'Tạo instance mới cho mỗi lần resolve'], ['Scoped', 'Dùng chung trong cùng scope'], ['Singleton', 'Dùng chung trong cùng service provider']], [108, 379])),
            ('text', 'Trong web ASP.NET Core, request thường là một scope. WPF không tự tạo request scope như web: phải chủ động quản lý scope hoặc dùng factory phù hợp. Không đưa DbContext dùng ngắn hạn vào singleton rồi dùng đồng thời từ nhiều thao tác.'),
            ('h', 'Dữ kiện từ repo'),
            ('text', 'ASM2 đăng ký IProductRepository là Singleton, IProductService là Scoped và ba marker Transient/Scoped/Singleton để so sánh lifetime. DocuMind dùng IDbContextFactory<AppDbContext>; các store tạo và dispose DbContext cho mỗi thao tác. HttpClient dùng chung qua DI. ViewModel nhận service qua constructor để có thể thay bằng fake trong test.'),
            ('text', 'Mã neo DI-212: trong cùng scope, hai lần resolve một service Scoped thường nhận cùng instance; hai scope khác nhau có instance Scoped khác nhau. Singleton dùng chung trong cùng provider, không phải một đối tượng duy nhất cho mọi chương trình trên máy.'),
            ('text', 'Câu hỏi ôn tập: Vì sao không mang nguyên giả định một HTTP request là một scope sang WPF? Đáp án cần nhắc rằng desktop phải tự quản lý ranh giới lifetime.')
        ], 'FinalProject/DocuMind_WPF/ASM2/DependencyInjectionMvcDemo/Program.cs; DocuMind.WPF/App.xaml.cs; Infrastructure/Persistence/EfDocumentStore.cs'),
        page('RAG, embedding và context', '04 / Phân biệt ba nguyên nhân lỗi đã gặp khi demo.', [
            ('h', 'Luồng xử lý tài liệu'),
            ('text', 'Parser đọc văn bản PDF theo trang. App chia đoạn mặc định tối đa 180 từ, chồng lặp 30 từ; tạo embedding và lưu SQLite. Khi hỏi RAG, app embedding câu hỏi, lọc profile tương thích, xếp theo cosine similarity; chat mặc định lấy tối đa 4 đoạn đạt ngưỡng 0,3. Đây là thiết kế hiện tại của DocuMind, không phải mặc định chung cho mọi hệ thống RAG.'),
            ('h', 'Profile quyết định khả năng dùng lại index'),
            ('text', 'Profile gồm provider, model, endpoint, số chiều, normalization và phiên bản pipeline. Hai model cùng số chiều chưa chắc có vector tương thích. Đổi model embedding hoặc endpoint có thể cần re-index. Thay chat model không đồng nghĩa bắt buộc thay embedding model.'),
            ('table', ([['Dấu hiệu', 'Điều cần kiểm tra'], ['Connection refused, cổng 11434', 'Ollama API server có hoạt động không'], ['Hết thời gian chờ', 'Thời gian nạp model và tốc độ sinh token'], ['Prompt vượt context', 'Token của prompt, lịch sử và phần trả lời']], [230, 257])),
            ('text', 'Bản sửa demo đặt context Ollama 8.192 token qua API native và tối đa 3.072 token sinh ra. HTTP, chat và quiz chờ tối đa 10 phút; test cấu hình 2 phút. Giới hạn là ngân sách mỗi lượt, không phải quota dùng hằng ngày. Context lớn dùng thêm bộ nhớ, không bảo đảm trả lời nhanh hơn.'),
            ('text', 'Tài liệu được đưa vào prompt dưới dạng dữ liệu tham khảo, không phải chỉ thị để model làm theo. Khi căn cứ thiếu, câu trả lời phải nêu giới hạn. Hỏi ngày thi chính thức hoặc mật khẩu database là phép thử thiếu dữ kiện: bộ PDF này không cung cấp các thông tin đó.')
        ], 'DocuMind.Application/Services/{VectorMathHelper,RagOrchestratorService}.cs; Infrastructure/AiProviders/OpenAiCompatibleService.cs; docs/06_Ollama_Local.md')
    ]),
]


def build(document):
    story = []
    for index, content in enumerate(document['pages']):
        if index:
            story.append(PageBreak())
        story += [para(document['label'].upper(), 'small'), para(content['title'], 'title'), para(content['lead'], 'deck')]
        for kind, value in content['sections']:
            if kind == 'h':
                story.append(para(value, 'heading'))
            elif kind == 'text':
                story.append(para(value))
            elif kind == 'code':
                story.append(Preformatted(value, styles['code']))
            elif kind == 'table':
                rows, widths = value
                table = Table([[para(cell, 'headcell' if ri == 0 else 'cell') for cell in row] for ri, row in enumerate(rows)], colWidths=widths, hAlign='LEFT')
                table.setStyle(TableStyle([
                    ('BACKGROUND', (0, 0), (-1, 0), TEAL),
                    ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.HexColor('#F0F5F4'), colors.HexColor('#FAFBFA')]),
                    ('VALIGN', (0, 0), (-1, -1), 'TOP'),
                    ('LEFTPADDING', (0, 0), (-1, -1), 9), ('RIGHTPADDING', (0, 0), (-1, -1), 9),
                    ('TOPPADDING', (0, 0), (-1, -1), 9), ('BOTTOMPADDING', (0, 0), (-1, -1), 9),
                    ('LINEBELOW', (0, -1), (-1, -1), .5, colors.HexColor('#D8E1E1')),
                ]))
                story += [table, Spacer(1, 12)]
        story += [Spacer(1, 8), para('Đối chiếu source (tính từ D:/FPT/PRN212, trừ các đường DocuMind viết gọn): ' + content['source'], 'small')]
    def decorate(c, doc):
        width, height = A4
        c.saveState()
        c.setFillColor(ORANGE)
        c.rect(0, height - 8, width, 8, fill=1, stroke=0)
        c.setFont('Arial-Bold', 8)
        c.setFillColor(MUTED)
        c.drawString(54, height - 32, 'PRN212  /  DOCUMIND DEMO LIBRARY')
        c.setStrokeColor(colors.HexColor('#D8E1E1'))
        c.line(54, 47, width - 54, 47)
        c.setFont('Arial', 8)
        c.drawString(54, 31, 'Tự biên soạn để kiểm thử  |  16/11/2026  |  ' + document['label'])
        c.drawRightString(width - 54, 31, f'Trang {doc.page} / 4')
        c.restoreState()
    pdf = SimpleDocTemplate(str(ROOT / document['file']), pagesize=A4,
        rightMargin=54, leftMargin=54, topMargin=54, bottomMargin=63,
        title=document['label'] + ' - PRN212', author='DocuMind DemoProject', subject=document['subtitle'],
        pageCompression=1)
    pdf.build(story, onFirstPage=decorate, onLaterPages=decorate)
    print(document['file'])


if __name__ == '__main__':
    for document in DOCS:
        build(document)
