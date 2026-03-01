// pure c# code, no unity functions, we want MINIMUM OVERHEAD

//this script will hold GLOBAL INFORMATIONS ABOUT BLOCKS/chunks
//static -> no need to instantiate, can easily access variables with VoxelData.var

public static class VoxelData
{
    //Chunk Sizes
    public const int ChunkWidth = 16; //x
    public const int ChunkHeight = 128; //y
    public const int ChunkDepth = 16; //z

    public const int ChunkVolume = ChunkWidth * ChunkHeight * ChunkDepth;

    //for array flattening
    public static int Get1DIndex( int x, int y, int z) {
        return x + (y * ChunkWidth) + (z * ChunkWidth * ChunkHeight);
    }

}
