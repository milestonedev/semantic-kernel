﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Orchestration;
using RepoUtils;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

/**
 * The following example shows how to plug into SK a custom text completion model.
 *
 * This might be useful in a few scenarios, for example:
 * - You are not using OpenAI or Azure OpenAI models
 * - You are using OpenAI/Azure OpenAI models but the models are behind a web service with a different API schema
 * - You want to use a local model
 *
 * Note that all text completion models are deprecated by OpenAI and will be removed in a future release.
 *
 * Refer to example 33 for streaming chat completion.
 */
public class MyTextCompletionService : ITextCompletion
{
    public string? ModelId { get; private set; }

    public IReadOnlyDictionary<string, string> Attributes => new Dictionary<string, string>();

    public Task<IReadOnlyList<ITextResult>> GetCompletionsAsync(string text, AIRequestSettings? requestSettings, CancellationToken cancellationToken = default)
    {
        this.ModelId = requestSettings?.ModelId;

        return Task.FromResult<IReadOnlyList<ITextResult>>(new List<ITextResult>
        {
            new MyTextCompletionStreamingResult()
        });
    }

    public async IAsyncEnumerable<ITextStreamingResult> GetStreamingCompletionsAsync(string text, AIRequestSettings? requestSettings, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new MyTextCompletionStreamingResult();
    }

    public async IAsyncEnumerable<T> GetStreamingContentAsync<T>(string prompt, AIRequestSettings? requestSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (typeof(T) == typeof(MyStreamingContent))
        {
            yield return (T)(object)new MyStreamingContent("llm content update 1");
            yield return (T)(object)new MyStreamingContent("llm content update 2");
        }
    }
}

public class MyStreamingContent : StreamingContent
{
    public override int ChoiceIndex => 0;

    public string Content { get; }

    public MyStreamingContent(string content) : base(content)
    {
        this.Content = content;
    }

    public override byte[] ToByteArray()
    {
        return Encoding.UTF8.GetBytes(this.Content);
    }

    public override string ToString()
    {
        return this.Content;
    }
}

public class MyTextCompletionStreamingResult : ITextStreamingResult, ITextResult
{
    private readonly ModelResult _modelResult = new(new
    {
        Content = Text,
        Message = "This is my model raw response",
        Tokens = Text.Split(' ').Length
    });

    private const string Text = @" ..output from your custom model... Example:
AI is awesome because it can help us solve complex problems, enhance our creativity,
and improve our lives in many ways. AI can perform tasks that are too difficult,
tedious, or dangerous for humans, such as diagnosing diseases, detecting fraud, or
exploring space. AI can also augment our abilities and inspire us to create new forms
of art, music, or literature. AI can also improve our well-being and happiness by
providing personalized recommendations, entertainment, and assistance. AI is awesome";

    public ModelResult ModelResult => this._modelResult;

    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        // Forcing a 2 sec delay (Simulating custom LLM lag)
        await Task.Delay(2000, cancellationToken);

        return Text;
    }

    public async IAsyncEnumerable<string> GetCompletionStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return Environment.NewLine;

        // Your model logic here
        var streamedOutput = Text.Split(' ');
        foreach (string word in streamedOutput)
        {
            await Task.Delay(50, cancellationToken);
            yield return $"{word} ";
        }
    }
}

// ReSharper disable StringLiteralTypo
// ReSharper disable once InconsistentNaming
public static class Example16_CustomLLM
{
    public static async Task RunAsync()
    {
        await CustomTextCompletionWithSKFunctionAsync();

        await CustomTextCompletionAsync();
        await CustomTextCompletionStreamAsync();
    }

    private static async Task CustomTextCompletionWithSKFunctionAsync()
    {
        Console.WriteLine("======== Custom LLM - Text Completion - SKFunction ========");

        Kernel kernel = new KernelBuilder()
            .WithLoggerFactory(ConsoleLogger.LoggerFactory)
            // Add your text completion service as a singleton instance
            .WithAIService<ITextCompletion>("myService1", new MyTextCompletionService())
            // Add your text completion service as a factory method
            .WithAIService<ITextCompletion>("myService2", (log) => new MyTextCompletionService())
            .Build();

        const string FunctionDefinition = "Does the text contain grammar errors (Y/N)? Text: {{$input}}";

        var textValidationFunction = kernel.CreateFunctionFromPrompt(FunctionDefinition);

        var result = await textValidationFunction.InvokeAsync(kernel, "I mised the training session this morning");
        Console.WriteLine(result.GetValue<string>());

        // Details of the my custom model response
        Console.WriteLine(JsonSerializer.Serialize(
            result.GetModelResults(),
            new JsonSerializerOptions() { WriteIndented = true }
        ));
    }

    private static async Task CustomTextCompletionAsync()
    {
        Console.WriteLine("======== Custom LLM  - Text Completion - Raw ========");
        var completionService = new MyTextCompletionService();

        var result = await completionService.CompleteAsync("I missed the training session this morning");

        Console.WriteLine(result);
    }

    private static async Task CustomTextCompletionStreamAsync()
    {
        Console.WriteLine("======== Custom LLM  - Text Completion - Raw Streaming ========");

        Kernel kernel = new KernelBuilder().WithLoggerFactory(ConsoleLogger.LoggerFactory).Build();
        ITextCompletion textCompletion = new MyTextCompletionService();

        var prompt = "Write one paragraph why AI is awesome";
        await TextCompletionStreamAsync(prompt, textCompletion);
    }

    private static async Task TextCompletionStreamAsync(string prompt, ITextCompletion textCompletion)
    {
        var requestSettings = new OpenAIRequestSettings()
        {
            MaxTokens = 100,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            Temperature = 1,
            TopP = 0.5
        };

        Console.WriteLine("Prompt: " + prompt);
        await foreach (string message in textCompletion.CompleteStreamAsync(prompt, requestSettings))
        {
            Console.Write(message);
        }

        Console.WriteLine();
    }
}
