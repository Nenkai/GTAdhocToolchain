using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTAdhocToolchain.Core.Variables;

namespace GTAdhocToolchain.Core.Stack
{
    public class AdhocStack : IAdhocStack
    {
        /// <summary>
        /// Heap for all variables for the current block.
        /// </summary>
        public List<LocalVariable> LocalVariableStorage { get; set; } = new()
        {
            null, // Always an empty one
        };

        /// <summary>
        /// Heap for all variables for the current block.
        /// </summary>
        public List<StaticVariable> StaticVariableStorage { get; set; } = new();

        public int LocalVariableStorageSize { get; set; } = 1;

        public int StaticVariableStorageSize { get; set; } = 0;

        // Used for counting the stack size within a block
        private int _stackSizeCounter;
        public int StackSizeCounter
        {
            get => _stackSizeCounter;
            set
            {
                if (value > StackSize)
                    StackSize = value;

                _stackSizeCounter = value;
            }
        }

        /// <summary>
        /// Max stack space used for this block.
        /// </summary>
        public int StackSize { get; set; }

        public void IncrementStackCounter()
        {
            StackSizeCounter++;
        }

        public void IncreaseStackCounter(int count)
        {
            StackSizeCounter += count;
        }

        public void DecrementStackCounter()
        {
            StackSizeCounter--;
        }

        public void DecreaseStackCounter(int count)
        {
            StackSizeCounter -= count;
        }

        public int GetStackSize()
        {
            return StackSize;
        }

        public bool TryAddStaticVariable(AdhocSymbol symbol, out Variable variable)
        {
            variable = StaticVariableStorage.Find(e => e?.Symbol == symbol);
            if (variable is null)
            {
                var newVar = new StaticVariable() { Symbol = symbol };
                AddStaticVariable(newVar);

                variable = newVar;
                return true;
            }

            return false;
        }

        public bool TryAddLocalVariable(AdhocSymbol symbol, out Variable variable)
        {
            variable = GetLocalVariableBySymbol(symbol);
            if (variable is null)
            {
                var newVar = new LocalVariable() { Symbol = symbol };
                AddLocalVariable(newVar);

                variable = newVar;
                return true;
            }

            return false;
        }

        public void AddLocalVariable(LocalVariable variable)
        {
            int freeIdx = LocalVariableStorage.IndexOf(null, 1);
            if (freeIdx == -1) // No free space? If so grow storage
            {
                LocalVariableStorage.Add(variable); // Expand
                if (LocalVariableStorage.Count > LocalVariableStorageSize)
                    LocalVariableStorageSize = LocalVariableStorage.Count;
            }
            else
            {
                LocalVariableStorage[freeIdx] = variable;
            }
        }

        public void AddStaticVariable(StaticVariable variable)
        {
            StaticVariableStorage.Add(variable); // Expand
            if (StaticVariableStorage.Count > StaticVariableStorageSize)
                StaticVariableStorageSize = StaticVariableStorage.Count;
        }

        public bool HasStaticVariable(AdhocSymbol symbol)
        {
            return StaticVariableStorage.Find(e => e?.Symbol == symbol) != null;
        }

        public bool HasLocalVariable(AdhocSymbol symbol)
        {
            return LocalVariableStorage.Find(e => e?.Symbol == symbol) != null;
        }

        public LocalVariable GetLocalVariableBySymbol(AdhocSymbol symbol)
        {
            return LocalVariableStorage.Find(e => e?.Symbol == symbol);
        }

        public StaticVariable GetStaticVariableBySymbol(AdhocSymbol symbol)
        {
            return StaticVariableStorage.Find(e => e?.Symbol == symbol);
        }

        public int GetLocalVariableIndex(LocalVariable local)
        {
            return LocalVariableStorage.IndexOf(local);
        }

        public int GetStaticVariableIndex(StaticVariable local)
        {
            return StaticVariableStorage.IndexOf(local);
        }

        public int GetLastLocalVariableIndex()
        {
            int highestTakenIndex = LocalVariableStorage.Count;
            for (int i = LocalVariableStorage.Count - 1; i >= 1; i--) // x -> 1 (0 is reserved by self)
            {
                if (LocalVariableStorage[i] == null) // Free'd up space
                    highestTakenIndex = i;
                else
                    return highestTakenIndex;
            }

            return highestTakenIndex;
        }

        public void FreeLocalVariable(LocalVariable var)
        {
            var idx = GetLocalVariableIndex(var);
            if (idx != -1)
                LocalVariableStorage[idx] = null;
        }

        public void FreeStaticVariable(StaticVariable var)
        {
            var idx = GetStaticVariableIndex(var);
            if (idx != -1)
                StaticVariableStorage[idx] = null;
        }

        public int GetLocalVariableStorageSize()
        {
            return LocalVariableStorageSize;
        }

        public int GetStaticVariableStorageSize()
        {
            return StaticVariableStorageSize;
        }
    }
}
