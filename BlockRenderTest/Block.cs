using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockRenderTest
{
    /*
    These three classes are used to represent the JSON files of blockstates.
    
    The "Block" class stores the name of an individual block and a list of
    all the blockstates of that block.

    The "BlockState" class stores the name of a specific blockstate and a list
    of all the models that blockstate can have.

    The "ModelReference" class stores the path to the actual model file.
    */ 
    public class Block
    {
        public string Name;
        public string CreativeTab; //Currently unused
        public List<BlockState> BlockStates = new List<BlockState>();

        public Block(string BlockName, List<BlockState> States)
        {
            Name = BlockName;
            BlockStates = States;
        }
    }

    public class BlockState
    {
        public string Name;
        public List<ModelReference> Models = new List<ModelReference>();

        public BlockState(string BlockStateName, List<ModelReference> BlockStateModels)
        {
            Name = BlockStateName;
            Models = BlockStateModels;
        }
    }

    public class ModelReference
    {
        public string Path;

        public ModelReference(string ModelPath)
        {
            Path = ModelPath;
        }
    }
}
