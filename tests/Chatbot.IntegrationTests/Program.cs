using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using AIPlatform.Api.Controllers;
using AIPlatform.Api.Models;
using AIPlatform.Api.Options;
using AIPlatform.Api.Services;
using Chatbot.Models;
using Chatbot.Persistence;
using Chatbot.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

string connectionString = Environment.GetEnvironmentVariable("CHATBOT_TEST_CONNECTION") ?? throw new InvalidOperationException("Set CHATBOT_TEST_CONNECTION to the disposable test database.");
if (new NpgsqlConnectionStringBuilder(connectionString).Database != "chatbot_persistence_tests")
    throw new InvalidOperationException("Tests require the dedicated chatbot_persistence_tests database.");
await using var dataSource = NpgsqlDataSource.Create(connectionString);
var checks = new List<string>();
void Check(bool condition, string label)
{
    if (!condition) throw new Exception("FAIL: " + label);
    checks.Add(label); Console.WriteLine("PASS: " + label);
}
async Task Throws<T>(Func<Task> action, string label) where T : Exception
{
    try { await action(); }
    catch (T) { Check(true, label); return; }
    throw new Exception("Expected " + typeof(T).Name + ": " + label);
}
ClaimsPrincipal User(string? subject, string email = "shared@example.test") => new(new ClaimsIdentity(
    (subject is null ? Array.Empty<Claim>() : [new Claim(ClaimTypes.NameIdentifier, subject)])
    .Append(new Claim(ClaimTypes.Email, email)), "Test"));
string run = Guid.NewGuid().ToString("N");
var alice = User("alice-" + run); var bob = User("bob-" + run);
var fake = new FakeOllama();
var store = new ChatStore(dataSource);
using var logs = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning).AddConsole());
using var queryEmbedding = new QueryEmbedding(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
{
    ["Documents:ModelRoot"] = Environment.GetEnvironmentVariable("CHATBOT_TEST_MODEL_ROOT") ?? Path.GetFullPath("experiments/embedding-spike/models")
}).Build());
ChatService Service(ChatStore s, NpgsqlDataSource? ds = null) => new(s, fake, logs.CreateLogger<ChatService>(), new DocumentRetrieval(ds ?? dataSource,queryEmbedding));
var service = Service(store);
var deploymentConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
{
    ["Database:Host"]="chatbot-postgres", ["Database:Name"]="chatbot", ["Database:Username"]="chatbot",
    ["Database:Password"]="test;password=with\"delimiters"
}).Build();
var parsedDeployment = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.ConnectionString(deploymentConfiguration));
Check(parsedDeployment.Host=="chatbot-postgres" && parsedDeployment.Password=="test;password=with\"delimiters" && parsedDeployment.Port==5432,
    "Kubernetes database fields safely encode password delimiters and internal DNS");
var legacyConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:Chatbot"]=connectionString }).Build();
Check(DatabaseConfiguration.ConnectionString(legacyConfiguration)==connectionString,"Existing local connection-string configuration remains compatible");
await Task.WhenAll(ChatDatabase.InitializeAsync(dataSource), ChatDatabase.InitializeAsync(dataSource));
Check(true, "Concurrent schema initialization succeeds");
await Throws<UnauthorizedAccessException>(() => service.ListAsync(new ClaimsPrincipal()), "Anonymous service access denied");
await Throws<UnauthorizedAccessException>(() => service.ListAsync(User(null)), "Authenticated identity without stable subject denied");
var a = await service.CreateAsync(alice, "Kubernetes");
var mortgage = await service.CreateAsync(alice, "Mortgage");
var b = await service.CreateAsync(bob, "Bob private");
Check((await service.ListAsync(alice)).Select(s => s.Id).ToHashSet().SetEquals([a.Id, mortgage.Id]), "Multiple tabs belong to stable subject, not shared email");
await service.RenameAsync(alice, a.Id, "  Cluster notes  ");
Check((await service.GetAsync(alice, a.Id)).Session.Title == "Cluster notes", "Title is persisted and trimmed");
await Throws<ArgumentException>(() => service.RenameAsync(alice, a.Id, " "), "Blank title rejected");
await Throws<ArgumentException>(() => service.RenameAsync(alice, a.Id, new string('x', 121)), "Oversized title rejected");
await Throws<ChatNotFoundException>(() => service.GetAsync(bob, a.Id), "Cross-owner read returns not found");
await Throws<ChatNotFoundException>(() => service.RenameAsync(bob, a.Id, "stolen"), "Cross-owner rename denied");
async Task Drain(ChatService chat, ClaimsPrincipal user, Guid id, string message, CancellationToken ct = default)
{
    await foreach (var _ in chat.SendAsync(user, id, message, ct)) { }
}
await Throws<ChatNotFoundException>(() => Drain(service, bob, a.Id, "attack"), "Cross-owner send denied before model invocation");
Check(fake.Calls.Count == 0, "Unauthorized messages never reach Ollama");
await Throws<ArgumentException>(() => Drain(service, alice, a.Id, new string('x', 16001)), "Oversized prompt rejected");
var seedDocuments = new List<Guid>();
async Task SeedChatDocument(ClaimsPrincipal user, Guid chat)
{
    using var gate = new DocumentUploads();
    var docs = new DocumentService(dataSource,gate,logs.CreateLogger<DocumentService>());
    var doc = await docs.UploadAsync(user,chat,"chat-test.txt",new MemoryStream(System.Text.Encoding.UTF8.GetBytes("# Cedar\nCedar is the project for this conversation fixture.")));
    seedDocuments.Add(doc.Id);
    using var model = Chatbot.Processing.DocumentWorker.LoadModel(Environment.GetEnvironmentVariable("CHATBOT_TEST_MODEL_ROOT")!);
    var worker = new Chatbot.Processing.DocumentWorker(dataSource,new Chatbot.Processing.DocumentProcessor(model),logs.CreateLogger<Chatbot.Processing.DocumentWorker>());
    while(await worker.ProcessNextAsync()) { }
}
await SeedChatDocument(alice,a.Id);
await SeedChatDocument(alice,mortgage.Id);
await Drain(service, alice, a.Id, "Remember cedar");
await Drain(service, alice, a.Id, "What did I say?");
var conversation = await service.GetAsync(alice, a.Id);
Check(conversation.Messages.Count == 4 && conversation.Messages.All(m => m.Status == "completed") &&
    conversation.Messages.Select(m => m.Role).SequenceEqual(["user", "assistant", "user", "assistant"]), "Ordered user and assistant messages persist");
Check(fake.Calls.Last().Where(m => m.Role != "system" && !m.Content.StartsWith("Uploaded-document context for this turn")).Select(m => m.Content).SequenceEqual(["Remember cedar", "answer: Remember cedar", "What did I say?"]), "Ollama receives ordered previous completed conversation");
await Drain(service, alice, mortgage.Id, "Separate topic");
Check(fake.Calls.Last().Count == 3, "No context leakage between tabs");

await using (var independentSource = NpgsqlDataSource.Create(connectionString))
{
    await ChatDatabase.InitializeAsync(independentSource);
    var restored = await Service(new ChatStore(independentSource)).GetAsync(User("alice-" + run, "changed@example.test"), a.Id);
    Check(restored.Session.Title == "Cluster notes" && JsonSerializer.Serialize(restored.Messages) == JsonSerializer.Serialize(conversation.Messages), "New application services and changed email restore identical persisted history");
}

await MessageDeletionChecks.Run(service, alice, bob, fake, Check);

