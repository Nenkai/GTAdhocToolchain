using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler
{
    public class AdhocStack
    {
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
    }
}
