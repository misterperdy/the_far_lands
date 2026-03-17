// pure c# code, no unity editor functionality (only minimum required ones like usage of vector3 for ex.), we want MINIMUM OVERHEAD

// this script will hold the information for one chunk

using System;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using UnityEngine;

public class ChunkData
{
    //1d array that holds the blocks in this chunk
    public byte[] voxelMap;

    private _worldManager _world; //reference passed with dependency injection in constructor from Chunk script

    private int terrainHeight;

    public ChunkData(_worldManager _world) {
        //constructor, initialize the chunk's blocks 1d array
        voxelMap = new byte[VoxelData.ChunkVolume]; // length of 1d array = total volume of chunk
        
        this._world = _world;
    }

    //procedurally generate terrain based on perlin noise that takes in consideration local coord and global chunk coord
    public void GenerateTerrain(Vector3Int chunkCoord) {
        //use system random for multithreading support(we can't multitread unity api stuff)
        System.Random rng = new System.Random(_world.seed + chunkCoord.x * 1000 + chunkCoord.z);

        //generate 3d cave noise from 4 in 4 blocks
        int step = 4;
        int gridX = (VoxelData.ChunkWidth / step) + 1;
        int gridY = (VoxelData.ChunkHeight / step) + 1;
        int gridZ = (VoxelData.ChunkDepth / step) + 1;

        float[,,] caveNoiseGrid = new float[gridX, gridY, gridZ]; //dense array

        for(int gx =0 ; gx < gridX; gx++) {
            for(int gz= 0; gz < gridZ; gz++) {
                for(int gy = 0; gy < gridY; gy++) {
                    float globalX = (chunkCoord.x * VoxelData.ChunkWidth) + (gx * step);
                    float globalZ = (chunkCoord.z * VoxelData.ChunkDepth) + (gz * step);
                    float globalY = (gy * step) * 2.5f;

                    caveNoiseGrid[gx, gy, gz] = _world.caveNoise.GetNoise(globalX, globalY, globalZ);
                }
            }
        }


        //go through all 2D flat coordinates, figure out for each what height terrain to reach


        for (int x = 0; x < VoxelData.ChunkWidth; x++) {
            for (int z = 0; z < VoxelData.ChunkDepth; z++) {
                //get global coordinates of this block
                float globalX = (chunkCoord.x * VoxelData.ChunkWidth + x);
                float globalZ = (chunkCoord.z * VoxelData.ChunkDepth + z);

                //old perlin noise
                //generate perlin noise value based on global block coordinates
                //float noiseValue = Mathf.PerlinNoise((globalX + _world.offsetX )* VoxelData.TerrainNoiseScale, (globalZ + _world.offsetZ) * VoxelData.TerrainNoiseScale);

                //new simplex noise with fastnoiselite
                float rawSurfaceNoise = _world.surfaceNoise.GetNoise(globalX, globalZ); // get the noise at the chunk coordinates
                //simplex gives noise from -1 to 1 while mathf.perlinnoise what we used in the paste gives from 0 to 1, so we normalize to not break the existing terrain logic
                float normalizedSurfaceNoise = (rawSurfaceNoise + 1f) / 2f;

                //flatten the noise, smaller values will get smaller, bigger values will remain big
                float flattenedNoise = Mathf.Pow(normalizedSurfaceNoise, VoxelData.flattenNoiseExponent);

                //round/multiply to actual terrain height
                terrainHeight = Mathf.RoundToInt(flattenedNoise * VoxelData.TerrainHeightMultiplier) + VoxelData.TerrainSolidGroundHeight;

                //set Y=0 to bedrock
                int index = VoxelData.Get1DIndex(x, 0, z);
                voxelMap[index] = (byte)BlockType.Bedrock;

                //TRILINEAR INTERPOLATION FOR CAVES VARIABLE DECLARATION
                int gX0 = x / step;
                int gX1 = gX0 + 1;
                float tx = (x % step) / (float)step;

                int gZ0 = z / step;
                int gZ1 = gZ0 + 1;
                float tz = (z % step) / (float) step;

                //fill with blocks from y=1
                for (int y = 1; y < VoxelData.ChunkHeight; y++) {
                    index = VoxelData.Get1DIndex(x, y, z);

                    //if its air skip
                    if(y > terrainHeight + 1) {
                        voxelMap[index] = (byte)BlockType.Air;
                        continue;
                    }

                    // CAVE TRILINEAR INTERPOLATION
                    int gY0 = y / step;
                    int gY1 = gY0 + 1;
                    float ty = (y % step) / (float)step;

                    //get values from 8 corners of current "cube"
                    float c000 = caveNoiseGrid[gX0, gY0, gZ0];
                    float c100 = caveNoiseGrid[gX1, gY0, gZ0];
                    float c010 = caveNoiseGrid[gX0, gY1, gZ0];
                    float c110 = caveNoiseGrid[gX1, gY1, gZ0];
                    float c001 = caveNoiseGrid[gX0, gY0, gZ1];
                    float c101 = caveNoiseGrid[gX1, gY0, gZ1];
                    float c011 = caveNoiseGrid[gX0, gY1, gZ1];
                    float c111 = caveNoiseGrid[gX1, gY1, gZ1];

                    //lerp on x-axis
                    float c00 = Mathf.Lerp(c000, c100, tx);
                    float c10 = Mathf.Lerp(c010, c110, tx);
                    float c01 = Mathf.Lerp(c001, c101, tx);
                    float c11 = Mathf.Lerp(c011, c111, tx);

                    //lerp on y
                    float c0 = Mathf.Lerp(c00, c10, ty);
                    float c1 = Mathf.Lerp(c01, c11, ty);

                    //lerp and "guess" noise on z
                    float currentCaveNoise = Mathf.Lerp(c0, c1, tz);

                    float surfaceProximity = (float) y / terrainHeight;
                    float noiseThreshold = Mathf.Lerp(VoxelData.deepTunnelThreshold, VoxelData.surfaceTunnelThreshold, surfaceProximity);

                    bool isCave = currentCaveNoise > noiseThreshold;

                    if (isCave && y <= terrainHeight) {
                        voxelMap[index] = (byte)BlockType.Air;
                    } else if (y == terrainHeight) { //top block  is grass
                        voxelMap[index] = (byte)BlockType.Grass;
                    } else if (y < terrainHeight && y > terrainHeight - 5) { // next 4 blocks are dirt
                        voxelMap[index] = (byte)BlockType.Dirt;
                    } else if (y <= terrainHeight - 5) { // rest below is stone
                        voxelMap[index] = (byte)BlockType.Stone;
                    } else if (y == terrainHeight + 1) { //sometimes add TALL GRASS
                        float randomChance = (float)rng.NextDouble();

                        if (randomChance < VoxelData.grassChance && voxelMap[VoxelData.Get1DIndex(x,y-1,z)] == (byte)BlockType.Grass) {
                            //check to only generate tall grass over grass
                            voxelMap[index] = (byte)BlockType.TallGrass;
                        } else {
                            voxelMap[index] = (byte)BlockType.Air;
                        }
                    } else { // what is above will be air
                        voxelMap[index] = (byte)BlockType.Air;
                    }
                }
            }
        }

        //check chunk again to add trees
        for (int x=2;x<VoxelData.ChunkWidth-2;x++) {
            for(int z = 2; z < VoxelData.ChunkDepth - 2; z++) {

                //check where is the grass
                int surfaceY = 0;
                for(int y = VoxelData.ChunkHeight - 1; y > 0; y--) {
                    int index = VoxelData.Get1DIndex(x, y, z);
                    if (voxelMap[index] == (byte)BlockType.Grass) {
                        surfaceY = y;
                        break;
                    }
                }

                //if 
                if(surfaceY > 0) {
                    if (rng.NextDouble() < VoxelData.treeChance) {
                        GenerateTree(x, surfaceY + 1, z);
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

    // ** "CODE PREFABS" - structures

    //tree
    private void GenerateTree(int baseX, int baseY, int baseZ) {
        System.Random treeRng = new System.Random(baseX * 100 + baseZ);
        int trunkHeight = treeRng.Next(4,7); //trhunk height

        //generate trunk
        for (int i = 0; i < trunkHeight; i++) {
            if (baseY + i >= VoxelData.ChunkHeight) break; //check to not go over Y world limit

            //set to wood
            int index = VoxelData.Get1DIndex(baseX, baseY + i, baseZ);
            voxelMap[index] = (byte)BlockType.Wood;
        }

        //generate leaves
        int leafRadius = 2;
        int leavesStart = baseY + trunkHeight - 2;
        int leavesEnd = baseY + trunkHeight + 1;

        for (int y = leavesStart; y <= leavesEnd; y++) {
            //as we go up make the radius smaller
            int currentRadius = (y == leavesEnd) ? leafRadius - 1 : leafRadius;

            for (int x = -currentRadius; x <= currentRadius; x++) {
                for (int z = -currentRadius; z <= currentRadius; z++) {
                    int targetX = baseX + x;
                    int targetZ = baseZ + z;

                    //cut corners so its not a cube
                    if (Mathf.Abs(x) == currentRadius && Mathf.Abs(z) == currentRadius ) {
                        if(treeRng.NextDouble() > 0.5f) {
                            continue;
                        }
                        if(y != leavesStart) {
                            continue;
                        }
                    }


                    //check to not exit chunk
                    if(targetX >= 0 && targetX < VoxelData.ChunkWidth &&  targetZ >= 0 && targetZ < VoxelData.ChunkDepth && y < VoxelData.ChunkHeight) {
                        int index = VoxelData.Get1DIndex(targetX, y, targetZ);

                        //if its not trunk place leaves
                        if (voxelMap[index] != (byte)BlockType.Wood) {
                            voxelMap[index] = (byte)BlockType.Leaves;
                        }
                    }
                }
            }
        }
    }
}
