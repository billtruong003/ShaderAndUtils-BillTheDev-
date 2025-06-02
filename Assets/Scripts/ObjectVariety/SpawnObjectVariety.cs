using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnObjectVariety : PersistableObject
{
    [SerializeField] private bool enableScript;
    [Serializable]
    public class Settings
    {
        public KeyCode SpawnKey = KeyCode.Space;
        public KeyCode NewGameKey = KeyCode.N;
        public KeyCode SaveKey = KeyCode.S;
        public KeyCode LoadKey = KeyCode.L;
    }

    [SerializeField] private Settings settings;
    [SerializeField] private ShapeFactory shapeFactory;
    [SerializeField] private List<Shape> spawnedObjects = new List<Shape>();
    [SerializeField] private string savePath;
    [SerializeField] private string[] objectName = { "Cube", "Sphere", "Capsule", "Cylinder" };
    private const int saveVersion = 1;

    private void Awake()
    {
        if (!enableScript)
            return;
        InitSavePath();

    }

    private void Update()
    {
        if (!enableScript)
            return;

        if (Input.GetKeyDown(settings.SpawnKey))
        {
            Spawn();
        }
        else if (Input.GetKeyDown(settings.NewGameKey))
        {
            BeginNewGame();
        }
        else if (Input.GetKeyDown(settings.SaveKey))
        {
            Save(new ObjectDataWriter(null));
            Debug.Log("Game Saved");
        }
        else if (Input.GetKeyDown(settings.LoadKey))
        {
            Load(new ObjectDataReader(null));
            Debug.Log("Game Loaded");
        }
    }

    private void InitSavePath()
    {
        savePath = Path.Combine(Application.persistentDataPath, "saveFile");
    }

    private void Spawn()
    {
        PerformanceProfiler.BeginProfile("SpawnObjectVariety.Spawn");
        if (shapeFactory != null)
        {
            Shape spawnShape = shapeFactory.GetRandom(transform);
            Transform shapeTransform = spawnShape.transform;
            shapeTransform.localPosition = Random.insideUnitSphere * 5f;
            shapeTransform.localRotation = Random.rotation;
            shapeTransform.localScale = Vector3.one * Random.Range(0.1f, 1.5f);
            shapeTransform.gameObject.name = shapeTransform.name + "_" + (spawnedObjects.Count + 1);

            spawnShape.SetColor(Random.ColorHSV(
                hueMin: 0f, hueMax: 1f,
                saturationMin: 0.5f, saturationMax: 1f,
                valueMin: 0.25f, valueMax: 1f,
                alphaMin: 0.7f, alphaMax: 1f
            ));
            int textureIndex = shapeFactory.GetRandomTextureIndex();
            spawnShape.SetTexture(shapeFactory.GetTexture(textureIndex), textureIndex);
            spawnShape.SetMetallic(Random.Range(0f, 1f));
            spawnShape.SetSmoothness(Random.Range(0f, 1f));
            spawnedObjects.Add(spawnShape);
        }
        else
        {
            Debug.LogWarning("Object to spawn is not assigned.");
        }
        PerformanceProfiler.EndProfile("SpawnObjectVariety.Spawn");
    }

    private void BeginNewGame()
    {
        PerformanceProfiler.BeginProfile("SpawnObjectVariety.BeginNewGame");
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i].gameObject);
            }
        }
        spawnedObjects.Clear();
        PerformanceProfiler.EndProfile("SpawnObjectVariety.BeginNewGame");
    }

    public override void Save(ObjectDataWriter writer)
    {
        PerformanceProfiler.BeginProfile("SpawnObjectVariety.Save");
        using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                writer = new ObjectDataWriter(binaryWriter);
                writer.WriteInt(saveVersion);
                writer.WriteInt(spawnedObjects.Count);
                foreach (Shape shape in spawnedObjects)
                {
                    if (shape != null)
                    {
                        writer.WriteInt(shape.ShapeId);
                        writer.WriteInt(shape.MaterialId);
                        writer.WriteInt(shape.TextureIndex);
                        shape.Save(writer);
                    }
                }
            }
        }
        PerformanceProfiler.EndProfile("SpawnObjectVariety.Save");
    }

    public override void Load(ObjectDataReader reader)
    {
        PerformanceProfiler.BeginProfile("SpawnObjectVariety.Load");
        BeginNewGame();
        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"Save file not found at: {savePath}");
            PerformanceProfiler.EndProfile("SpawnObjectVariety.Load");
            return;
        }
        using (FileStream fileStream = new FileStream(savePath, FileMode.Open, FileAccess.Read))
        {
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                reader = new ObjectDataReader(binaryReader);
                int version = reader.ReadInt();
                int count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    int shapeId = reader.ReadInt();
                    int materialId = reader.ReadInt();
                    int textureIndex = reader.ReadInt();
                    Shape shape = shapeFactory.Get(shapeId, transform, materialId, textureIndex);
                    shape.Load(reader);
                    spawnedObjects.Add(shape);
                }
            }
        }
        PerformanceProfiler.EndProfile("SpawnObjectVariety.Load");
    }
}
