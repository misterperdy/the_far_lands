using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _worldManager : MonoBehaviour {
    //assign in inspector
    public GameObject chunkPrefab;

    public int worldSizeInChunks = 10; // it will generate a grid of 10x10

    //MAP OF CHUNKS - basically the world storage map
    //dictionary of key-Coordinate:value-chunk data for instant access
    public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    private void Start() {
        //generate a world of fixed size for demonstration
        GenerateFixedWorld();
    }

    private void GenerateFixedWorld() {
        //we want player to be in middle so we loop from -halfsize to halfsize
        int halfSize = worldSizeInChunks / 2;

        for (int x = -halfSize; x <= halfSize; x++) {
            for (int z = -halfSize; z <= halfSize; z++) {
                //coordinate of chunk
                Vector3Int chunkCoord = new Vector3Int(x, 0, z);

                //actual 3d position in scene
                Vector3 spawnPosition = new Vector3(x * VoxelData.ChunkWidth, 0, z * VoxelData.ChunkDepth);

                //insantiate prefab in scene
                GameObject newChunk = Instantiate(chunkPrefab, spawnPosition, Quaternion.identity, this.transform);

                //init the chunk
                Chunk newChunkScript = newChunk.GetComponent<Chunk>();
                //DEPENDENCY INJECTION - send this world object in init of new chunk to avoid static variables (problems with unity scene changing)
                newChunkScript.Init(chunkCoord, this);

                //save chunk in dictionary
                chunks.Add(chunkCoord, newChunkScript);
            }
        }

        //after all chunks have been initialized, generate geometry
        foreach (var chunk in chunks.Values) {
            chunk.GenerateMesh();
        }
    }

    //function that takes coordinates and looks in chunks dictionary->chunk's array to find the exact block at those coordinates
    public byte GetVoxelGlobal(Vector3Int globalPos) {
        //find the chunk, floorToInt used to make sure negative numbers round correctly
        int chunkX = Mathf.FloorToInt((float)globalPos.x / VoxelData.ChunkWidth);
        int chunkZ = Mathf.FloorToInt((float)globalPos.z / VoxelData.ChunkDepth);

        //we have the exact chunk's coords
        Vector3Int targetchunkCoord = new Vector3Int(chunkX, 0, chunkZ);

        // check if chunk is in dictionary
        if(chunks.TryGetValue(targetchunkCoord, out Chunk neighbourChunk)) {
            
            //get local block coordinate
            int localX = globalPos.x - (targetchunkCoord.x * VoxelData.ChunkWidth);
            int localY = globalPos.y; // y is unchanged
            int localZ = globalPos.z - (targetchunkCoord.z * VoxelData.ChunkDepth);

            //get block
            return neighbourChunk.GetVoxelFromChunkData(localX, localY, localZ);
        }
        
        //fallback return air
        return (byte)BlockType.Air;
    }
}
