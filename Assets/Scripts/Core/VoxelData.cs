// pure c# code, no unity functions, we want MINIMUM OVERHEAD

//this script will hold GLOBAL INFORMATIONS ABOUT BLOCKS/chunks
//static -> no need to instantiate, can easily access variables with VoxelData.var

using UnityEngine;

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


    //global data for creating cube meshes

    //the cube will start on 0,0,0 coordinates on the bottom left behind point

    //reminder: static = one variable for the entire class, readonly = after assignmenmt it can't be changed
    //industry standard for global variables like this that are fixed

    //edges(vertices) of cube 3d POINTS
    public static readonly Vector3[] voxelVerts = new Vector3[8] {
        //behind face
        new Vector3(0.0f,0.0f,0.0f), // 0 bottom left
        new Vector3(1.0f,0.0f,0.0f), // 1 bottom right
        new Vector3(0.0f,1.0f,0.0f), // 2 top left
        new Vector3(1.0f,1.0f,0.0f), // 3 top right

        //front face
        new Vector3(0.0f,0.0f,1.0f), // 4 bottom left
        new Vector3(1.0f,0.0f,1.0f), // 5 bottom right
        new Vector3(0.0f,1.0f,1.0f), // 6 top left
        new Vector3(1.0f,1.0f,1.0f), // 7 top right
    };

    //faces of cube (which of the previous points connect to draw the faces)
    //FOR UNITY DRAW ORDER OF VERTICES NEED TO BE CLOCKWISE

    public static readonly int[,] voxelTris = new int[6, 4] {
        {1,0,2,3 }, //back face
        {4,5,7,6 }, //front face
        {6,7, 3, 2 }, //top
        {0,1, 5, 4 }, //bottom
        {0,4,6,2 }, //left
        {5,1,3,7 } //right
    };

    //For face culling,to only render needed faces, we will respect the order of above for the face drawing , and will have a value to increment on the face direction to check if it's next to air
    //aka offsets for face culling neighbouring block checking
    public static readonly Vector3Int[] faceChecks = new Vector3Int[6] {
        new Vector3Int(0,0,-1), //back
        new Vector3Int(0,0,1), //front
        new Vector3Int(0,1,0), //top
        new Vector3Int(0,-1,0), //bottom
        new Vector3Int(-1,0,0), //left
        new Vector3Int(1,0,0) //right
    };

    //for texturing blocks, UV default standard coordinates for any square face, lookup table
    public static readonly Vector2[] voxelUVs = new Vector2[4] {
        new Vector2 (0.0f, 0.0f), // bottom left
        new Vector2 (1.0f, 0.0f), // bottom right
        new Vector2 (0.0f, 1.0f), // top left
        new Vector2 (1.0f, 1.0f) // top right
    };

    //texture atlas = "spritesheet"
    //how many textures of blocks per line in spritesheet (to know how to cut it)
    public static readonly int TextureAtlasBlocksPerLine = 9; 

    //fractional value for how much a block occupies - 100%/9 blocks per line = 0.111f
    public static readonly float NormalizedBlockTextureSize = 1f / 9f;

    //Function to get texture from atlas for a block id - moved from Chunk.cs

    //get texture position from atlas of blocks, we are using UV coordinates so its starts from bottom left and in form of (u,v) aka (x,y)
    //HERE WE ADD MORE BLOCK TEXTURES AFTER ADDING BLOCK TYPE
    public static Vector2 GetTexturePosition(byte blockID, int faceIndex = 0) { //default face 0 for UI
        if (blockID == (byte)BlockType.Stone) {
            return new Vector2(3, 4);

        } else if (blockID == (byte)BlockType.Dirt) {
            return new Vector2(7, 3);

        } else if (blockID == (byte)BlockType.Grass) {
            //based on which face it is, show top grass block or side grass block or dirt (bottom)
            if (faceIndex == 2) { // top
                return new Vector2(6, 7);
            }
            if (faceIndex == 3) { //bottom
                return new Vector2(7, 3);
            } else { //3d sides
                return new Vector2(7, 4);
            }

        } else if (blockID == (byte)BlockType.Planks) {
            return new Vector2(0, 8);

        } else if (blockID == (byte)BlockType.Bricks) {
            return new Vector2(8, 5);

        } else if (blockID == (byte)BlockType.StoneBricks) {
            return new Vector2(4, 6);

        } else if (blockID == (byte)BlockType.Sand) {
            return new Vector2(3, 2);

        } else if (blockID == (byte)BlockType.Glass) {
            return new Vector2(6, 0);

        } else if (blockID == (byte)BlockType.Bedrock) {
            return new Vector2(5, 1);

        }

        //default value - light gray log (to know if it ever reaches this edge case for debugging)
        return new Vector2(0, 0);
    }

    //fucntion to check if block is transparent(air,glass) - we will add water, lava, and blocks like leaves mushrooms here
    public static bool IsTransparent(byte blockID) {
        if (blockID == (byte)BlockType.Air || blockID == (byte)BlockType.Glass) return true;
        return false;
    }

    // -------- Terrain Generation data --------
    public static readonly float TerrainNoiseScale = 0.05f; // zoom on noisemap ; smaller = smoother terrain ; larger = rougher terrain
    public static readonly int TerrainHeightMultiplier = 15; //how tall mountains will be
    public static readonly int TerrainSolidGroundHeight = 10; // base height of world
}
