'use strict';
import * as vscode from 'vscode';

export function activate(context: vscode.ExtensionContext) {
    var type = "build_script";
    vscode.tasks.registerTaskProvider(type, {
        provideTasks(token?: vscode.CancellationToken) {
            if (vscode.workspace.workspaceFolders === undefined)
                return;

            var currentlyOpenTabfilePath = vscode.window.activeTextEditor?.document.uri.fsPath;
            if (currentlyOpenTabfilePath == undefined)
                return undefined;

            var execution = new vscode.ShellExecution(`adhoc build -i ${currentlyOpenTabfilePath}`);
            return [
                new vscode.Task({type: type}, vscode.TaskScope.Workspace,
                    "Build Adhoc Script", "Adhoc", execution, undefined)
            ];
        },
        resolveTask(task: vscode.Task, token?: vscode.CancellationToken) {
            return task;
        }
    });
}