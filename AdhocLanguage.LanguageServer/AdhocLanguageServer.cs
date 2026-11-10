//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AdhocLanguage.LanguageServer.Handlers;
using AdhocLanguage.LanguageServer.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AdhocLanguage.LanguageServer;

public class AdhocLanguageServer
{
    private IServiceProvider _services;

    public AdhocLanguageServer()
    {

    }

    public async Task StartAsync()
    {


        try
        { 
            var server = EmmyLua.LanguageServer.Framework.Server.LanguageServer.From(Console.OpenStandardInput(), Console.OpenStandardOutput());

            _services = new ServiceCollection()
                .AddSingleton(server)

                // Handlers
                .AddSingleton<TextDocumentSyncHandler>()
                .AddSingleton<CompletionHandler>()
                .AddSingleton<DocumentSymbolHandler>()
                .BuildServiceProvider();

            server.OnInitialize((request, serverInfo) =>
            {
                serverInfo.Name = "Adhoc Language Server";
                serverInfo.Version = "1.0.0";
                return Task.CompletedTask;
            });

            server.OnInitialized(async (request) =>
            {
                await server.Client.LogInfo("Server initialized!");
            });

            server.AddHandler(_services.GetRequiredService<TextDocumentSyncHandler>());
            server.AddHandler(_services.GetRequiredService<CompletionHandler>());
            server.AddHandler(_services.GetRequiredService<DocumentSymbolHandler>());
            await server.Run();
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync(
                Path.Combine(Path.GetTempPath(), "adhoc-lsp-crash.log"),
                ex.ToString());
        }
    }
}
