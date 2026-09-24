namespace Chatbot.Models;

public sealed record ChatSession(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record StoredChatMessage(Guid Id, Guid TurnId, long Sequence, string Role, string Content, string Status, DateTime CreatedAt,
    IReadOnlyList<DocumentSource>? Sources = null);
public sealed record ChatConversation(ChatSession Session, IReadOnlyList<StoredChatMessage> Messages);
public sealed record ConversationMessage(string Role, string Content);
public sealed class ChatNotFoundException() : Exception("Chat not found.");
public sealed class ChatBusyException() : Exception("This chat is already generating a reply. Wait for it to finish or cancel it in the other tab.");