using (var cancellation = new CancellationTokenSource())
{
    await using var stream = service.SendAsync(alice, a.Id, "hold", cancellation.Token).GetAsyncEnumerator();
    Check(await stream.MoveNextAsync() && stream.Current == "partial", "Partial streaming reaches caller");
    await Throws<ChatBusyException>(() => Drain(Service(new ChatStore(dataSource)), alice, a.Id, "concurrent"), "Separate service instances cannot interleave a chat turn");
    Check((await service.GetAsync(alice, a.Id)).Messages.Last().Status == "streaming", "Reading an active chat does not mark its generation interrupted");
    cancellation.Cancel();
    await Throws<OperationCanceledException>(async () => { await stream.MoveNextAsync(); }, "Cancellation reaches generation");
}
Check((await service.GetAsync(alice, a.Id)).Messages.Last() is { Status: "cancelled", Content: "partial" }, "Cancelled partial reply is durably saved");
await Throws<IOException>(() => Drain(service, alice, a.Id, "fail"), "Ollama failure reaches caller");
Check((await service.GetAsync(alice, a.Id)).Messages.Last() is { Status: "failed", Content: "partial" }, "Failed partial reply is durably saved");
await Throws<InvalidDataException>(() => Drain(service, alice, a.Id, "empty"), "Empty model reply is not reported as completed");
await Drain(service, alice, a.Id, "After failure");
Check(!fake.Calls.Last().Any(m => m.Content is "hold" or "fail" or "empty" or "partial"), "Failed and cancelled turns excluded from model history");

var interrupted = await store.BeginTurnAsync("alice-" + run, a.Id, "process interrupted", default);
await store.SaveReplyAsync(interrupted, "checkpoint", "streaming", default);
await interrupted.DisposeAsync(); // Simulate process/connection loss: the lock is gone.
Check((await service.GetAsync(alice, a.Id)).Messages.Last() is { Status: "interrupted", Content: "checkpoint" }, "Orphaned generation recovers with its saved checkpoint");
await using (var nextTurn = await store.BeginTurnAsync("alice-" + run, a.Id, "new turn", default))
{
    await Throws<ChatNotFoundException>(() => store.SaveReplyAsync(interrupted, "stale overwrite", "completed", default), "Generation fence rejects stale writer after ownership of turn changes");
    await store.SaveReplyAsync(nextTurn, "new answer", "completed", default);
}

