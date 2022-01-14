using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocCompiler.Variables;

namespace GTAdhocCompiler
{
    public class AdhocStack
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

        public List<Variable> GlobalVariableStorage { get; set; } = new()
        {
            null
        };

        public int LocalVariableStorageSize { get; set; } = 1;

        public int StaticVariableStorageSize { get; set; } = 0;

        // Used for counting the stack size within a block
        private int _stackStorageCounter;
        public int StackStorageCounter
        {
            get => _stackStorageCounter;
            set
            {
                if (value > StackSize)
                    StackSize = value;

                _stackStorageCounter = value;
            }
        }

        /// <summary>
        /// Max stack space used for this block.
        /// </summary>
        public int StackSize { get; set; }

        public int GetLastVariableStorageIndex()
        {
            int highestTakenIndex = GlobalVariableStorage.Count;
            for (int i = GlobalVariableStorage.Count - 1; i >= 1; i --)
            {
                if (GlobalVariableStorage[i] == null) // Free'd up space
                    highestTakenIndex = i;
                else
                    return highestTakenIndex;
            }

            return GlobalVariableStorage.Count;
        }

        public bool TryAddStaticVariable(AdhocSymbol symbol, out Variable variable)
        {
            variable = StaticVariableStorage.Find(e => e.Symbol == symbol);
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
            variable = LocalVariableStorage.Find(e => e?.Symbol == symbol);
            if (variable is null)
            {
                var newVar = new LocalVariable() { Symbol = symbol };
                AddLocalVariable(newVar);

                variable = newVar;
                return true;
            }

            return false;
        }

        public int AddVariableToGlobalSpace(Variable variable)
        {
            if (variable is StaticVariable)
            {
                GlobalVariableStorage.Add(variable);
                return GlobalVariableStorage.Count - 1;
            }
            else
            {
                int freeIdx = GlobalVariableStorage.IndexOf(null, 1);
                if (freeIdx == -1) // No free space? If so grow storage
                {
                    GlobalVariableStorage.Add(variable);
                    return GlobalVariableStorage.Count - 1;
                }
                else
                {
                    GlobalVariableStorage[freeIdx] = variable;
                    return freeIdx;
                }
            }
        }

        public void AddLocalVariable(LocalVariable variable)
        {
            LocalVariableStorage.Add(variable); // Expand
            if (LocalVariableStorage.Count > LocalVariableStorageSize)
                LocalVariableStorageSize = LocalVariableStorage.Count;
        }

        public void AddStaticVariable(StaticVariable variable)
        {
            StaticVariableStorage.Add(variable); // Expand
            if (StaticVariableStorage.Count > StaticVariableStorageSize)
                StaticVariableStorageSize = StaticVariableStorage.Count;
        }

        public int GetGlobalVariableIndex(Variable variable)
        {
            return GlobalVariableStorage.IndexOf(variable, 1);
            
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

        public void FreeLocalVariable(LocalVariable var)
        {
            LocalVariableStorage.Remove(var);

            // Free up slot from global space
            int idx = GlobalVariableStorage.IndexOf(var);
            GlobalVariableStorage[idx] = null;
        }
    }
}
