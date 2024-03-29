﻿using GTAdhocToolchain.Core.Variables;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core.Stack
{
    public interface IAdhocStack
    {
        void IncrementStackCounter();

        void IncreaseStackCounter(int count);

        void DecrementStackCounter();

        void DecreaseStackCounter(int count);

        int GetStackSize();

        bool TryAddStaticVariable(AdhocSymbol symbol, out Variable variable);

        bool TryAddLocalVariable(AdhocSymbol symbol, out Variable variable);

        void AddLocalVariable(LocalVariable variable);

        void AddStaticVariable(StaticVariable variable);

        bool HasStaticVariable(AdhocSymbol symbol);

        bool HasLocalVariable(AdhocSymbol symbol);

        LocalVariable GetLocalVariableBySymbol(AdhocSymbol symbol);

        StaticVariable GetStaticVariableBySymbol(AdhocSymbol symbol);

        int GetLocalVariableIndex(LocalVariable local);

        int GetStaticVariableIndex(StaticVariable local);

        int GetLastLocalVariableIndex();

        void FreeLocalVariable(LocalVariable var);

        void FreeStaticVariable(StaticVariable var);

        int GetStaticVariableStorageSize();

        int GetLocalVariableStorageSize();
    }
}
