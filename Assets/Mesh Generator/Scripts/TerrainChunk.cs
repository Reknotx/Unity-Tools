using UnityEngine;

namespace Mesh_Generator.Scripts
{
    //#TODO - example TODO comment
    public class TerrainChunk
    {
        private const float colliderGenerationDistThreshold = 5;

        public event System.Action<TerrainChunk, bool> onVisibilityChanged;
        public Vector2 coord;
        
        private readonly GameObject meshObject;
        private Vector2 sampleCenter;
        private Bounds bounds;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        public bool IsVisible => meshObject.activeSelf;

        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private int colliderLODIndex;

        private HeightMap heightMap;
        private bool heightMapReceived;
        private int previousLODIndex = -1;

        private bool hasSetCollider;

        private float maxViewDist;

        private HeightMapSettings heightMapSettings;
        private MeshSettings meshSettings;
        private Transform viewer;

        private Vector2 viewerPosition => new Vector2(viewer.position.x, viewer.position.z);
        
        public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
        {
            this.coord = coord;
            this.heightMapSettings = heightMapSettings;
            this.meshSettings = meshSettings;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;
            this.viewer = viewer;

            sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.scale;
            Vector2 position = coord * meshSettings.MeshWorldSize;
            bounds = new Bounds(sampleCenter, Vector2.one * meshSettings.MeshWorldSize);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;
            
            meshObject.transform.position = new Vector3(position.x, 0, position.y);
            meshObject.transform.parent = parent;
            
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex) lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }

            maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
            

        }

        public void Load()
        {
            ThreadedDataRequester.RequestData(
                () => HeightMapGenerator.GenerateHeightMap(meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine,
                    heightMapSettings, sampleCenter), OnHeightMapReceived);
        }

        void OnHeightMapReceived(object heightMapObject)
        {
            this.heightMap = (HeightMap) heightMapObject;
            heightMapReceived = true;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (heightMapReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                bool wasVisible = IsVisible;
                bool visible = viewerDstFromNearestEdge <= maxViewDist;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDistThreshold) lodIndex = i + 1;
                        else break;
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh) lodMesh.RequestMesh(heightMap, meshSettings);
                    }
                }

                if (wasVisible != visible)
                {
                    SetVisible(visible);
                    if (onVisibilityChanged != null) onVisibilityChanged(this, visible);
                }
            }
        }

        public void UpdateCollisionMesh()
        {
            if (!hasSetCollider)
            {
                float sqrDistFromViewerToEdge = bounds.SqrDistance(viewerPosition);

                if (sqrDistFromViewerToEdge < detailLevels[colliderLODIndex].SqrVisibleDistThreshold)
                    if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                    {
                        lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                    }

                if (sqrDistFromViewerToEdge < colliderGenerationDistThreshold * colliderGenerationDistThreshold)
                    if (lodMeshes[colliderLODIndex].hasMesh)
                    {
                        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                        hasSetCollider = true;
                    }
            }
        }
        
        public void SetVisible(bool visible) => meshObject.SetActive(visible);
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        void OnMeshDataReceived(object meshDataObject)
        {
            mesh = ((MeshData)meshDataObject).CreateMesh();
            hasMesh = true;
            updateCallback();
        }

        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
        }
    }
}