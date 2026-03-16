using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class _worldManager : MonoBehaviour {
    //assign in inspector
    public GameObject chunkPrefab;
    public Transform playerTransform;

    [Header("Render Distance Settings")]
    public int renderDistance = 4;
    public float chunkUpdateInterval = 0.5f; // every 0.5 seconds look if need to show new chunks
    private float chunkUpdateTimer = 0f; // internal timer
    public float lazyChunkLoadingInterval = 0.1f; // break between rendering new chunks
    private float lazyChunkLoadingTimer = 0f;

    [Header("World Generator Settings")]
    public int seed;
    public bool useRandomSeed = true;

    [Header("Random Tick System")]
    public float tickInterval = 0.05f; //20 tps
    public int randomTicksPerChunk = 24; //how many blocks to check per chunk to update
    private float tickTimer = 0f; // internal timer

    [Header("Particle System")]
    public GameObject[] blockBreakParticlePrefabs;

    //spawning variables/timer
    private bool isPlayerSpawned = false;
    private float spawnCheckTimer = 0.5f;

    // 2 dictionaries, one for CHUNKDATA for actual content of every chunk - all of it instantiated and present on runtime
    private Dictionary<Vector3Int, ChunkData> worldData = new Dictionary<Vector3Int, ChunkData> (); 

    // one for CHUNKS visual representation, will only instantiate what is required based on radius/render distance
    private Dictionary<Vector3Int, Chunk> activeChunks = new Dictionary<Vector3Int, Chunk> ();

    //object pool for chunks
    //we will create our own queue instead of using Unity's object pool for 100% code transparency
    private Queue<Chunk> chunkPool = new Queue<Chunk>();

    //waiting queue for loading chunk tasks
    private Queue<Vector3Int> chunksToLoadQueue = new Queue<Vector3Int> ();

    CharacterController _playerCharController;

    //noises
    [HideInInspector]public FastNoiseLite caveNoise; //reference to cave noise script (3D)
    [HideInInspector] public FastNoiseLite surfaceNoise; // surface noise reference (2D)

    // **OLD UNUSED VARIABLES**

    //MAP OF CHUNKS - basically the world storage map
    //dictionary of key-Coordinate:value-chunk data for instant access
    //public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    //public int worldSizeInChunks = 10; // it will generate a grid of 10x10 - no longer used we have infinite worlds now

    private void Start() {

        if (useRandomSeed) {
            seed = Random.Range(-99999, 99999);
        }

        Debug.Log("generating world with Seed: " + seed);

        //initialize  noise
        InitializeNoise();

        //init pool
        int poolSize = (renderDistance * 2 + 1) * (renderDistance * 2 + 1) + renderDistance; //render distance squiared + render distance safety padding 

        _playerCharController = playerTransform.GetComponent<CharacterController>();
        if (_playerCharController != null) {
            _playerCharController.enabled = false; //freeze player until chunks generated
        }

        for (int i = 0; i < poolSize; i++) {
            GameObject newChunkObj = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity);
            newChunkObj.SetActive(false);

            Chunk newChunk = newChunkObj.GetComponent<Chunk>();
            chunkPool.Enqueue(newChunk);
        }

        //force first generation instantly
        UpdateChunksAroundPlayer();
    }

    //function to set up cave (3d) + top (2d) noise script with given parameters
    private void InitializeNoise() {
        //migrated from perlin noise to OpenSimplex2 (open source variant of simplex noise) it looks better for terrain

        //top noise
        surfaceNoise = new FastNoiseLite();
        surfaceNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        surfaceNoise.SetSeed(seed);
        surfaceNoise.SetFrequency(VoxelData.TerrainNoiseScale);
        surfaceNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        surfaceNoise.SetFractalOctaves(3);

        //cave noise
        caveNoise = new FastNoiseLite();
        caveNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2); // looks more natural than 3D perlin, more organic
        caveNoise.SetSeed(seed);
        caveNoise.SetFrequency(VoxelData.caveNoiseFrequency); // zoom in/out of noise
        caveNoise.SetFractalType(FastNoiseLite.FractalType.FBm); //fractal brownian motion, more layers of noise, and they get add up
        caveNoise.SetFractalOctaves(3);
        //octave 1 is the base of the cave rooms, 2 adds more variety/holes in ground/top/walls, and third adds final details, gives realistic cave look
    }

    private void Update() {
        //spawning player logic
        if (!isPlayerSpawned) {
            spawnCheckTimer -= Time.deltaTime;

            if (spawnCheckTimer <= 0f) {
                CheckSpawn(); //try to see if chunks loaded and can spawn
                spawnCheckTimer = 0.5f; //if not, load the timer again
            }
        }

        //generate 1 chunk per time interval for loading queue to avoid lag spikes
        lazyChunkLoadingTimer += Time.deltaTime;

        if (chunksToLoadQueue.Count > 0 && lazyChunkLoadingTimer >= lazyChunkLoadingInterval) {
            lazyChunkLoadingTimer = 0f;

            Vector3Int nextChunkCoord = chunksToLoadQueue.Dequeue();

            //make sure its not already loaded
            if (!activeChunks.ContainsKey(nextChunkCoord)) {
                LoadChunk(nextChunkCoord); // load it
            }
        }

        //internal clock to check new chunks, avoinding IEnumerators
        chunkUpdateTimer -= Time.deltaTime;

        if(chunkUpdateTimer <= 0f) {
            //look around to inspect new (or saved) chunks to be drawn
            UpdateChunksAroundPlayer();
            chunkUpdateTimer = chunkUpdateInterval;
        }

        //internal clock to check chunk update ticks
        tickTimer += Time.deltaTime;

        if(tickTimer >= tickInterval) {
            tickTimer = 0f;
            ProcessRandomTicks();
        }
    }

    //pick a few blocks per chunk to update every time function hits - right now used for dirt-grass conversion
    private void ProcessRandomTicks() {
        //only go through active chunks
        foreach (Chunk chunk in activeChunks.Values) {

            //pick random blocks
            for (int i = 0; i < randomTicksPerChunk; i++) {

                //Grass-Dirt-Grass conversion logic:

                //local random coords
                int x = Random.Range(0, VoxelData.ChunkWidth);
                int y = Random.Range(0, VoxelData.ChunkHeight);
                int z = Random.Range(0, VoxelData.ChunkDepth);

                //get block
                byte blockID = chunk.GetVoxelFromChunkData(x, y, z);

                //grass -> dirt conversion when its obstructed
                if (blockID == (byte)BlockType.Grass) {
                    byte blockAbove = chunk.GetVoxelFromChunkData(x, y + 1, z);

                    //if its not air and its not transparent turn grass to dirt
                    if (blockAbove != (byte)BlockType.Air && !VoxelData.IsTransparent(blockAbove)) {
                        chunk.SetVoxelToChunkData(x, y, z, (byte)BlockType.Dirt);
                        chunk.GenerateMesh(); // regenerate mesh
                    }

                }//else if its dirt and next to grass, turn it into grass spread
                else if (blockID == (byte)BlockType.Dirt) {
                    byte blockAbove = chunk.GetVoxelFromChunkData(x, y + 1, z);

                    //make sure its air/transparent
                    if (blockAbove == (byte)BlockType.Air || VoxelData.IsTransparent(blockAbove)) {

                        //pick random neighbour
                        int randomOffsetX = Random.Range(-1, 2);
                        int randomOffsetY = Random.Range(-1, 2);
                        int randomOffsetZ = Random.Range(-1, 2);

                        //check its local coordinate
                        int checkX = x + randomOffsetX;
                        int checkY = y + randomOffsetY;
                        int checkZ = z + randomOffsetZ;

                        byte neighbourID;

                        //if its inside this chunk
                        if(checkX >= 0 && checkX < VoxelData.ChunkWidth && checkY >= 0 && checkY < VoxelData.ChunkHeight && checkZ >= 0 && checkZ < VoxelData.ChunkDepth) {
                            //grab from chunk data
                            neighbourID = chunk.GetVoxelFromChunkData(checkX, checkY, checkZ);
                        } else {
                            //grab from entire world
                            neighbourID = GetVoxelGlobal(new Vector3Int(
                                checkX + (chunk.chunkCoord.x * VoxelData.ChunkWidth),
                                checkY,
                                checkZ + (chunk.chunkCoord.z * VoxelData.ChunkDepth)));
                        }

                        //check if neighbour is grass
                        if (neighbourID == (byte)BlockType.Grass) {
                            //convert ourselves to grass
                            chunk.SetVoxelToChunkData(x, y, z, (byte)BlockType.Grass);
                            chunk.GenerateMesh(); // regenerate mesh
                        }
                    }
                }
            }
        }
    }

    //check to see if 0,y,0 is generated and spawn the player
    private void CheckSpawn() {
        //if middle chunk is generated
        if (activeChunks.ContainsKey(Vector3Int.zero)) {
            int spawnY = VoxelData.ChunkHeight - 1;

            //FIND OUT WHAT IS THE highest terrain point, go down until its no longer air
            while(GetVoxelGlobal(new Vector3Int(0, spawnY, 0))==0 && spawnY > 0) {
                spawnY--;
            }

            //move player and unfreze
            playerTransform.position = new Vector3(0, spawnY + 2.5f, 0);

            if(_playerCharController != null) {
                _playerCharController.enabled = true;
            }

            isPlayerSpawned = true;
            Debug.Log("chunk found, player spawned");
        } else {
            Debug.Log("chunk not found, can't spawn player yet");
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
            //set hidden and add to queue pool
            Chunk chunk = activeChunks[coord];

            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);

            activeChunks.Remove(coord); // remove from active list
            //we let it remain in worldData so any changes to it remain
        }

        //generate new chunks/ load existing ones from worldData where needed
        for (int x = -renderDistance; x <= renderDistance; x++) {
            for (int z = -renderDistance; z <= renderDistance; z++) {
                Vector3Int coord = new Vector3Int(playerChunkX + x, 0, playerChunkZ + z);

                //if its not already visible, add to loading queue
                if (!activeChunks.ContainsKey(coord) && !chunksToLoadQueue.Contains(coord)) {
                    chunksToLoadQueue.Enqueue(coord);
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

        //try grab from pool avaialble chunk
        Chunk targetChunk;

        if(chunkPool.Count > 0) {
            targetChunk = chunkPool.Dequeue();
            targetChunk.gameObject.SetActive(true);
        } else {
            Debug.Log("chunk pool empty, instantiating new chunk");
            GameObject newChunkObj = Instantiate(chunkPrefab);
            targetChunk = newChunkObj.GetComponent<Chunk>();
        }

        //new transform
        targetChunk.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0, coord.z * VoxelData.ChunkDepth);

        //init with new data and redraw it
        targetChunk.Init(coord, this, worldData[coord]);
        targetChunk.GenerateMesh();

        //add to list of active chunks
        activeChunks.Add(coord, targetChunk);

        //Culling: when a chunk is generated, regenerate mesh of his 4 neighbors, to prevent any "undegraound walls"
        UpdateChunkMesh(new Vector3Int (coord.x-1, coord.y, coord.z));
        UpdateChunkMesh(new Vector3Int(coord.x+1, coord.y, coord.z));
        UpdateChunkMesh(new Vector3Int(coord.x, coord.y, coord.z-1));
        UpdateChunkMesh(new Vector3Int(coord.x, coord.y, coord.z+1));

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
        //vertical protection
        if (globalPos.y < 0 || globalPos.y >= VoxelData.ChunkHeight) return (byte)BlockType.Air;

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
        //prevent out of bounds veritcal
        if (globalPos.y < 0 || globalPos.y >= VoxelData.ChunkHeight) return;

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

    //spawn break block aprticles from prefab
    public void SpawnBlockParticles(Vector3Int pos, byte blockID) {
        Vector3 spawnPos = new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z + 0.5f);

        if(blockBreakParticlePrefabs[blockID] != null) {
            GameObject fx = Instantiate(blockBreakParticlePrefabs[blockID], spawnPos, Quaternion.identity);

            fx.GetComponent<ParticleSystem>().Play();
        } else {
            Debug.Log("block break has no particles");
        }
    }
}
