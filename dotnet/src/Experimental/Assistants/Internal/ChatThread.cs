﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Experimental.Assistants.Extensions;
using Microsoft.SemanticKernel.Experimental.Assistants.Models;

namespace Microsoft.SemanticKernel.Experimental.Assistants.Internal;

/// <summary>
/// Represents a thread that contains messages.
/// </summary>
internal sealed class ChatThread : IChatThread
{
    /// <inheritdoc/>
    public string Id { get; private set; }

    private readonly OpenAIRestContext _restContext;

    /// <summary>
    /// Create a new thread.
    /// </summary>
    /// <param name="restContext">A context for accessing OpenAI REST endpoint</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An initialized <see cref="ChatThread"> instance.</see></returns>
    public static async Task<IChatThread> CreateAsync(OpenAIRestContext restContext, CancellationToken cancellationToken = default)
    {
        var threadModel =
            await restContext.CreateThreadModelAsync(cancellationToken).ConfigureAwait(false) ??
            throw new SKException("Unexpected failure creating thread: no result.");

        return new ChatThread(threadModel, messageListModel: null, restContext);
    }

    /// <summary>
    /// Retrieve an existing thread.
    /// </summary>
    /// <param name="restContext">A context for accessing OpenAI REST endpoint</param>
    /// <param name="threadId">The thread identifier</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An initialized <see cref="ChatThread"> instance.</see></returns>
    public static async Task<IChatThread> GetAsync(OpenAIRestContext restContext, string threadId, CancellationToken cancellationToken = default)
    {
        var threadModel =
            await restContext.GetThreadModelAsync(threadId, cancellationToken).ConfigureAwait(false) ??
            throw new SKException($"Unexpected failure retrieving thread: no result. ({threadId})");

        var messageListModel =
            await restContext.GetMessagesAsync(threadId, cancellationToken).ConfigureAwait(false) ??
            throw new SKException($"Unexpected failure retrieving thread: no result. ({threadId})");

        return new ChatThread(threadModel, messageListModel, restContext);
    }

    /// <inheritdoc/>
    public async Task<IChatMessage> AddUserMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        var messageModel =
            await this._restContext.CreateUserTextMessageAsync(
                this.Id,
                message,
                cancellationToken).ConfigureAwait(false);

        return new ChatMessage(messageModel);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IChatMessage>> InvokeAsync(IAssistant assistant, CancellationToken cancellationToken)
    {
        var tools = assistant.Plugins.SelectMany(p => p.Select(f => f.ToToolModel(p.Name)));
        var runModel = await this._restContext.CreateRunAsync(this.Id, assistant.Id, assistant.Instructions, tools, cancellationToken).ConfigureAwait(false);

        var run = new ChatRun(runModel, assistant.Kernel, this._restContext);
        var results = await run.GetResultAsync(cancellationToken).ConfigureAwait(false);

        var messages = await this.GetMessagesAsync(results, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);

        return messages;
    }

    /// <summary>
    /// Delete an existing thread.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    public Task DeleteThreadAsync(CancellationToken cancellationToken)
    {
        return this._restContext.DeleteThreadModelAsync(this.Id, cancellationToken);
    }

    private async IAsyncEnumerable<IChatMessage> GetMessagesAsync(
        IList<string> messageIds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messages = await this._restContext.GetMessagesAsync(this.Id, messageIds, cancellationToken).ConfigureAwait(false);
        foreach (var message in messages)
        {
            yield return new ChatMessage(message);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatThread"/> class.
    /// </summary>
    private ChatThread(
        ThreadModel threadModel,
        ThreadMessageListModel? messageListModel,
        OpenAIRestContext restContext)
    {
        this.Id = threadModel.Id;
        this._restContext = restContext;
    }
}