// Exercise the real HTTP controller, authentication and antiforgery filters on Kestrel.
var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Warning);
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddSingleton(dataSource);
builder.Services.AddSingleton<IOllamaClient>(fake);
builder.Services.AddSingleton<IQueryEmbedding>(queryEmbedding); builder.Services.AddScoped<DocumentRetrieval>();
builder.Services.AddScoped<ChatStore>(); builder.Services.AddScoped<IChatStore>(sp => sp.GetRequiredService<ChatStore>());
builder.Services.AddScoped<IDocumentRetrieval>(sp => sp.GetRequiredService<DocumentRetrieval>()); builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<DocumentUploads>(); builder.Services.AddScoped<DocumentService>();
builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
builder.Services.AddAuthorization(); builder.Services.AddAntiforgery();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres");
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(ChatController).Assembly);
await using var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization(); app.UseAntiforgery(); app.MapControllers();
app.MapChatbotHealthEndpoints();
app.MapRazorComponents<Chatbot.Components.App>().AddInteractiveServerRenderMode();
await app.StartAsync();
string url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
HttpClient Client(string? subject)
{
    var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(url) };
    if (subject is not null) client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
    return client;
}
async Task Token(HttpClient client)
{
    var json = await client.GetFromJsonAsync<JsonElement>("/api/chat/antiforgery");
    client.DefaultRequestHeaders.Add("RequestVerificationToken", json.GetProperty("token").GetString());
}
using var anonymous = Client(null); using var httpA = Client("alice-" + run); using var httpB = Client("bob-" + run);
Check((await anonymous.GetAsync("/health/live")).StatusCode==HttpStatusCode.OK,"Anonymous Kubernetes liveness endpoint responds without authentication");
Check((await anonymous.GetAsync("/health/ready")).StatusCode==HttpStatusCode.OK,"Readiness checks the actual PostgreSQL connection");
await using (var unavailableDatabase=NpgsqlDataSource.Create("Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=test;Timeout=1"))
{
    var health=await new DatabaseHealthCheck(unavailableDatabase).CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());
    Check(health.Status==Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy && health.Exception is null,"Database readiness failure exposes no connection details");
}
Check((await anonymous.GetAsync("/api/chat/sessions")).StatusCode == HttpStatusCode.Unauthorized, "HTTP anonymous history rejected");
var withoutToken = await httpA.PostAsJsonAsync("/api/chat/sessions", new { title = "No CSRF token" });
Check(withoutToken.StatusCode == HttpStatusCode.BadRequest, $"Cookie-auth mutations require antiforgery token (actual {(int)withoutToken.StatusCode})");
await Token(httpA); await Token(httpB);
var created = await httpA.PostAsJsonAsync("/api/chat/sessions", new { title = "HTTP chat", ownerSubject = "bob-" + run });
Check(created.StatusCode == HttpStatusCode.Created, "HTTP session creation succeeds");
var httpSession = (await created.Content.ReadFromJsonAsync<ChatSession>())!;
Check((await httpB.GetAsync($"/api/chat/sessions/{httpSession.Id}")).StatusCode == HttpStatusCode.NotFound, "HTTP ignores supplied owner and isolates subjects");
Check((await httpB.PatchAsJsonAsync($"/api/chat/sessions/{httpSession.Id}", new { title = "stolen" })).StatusCode == HttpStatusCode.NotFound, "HTTP cross-owner rename returns 404");
Check((await httpB.PostAsJsonAsync($"/api/chat/sessions/{httpSession.Id}/messages", new { message = "steal" })).StatusCode == HttpStatusCode.NotFound, "HTTP cross-owner send returns 404 before streaming");
await SeedChatDocument(alice,httpSession.Id);
var reply = await httpA.PostAsJsonAsync($"/api/chat/sessions/{httpSession.Id}/messages", new { message = "HTTP hello" });
Check(reply.IsSuccessStatusCode && await reply.Content.ReadAsStringAsync() == "answer: HTTP hello", "HTTP streaming endpoint returns persisted assistant content");
var apiHistory = (await httpA.GetFromJsonAsync<ChatConversation>($"/api/chat/sessions/{httpSession.Id}"))!;
Check(apiHistory.Messages.Count == 2, "HTTP history includes both persisted messages");
var rendered = await httpA.GetStringAsync($"/chat/{httpSession.Id}");
Check(rendered.Contains("HTTP chat") && rendered.Contains("HTTP hello") && rendered.Contains("Save title") && rendered.Contains("New chat"), "Razor page renders saved tabs, title controls and restored messages");
var forbiddenPage = await httpB.GetStringAsync($"/chat/{httpSession.Id}");
Check(!forbiddenPage.Contains("HTTP hello") && forbiddenPage.Contains("This chat is unavailable"), "Razor page does not render another account's conversation");
await using (var held = await store.BeginTurnAsync("alice-" + run, httpSession.Id, "held for HTTP test", default))
{
    var busy = await httpA.PostAsJsonAsync($"/api/chat/sessions/{httpSession.Id}/messages", new { message = "race" });
    Check(busy.StatusCode == HttpStatusCode.Conflict, "HTTP concurrent send returns 409 before streaming");
    await store.SaveReplyAsync(held, "test complete", "completed", default);
}
var compatibility = await httpA.PostAsJsonAsync("/api/chat", new { message = "Legacy URL" });
Check(compatibility.IsSuccessStatusCode && compatibility.Headers.Contains("X-Chat-Session-Id"), "Original API URL returns reusable persistent session ID");
using var loggedInAgain = Client("alice-" + run);
Check((await loggedInAgain.GetFromJsonAsync<ChatConversation>($"/api/chat/sessions/{httpSession.Id}"))!.Messages.Count == 4, "A fresh authenticated browser session restores history");

// Remove only synthetic fixture originals before upload lifecycle assertions; source snapshots remain.
await using (var cleanup = dataSource.CreateCommand("DELETE FROM chat_documents WHERE id=ANY($1)"))
{ cleanup.Parameters.AddWithValue(seedDocuments.ToArray()); await cleanup.ExecuteNonQueryAsync(); }
// Milestone 2: original-document persistence, ownership and upload lifecycle.
using var uploadGate = new DocumentUploads();
DocumentService DocumentServiceFor(NpgsqlDataSource ds) => new(ds, uploadGate, logs.CreateLogger<DocumentService>());
var documents = DocumentServiceFor(dataSource);
byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes("Original text with accents: caf\u00e9.\r\nKeep these exact bytes.\n");
var document = await documents.UploadAsync(alice, a.Id, "../notes.txt", new MemoryStream(originalBytes));
Check(document is { Status: "uploaded", FileName: "notes.txt", MediaType: "text/plain" } && document.ByteLength == originalBytes.Length,
    "Document original is attached to selected chat with sanitized metadata and Uploaded status");
