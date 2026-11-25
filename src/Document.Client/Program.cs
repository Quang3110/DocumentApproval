using System.Net.Http.Json;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var apiUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var baseUri) ||
    (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
{
    Console.Error.WriteLine($"Địa chỉ API không hợp lệ: {apiUrl}");
    return 1;
}

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/"),
    Timeout = TimeSpan.FromSeconds(10)
};

try
{
    Console.WriteLine($"API: {apiUrl}");
    Console.WriteLine();
    Console.WriteLine("GET /documents ...");

    var documents = await httpClient.GetFromJsonAsync<List<Document>>("documents");

    Console.WriteLine($"Danh sách tài liệu ({documents?.Count ?? 0}):");
    if (documents is null || documents.Count == 0)
    {
        Console.WriteLine("  (không có tài liệu)");
    }
    else
    {
        foreach (var document in documents)
        {
            Console.WriteLine($"  - {document.Title} | Người tạo: {document.CreatedBy} | Trạng thái: {document.Status}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("POST /documents ...");

    var request = new DocumentRequest("Tai lieu tao tu Console Client", "alice");
    using var response = await httpClient.PostAsJsonAsync("documents", request);

    Console.WriteLine($"HTTP {(int)response.StatusCode} ({response.StatusCode})");

    if (!response.IsSuccessStatusCode)
    {
        var errorBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Nội dung phản hồi:");
        Console.WriteLine(errorBody);
    }

    response.EnsureSuccessStatusCode();

    var created = await response.Content.ReadFromJsonAsync<Document>();
    if (created is null)
    {
        Console.Error.WriteLine("Không đọc được JSON của tài liệu vừa tạo.");
        return 1;
    }

    Console.WriteLine("Đã tạo tài liệu:");
    Console.WriteLine($"  Id: {created.Id}");
    Console.WriteLine($"  Tiêu đề: {created.Title}");
    Console.WriteLine($"  Trạng thái: {created.Status}");

    return 0;
}
catch (HttpRequestException ex) when (ex.StatusCode is not null)
{
    Console.Error.WriteLine("Dừng chương trình do API trả về lỗi.");
    return 1;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine(
        $"Không kết nối được API tại {apiUrl}. Hãy kiểm tra API đã chạy và địa chỉ có đúng không. Chi tiết: {ex.Message}");
    return 1;
}
catch (TaskCanceledException)
{
    Console.Error.WriteLine($"Yêu cầu tới API tại {apiUrl} đã quá thời gian chờ.");
    return 1;
}

record Document(Guid Id, string Title, string CreatedBy, string Status, DateTime CreatedAt);

record DocumentRequest(string Title, string CreatedBy);
