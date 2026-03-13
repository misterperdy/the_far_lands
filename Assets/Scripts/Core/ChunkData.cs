// pure c# code, no unity editor functionality (only minimum required ones like usage of vector3 for ex.), we want MINIMUM OVERHEAD

// this script will hold the information for one chunk

using System;
using UnityEngine;

public class ChunkData
{
    //1d array that holds the blocks in this chunk
    public byte[] voxelMap;

    private _worldManager _world; //reference passed with dependency injection in constructor from Chunk script

    public ChunkData(_worldManager _world) {
        //constructor, initialize the chunk's blocks 1d array
        voxelMap = new byte[VoxelData.ChunkVolume]; // length of 1d array = total volume of chunk
        
        this._world = _world;
    }

    //procedurally generate terrain based on perlin noise that takes in consideration local coord and global chunk coord
    public void GenerateTerrain(Vector3Int chunkCoord) {
        //go through all 2D flat coordinates, figure out for each what height terrain to reach
        for (int x = 0; x < VoxelData.ChunkWidth; x++) {
            for (int z = 0; z < VoxelData.ChunkDepth; z++) {
                //get global coordinates of this block
                float globalX = (chunkCoord.x * VoxelData.ChunkWidth + x);
                float globalZ = (chunkCoord.z * VoxelData.ChunkDepth + z);

                //generate perlin noise value based on global block coordinates
                float noiseValue = Mathf.PerlinNoise((globalX + _world.offsetX )* VoxelData.TerrainNoiseScale, (globalZ + _world.offsetZ) * VoxelData.TerrainNoiseScale);

                //round/multiply to actual terrain height
                int terrainHeight = Mathf.RoundToInt(noiseValue * VoxelData.TerrainHeightMultiplier) + VoxelData.TerrainSolidGroundHeight;

                //set Y=0 to bedrock
                int index = VoxelData.Get1DIndex(x, 0, z);
                voxelMap[index] = (byte)BlockType.Bedrock;

                //fill with blocks from y=1
                for (int y = 1; y < VoxelData.ChunkHeight; y++) {
                    index = VoxelData.Get1DIndex(x, y, z);

                    if (y == terrainHeight) { //top block  is grass
                        voxelMap[index] = (byte)BlockType.Grass;
                    } else if (y < terrainHeight & y > terrainHeight - 5) { // next 4 blocks are dirt
                        voxelMap[index] = (byte)BlockType.Dirt;
                    } else if (y <= terrainHeight - 5) { // rest below is stone
                        voxelMap[index] = (byte)BlockType.Stone;
                    } else { // what is above will be air
                        voxelMap[index] = (byte)BlockType.Air;
                    }
                }
            }
        }
    }

    //old function to generate dummy chunk to test rendering of blocks
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

                    } else if (y == surfaceHeight) {// 64 with grass

                        voxelMap[index] = (byte)BlockType.Grass;

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

    //set coords to certain block
    public void SetVoxel(int x,int y, int z, byte blockID) {
        int index = VoxelData.Get1DIndex(x, y, z);
        voxelMap[index] = blockID;
    }
}
