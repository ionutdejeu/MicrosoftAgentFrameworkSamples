//YouTube video that cover this sample: https://youtu.be/p5AvoMbgPtI
// ReSharper disable HeuristicUnreachableCode

#pragma warning disable CS0162 // Unreachable code detected
using Microsoft.Agents.AI;
using OpenAI;
using Shared;
using System.ClientModel;
using ConversationThreads;
using Microsoft.Extensions.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;


Dictionary<string,string> env =  EnvLoader.LoadEnv(".env");

Configuration configuration = ConfigurationManager.GetConfiguration();

// Use OpenAI client with the OpenAI API key instead of Azure OpenAI
OpenAIClient client = new(env["OPENAI_API_KEY"]);

var agent = client
    .GetChatClient("gpt-4o-mini")
    .CreateAIAgent(instructions: "You are a Friendly AI Bot, answering questions");

AgentThread thread;

const bool optionToResume = true; //Set this to true to test resume of previous conversations

if (optionToResume)
{
    thread = await AgentThreadPersistence.ResumeChatIfRequestedAsync(agent);
}
else
{
    thread = agent.GetNewThread();
}

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
    {
        ChatMessage message = new(ChatRole.User, input);
        await foreach (AgentRunResponseUpdate update in agent.RunStreamingAsync(message, thread))
        {
            Console.Write(update);
        }
    }

    Utils.Separator();

    if (optionToResume)
    {
        await AgentThreadPersistence.StoreThreadAsync(thread);
    }
}