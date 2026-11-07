using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DocuMind.Bootstrapper;

internal static class Program
{
    private const string OllamaInstallScript = "https://ollama.com/install.ps1";

    public static async Task<int> Main(string[] args)
    {
        var options = SetupOptions.Parse(args);
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "DocuMind - Local Ollama Setup";

        PrintHeader();

        try
        {
            var hardware = HardwareSurvey.Inspect();
            var plan = ModelSelector.Select(hardware);
            PrintSurvey(hardware, plan);

            if (!plan.IsSupported)
            {
                Console.Error.WriteLine($"\nKhông thể tự cài an toàn: {plan.Reason}");
                return Finish(2, options);
            }

            var modelDirectory = StorageSelector.Select(hardware.Drives, plan.MinimumFreeSpaceGb);
            if (modelDirectory is null)
            {
                Console.Error.WriteLine($"\nKhông tìm thấy ổ cứng còn trống ít nhất {plan.MinimumFreeSpaceGb:F0} GB.");
                return Finish(3, options);
            }

            Console.WriteLine($"Thư mục model được chọn: {modelDirectory}");
            Console.WriteLine("Ollama local không cần API key. Chỉ chạy trên localhost của máy này.");

            if (options.DryRun)
            {
                Console.WriteLine("\nChế độ --dry-run: chưa cài đặt hoặc tải model.");
                WriteSetupSummary(hardware, plan, modelDirectory, "Chưa thực hiện vì chạy --dry-run.");
                return Finish(0, options);
            }

            Directory.CreateDirectory(modelDirectory);
            Environment.SetEnvironmentVariable("OLLAMA_MODELS", modelDirectory, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("OLLAMA_MODELS", modelDirectory, EnvironmentVariableTarget.Process);

            var ollamaPath = FindOllama();
            if (ollamaPath is null)
            {
                Console.WriteLine("\nĐang cài Ollama bản ổn định mới nhất từ ollama.com...");
                await RunOrThrowAsync(
                    "powershell.exe",
                    $"-NoProfile -ExecutionPolicy Bypass -Command \"irm {OllamaInstallScript} | iex\"");
                ollamaPath = WaitForOllama();
            }

            if (ollamaPath is null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy ollama.exe sau khi cài. Hãy cài từ https://ollama.com/download/windows rồi chạy lại tiện ích.");
            }

            Console.WriteLine($"Đã tìm thấy Ollama: {ollamaPath}");
            await EnsureServerReadyAsync(ollamaPath);

            foreach (var model in plan.Models)
            {
                Console.WriteLine($"\nĐang tải {model.Role}: {model.Name} ...");
                await RunOrThrowAsync(ollamaPath, $"pull {model.Name}");
            }

            WriteSetupSummary(hardware, plan, modelDirectory, "Hoàn tất. Đã cài Ollama và tải đủ model.");
            Console.WriteLine("\nHOÀN TẤT. Mở DocuMind, vào Cài đặt API Key & Nền tảng và nhập theo file hướng dẫn.");
            Console.WriteLine("File tóm tắt đã được tạo cạnh file EXE để người dùng điền đúng tên model.");
            return Finish(0, options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\nThiết lập thất bại: {ex.Message}");
            Console.Error.WriteLine("Không xóa dữ liệu nào. Có thể chạy lại tiện ích sau khi kiểm tra Internet và dung lượng ổ đĩa.");
            return Finish(1, options);
        }
    }

    private static void PrintHeader()
    {
        Console.WriteLine("===============================================================");
        Console.WriteLine("             DocuMind Local Ollama Setup");
        Console.WriteLine("===============================================================");
        Console.WriteLine("Tiện ích kiểm tra cấu hình, cài Ollama và tải model local phù hợp.");
        Console.WriteLine();
    }

    private static void PrintSurvey(HardwareInfo hardware, SetupPlan plan)
    {
        Console.WriteLine("Cấu hình phát hiện:");
        Console.WriteLine($"  CPU logic: {hardware.LogicalProcessorCount}");
        Console.WriteLine($"  RAM: {hardware.MemoryGb:F1} GB");
        Console.WriteLine($"  GPU NVIDIA: {(hardware.NvidiaGpuName ?? "Không phát hiện")}");
        Console.WriteLine($"  VRAM NVIDIA: {(hardware.NvidiaVramGb > 0 ? $"{hardware.NvidiaVramGb:F1} GB" : "Không xác định")}");
        Console.WriteLine("  Ổ trống: " + string.Join(", ", hardware.Drives.Select(d => $"{d.Root} {d.FreeGb:F0} GB{(string.IsNullOrWhiteSpace(d.BusType) ? string.Empty : $" ({d.BusType})")}")));
        Console.WriteLine();
        Console.WriteLine("Cấu hình được chọn:");
        Console.WriteLine($"  Mức: {plan.Name}");
        Console.WriteLine($"  Chat: {plan.ChatModel}");
        Console.WriteLine($"  Embedding: {plan.EmbeddingModel}");
        Console.WriteLine($"  Lý do: {plan.Reason}");
    }

    private static string? FindOllama()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe"),
        };

        var fromKnownPath = candidates.FirstOrDefault(File.Exists);
        if (fromKnownPath is not null)
        {
            return fromKnownPath;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim(), "ollama.exe");
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    private static string? WaitForOllama()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var path = FindOllama();
            if (path is not null) return path;
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        return null;
    }

