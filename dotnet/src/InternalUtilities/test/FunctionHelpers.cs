﻿// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace SemanticKernel.UnitTests;

/// <summary>Test helpers for working with native functions.</summary>
internal static class FunctionHelpers
{
    /// <summary>
    /// Invokes a function on a plugin instance via the kernel.
    /// </summary>
    public static Task<FunctionResult> CallViaKernelAsync(
        object pluginInstance,
        string methodName,
        params (string Name, object Value)[] variables)
    {
        var kernel = new KernelBuilder().Build();

        ISKPlugin plugin = kernel.ImportPluginFromObject(pluginInstance);

        SKContext context = kernel.CreateNewContext();
        foreach ((string Name, object Value) pair in variables)
        {
            context.Variables.Set(pair.Name, pair.Value.ToString());
        }

        return kernel.RunAsync(plugin[methodName], context.Variables);
    }
}
