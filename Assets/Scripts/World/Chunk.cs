using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour
{
    //component references
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private ChunkData chunkData;

    //dynamic sized lists we will use to draw the mesh
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    //UV list, UV coordinate for each vertex
    private List<Vector2> uvs = new List<Vector2>();

    //vertex count
    private int vertexIndex = 0;

    private void Start() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        //init new chunk data, with dummy contrsusctor for testing
        chunkData = new ChunkData();

        GenerateMesh();
    }

    //generate the mesh from chunk data
    private void GenerateMesh() {
        for (int x = 0; x < VoxelData.ChunkWidth; x++) {
            for (int y = 0; y < VoxelData.ChunkHeight; y++) {
                for (int z = 0; z < VoxelData.ChunkDepth; z++) {
                    //what block it is at current coordinates
                    byte blockID = chunkData.GetVoxel(x, y, z);

                    //cycle through whole chunk, if the current block is NOT air, check if its faces need to be drawn
                    if (blockID != (byte)BlockType.Air) {
                        //send info to function to draw that block at that coordinates
                        UpdateMeshData(new Vector3Int(x, y, z), blockID);
                    }
                }
            }
        }

        RenderMesh();
    }

    //add to mesh required/to be drawn faces for this block
    private void UpdateMeshData(Vector3Int pos, byte blockID) {
        //check each direction to see if its air next to it
        for (int i = 0; i < 6; i++) {
            //add offset from voxelData to block coords to check whats next to it
            if (CheckAir(pos + VoxelData.faceChecks[i])) {

                //if check air test passes it means we need to draw this face

                //add vertices of default cube added to chunk coordonate in space
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[i, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[i, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[i, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[i, 3]]);

                //UV texturing ,also send face index, for expansion capabilities for different block textures based on face orientation
                AddTexture(blockID, i);

                //add to list of points to draw triangles (to draw a square, we draw 2 triangles)

                //unity will take the triangle info from here and effective points from vertices
                //in triangles array from 3 in 3 will draw a triangle USING the element at triangles[value] index from vertices as a point.
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);

                //secodn triangle, both combined will form the square to be drawn
                triangles.Add(vertexIndex + 0);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);

                //move index 4 pozitions forwatd cause we used 4 vertices
                vertexIndex += 4;
            }
        }
    }

    //helper function to check if block from vector3 is air , with out of bounds protection
    private bool CheckAir(Vector3Int pos) {
        byte blockID = chunkData.GetVoxel(pos.x, pos.y, pos.z);
        if (blockID == (byte)BlockType.Air) return true;

        return false;
    }

    //
    private void AddTexture(byte blockID, int faceIndex) {
        //get texture position from texture atlas
        Vector2 texturePos = GetTexturePosition(blockID, faceIndex);

        //texture atlas coordinates simplified
        float x = texturePos.x;
        float y = texturePos.y;

        //add UV points
        uvs.Add(new Vector2(x, y) * VoxelData.NormalizedBlockTextureSize);
        uvs.Add(new Vector2(x + 1, y) * VoxelData.NormalizedBlockTextureSize);
        uvs.Add(new Vector2(x + 1, y + 1) * VoxelData.NormalizedBlockTextureSize);
        uvs.Add(new Vector2(x, y + 1) * VoxelData.NormalizedBlockTextureSize);
    }

    //get texture position from atlas of blocks, we are using UV coordinates so its starts from bottom left and in form of (u,v) aka (x,y)
    private Vector2 GetTexturePosition(byte blockID, int faceIndex) {
        if (blockID == (byte)BlockType.Stone) {
            return new Vector2(3, 4);
        }else if (blockID == (byte)BlockType.Dirt) {
            return new Vector2(7, 3);
        }

        //default value - light gray log (to know if it ever reaches this edge case for debugging)
        return new Vector2(0, 0);
    }

    //render into unity
    private void RenderMesh() {
        Mesh mesh = new Mesh();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0); // submesh 0, aka default

        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals(); // for shadows and lights to shine correctly

        meshFilter.mesh = mesh;
    }
}