    private static async Task EnsureServerReadyAsync(string ollamaPath)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        if (await IsServerReadyAsync(client)) return;

        Console.WriteLine("Đang khởi động dịch vụ Ollama local...");
        var startInfo = new ProcessStartInfo(ollamaPath, "serve")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.Environment["OLLAMA_MODELS"] = Environment.GetEnvironmentVariable("OLLAMA_MODELS")!;
        _ = Process.Start(startInfo);

        for (var attempt = 0; attempt < 15; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            if (await IsServerReadyAsync(client)) return;
        }

        throw new InvalidOperationException("Ollama không khởi động được ở http://localhost:11434. Hãy đóng Ollama ở khay hệ thống và chạy lại.");
    }

    private static async Task<bool> IsServerReadyAsync(HttpClient client)
    {
        try
        {
            using var response = await client.GetAsync("http://127.0.0.1:11434/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private static async Task RunOrThrowAsync(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = false,
        }) ?? throw new InvalidOperationException($"Không thể chạy {fileName}.");

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Lệnh {Path.GetFileName(fileName)} trả về mã lỗi {process.ExitCode}.");
        }
    }

    private static void WriteSetupSummary(HardwareInfo hardware, SetupPlan plan, string modelDirectory, string status)
    {
        var outputDirectory = AppContext.BaseDirectory;
        var outputPath = Path.Combine(outputDirectory, "DocuMind-Ollama-Settings.txt");
        var content = $"""
            DocuMind Local Ollama Setup
            ============================
            {status}

            Thời gian: {DateTimeOffset.Now:yyyy-MM-dd HH:mm zzz}
            RAM: {hardware.MemoryGb:F1} GB
            GPU NVIDIA: {hardware.NvidiaGpuName ?? "Không phát hiện"}
            VRAM NVIDIA: {(hardware.NvidiaVramGb > 0 ? $"{hardware.NvidiaVramGb:F1} GB" : "Không xác định")}
            Thư mục model: {modelDirectory}

            Nhập các giá trị sau trong DocuMind > Cài đặt API Key & Nền tảng:
            Provider: OllamaLocal
            API Key: để trống
            Chat Model: {plan.ChatModel}
            Embedding Model: {plan.EmbeddingModel}
            Base URL: http://localhost:11434/v1
            Temperature: 0.3
            Dùng provider này cho embedding/RAG: Bật

            Sau đó bấm Kiểm tra kết nối, Kiểm tra embedding, rồi Lưu cấu hình.
            """;
        File.WriteAllText(outputPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Console.WriteLine($"Tóm tắt cấu hình: {outputPath}");
    }

    private static int Finish(int code, SetupOptions options)
    {
        if (!options.NoPause)
        {
            Console.WriteLine("\nNhấn phím bất kỳ để đóng cửa sổ.");
            Console.ReadKey(intercept: true);
        }
        return code;
    }
}

internal sealed record SetupOptions(bool DryRun, bool NoPause)
{
    public static SetupOptions Parse(IEnumerable<string> args) => new(
        args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase),
        args.Contains("--no-pause", StringComparer.OrdinalIgnoreCase));
}

internal sealed record HardwareInfo(
    double MemoryGb,
    int LogicalProcessorCount,
    string? NvidiaGpuName,
    double NvidiaVramGb,
    IReadOnlyList<DriveSnapshot> Drives);

internal sealed record DriveSnapshot(string Root, double FreeGb, bool IsSystemDrive, string? BusType);

internal static class HardwareSurvey
{
    public static HardwareInfo Inspect()
    {
        var gpu = ProbeNvidiaGpu();
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d => new DriveSnapshot(
                d.RootDirectory.FullName,
                Math.Round(d.AvailableFreeSpace / 1024d / 1024d / 1024d, 1),
                string.Equals(d.RootDirectory.FullName, Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase),
                ProbeDriveBusType(d.RootDirectory.FullName)))
            .OrderByDescending(d => d.FreeGb)
            .ToList();

