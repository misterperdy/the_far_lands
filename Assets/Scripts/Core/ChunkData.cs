// pure c# code, no unity functions, we want MINIMUM OVERHEAD

// this script will hold the information for one chunk
public class ChunkData
{
    //1d array that holds the blocks in this chunk
    public byte[] voxelMap;

    public ChunkData() {
        //constructor
        voxelMap = new byte[VoxelData.ChunkVolume]; // length of 1d array = total volume of chunk

        //temp function to populate with blocks until we get to procedural generation
        PopulateDummyData();
    }

    public void PopulateDummyData() {

        int surfaceHeight = 64;//populate to Y=64 ( max y is 127)

        for( int x =0; x< VoxelData.ChunkWidth; x++) {
            for(int y=0; y< VoxelData.ChunkHeight; y++) {
                for (int z = 0; z < VoxelData.ChunkDepth; z++) {
                    int index = VoxelData.Get1DIndex(x, y, z); //get array index for current coordinates

                    //populate 0-59 with stone
                    if (y < surfaceHeight - 4) {
                        //convert block type to byte value
                        voxelMap[index] = (byte)BlockType.Stone;
                    } else if (y >= surfaceHeight - 4 && y < surfaceHeight) {// 60-63 with dirt

                        voxelMap[index] = (byte)BlockType.Dirt;

                    } else {
                        //everything else y64->max is air
                        voxelMap[index] = (byte)BlockType.Air;
                    }
                }
            }
        }
    }

    //function to return a block from the flattened array given the 3D coordinates
    //WITH check for block coordinates to be valid
    public byte GetVoxel(int x, int y, int z) {
        //check if block is not in chunk , then return air

        if(x < 0 || x >= VoxelData.ChunkWidth ||
           y < 0 || y >= VoxelData.ChunkHeight ||
           z < 0 || z >= VoxelData.ChunkDepth) {
            return (byte)BlockType.Air;
        }

        int index = VoxelData.Get1DIndex(x, y, z);
        return voxelMap[index];
    }
}
