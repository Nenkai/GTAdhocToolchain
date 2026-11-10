//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AdhocLanguage.LanguageServer.Services;

using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.TextDocument;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using EmmyLua.LanguageServer.Framework.Server.Handler;

using LangServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

namespace AdhocLanguage.LanguageServer.Handlers;

class TextDocumentSyncHandler : TextDocumentHandlerBase
{
    private readonly LangServer _server;
    private readonly AdhocDocumentService _documents;

    public TextDocumentSyncHandler(LangServer langServer, AdhocDocumentService documents)
    {
        _server = langServer;
        _documents = documents;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    protected override async Task Handle(DidOpenTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: DidOpenTextDocumentParams {request.TextDocument.Uri}");

        _documents.ScanFile(request.TextDocument.Uri.FileSystemPath, request.TextDocument.Text, request.TextDocument.Version, force: true);
        //PublishDiagnostics(uri, ast);

        DocumentState? doc = _documents.GetDocument(request.TextDocument.Uri.FileSystemPath);
        if (doc is not null)
        {
            await _server.Client.PublishDiagnostics(new PublishDiagnosticsParams()
            {
                Uri = request.TextDocument.Uri,
                Diagnostics = doc.Diagnostics,
            });
        }
    }

    protected override Task Handle(DidChangeTextDocumentParams request, CancellationToken token)
    {
        var newText = request.ContentChanges.LastOrDefault()?.Text;
        if (newText is not null)
            _documents.ScanFile(request.TextDocument.Uri.FileSystemPath, newText, request.TextDocument.Version);
        
        Console.Error.WriteLine($"TextDocumentHandler: DidChangeTextDocumentParams {request.TextDocument.Uri}");
        return Task.CompletedTask;
    }

    protected override Task Handle(DidCloseTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: DidCloseTextDocument {request.TextDocument.Uri}");

        _documents.Remove(request.TextDocument.Uri.FileSystemPath);

        return Task.CompletedTask;
    }

    protected override Task Handle(WillSaveTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: WillSaveTextDocument {request.TextDocument.Uri}");
        return Task.CompletedTask;
    }

    protected override Task<List<TextEdit>?> HandleRequest(WillSaveTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"TextDocumentHandler: WillSaveTextDocumentRequest {request.TextDocument.Uri}");
        return Task.FromResult<List<TextEdit>?>(null);
    }

    public override void RegisterCapability(ServerCapabilities serverCapabilities, ClientCapabilities clientCapabilities)
    {
        serverCapabilities.TextDocumentSync = new TextDocumentSyncOptions
        {
            Change = TextDocumentSyncKind.Full,
            OpenClose = true,
            WillSave = true,
            WillSaveWaitUntil = true,
            Save = true
        };
    }
}

