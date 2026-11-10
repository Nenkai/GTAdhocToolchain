//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using AdhocLanguage.LanguageServer.Services;

using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Completion;
using EmmyLua.LanguageServer.Framework.Protocol.Message.DocumentSymbol;
using EmmyLua.LanguageServer.Framework.Protocol.Message.SelectionRange;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Server.Handler;

using GTAdhocToolchain.Core;

using GTAdhocToolchain.Core.Variables;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdhocLanguage.LanguageServer.Handlers;

public class DocumentSymbolHandler : DocumentSymbolHandlerBase
{
    private readonly AdhocDocumentService _documents;

    public DocumentSymbolHandler(AdhocDocumentService documents)
    {
        _documents = documents;
    }

    public override void RegisterCapability(ServerCapabilities serverCapabilities, ClientCapabilities clientCapabilities)
    {
        serverCapabilities.DocumentSymbolProvider = true;
    }

    protected override Task<DocumentSymbolResponse> Handle(DocumentSymbolParams request, CancellationToken token)
    {
        DocumentState? document = _documents.GetDocument(request.TextDocument.Uri.FileSystemPath);
        if (document is null)
            return Task.FromResult<DocumentSymbolResponse>(null);

        List<DocumentSymbol> symbols = [];

        foreach (var variable in document.Analyser.TopLevelScope.Variables)
            DoScope(symbols, variable.Value);

        return Task.FromResult(new DocumentSymbolResponse(symbols));
    }

    private void DoScope(List<DocumentSymbol> symbols, Variable variable)
    {
        if (variable.Symbol.Name == AdhocConstants.SELF)
            return;

        SymbolKind kind = variable.Type switch
        {
            AdhocVariableType.Attribute => SymbolKind.Property,
            AdhocVariableType.Class => SymbolKind.Class,
            AdhocVariableType.Delegate => SymbolKind.Event,
            AdhocVariableType.Function => SymbolKind.Function,
            AdhocVariableType.LocalVariable => SymbolKind.Variable,
            AdhocVariableType.Method => SymbolKind.Method,
            AdhocVariableType.Module => SymbolKind.Module,
            AdhocVariableType.Static => SymbolKind.Field,
            AdhocVariableType.Undef => SymbolKind.Null,
            AdhocVariableType.Unknown => SymbolKind.Null,
            _ => SymbolKind.Null,
        };

        var symbol = new DocumentSymbol
        {
            Name = variable.Symbol.Name,
            Kind = kind,
        };

        if (variable.BodyLocation is not null)
        {
            symbol.Range = new DocumentRange(
                new Position(variable.BodyLocation.Value.Start.Line - 1, variable.BodyLocation.Value.Start.Column),
                new Position(variable.BodyLocation.Value.End.Line - 1, variable.BodyLocation.Value.End.Column));
        }
        else
            ;

        if (variable.IdLocation is not null)
        {
            symbol.SelectionRange = new DocumentRange(
                new Position(variable.IdLocation.Value.Start.Line - 1, variable.IdLocation.Value.Start.Column),
                new Position(variable.IdLocation.Value.End.Line - 1, variable.IdLocation.Value.End.Column));
        }
        else
            ;

        if (variable.Children.Count > 0)
        {
            var childList = new List<DocumentSymbol>();
            foreach (var childScope in variable.Children)
                DoScope(childList, childScope);
            symbol.Children = childList;
        }
        symbols.Add(symbol);
    }
}
