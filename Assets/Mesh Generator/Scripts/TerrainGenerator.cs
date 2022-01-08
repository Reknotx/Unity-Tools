using System.Collections.Generic;
using UnityEngine;

namespace Mesh_Generator.Scripts
{
    public class TerrainGenerator : MonoBehaviour
    {
    
        private const float moveDistForChunkUpdate = 25f;
        private const float sqrMoveDistForChunkUpdate = moveDistForChunkUpdate * moveDistForChunkUpdate;
    
        public int colliderLODIndex;
        public LODInfo[] detailLevels;
    
        public Transform viewer;
        public Material mapMaterial;

        public MeshSettings meshSettings;
        public HeightMapSettings heightMapSettings;
        public TextureData textureSettings;

        private Vector2 viewerPosition;
        private Vector2 viewerPositionOld;
        private float meshWorldSize;
        private int chunksVisibleInViewDist;

        private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

        List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

        private void Start()
        {
            textureSettings.ApplyToMaterial(mapMaterial);
            textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        
            float maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
            meshWorldSize = meshSettings.MeshWorldSize;
            chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / meshWorldSize);
        
            UpdateVisibleChunks();
        }

        private void Update()
        {
            viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

            if (viewerPosition != viewerPositionOld)
            {
                foreach (TerrainChunk chunk in visibleTerrainChunks)
                {
                    chunk.UpdateCollisionMesh();
                }
            }
        
            if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrMoveDistForChunkUpdate)
            {
                viewerPositionOld = viewerPosition;
                UpdateVisibleChunks();
            }
        }

        void UpdateVisibleChunks()
        {
            HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
            for (var i = visibleTerrainChunks.Count - 1; i >= 0; i--)
            {
                alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
                visibleTerrainChunks[i].UpdateTerrainChunk();
            }

            int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
            int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

            for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++)
                {
                    Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                    if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                    {
                        if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                            terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                        else
                        {
                            TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings,
                                detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                            terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                            newChunk.onVisibilityChanged += OnChunkVisibilityChanged;
                            newChunk.Load();
                        }
                    }
                }
            }
        }

        void OnChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
        {
            if (isVisible) visibleTerrainChunks.Add(chunk);
            else visibleTerrainChunks.Remove(chunk);
        }
    } 

    [System.Serializable]
    public struct LODInfo
    {
        [Range(0, MeshSettings.numSupportedLODs - 1)]
        public int lod;
        public float visibleDistThreshold;

        public float SqrVisibleDistThreshold => visibleDistThreshold * visibleDistThreshold;
    }
}