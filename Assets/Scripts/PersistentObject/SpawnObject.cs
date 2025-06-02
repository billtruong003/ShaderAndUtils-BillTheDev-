using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnObject : PersistableObject
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
    [SerializeField] private GameObject objectToSpawn;
    [SerializeField] private List<PersistableObject> spawnedObjects = new List<PersistableObject>();
    [SerializeField] private string savePath;
    [SerializeField] private string[] objectName = { "Cube", "Sphere", "Capsule", "Cylinder" };

    private void Awake()
    {
        if (!enableScript)
            return;

        string directoryPath = Path.Combine(Application.persistentDataPath, "saveFile");
        savePath = Path.Combine(directoryPath);

        objectToSpawn = Resources.Load<GameObject>("Prefabs/Cube");
        if (objectToSpawn == null)
        {
            Debug.LogError("Failed to load prefab: Prefabs/Cube");
        }
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

    private void Spawn()
    {
        if (objectToSpawn != null)
        {
            Transform spawnObject = Instantiate(objectToSpawn, transform).transform;
            spawnObject.localPosition = Random.insideUnitSphere * 5f;
            spawnObject.localRotation = Random.rotation;
            spawnObject.localScale = Vector3.one * Random.Range(0.1f, 1.5f);
            spawnObject.gameObject.name = objectToSpawn.name + "_" + (spawnedObjects.Count + 1);
            spawnedObjects.Add(spawnObject.GetComponent<PersistableObject>());
        }
        else
        {
            Debug.LogWarning("Object to spawn is not assigned.");
        }
    }

    private void BeginNewGame()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i].gameObject);
            }
        }
        spawnedObjects.Clear();
    }

    public override void Save(ObjectDataWriter writer)
    {
        using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            {
                writer = new ObjectDataWriter(binaryWriter);
                writer.WriteInt(spawnedObjects.Count);
                for (int i = 0; i < spawnedObjects.Count; i++)
                {
                    spawnedObjects[i].Save(writer);
                }
            }
        }
    }

    public override void Load(ObjectDataReader reader)
    {
        if (File.Exists(savePath))
        {
            using (FileStream fileStream = new FileStream(savePath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    reader = new ObjectDataReader(binaryReader);
                    BeginNewGame();
                    int count = reader.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        PersistableObject persistentObject = Instantiate(objectToSpawn.GetComponent<PersistableObject>(), transform);
                        persistentObject.Load(reader);
                        spawnedObjects.Add(persistentObject);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Save file not found at: " + savePath);
        }
    }
}