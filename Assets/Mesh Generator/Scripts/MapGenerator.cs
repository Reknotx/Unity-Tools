using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, Mesh, FalloffMap }
    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;
    
    [Range(0,6)]
    public int editorPreviewLOD;

    public bool autoUpdate;
    

    private float[,] falloffMap;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
    
    //Changed mapChunkSize to 239 from 241 as we need to add 2 to it for the normals
    //when we generate the map data
    public int mapChunkSize => terrainData.useFlatShading ? 95 : 239;

    void OnValuesUpdated()
    {
        if (!Application.isPlaying) DrawMapInEditor();
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }
    
    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap,
                    terrainData.meshHeightMultiplier,
                    terrainData.meshHeightCurve,
                    editorPreviewLOD, 
                    terrainData.useFlatShading));
        else if (drawMode == DrawMode.FalloffMap)
            display.DrawTexture(
                TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));

    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(center, callback); };
        
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue) mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, lod, callback); };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap,
                                                              terrainData.meshHeightMultiplier,
                                                              terrainData.meshHeightCurve,
                                                              lod, 
                                                              terrainData.useFlatShading);
        lock (meshDataThreadInfoQueue) meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }


    private MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2,
                                                   mapChunkSize + 2,
                                                   noiseData.seed,
                                                   noiseData.noiseScale,
                                                   noiseData.octaves,
                                                   noiseData.persistence,
                                                   noiseData.lacunarity,
                                                   center + noiseData.offset,
                                                   noiseData.normalizeMode);

        if (terrainData.useFalloff)
        {
            if (falloffMap == null) falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            
            for (int y = 0; y < mapChunkSize + 2; y++)
            {
                for (int x = 0; x < mapChunkSize + 2; x++)
                {
                    if (terrainData.useFalloff) noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
            }    
        }
        
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        
        return new MapData(noiseMap);
    }
 
    private void OnValidate()
    {
        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public struct MapData
{
    public float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}