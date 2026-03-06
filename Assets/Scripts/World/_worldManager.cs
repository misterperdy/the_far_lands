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
                newChunkScript.Init(chunkCoord);

                //save chunk in dictionary
                chunks.Add(chunkCoord, newChunkScript);
            }
        }
    }
}
