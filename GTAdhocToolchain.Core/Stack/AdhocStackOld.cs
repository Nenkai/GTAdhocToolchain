using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTAdhocToolchain.Core.Variables;

namespace GTAdhocToolchain.Core.Stack
{
    /// <summary>
    /// Adhoc version < 12
    /// GT4
    /// </summary>
    public class AdhocStackOld : IAdhocStack
    {
        /// <summary>
        /// Storage for all variables for the current block.
        /// </summary>
        public List<Variable> VariableStorage { get; set; } = new()
        {
            // Empty unlike new
        };

        public int VariableStorageSize { get; set; } = 0;

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
            variable = VariableStorage.Find(e => e?.Symbol == symbol && e is StaticVariable);
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
            int freeIdx = VariableStorage.IndexOf(null);
            if (freeIdx == -1) // No free space? If so grow storage
            {
                VariableStorage.Add(variable); // Expand
                if (VariableStorage.Count > VariableStorageSize)
                    VariableStorageSize = VariableStorage.Count;
            }
            else
            {
                VariableStorage[freeIdx] = variable;
            }
        }

        public void AddStaticVariable(StaticVariable variable)
        {
            VariableStorage.Add(variable); // Expand
            if (VariableStorage.Count > VariableStorageSize)
                VariableStorageSize = VariableStorage.Count;
        }

        public bool HasStaticVariable(AdhocSymbol symbol)
        {
            return VariableStorage.Find(e => e?.Symbol == symbol && e is StaticVariable) != null;
        }

        public bool HasLocalVariable(AdhocSymbol symbol)
        {
            return VariableStorage.Find(e => e?.Symbol == symbol && e is LocalVariable) != null;
        }

        public LocalVariable GetLocalVariableBySymbol(AdhocSymbol symbol)
        {
            return VariableStorage.Find(e => e?.Symbol == symbol && e is LocalVariable) as LocalVariable;
        }

        public StaticVariable GetStaticVariableBySymbol(AdhocSymbol symbol)
        {
            return VariableStorage.Find(e => e?.Symbol == symbol && e is StaticVariable) as StaticVariable;
        }

        public int GetLocalVariableIndex(LocalVariable local)
        {
            return VariableStorage.IndexOf(local);
        }

        public int GetStaticVariableIndex(StaticVariable local)
        {
            return VariableStorage.IndexOf(local);
        }

        public int GetLastLocalVariableIndex()
        {
            int highestTakenIndex = VariableStorage.Count;
            for (int i = VariableStorage.Count - 1; i >= 1; i--) // x -> 1 (0 is reserved by self)
            {
                if (VariableStorage[i] == null) // Free'd up space
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
                VariableStorage[idx] = null;
        }

        public void FreeStaticVariable(StaticVariable var)
        {
            var idx = GetStaticVariableIndex(var);
            if (idx != -1)
                VariableStorage[idx] = null;
        }

        public int GetLocalVariableStorageSize()
        {
            return VariableStorageSize;
        }

        public int GetStaticVariableStorageSize()
        {
            throw new NotSupportedException("Cannot get static storage size for old stack.");
        }
    }
}