Check(document.Sha256 == Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(originalBytes)), "Document digest calculated from original bytes");
Check((await documents.DownloadAsync(alice, a.Id, document.Id)).Content.SequenceEqual(originalBytes), "Downloaded original is byte-for-byte identical");
Check((await documents.ListAsync(alice, mortgage.Id)).Count == 0, "Other chat does not list this document");
await Throws<ChatNotFoundException>(() => documents.GetAsync(alice, mortgage.Id, document.Id), "Document ID cannot bypass chat attachment boundary");
await Throws<ChatNotFoundException>(() => documents.DownloadAsync(bob, a.Id, document.Id), "Foreign account cannot download original");
await Throws<ChatNotFoundException>(() => documents.ListAsync(bob, a.Id), "Foreign account cannot list documents");
var unread = new MemoryStream(originalBytes);
await Throws<ChatNotFoundException>(() => documents.UploadAsync(bob, a.Id, "notes.txt", unread), "Foreign upload denied before reading stream");
Check(unread.Position == 0, "Unauthorized upload stream remains unread");
await Throws<UnauthorizedAccessException>(() => documents.ListAsync(User(null), a.Id), "Document service requires stable authenticated subject");
await Throws<ArgumentException>(() => documents.UploadAsync(alice, a.Id, "bad.html", new MemoryStream(originalBytes)), "Unsupported document extension rejected");
await Throws<ArgumentException>(() => documents.UploadAsync(alice, a.Id, "fake.pdf", new MemoryStream(originalBytes)), "PDF signature checked before publishing original");
await Throws<ArgumentException>(() => documents.UploadAsync(alice, a.Id, "bad.txt", new MemoryStream([0xff, 0xfe, 0x00])), "Invalid UTF-8 rejected");
await Throws<ArgumentException>(() => documents.UploadAsync(alice, a.Id, "empty.txt", new MemoryStream()), "Empty original rejected");
var failedDocs = await documents.ListAsync(alice, a.Id);
Check(failedDocs.Count(d => d.Status == "failed" && d.ErrorCode == "invalid_content") == 3, "Invalid-content failures persist with inspectable status");
await Throws<ChatNotFoundException>(() => documents.DownloadAsync(alice, a.Id, failedDocs.First(d => d.Status == "failed").Id), "Failed uploads have no downloadable original");
await Throws<ArgumentException>(() => documents.UploadAsync(alice, a.Id, "large.txt", new MemoryStream(new byte[DocumentService.MaxBytes + 1])), "Actual streamed bytes enforce file-size limit");
using (var uploadCancellation = new CancellationTokenSource())
{
    var stalled = new StalledUploadStream();
    Task<ChatDocument> uploading = documents.UploadAsync(alice, a.Id, "cancel.txt", stalled, uploadCancellation.Token);
    await stalled.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
    Check((await documents.ListAsync(alice, a.Id)).Any(d => d.FileName == "cancel.txt" && d.Status == "uploading"), "Uploading status visible before original publication");
    uploadCancellation.Cancel();
    await Throws<OperationCanceledException>(async () => { await uploading; }, "Upload cancellation stops reading");
}
Check((await documents.ListAsync(alice, a.Id)).Single(d => d.FileName == "cancel.txt").ErrorCode == "upload_cancelled", "Cancelled upload status persists independently of request token");
await using (var restoredSource = NpgsqlDataSource.Create(connectionString))
    Check((await DocumentServiceFor(restoredSource).DownloadAsync(User("alice-" + run, "new-email@example.test"), a.Id, document.Id)).Content.SequenceEqual(originalBytes),
        "New services and changed email restore original document bytes");

