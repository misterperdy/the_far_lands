// pure c# code, no unity functions, we want MINIMUM OVERHEAD

// this script will hold the information for one chunk

using System;
using UnityEngine;

public class ChunkData
{
    //1d array that holds the blocks in this chunk
    public byte[] voxelMap;

    public ChunkData() {
        //constructor
        voxelMap = new byte[VoxelData.ChunkVolume]; // length of 1d array = total volume of chunk

        //temp function to populate with blocks until we get to procedural generation
        GenerateTerrain();
    }

    //procedurally generate terrain based on perlin noise
    public void GenerateTerrain() {
        for (int x = 0; x < VoxelData.ChunkWidth; x++) {
            for (int z = 0; z < VoxelData.ChunkDepth; z++) {
                //generate perlin noise value
                float noiseValue = Mathf.PerlinNoise(x * VoxelData.TerrainNoiseScale, z * VoxelData.TerrainNoiseScale);

                //round/multiply to actual terrain height
                int terrainHeight = Mathf.RoundToInt(noiseValue * VoxelData.TerrainHeightMultiplier) + VoxelData.TerrainSolidGroundHeight;

                //fill with blocks
                for (int y = 0; y < VoxelData.ChunkHeight; y++) {
                    int index = VoxelData.Get1DIndex(x, y, z);

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
}
