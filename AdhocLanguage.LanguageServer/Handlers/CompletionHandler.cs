//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AdhocLanguage.LanguageServer.Services;

using GTAdhocToolchain.Core;

using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Completion;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Kind;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using GTAdhocToolchain.Core.Variables;

namespace AdhocLanguage.LanguageServer.Handlers;

public class CompletionHandler : CompletionHandlerBase
{
    private readonly AdhocDocumentService _documents;

    public CompletionHandler(AdhocDocumentService documents)
    {
        _documents = documents;
    }

    protected override Task<CompletionResponse?> Handle(CompletionParams request, CancellationToken token)
    {
        Console.Error.WriteLine("CompletionHandler.Handle");

        DocumentState? document = _documents.GetDocument(request.TextDocument.Uri.FileSystemPath);
        if (document is null)
            return Task.FromResult<CompletionResponse?>(null);

        var scope = document.Analyser.GetScope(request.Position.Line, request.Position.Character);
        if (scope is null)
            return Task.FromResult<CompletionResponse?>(null);

        List<CompletionItem> items = new List<CompletionItem>(scope.Variables.Count);

        while (true)
        {
            foreach (var variable in scope.Variables)
            {
                Variable? var = variable.Value;

                CompletionItemKind kind = var.Type switch
                {
                    AdhocVariableType.Attribute => CompletionItemKind.Property,
                    AdhocVariableType.Class => CompletionItemKind.Class,
                    AdhocVariableType.Delegate => CompletionItemKind.Event,
                    AdhocVariableType.Function => CompletionItemKind.Function,
                    AdhocVariableType.LocalVariable => CompletionItemKind.Variable,
                    AdhocVariableType.Method => CompletionItemKind.Method,
                    AdhocVariableType.Module => CompletionItemKind.Module,
                    AdhocVariableType.Static => CompletionItemKind.Field,
                    AdhocVariableType.Undef => 0,
                    AdhocVariableType.Unknown => 0,
                    _ => 0,
                };

                items.Add(new CompletionItem
                {
                    Label = var.Symbol.Name,
                    Kind = kind,
                    Detail = variable.Value.Type.ToString(),
                    Documentation = "",
                    InsertText = var.Symbol.Name,
                    InsertTextFormat = InsertTextFormat.PlainText,
                    SortText = var.Symbol.Name,
                    FilterText = var.Symbol.Name,
                    Tags = [],
                });
            }

            if (scope.Parent is null)
                break;

            scope = scope.Parent;
        }

        return Task.FromResult(new CompletionResponse(items))!;
    }

    protected override Task<CompletionItem> Resolve(CompletionItem item, CancellationToken token)
    {
        Console.Error.WriteLine("CompletionHandler.Resolve");
        return Task.FromResult(item)!;
    }

    public override void RegisterCapability(ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities)
    {
        serverCapabilities.CompletionProvider = new CompletionOptions
        {
            TriggerCharacters = [".", ":"],
            ResolveProvider = true
        };
    }
}
