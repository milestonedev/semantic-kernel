﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Plugins;
using RepoUtils;

// ReSharper disable once InconsistentNaming
public static class Example03_Variables
{
    private static readonly ILoggerFactory s_loggerFactory = ConsoleLogger.LoggerFactory;

    public static async Task RunAsync()
    {
        Console.WriteLine("======== Variables ========");

        Kernel kernel = new KernelBuilder().WithLoggerFactory(s_loggerFactory).Build();
        var textPlugin = kernel.ImportPluginFromObject<StaticTextPlugin>();

        var variables = new ContextVariables("Today is: ");
        variables.Set("day", DateTimeOffset.Now.ToString("dddd", CultureInfo.CurrentCulture));

        var result = await kernel.RunAsync(textPlugin["AppendDay"], variables);

        Console.WriteLine(result.GetValue<string>());
    }
}
