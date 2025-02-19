﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

/**
 * This example shows how to use chat completion prompts.
 */
// ReSharper disable once InconsistentNaming
public static class Example63_ChatCompletionPrompts
{
    public static async Task RunAsync()
    {
        const string TextPrompt = "What is Seattle?";
        const string ChatPrompt = @"
            <message role=""user"">What is Seattle?</message>
            <message role=""system"">Respond with JSON.</message>
        ";

        var kernel = new KernelBuilder()
            .WithOpenAIChatCompletionService(
                modelId: TestConfiguration.OpenAI.ChatModelId,
                apiKey: TestConfiguration.OpenAI.ApiKey)
            .Build();

        var textSemanticFunction = kernel.CreateFunctionFromPrompt(TextPrompt);
        var chatSemanticFunction = kernel.CreateFunctionFromPrompt(ChatPrompt);

        var textPromptResult = await kernel.RunAsync(textSemanticFunction);
        var chatPromptResult = await kernel.RunAsync(chatSemanticFunction);

        Console.WriteLine("Text Prompt:");
        Console.WriteLine(TextPrompt);
        Console.WriteLine("Text Prompt Result:");
        Console.WriteLine(textPromptResult);

        Console.WriteLine();

        Console.WriteLine("Chat Prompt:");
        Console.WriteLine(ChatPrompt);
        Console.WriteLine("Chat Prompt Result:");
        Console.WriteLine(chatPromptResult);

        /*
        Text Prompt:
        What is Seattle?
        Text Prompt Result:
        Seattle is a city located in the state of Washington in the United States...

        Chat Prompt:
        <message role="user">What is Seattle?</message>
        <message role="system">Respond with JSON.</message>

        Chat Prompt Result:
        {
          "Seattle": {
            "Description": "Seattle is a city located in the state of Washington, in the United States...",
            "Population": "Approximately 753,675 as of 2019",
            "Area": "142.5 square miles",
            ...
          }
        }
         */
    }
}