var quotaUser = User("quota-" + run);
var quotaChat = await service.CreateAsync(quotaUser);
await using (var reserve = dataSource.CreateCommand("INSERT INTO chat_documents(id,session_id,filename,media_type,status) SELECT gen_random_uuid(),$1,'pending.txt','text/plain','uploading' FROM generate_series(1,10)"))
{ reserve.Parameters.AddWithValue(quotaChat.Id); await reserve.ExecuteNonQueryAsync(); }
await Throws<DocumentLimitException>(() => documents.UploadAsync(quotaUser, quotaChat.Id, "quota.txt", new MemoryStream(originalBytes)), "Account quota includes simultaneous upload reservations");
await using (var expire = dataSource.CreateCommand("UPDATE chat_documents SET upload_deadline=now()-interval '1 second' WHERE session_id=$1"))
{ expire.Parameters.AddWithValue(quotaChat.Id); await expire.ExecuteNonQueryAsync(); }
Check((await documents.ListAsync(quotaUser, quotaChat.Id)).All(d => d.Status == "failed" && d.ErrorCode == "upload_interrupted"), "Expired upload reservations recover after process loss");
Check((await documents.UploadAsync(quotaUser, quotaChat.Id, "after-expiry.txt", new MemoryStream(originalBytes))).Status == "uploaded", "Expired reservations release byte quota");

MultipartFormDataContent Multipart(string filename, byte[] bytes)
{
    var form = new MultipartFormDataContent();
    form.Add(new ByteArrayContent(bytes), "file", filename);
    return form;
}
using (var noCsrfClient = Client("alice-" + run))
using (var form = Multipart("notes.txt", originalBytes))
    Check((await noCsrfClient.PostAsync($"/api/chat/sessions/{a.Id}/documents", form)).StatusCode == HttpStatusCode.BadRequest, "Multipart uploads require antiforgery validation");
using (var form = Multipart("notes.txt", originalBytes))
    Check((await httpB.PostAsync($"/api/chat/sessions/{a.Id}/documents", form)).StatusCode == HttpStatusCode.NotFound, "HTTP upload to another account's chat returns 404");
using (var form = Multipart("api-notes.txt", originalBytes))
{
    var uploadResponse = await httpA.PostAsync($"/api/chat/sessions/{a.Id}/documents", form);
    Check(uploadResponse.StatusCode == HttpStatusCode.Created, "Multipart HTTP upload returns persisted document and status URL");
    var uploaded = (await uploadResponse.Content.ReadFromJsonAsync<ChatDocument>())!;
    var download = await httpA.GetAsync($"/api/chat/sessions/{a.Id}/documents/{uploaded.Id}/original");
    Check(download.IsSuccessStatusCode && (await download.Content.ReadAsByteArrayAsync()).SequenceEqual(originalBytes)
        && download.Content.Headers.ContentDisposition?.DispositionType == "attachment"
        && download.Headers.GetValues("X-Content-Type-Options").Single() == "nosniff", "HTTP original download forces attachment and preserves bytes");
    Check((await httpB.GetAsync($"/api/chat/sessions/{a.Id}/documents/{uploaded.Id}/original")).StatusCode == HttpStatusCode.NotFound, "HTTP cross-account original download returns 404");
}
var documentPage = await httpA.GetStringAsync($"/chat/{a.Id}");
Check(documentPage.Contains("api-notes.txt") && documentPage.Contains("Uploaded - original saved") && documentPage.Contains("Download original"), "Razor page restores document names, status and original-download links");
Check(!fake.Calls.Any(c => c.Any(m => m.Content.Contains("Original text with accents"))), "Uploaded document content never enters Ollama context in milestone 2");

var signatureFixture = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF\n");
var pdf = await documents.UploadAsync(alice, mortgage.Id, "signature-fixture.pdf", new MemoryStream(signatureFixture));
Check(pdf.MediaType == "application/pdf" && (await documents.DownloadAsync(alice, mortgage.Id, pdf.Id)).Content.SequenceEqual(signatureFixture),
    "PDF signature-only acceptance preserves original without pretending to parse it");

