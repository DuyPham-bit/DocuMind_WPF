# Quy trình làm việc của nhóm

Repo chứa 3 project môn PRN212: `DocuMind_WPF`, `ASM1`, `ASM2`. Để `main` luôn build và chạy được, nhóm thống nhất các quy tắc sau.

## 1. Không push thẳng lên `main`

Mọi thay đổi đi qua pull request:

1. Tạo nhánh từ `main` mới nhất, đặt tên theo loại việc: `feat/...`, `fix/...`, `docs/...`, `ci/...`.
2. Commit nhỏ, mỗi commit một ý, message theo dạng `fix(DocuMind_WPF): ...`.
3. Mở PR vào `main`, điền đủ template.
4. Chờ **CI xanh** và **ít nhất 1 thành viên khác approve** rồi mới merge.
5. Mỗi PR nên chỉ làm một việc. Đổi UI, sửa lỗi và cấu trúc lại thư mục thì tách thành các PR riêng.

## 2. CI

Workflow `.github/workflows/ci.yml` chạy `dotnet build` và `dotnet test` cho cả 3 solution trên `windows-latest` mỗi khi có PR hoặc push vào `main`. Kết quả test (`.trx`) được đính kèm ở mục Artifacts của từng lần chạy. Có thể chạy tay ở tab **Actions → CI → Run workflow**.

Trước khi mở PR, hãy chạy trên máy mình:

```powershell
dotnet test DocuMind_WPF/DocuMind.slnx
dotnet test ASM1/ASM1.slnx
dotnet test ASM2/ASM2.slnx
```

## 3. Bảo vệ nhánh `main` (chủ repo cài một lần)

Vào **Settings → Branches → Add branch ruleset** (hoặc *Add rule*) cho `main`, rồi bật:

- *Require a pull request before merging*, với *Required approvals* = 1
- *Require status checks to pass*: chọn 3 check `DocuMind_WPF`, `ASM1`, `ASM2` (chúng xuất hiện sau lần chạy CI đầu tiên)
- *Block force pushes*

## 4. Không commit bí mật

API key, connection string có mật khẩu và file `*.db` không được đưa lên repo. `.gitignore` ở thư mục gốc đã chặn các file thường gặp. DocuMind lưu key bằng DPAPI trong `%LocalAppData%\DocuMind`, không lưu trong repo.
