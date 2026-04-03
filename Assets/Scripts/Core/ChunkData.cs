// pure c# code, no unity editor functionality (only minimum required ones like usage of vector3 for ex.), we want MINIMUM OVERHEAD

// this script will hold the information for one chunk

using JetBrains.Annotations;
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

                //TRILINEAR INTERPOLATION FOR CAVES VARIABLE DECLARATION
                int gX0 = x / step;
                int gX1 = gX0 + 1;
                float tx = (x % step) / (float)step;

                int gZ0 = z / step;
                int gZ1 = gZ0 + 1;
                float tz = (z % step) / (float)step;

                //check column
                for (int y = 0; y < VoxelData.ChunkHeight; y++) {

                    int index = VoxelData.Get1DIndex(x, y, z);

                    //1st pass - air & water
                    if (y > terrainHeight) {
                        if (y <= VoxelData.waterLevel) {
                            SetVoxel(x, y, z, (byte)BlockType.Water); // puddles
                        } else {
                            SetVoxel(x, y, z, (byte)BlockType.Air); // air
                        }
                        continue; // skip to jnext Y
                    }

                    //2nd pass - solid terrain
                    byte blockToPlace = (byte)BlockType.Stone;

                    if (y == 0) { // bottom layer is unbreakable bedrock
                        blockToPlace = (byte)BlockType.Bedrock;
                    }else if( y >= terrainHeight - 3) {
                        //last 3 layers

                        if(terrainHeight <= VoxelData.waterLevel + 1) {
                            blockToPlace = (byte)BlockType.Sand; // sand if we are next to water
                        } else {
                            //normal block
                            blockToPlace = (y == terrainHeight) ? (byte)BlockType.Grass : (byte)BlockType.Dirt; //grass on top and dirt below
                        }

                    }

                    //caves
                    if(blockToPlace != (byte)BlockType.Bedrock) {

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

                        float surfaceProximity = (float)y / terrainHeight;
                        float noiseThreshold = Mathf.Lerp(VoxelData.deepTunnelThreshold, VoxelData.surfaceTunnelThreshold, surfaceProximity);

                        bool isCave = currentCaveNoise > noiseThreshold;

                        if (isCave) {
                            bool isUnderwater = (terrainHeight <= VoxelData.waterLevel);
                            bool isTooCloseToSurface = (y >= terrainHeight - 3);

                            //if it would hit water don't make air
                            if(!(isUnderwater && isTooCloseToSurface)) {
                                blockToPlace = (byte)BlockType.Air;
                            }

                            
                        }
                    }
                    voxelMap[index] = blockToPlace;
                }
            }
        }

        //** WORM TUNNELS

        if (rng.NextDouble() < VoxelData.WormTunnelChance) {
            //5% chance to make a tunn el
            int startX = rng.Next(4, VoxelData.ChunkWidth - 4);
            int startZ = rng.Next(4, VoxelData.ChunkDepth - 4);

            GenerateWormTunnel(startX, startZ, chunkCoord, rng);
        }

        //3rd pass - add grass to top blocks made dirt by cave generation
        for(int x = 0; x < VoxelData.ChunkWidth; x++) {
            for(int z = 0; z < VoxelData.ChunkDepth; z++) {

                for(int y = VoxelData.ChunkHeight -1; y >= 0; y--) {
                    byte blockID = GetVoxel(x, y, z);

                    //if its air or leaves or cross block skip
                    if(blockID == (byte)BlockType.Air || VoxelData.IsCrossModel(blockID) || VoxelData.IsTransparent(blockID)) {
                        continue;
                    }

                    //if its dirt turn to gress
                    if(blockID == (byte)BlockType.Dirt) {
                        SetVoxel(x, y, z, (byte)BlockType.Grass);
                    }

                    break; //dont go below, go to next column
                }

            }
        }

        //add ores
        GenerateOres(rng);

        //bottom lava lakes
        for(int x= 0; x < VoxelData.ChunkWidth; x++) {
            for(int z=0;z<VoxelData.ChunkDepth; z++) {

                //only on Y=1 check
                int index = VoxelData.Get1DIndex(x, 1, z);

                if (voxelMap[index] == (byte)BlockType.Air) {
                    voxelMap[index] = (byte)BlockType.Lava; //set to lava only if its air to keep stone and ores
                }
            }
        }

        //structures: check chunk again to add trees & tall grass
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
                    } else if (rng.NextDouble() < VoxelData.grassChance) {
                        //tall grass
                        int grassIndex = VoxelData.Get1DIndex(x, surfaceY + 1, z);
                        voxelMap[grassIndex] = (byte)BlockType.TallGrass;
                    }
                }
            }
        }
    }

    //generate tunnel go down similar to drunk walk ore style, to recreate natural style
    private void GenerateWormTunnel(int startX, int startZ, Vector3Int chunkCoord, System.Random prng) {
        //find surface y
        int surfaceY = 0;
        for (int y = VoxelData.ChunkHeight; y > 0; y--) {
            int index = VoxelData.Get1DIndex(startX, y, startZ);

            if (voxelMap[index] != (byte)BlockType.Air && voxelMap[index] != (byte)BlockType.Water) {
                //found first solid block from top
                surfaceY = y;
                break;
            }
        }

        //make sure its not water level (prevent flooding)
        if (surfaceY <= VoxelData.waterLevel + 2) return;

        //find if there is a cave below us
        int targetY = -1;
        for(int y = surfaceY - 5; y > 10; y--) {
            float globalX = (chunkCoord.x * VoxelData.ChunkWidth) + startX;
            float globalZ = (chunkCoord.z * VoxelData.ChunkDepth) + startZ;
            float globalY = y * 2.5f;

            //regenerate same noise used for cave
            float noiseVal = _world.caveNoise.GetNoise(globalX, globalY, globalZ);
            float surfaceProximity = (float)y / surfaceY;
            float noiseThreshold = Mathf.Lerp(VoxelData.deepTunnelThreshold, VoxelData.surfaceTunnelThreshold, surfaceProximity);

            if(noiseVal > noiseThreshold) {
                targetY = y; //roof of cave found
                break;
            }
        }

        //if we haven't found a cave, abandon the worm
        if (targetY == -1) return;

        //drunk walk worm, reach the target Y from surface Y
        Vector3 currentPos = new Vector3(startX, surfaceY, startZ);

        float radius = 2.5f;

        while(currentPos.y > targetY) {
            //carve sphere
            CarveSphere(Mathf.RoundToInt(currentPos.x), Mathf.RoundToInt(currentPos.y), Mathf.RoundToInt(currentPos.z), radius);

            //go below and deviate the trajectory for a more realistic look
            currentPos.y -= 1f;
            currentPos.x += (float)(prng.NextDouble() * 2f - 1f);
            currentPos.z += (float)(prng.NextDouble() * 2f - 1f);

            //pulse the radius
            radius = 1.5f + (float)(prng.NextDouble() * 1.5f); //between 1.5 and 3.0
        }
    }

    //carve a sphere at these coordinates, used for worm caves tunnels to look more realistic
    private void CarveSphere(int cx, int cy, int cz, float radius) {
        int r = Mathf.CeilToInt(radius);
        float radiusSquared = radius * radius;

        for (int x = -r; x <= r; x++) {
            for (int y = -r; y <= r; y++) {
                for (int z = -r; z <= r; z++) {
                    //check we are inside the sphere
                    if (x * x + y * y + z * z <= radiusSquared) {

                        int targetX = cx + x;
                        int targetY = cy + y;
                        int targetZ = cz + z;

                        //check to not exit the chunk borders or to break bedrock
                        if (targetX >= 0 && targetX < VoxelData.ChunkWidth && targetY > 0 && targetY < VoxelData.ChunkHeight && targetZ >= 0 && targetZ < VoxelData.ChunkDepth) {

                            int index = VoxelData.Get1DIndex(targetX, targetY, targetZ);
                            byte currentBlock = voxelMap[index];

                            //if we hit liqiud/bedorck, don't 
                            if (currentBlock != (byte)BlockType.Water && currentBlock != (byte)BlockType.Lava && currentBlock != (byte)BlockType.Bedrock) {
                                voxelMap[index] = (byte)BlockType.Air; //set to air
                            }
                        }
                    }
                }
            }
        }
    }

    //functions that parse the underground terrain and add ores by "drunk walking" veins (*prng is Pseudo Random Number Generator, cause CPU can't roll dice)
    private void GenerateOres(System.Random prng) {
        foreach (VoxelData.OreSettings ore in VoxelData.Ores) {
            for(int i = 0; i < ore.spawnAttempts; i++) {
                //chose start location
                int startX = prng.Next(0, VoxelData.ChunkWidth);
                int startY = prng.Next(ore.minY, ore.maxY + 1); //exclusive max
                int startZ = prng.Next(0, VoxelData.ChunkDepth);

                int index = VoxelData.Get1DIndex(startX, startY, startZ);

                //check if its stone
                if (voxelMap[index] == (byte)BlockType.Stone) {
                    int veinSize = prng.Next(ore.minVeinSize, ore.maxVeinSize + 1);
                    GenerateVein(startX, startY, startZ, veinSize, ore.blockID, prng);
                }
            }
        }
    }

    //random walk allgorithm to generate the vein
    private void GenerateVein(int startX, int startY, int startZ, int veinSize, byte blockID, System.Random prng) {
        int currentX = startX;
        int currentY = startY;
        int currentZ = startZ;

        //try place for vein size amount of times
        for(int i = 0; i < veinSize; i++) {
            //check to not exit chunk
            if(currentX < 0 || currentX >= VoxelData.ChunkWidth || currentY < 1 || currentY >= VoxelData.ChunkHeight || currentZ < 0 || currentZ >= VoxelData.ChunkDepth) {
                break;
            }

            int index = VoxelData.Get1DIndex(currentX, currentY, currentZ);

            //only place ore if it's still stone
            if (voxelMap[index] == (byte)BlockType.Stone) {
                voxelMap[index] = blockID;
            }

            //randomyl choose next direction to go to
            int direction = prng.Next(0, 6);
            switch (direction) {
                case 0: currentX++; break;
                case 1: currentY++; break;
                case 2: currentZ++; break;
                case 3: currentX--; break;
                case 4: currentY--; break;
                case 5: currentZ--; break;
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
