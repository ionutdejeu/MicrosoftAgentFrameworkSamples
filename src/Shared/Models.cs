// Converted from the provided TypeScript interfaces and types to C# records/classes.
// Note: Some TypeScript-specific constructs (union types, structural typing, flexible schemas, AbortSignal, etc.)
// don't have direct 1:1 equivalents in C#. Where the TS type allowed several shapes (e.g. content can be string or array
// of parts), these are represented as object or JsonElement so they can be deserialized from JSON easily.
// Provider-specific / flexible types (FlexibleSchema, SharedV2ProviderOptions) are represented as generic objects
// or dictionaries so you can plug in a concrete implementation later.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AiSdk.Models
{
    // JSON-ish value. Using object to represent arbitrary JSON (string, number, bool, array, object or null).
    // You can replace object with System.Text.Json.Nodes.JsonNode if you prefer a typed JSON representation.
    using JSONValue = System.Object;

    // Provider options / metadata are represented as dictionaries of string -> arbitrary JSON values.
    // The original TS referenced SharedV2ProviderOptions which wasn't in the snippet; this is a compatible representation.
    public record ProviderOptions(Dictionary<string, JSONValue> Values);

    // Provider metadata alias (SharedV2ProviderMetadata in TS)
    public record ProviderMetadata(Dictionary<string, Dictionary<string, JSONValue>> Values);

    // DataContent: in TS this can be string | Uint8Array | ArrayBuffer | Buffer
    // In C# we support string (e.g. base64 or data URL), byte[] (binary), and Uri (for URLs).
    public record DataContent
    {
        public string? Text { get; init; }           // e.g. base64-encoded string or data url
        public byte[]? Binary { get; init; }         // raw bytes
        public Uri? Url { get; init; }               // remote URL

        public static DataContent FromText(string text) => new() { Text = text };
        public static DataContent FromBinary(byte[] data) => new() { Binary = data };
        public static DataContent FromUrl(string url) => new() { Url = new Uri(url, UriKind.RelativeOrAbsolute) };
    }

    // Base class for Prompt / Message Parts (discriminated by Type property).
    public abstract record MessagePart
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; }
        // providerOptions is present on most parts
        public ProviderOptions? ProviderOptions { get; init; }
    }

    public record TextPart : MessagePart
    {
        public override string Type => "text";
        public string Text { get; init; } = string.Empty;
    }

    public record ImagePart : MessagePart
    {
        public override string Type => "image";
        // either DataContent or URL; represented by DataContent or Uri
        public DataContent? ImageData { get; init; }
        public Uri? ImageUrl { get; init; }
        public string? MediaType { get; init; }
    }

    public record FilePart : MessagePart
    {
        public override string Type => "file";
        // either DataContent or URL
        public DataContent? Data { get; init; }
        public Uri? Url { get; init; }
        public string? Filename { get; init; }
        public string MediaType { get; init; } = string.Empty;
    }

    public record ReasoningPart : MessagePart
    {
        public override string Type => "reasoning";
        public string Text { get; init; } = string.Empty;
    }

    public record ToolCallPart : MessagePart
    {
        public override string Type => "tool-call";
        public string ToolCallId { get; init; } = string.Empty;
        public string ToolName { get; init; } = string.Empty;
        // input is a JSON-serializable object whose schema is tool-dependent; use JsonElement for fidelity.
        public JsonElement Input { get; init; }
        public bool? ProviderExecuted { get; init; }
    }

    // Placeholder for LanguageModelV2ToolResultOutput (not provided in snippet).
    // Represented as arbitrary JSON.
    public record LanguageModelV2ToolResultOutput(JsonElement Raw);

    public record ToolResultPart : MessagePart
    {
        public override string Type => "tool-result";
        public string ToolCallId { get; init; } = string.Empty;
        public string ToolName { get; init; } = string.Empty;
        public LanguageModelV2ToolResultOutput Output { get; init; } = new LanguageModelV2ToolResultOutput(default);
    }

    // Model messages (system, user, assistant, tool)
    public enum MessageRole
    {
        System,
        User,
        Assistant,
        Tool
    }

    // Note: content fields are union types in TS (e.g. string | array-of-parts).
    // We represent them as object so JSON can hold either string or array; when processing you can
    // inspect the runtime type (JsonElement, string, List<MessagePart>, etc.).
    public record SystemModelMessage
    {
        public string Role => "system";
        public string Content { get; init; } = string.Empty;
        public ProviderOptions? ProviderOptions { get; init; }
    }

    public record AssistantModelMessage
    {
        public string Role => "assistant";
        // string or array of parts (TextPart | FilePart | ReasoningPart | ToolCallPart | ToolResultPart)
        public object? Content { get; init; }
        public ProviderOptions? ProviderOptions { get; init; }
    }

    public record ToolModelMessage
    {
        public string Role => "tool";
        // array of ToolResultPart
        public List<ToolResultPart> Content { get; init; } = new();
        public ProviderOptions? ProviderOptions { get; init; }
    }

    public record UserModelMessage
    {
        public string Role => "user";
        // string or array of TextPart | ImagePart | FilePart
        public object? Content { get; init; }
        public ProviderOptions? ProviderOptions { get; init; }
    }

    // ModelMessage union type can be any of the above.
    // When deserializing JSON you can use a custom converter that inspects "role" to instantiate the right type.

    // Tool call related types
    public record ToolCallOptions
    {
        public string ToolCallId { get; init; } = string.Empty;
        // Messages that were sent to the language model to initiate the response that contained the tool call.
        // These messages do not include the system prompt nor the assistant response that contained the tool call.
        public List<object> Messages { get; init; } = new();

        // Optional abort signal: represented as CancellationToken for C#.
        [JsonIgnore]
        public CancellationToken? AbortSignal { get; init; }

        // Additional experimental context (arbitrary JSON)
        public JsonElement? ExperimentalContext { get; init; }
    }

    // Tool execute delegate - in TS it could return AsyncIterable, PromiseLike, or value.
    // Here we provide a standard async Task-based delegate which can also be used to stream results using IAsyncEnumerable.
    public delegate Task<OUTPUT> ToolExecuteFunctionAsync<INPUT, OUTPUT>(INPUT input, ToolCallOptions options, CancellationToken cancellationToken = default);

    // FlexibleSchema placeholder for schemas described in TS. You can replace with concrete schema representation later.
    public record FlexibleSchema<T>(T Schema);

    // Tool class: generic for input/output. Simplified to hold the primary properties from the TS model.
    public class Tool<INPUT, OUTPUT>
    {
        public string? Description { get; init; }
        public ProviderOptions? ProviderOptions { get; init; }
        public FlexibleSchema<INPUT>? InputSchema { get; init; }
        public FlexibleSchema<OUTPUT>? OutputSchema { get; init; }

        // Optional execute function
        [JsonIgnore]
        public ToolExecuteFunctionAsync<INPUT, OUTPUT>? Execute { get; init; }

        // Optional lifecycle hooks (simplified signatures)
        [JsonIgnore]
        public Func<ToolCallOptions, Task>? OnInputStartAsync { get; init; }

        [JsonIgnore]
        public Func<string, ToolCallOptions, Task>? OnInputDeltaAsync { get; init; } // inputTextDelta, options

        [JsonIgnore]
        public Func<INPUT?, ToolCallOptions, Task>? OnInputAvailableAsync { get; init; }

        // Optional conversion function
        [JsonIgnore]
        public Func<OUTPUT, object?>? ToModelOutput { get; init; }

        // Tool variants
        // type is: undefined|'function' | 'dynamic' | 'provider-defined'
        public string? Type { get; init; }
        public string? Id { get; init; }        // for provider-defined: `<provider>.<tool>`
        public string? Name { get; init; }      // for provider-defined
        public Dictionary<string, object?>? Args { get; init; } // provider-defined args
    }

    // Utilities used in UI message types
    public record UITool
    {
        public object? Input { get; init; }
        public object? Output { get; init; }
    }

    // UI messages and parts
    public record UIMessage
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        // 'system' | 'user' | 'assistant'
        public string Role { get; init; } = "user";
        public object? Metadata { get; init; }
        public List<UIMessagePart> Parts { get; init; } = new();
    }

    public abstract record UIMessagePart
    {
        public abstract string Type { get; }
        public ProviderMetadata? ProviderMetadata { get; init; }
    }

    public record TextUIPart : UIMessagePart
    {
        public override string Type => "text";
        public string Text { get; init; } = string.Empty;
        public string? State { get; init; } // "streaming" | "done"
    }

    public record ReasoningUIPart : UIMessagePart
    {
        public override string Type => "reasoning";
        public string Text { get; init; } = string.Empty;
        public string? State { get; init; } // "streaming" | "done"
    }

    public record SourceUrlUIPart : UIMessagePart
    {
        public override string Type => "source-url";
        public string SourceId { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string? Title { get; init; }
    }

    public record SourceDocumentUIPart : UIMessagePart
    {
        public override string Type => "source-document";
        public string SourceId { get; init; } = string.Empty;
        public string MediaType { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Filename { get; init; }
    }

    public record FileUIPart : UIMessagePart
    {
        public override string Type => "file";
        public string MediaType { get; init; } = string.Empty;
        public string? Filename { get; init; }
        public string Url { get; init; } = string.Empty;
    }

    public record StepStartUIPart : UIMessagePart
    {
        public override string Type => "step-start";
    }

    // DataUIPart is a mapped union type in TS. We model a generic holder for typed data parts.
    public record DataUIPart
    {
        public string Type { get; init; } = string.Empty; // e.g. "data-<name>"
        public string? Id { get; init; }
        public object? Data { get; init; }
    }

    // Tool/UI invocation parts are complex unions; we provide simplified dynamic versions:
    public record DynamicToolUIPart : UIMessagePart
    {
        public override string Type => "dynamic-tool";
        public string ToolName { get; init; } = string.Empty;
        public string ToolCallId { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty; // "input-streaming" | "input-available" | "output-available" | "output-error"
        public object? Input { get; init; }
        public object? Output { get; init; }
        public string? ErrorText { get; init; }
        public bool? ProviderExecuted { get; init; }
    }

    public record ToolUIPart : UIMessagePart
    {
        // For strongly-typed UI tool parts you can add the tool name into Type: e.g. "tool-<name>"
        public override string Type => "tool";
        public string ToolCallId { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public object? Input { get; init; }
        public object? Output { get; init; }
        public string? ErrorText { get; init; }
        public bool? ProviderExecuted { get; init; }
    }

    // Helper guard (isDataUIPart) - in C# you can use type checks at runtime when deserialized.
    public static class UiHelpers
    {
        public static bool IsDataUiPart(UIMessagePart part) => part is not null && part.GetType() == typeof(DataUIPart);
    }
}