        return new HardwareInfo(
            GetInstalledMemoryGb(),
            Environment.ProcessorCount,
            gpu.Name,
            gpu.VramGb,
            drives);
    }

    private static (string? Name, double VramGb) ProbeNvidiaGpu()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("nvidia-smi.exe", "--query-gpu=name,memory.total --format=csv,noheader,nounits")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null) return (null, 0);
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            if (process.ExitCode != 0) return (null, 0);

            var first = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (first is null) return (null, 0);
            var values = first.Split(',', StringSplitOptions.TrimEntries);
            return values.Length == 2 && double.TryParse(values[1], out var memoryMb)
                ? (values[0], Math.Round(memoryMb / 1024d, 1))
                : (values[0], 0);
        }
        catch
        {
            return (null, 0);
        }
    }

    private static double GetInstalledMemoryGb()
    {
        var memory = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref memory))
        {
            throw new InvalidOperationException("Không đọc được dung lượng RAM.");
        }
        return Math.Round(memory.TotalPhys / 1024d / 1024d / 1024d, 1);
    }

    private static string? ProbeDriveBusType(string root)
    {
        try
        {
            var letter = root.TrimEnd('\\', ':');
            using var process = Process.Start(new ProcessStartInfo(
                "powershell.exe",
                $"-NoProfile -Command \"(Get-Partition -DriveLetter '{letter}' | Get-Disk | Select-Object -First 1 -ExpandProperty BusType)\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(3000);
            return process.ExitCode == 0 && output.Length > 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}

internal sealed record ModelRequirement(string Role, string Name);

internal sealed record SetupPlan(
    string Name,
    string ChatModel,
    string EmbeddingModel,
    double MinimumFreeSpaceGb,
    string Reason,
    bool IsSupported = true)
{
    public IReadOnlyList<ModelRequirement> Models => new[]
    {
        new ModelRequirement("model chat", ChatModel),
        new ModelRequirement("model embedding", EmbeddingModel),
    };
}

internal static class ModelSelector
{
    public static SetupPlan Select(HardwareInfo hardware)
    {
        if (hardware.MemoryGb < 8)
        {
            return new SetupPlan("Không đủ tài nguyên", "", "", 0,
                "Cần ít nhất 8 GB RAM để chạy DocuMind và Ollama local ổn định.", false);
        }

        if (hardware.NvidiaVramGb >= 16 && hardware.MemoryGb >= 32)
        {
            return new SetupPlan("Mạnh", "qwen3:14b", "bge-m3", 24,
                "GPU NVIDIA từ 16 GB VRAM và RAM từ 32 GB.");
        }

        if (hardware.NvidiaVramGb >= 10 && hardware.MemoryGb >= 24)
        {
            return new SetupPlan("Cân bằng cao", "qwen3:8b", "bge-m3", 20,
                "GPU NVIDIA từ 10 GB VRAM và RAM từ 24 GB.");
        }

        if (hardware.NvidiaVramGb >= 6 && hardware.MemoryGb >= 16)
        {
            return new SetupPlan("Khuyến nghị", "qwen3:4b-instruct", "bge-m3", 16,
                "GPU NVIDIA từ 6 GB VRAM và RAM từ 16 GB. Đây là mức đã kiểm tra cho DocuMind.");
        }

        if (hardware.MemoryGb >= 24)
        {
            return new SetupPlan("CPU mạnh", "qwen3:4b-instruct", "bge-m3", 16,
                "Không phát hiện GPU NVIDIA đủ VRAM; dùng CPU với RAM từ 24 GB, tốc độ sẽ chậm hơn.");
        }

        return new SetupPlan("Tiết kiệm", "qwen3:1.7b", "nomic-embed-text", 12,
            "RAM từ 8 GB. Phù hợp dùng thử; RAG tiếng Việt có thể kém hơn BGE M3.");
    }
}

internal static class StorageSelector
{
    public static string? Select(IReadOnlyList<DriveSnapshot> drives, double minimumFreeSpaceGb)
    {
        var candidate = drives
            .Where(d => d.FreeGb >= minimumFreeSpaceGb)
            .OrderBy(d => d.IsSystemDrive)
            .ThenBy(d => string.Equals(d.BusType, "USB", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(d => d.FreeGb)
            .FirstOrDefault();
        return candidate is null ? null : Path.Combine(candidate.Root, "DocuMind", "OllamaModels");
    }
}