// Upgrade a real version-1 schema with existing chat data, isolated in a test-only schema.
string upgradeSchema = "upgrade_" + run;
await using (var createSchema = dataSource.CreateCommand($"CREATE SCHEMA {upgradeSchema}")) await createSchema.ExecuteNonQueryAsync();
var upgradeConnection = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = upgradeSchema }.ConnectionString;
await using (var upgradeSource = NpgsqlDataSource.Create(upgradeConnection))
{
    await using var versionOneStream = typeof(ChatDatabase).Assembly.GetManifestResourceStream("Chatbot.Persistence.Migrations.001_chat_sessions.sql")!;
    using var versionOneReader = new StreamReader(versionOneStream);
    await using (var v1 = upgradeSource.CreateCommand(await versionOneReader.ReadToEndAsync())) await v1.ExecuteNonQueryAsync();
    await using (var ledger = upgradeSource.CreateCommand("CREATE TABLE chatbot_schema_migrations(version integer PRIMARY KEY,applied_at timestamptz NOT NULL DEFAULT now()); INSERT INTO chatbot_schema_migrations(version) VALUES(1)")) await ledger.ExecuteNonQueryAsync();
    var upgradeStore = new ChatStore(upgradeSource);
    var upgradeChatService = Service(upgradeStore, upgradeSource);
    var prior = await upgradeChatService.CreateAsync(alice, "Existing milestone 1 chat");
    // Seed the old schema through its storage contract, before applying new application migrations.
    await using (var oldTurn = await upgradeStore.BeginTurnAsync(alice.FindFirstValue(ClaimTypes.NameIdentifier)!, prior.Id, "Keep this message", default))
        await upgradeStore.SaveReplyAsync(oldTurn, "Original saved answer", "completed", default);
    await ChatDatabase.InitializeAsync(upgradeSource);
    await ChatDatabase.InitializeAsync(upgradeSource);
    var afterUpgrade = await upgradeChatService.GetAsync(alice, prior.Id);
    var migratedDocument = await DocumentServiceFor(upgradeSource).UploadAsync(alice, prior.Id, "new.txt", new MemoryStream(originalBytes));
    Check(afterUpgrade.Session.Title == prior.Title && afterUpgrade.Messages.Select(m => m.Content).SequenceEqual(["Keep this message", "Original saved answer"]) && afterUpgrade.Messages.All(m => m.Sources is null) && migratedDocument.Status == "uploaded",
        "Version-1 database upgrades idempotently to documents without altering existing chat history");
}
await ProcessingChecks.Run(dataSource, documents, alice, bob, mortgage.Id, Check);
await RetrievalChecks.Run(dataSource, documents, alice, bob, queryEmbedding, httpA, Check);
// Delete indexed originals through the real authenticated endpoint; database cascade removes vectors.
var deleteTarget = (await documents.ListAsync(alice,mortgage.Id)).First(d=>d.ProcessingStatus=="ready" && d.ChunkCount>0);
string deleteUrl = $"/api/chat/sessions/{mortgage.Id}/documents/{deleteTarget.Id}";
using(var noDeleteToken=Client("alice-"+run))
    Check((await noDeleteToken.DeleteAsync(deleteUrl)).StatusCode==HttpStatusCode.BadRequest,"Document deletion requires antiforgery token");
