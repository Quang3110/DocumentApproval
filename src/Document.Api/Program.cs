var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var documents = new List<Document>
{
    new(Guid.NewGuid(), "Quy trình phê duyệt tài liệu", "system", "Draft", DateTime.UtcNow),
    new(Guid.NewGuid(), "Biểu mẫu đề nghị phê duyệt", "system", "Draft", DateTime.UtcNow)
};
var documentsLock = new object();

app.MapGet("/", () => Results.Ok(new
{
    service = "Document.Api",
    status = "running"
}));

app.MapGet("/documents", () =>
{
    lock (documentsLock)
    {
        return Results.Ok(documents.ToList());
    }
});

app.MapPost("/documents", (DocumentRequest? request) =>
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(request?.Title))
    {
        errors.Add("title là bắt buộc và không được để trống.");
    }

    if (string.IsNullOrWhiteSpace(request?.CreatedBy))
    {
        errors.Add("createdBy là bắt buộc và không được để trống.");
    }

    if (errors.Count > 0)
    {
        return Results.BadRequest(new
        {
            message = "Dữ liệu không hợp lệ.",
            errors
        });
    }

    var document = new Document(
        Guid.NewGuid(),
        request!.Title!.Trim(),
        request.CreatedBy!.Trim(),
        "Draft",
        DateTime.UtcNow);

    lock (documentsLock)
    {
        documents.Add(document);
    }

    return Results.Created($"/documents/{document.Id}", document);
});

app.Run();

record DocumentRequest(string? Title, string? CreatedBy);

record Document(Guid Id, string Title, string CreatedBy, string Status, DateTime CreatedAt);
