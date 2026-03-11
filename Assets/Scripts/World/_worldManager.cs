using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _worldManager : MonoBehaviour {
    //assign in inspector
    public GameObject chunkPrefab;
    public Transform playerTransform;

    [Header("Render Distance Settings")]
    public int renderDistance = 4;
    public float chunkUpdateInterval = 0.5f; // every 0.5 seconds look if need to show new chunks
    private float chunkUpdateTimer = 0f; // internal timer

    [Header("World Generator Settings")]
    public int seed;
    public bool useRandomSeed = true;

    [HideInInspector] public float offsetX; // offsets for perln noiuse map
    [HideInInspector] public float offsetZ;

    public int worldSizeInChunks = 10; // it will generate a grid of 10x10

    // **OLD**
    //MAP OF CHUNKS - basically the world storage map
    //dictionary of key-Coordinate:value-chunk data for instant access
    //public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    // 2 dictionaries, one for CHUNKDATA for actual content of every chunk - all of it instantiated and present on runtime
    private Dictionary<Vector3Int, ChunkData> worldData = new Dictionary<Vector3Int, ChunkData> (); 

    // one for CHUNKS visual representation, will only instantiate what is required based on radius/render distance
    private Dictionary<Vector3Int, Chunk> activeChunks = new Dictionary<Vector3Int, Chunk> ();

    private void Start() {

        if (useRandomSeed) {
            seed = Random.Range(-99999, 99999);
        }

        //init random generator to generate DIFFERENT numbers
        Random.InitState(seed);
        offsetX = Random.Range(-100000f, 100000f);
        offsetZ = Random.Range(-100000f, 100000f);

        Debug.Log("generating world with Seed: " + seed);

        //force first generation instantly
        UpdateChunksAroundPlayer();
    }

    private void Update() {
        //internal clock to check new chunks, avoinding IEnumerators
        chunkUpdateTimer -= Time.deltaTime;

        if(chunkUpdateTimer <= 0f) {
            //look around to inspect new (or saved) chunks to be drawn
            UpdateChunksAroundPlayer();
            chunkUpdateTimer = chunkUpdateInterval;
        }
    }

    //unload visual of non needed chunks, load visual of new ones required or existing ones which have been unloaded and their data is present in memory
    private void UpdateChunksAroundPlayer() {
        //find out current player chunk
        int playerChunkX = Mathf.FloorToInt(playerTransform.position.x / VoxelData.ChunkWidth);
        int playerChunkZ = Mathf.FloorToInt(playerTransform.position.z / VoxelData.ChunkDepth);

        //unload no longer needed chunk visuals
        List<Vector3Int> chunksToRemove = new List<Vector3Int> ();

        foreach (var kvp in activeChunks) {
            Vector3Int coord = kvp.Key;

            //if distance is bigger than render distance, add to remove list
            if (Mathf.Abs(coord.x - playerChunkX) > renderDistance || Mathf.Abs(coord.z - playerChunkZ) > renderDistance) {
                chunksToRemove.Add(coord);
            }
        }

        foreach (Vector3Int coord in chunksToRemove) {
            Destroy(activeChunks[coord].gameObject); // destroy chunk gameobject
            activeChunks.Remove(coord); // remove from active list
            //we let it remain in worldData so any changes to it remain
        }

        //generate new chunks/ load existing ones from worldData where needed
        for (int x = -renderDistance; x <= renderDistance; x++) {
            for (int z = -renderDistance; z <= renderDistance; z++) {
                Vector3Int coord = new Vector3Int(playerChunkX + x, 0, playerChunkZ + z);

                //if its not already visible, load it
                if (!activeChunks.ContainsKey(coord)) {
                    LoadChunk(coord); 
                }
            }
        }
    }

    //load chunk from data dictionary if it exists there, if not, generate new one
    private void LoadChunk(Vector3Int coord) {
        //check if it doesnt exist, generate it then
        if (!worldData.ContainsKey(coord)) {
            //dont have
            //generate
            ChunkData newData = new ChunkData(this); // pass world
            newData.GenerateTerrain(coord);
            worldData.Add(coord, newData);
        }

        //now instantiate visual chunk
        Vector3 spawnPosition = new Vector3(coord.x * VoxelData.ChunkWidth, 0, coord.z * VoxelData.ChunkDepth);
        GameObject newChunkObj = Instantiate(chunkPrefab, spawnPosition, Quaternion.identity);

        Chunk newChunk = newChunkObj.GetComponent<Chunk>();

        //initialize the chunk with data from dicitonary
        newChunk.Init(coord, this, worldData[coord]);
        newChunk.GenerateMesh();

        //add to list of active chunks
        activeChunks.Add(coord, newChunk);
    }

    /* previous fixed world generate function
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
    */

    //function that takes coordinates and looks in chunksdata dictionary->chunk's array to find the exact block at those coordinates
    public byte GetVoxelGlobal(Vector3Int globalPos) {
        //find the chunk, floorToInt used to make sure negative numbers round correctly
        int chunkX = Mathf.FloorToInt((float)globalPos.x / VoxelData.ChunkWidth);
        int chunkZ = Mathf.FloorToInt((float)globalPos.z / VoxelData.ChunkDepth);

        //we have the exact chunk's coords
        Vector3Int targetchunkCoord = new Vector3Int(chunkX, 0, chunkZ);

        // check if chunk is in dictionary
        if(worldData.TryGetValue(targetchunkCoord, out ChunkData neighbourChunk)) {
            
            //get local block coordinate
            int localX = globalPos.x - (targetchunkCoord.x * VoxelData.ChunkWidth);
            int localY = globalPos.y; // y is unchanged
            int localZ = globalPos.z - (targetchunkCoord.z * VoxelData.ChunkDepth);

            //get block
            return neighbourChunk.GetVoxel(localX, localY, localZ);
        }
        
        //fallback return air
        return (byte)BlockType.Air;
    }

    //function that takes coordinates & block id and looks in chunksdata dictionary->chunk's array to find the exact block at those coordinates and replace it with blockID
    public void SetVoxelGlobal(Vector3Int globalPos, byte blockID) {
        //find the chunk, floorToInt used to make sure negative numbers round correctly
        int chunkX = Mathf.FloorToInt((float)globalPos.x / VoxelData.ChunkWidth);
        int chunkZ = Mathf.FloorToInt((float)globalPos.z / VoxelData.ChunkDepth);

        //we have the exact chunk's coords
        Vector3Int targetchunkCoord = new Vector3Int(chunkX, 0, chunkZ);

        // check if chunk is in dictionary
        if (worldData.TryGetValue(targetchunkCoord, out ChunkData targetChunk)) {
            //get local block coordinate
            int localX = globalPos.x - (targetchunkCoord.x * VoxelData.ChunkWidth);
            int localY = globalPos.y; // y is unchanged
            int localZ = globalPos.z - (targetchunkCoord.z * VoxelData.ChunkDepth);

            //set new id
            targetChunk.SetVoxel(localX, localY, localZ, blockID);

            //if chunk is active, regenerate the mesh
            if(activeChunks.TryGetValue(targetchunkCoord, out Chunk activeChunk)) {
                activeChunk.GenerateMesh();
            }

            //If we are on chunk edge, then we also need to update the neighbour chunk, or else it would remain with empty edge face

            //if we are on X=0 we need to update chunk with x-1 coord
            if(localX == 0) {
                UpdateChunkMesh(new Vector3Int(targetchunkCoord.x - 1, targetchunkCoord.y, targetchunkCoord.z));
            }

            //x=15(width-1) we need to update chunk of x+1
            if (localX == VoxelData.ChunkWidth - 1) {
                UpdateChunkMesh(new Vector3Int(targetchunkCoord.x + 1, targetchunkCoord.y, targetchunkCoord.z));
            }

            //z=0->update chunk of z - 1 and z=15 update chunk of z + 1
            if (localZ == 0) {
                UpdateChunkMesh(new Vector3Int(targetchunkCoord.x, targetchunkCoord.y, targetchunkCoord.z - 1));
            }

            if (localZ == VoxelData.ChunkDepth - 1) {
                UpdateChunkMesh(new Vector3Int(targetchunkCoord.x, targetchunkCoord.y, targetchunkCoord.z + 1));
            }
        }
    }

    //helper function to get a chunk coordinates and if they exist in the dictionary update its mesh
    private void UpdateChunkMesh(Vector3Int coord) {
        if(activeChunks.TryGetValue(coord, out Chunk neighbourChunk)) {
            neighbourChunk.GenerateMesh();
        }
    }
}