Check((await httpB.DeleteAsync(deleteUrl)).StatusCode==HttpStatusCode.NotFound,"Other account cannot delete document");
Check((await httpA.DeleteAsync($"/api/chat/sessions/{a.Id}/documents/{deleteTarget.Id}")).StatusCode==HttpStatusCode.NotFound,"Document deletion cannot bypass chat boundary");
await Throws<UnauthorizedAccessException>(()=>documents.DeleteAsync(new ClaimsPrincipal(),mortgage.Id,deleteTarget.Id),"Anonymous service cannot delete document");
Check((await httpA.GetStringAsync($"/chat/{mortgage.Id}")).Contains("class=\"document-delete\""),"Document list renders small delete control beside names");
Check((await httpA.DeleteAsync(deleteUrl)).StatusCode==HttpStatusCode.NoContent,"Owner can delete indexed document through API");
await Throws<ChatNotFoundException>(()=>documents.DownloadAsync(alice,mortgage.Id,deleteTarget.Id),"Deleted original is no longer downloadable");
await using(var chunks=dataSource.CreateCommand("SELECT count(*) FROM document_chunks WHERE document_id=$1"))
{
    chunks.Parameters.AddWithValue(deleteTarget.Id);
    Check((long)(await chunks.ExecuteScalarAsync())! == 0,"Deleting document cascades to all indexed chunks and vectors");
}
Check(!(await documents.ListAsync(alice,mortgage.Id)).Any(d=>d.Id==deleteTarget.Id),"Deleted document disappears from saved list");
var inProgress = await documents.UploadAsync(alice,mortgage.Id,"delete-processing.txt",new MemoryStream(originalBytes));
Guid staleWorkerToken=Guid.NewGuid();
await using(var claim=dataSource.CreateCommand("UPDATE chat_documents SET processing_status='processing',processing_token=$2 WHERE id=$1"))
{ claim.Parameters.AddWithValue(inProgress.Id);claim.Parameters.AddWithValue(staleWorkerToken);await claim.ExecuteNonQueryAsync(); }
await documents.DeleteAsync(alice,mortgage.Id,inProgress.Id);
await using(var stale=dataSource.CreateCommand("UPDATE chat_documents SET processing_status='ready' WHERE id=$1 AND processing_token=$2"))
{
    stale.Parameters.AddWithValue(inProgress.Id);stale.Parameters.AddWithValue(staleWorkerToken);
    Check(await stale.ExecuteNonQueryAsync()==0,"In-flight worker cannot republish a deleted document");
}
await app.StopAsync();

// Test actual Ollama HTTP serialization and truncated-stream detection independently.
var handler = new OllamaHandler();
var realClient = new OllamaClient(new HttpClient(handler) { BaseAddress = new Uri("http://ollama.test") }, Options.Create(new OllamaOptions { BaseUrl = "http://ollama.test", Model = "qwen3:1.7b" }));
var context = new ConversationMessage[] { new("user", "first"), new("assistant", "answer"), new("user", "next") };
var output = "";
await foreach (var chunk in realClient.StreamChatAsync(context)) output += chunk;
Check(output == "ok" && handler.Body!.RootElement.GetProperty("messages").GetArrayLength() == 3 &&
    handler.Body.RootElement.GetProperty("messages")[1].GetProperty("role").GetString() == "assistant", "Ollama transport serializes full context with correct role names");
handler.Truncate = true;
await Throws<IOException>(async () => { await foreach (var _ in realClient.StreamChatAsync(context)) { } }, "Premature Ollama EOF is detected");
Console.WriteLine($"All {checks.Count} integration checks passed against real PostgreSQL.");

sealed class FakeOllama : IOllamaClient
{
    public List<IReadOnlyList<ConversationMessage>> Calls { get; } = [];
    public async IAsyncEnumerable<string> StreamChatAsync(IReadOnlyList<ConversationMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls.Add(messages.ToArray());
        string prompt = messages.Last().Content;
        if (prompt == "empty") yield break;
        if (prompt is "hold" or "fail")
        {
            yield return "partial";
            if (prompt == "hold") await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new IOException("Deliberate test failure");
        }
        yield return "answer: ";
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return prompt;
    }
}
sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string subject = Request.Headers["X-Test-Subject"].ToString();
        if (string.IsNullOrEmpty(subject)) return Task.FromResult(AuthenticateResult.NoResult());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, subject)], Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
sealed class OllamaHandler : HttpMessageHandler
{
    public bool Truncate { get; set; }
    public JsonDocument? Body { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Body?.Dispose(); Body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
        return new(HttpStatusCode.OK) { Content = new StringContent("{\"message\":{\"role\":\"assistant\",\"content\":\"ok\"},\"done\":false}\n" +
            (Truncate ? "" : "{\"message\":{\"content\":\"\"},\"done\":true}\n")) };
    }
}

sealed class StalledUploadStream : MemoryStream
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Started.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return 0;
    }
}
