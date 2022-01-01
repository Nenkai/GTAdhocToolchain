using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler
{
    public class AdhocStack
    {
        /// <summary>
        /// Heap for all variables for the current block.
        /// </summary>
        public List<string> LocalVariableStorage { get; set; } = new()
        {
            null, // Always an empty one
        };

        public int MaxLocalVariableStorageSize { get; set; }

        // Used for counting the stack size within a block
        private int _stackStorageCounter;
        public int StackStorageCounter
        {
            get => _stackStorageCounter;
            set
            {
                if (value > MaxStackStorageSize)
                    MaxStackStorageSize = value;

                _stackStorageCounter = value;
            }
        }

        /// <summary>
        /// Max stack space used for this block.
        /// </summary>
        public int MaxStackStorageSize { get; set; }

        public int GetDeclaredVariableIndex(AdhocSymbol adhoc)
        {
            return LocalVariableStorage.IndexOf(adhoc.Name);
        }

        public void RewindLocalVariableStorage(int count)
        {
            LocalVariableStorage.RemoveRange(LocalVariableStorage.Count - count, count);
        }

        /// <summary>
        /// Adds a variable to the variable storage.
        /// </summary>
        /// <param name="symbol">Symbol to add.</param>
        /// <param name="index">Index of the variable, regardless of if it was added or not.</param>
        /// <returns>Whether the variable was added to the local variable storage.</returns>
        public bool TryAddOrGetVariableIndex(AdhocSymbol symbol, out int index)
        {
            if (LocalVariableStorage.Contains(symbol.Name))
            {
                index = LocalVariableStorage.IndexOf(symbol.Name);
                return false;
            }
            else
            {
                LocalVariableStorage.Add(symbol.Name);
                if (LocalVariableStorage.Count > MaxLocalVariableStorageSize)
                    MaxLocalVariableStorageSize = LocalVariableStorage.Count;

                index = LocalVariableStorage.Count - 1;
                return true;
            }
        }
    }
}
