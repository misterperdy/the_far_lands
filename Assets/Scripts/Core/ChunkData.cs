// pure c# code, no unity editor functionality (only minimum required ones like usage of vector3 for ex.), we want MINIMUM OVERHEAD

// this script will hold the information for one chunk

using System;
using System.Xml.Schema;
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

                //old perlin noise
                //generate perlin noise value based on global block coordinates
                //float noiseValue = Mathf.PerlinNoise((globalX + _world.offsetX )* VoxelData.TerrainNoiseScale, (globalZ + _world.offsetZ) * VoxelData.TerrainNoiseScale);

                //new simplex noise with fastnoiselite
                float rawSurfaceNoise = _world.surfaceNoise.GetNoise(globalX, globalZ); // get the noise at the chunk coordinates
                //simplex gives noise from -1 to 1 while mathf.perlinnoise what we used in the paste gives from 0 to 1, so we normalize to not break the existing terrain logic
                float normalizedNoise = (rawSurfaceNoise + 1f) / 2f;

                //round/multiply to actual terrain height
                int terrainHeight = Mathf.RoundToInt(normalizedNoise * VoxelData.TerrainHeightMultiplier) + VoxelData.TerrainSolidGroundHeight;

                //set Y=0 to bedrock
                int index = VoxelData.Get1DIndex(x, 0, z);
                voxelMap[index] = (byte)BlockType.Bedrock;

                //fill with blocks from y=1
                for (int y = 1; y < VoxelData.ChunkHeight; y++) {
                    index = VoxelData.Get1DIndex(x, y, z);

                    if (y == terrainHeight) { //top block  is grass
                        voxelMap[index] = (byte)BlockType.Grass;
                    } else if (y < terrainHeight && y > terrainHeight - 5) { // next 4 blocks are dirt
                        voxelMap[index] = (byte)BlockType.Dirt;
                    } else if (y <= terrainHeight - 5) { // rest below is stone
                        voxelMap[index] = (byte)BlockType.Stone;
                    } else if (y == terrainHeight + 1) { //sometimes add TALL GRASS
                        float randomChance = UnityEngine.Random.value;

                        if(randomChance < VoxelData.grassChance) {
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
        for(int x=2;x<VoxelData.ChunkWidth-2;x++) {
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
                    if (UnityEngine.Random.value < VoxelData.treeChance) {
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
        int trunkHeight = UnityEngine.Random.Range(4, 7); //trhunk height

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
                        if(UnityEngine.Random.value > 0.5f) {
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
