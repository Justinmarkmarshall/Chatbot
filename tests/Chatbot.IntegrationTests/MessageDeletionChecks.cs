using System.Security.Claims;
using Chatbot.Models;
using Chatbot.Services;

static class MessageDeletionChecks
{
    public static async Task Run(ChatService chats, ClaimsPrincipal owner, ClaimsPrincipal other, FakeOllama ollama, Action<bool, string> check)
    {
        async Task Ask(Guid id, string text)
        {
            await foreach (var _ in chats.SendAsync(owner, id, text)) { }
        }
        async Task Denied(Func<Task> action, string label)
        {
            try { await action(); }
            catch (ChatNotFoundException) { check(true, label); return; }
            throw new Exception("Expected ownership rejection: " + label);
        }
        foreach (string role in new[] { "user", "assistant" })
        {
            var tab = await chats.CreateAsync(owner, "Delete " + role);
            var unrelated = await chats.CreateAsync(owner, "Other deletion tab");
            await Ask(tab.Id, "keep first");
            await Ask(tab.Id, "remove turn");
            await Ask(tab.Id, "keep last");
            var before = await chats.GetAsync(owner, tab.Id);
            var selected = before.Messages.Single(m => m.Role == role && m.Content.Contains("remove turn"));
            await Denied(() => chats.DeleteMessageAsync(other, tab.Id, selected.Id), $"Foreign owner cannot delete a {role} message");
            await Denied(() => chats.DeleteMessageAsync(owner, unrelated.Id, selected.Id), $"Message ID cannot bypass chat boundary for {role} deletion");
            await chats.DeleteMessageAsync(owner, tab.Id, selected.Id);
            var after = await chats.GetAsync(owner, tab.Id);
            check(after.Messages.Count == 5 && after.Messages.All(m => m.Id != selected.Id)
                && after.Messages.Count(m => m.TurnId == selected.TurnId) == 1,
                $"Deleting {role} removes only the selected saved message");
            await Ask(tab.Id, "follow up");
            check(ollama.Calls.Last().Select(m => m.Content).SequenceEqual(
                ["keep first", "answer: keep first", "keep last", "answer: keep last", "follow up"]),
                $"Deleting {role} excludes the incomplete turn from Ollama while preserving complete history order");
            await chats.DeleteAsync(owner, tab.Id);
            await chats.DeleteAsync(owner, unrelated.Id);
        }

        var historyTab = await chats.CreateAsync(owner, "Ten complete turns");
        for (int i = 0; i < 12; i++) await Ask(historyTab.Id, $"turn {i}");
        var history = await chats.GetAsync(owner, historyTab.Id);
        await chats.DeleteMessageAsync(owner, historyTab.Id, history.Messages.Single(m => m.Role == "user" && m.Content == "turn 10").Id);
        await Ask(historyTab.Id, "next");
        string[] expected = Enumerable.Range(1, 11).Where(i => i != 10)
            .SelectMany(i => new[] { $"turn {i}", $"answer: turn {i}" }).Append("next").ToArray();
        check(ollama.Calls.Last().Select(m => m.Content).SequenceEqual(expected),
            "History retains the latest ten complete turns after deleting a recent user message");
        await chats.DeleteAsync(owner, historyTab.Id);
    }
}
