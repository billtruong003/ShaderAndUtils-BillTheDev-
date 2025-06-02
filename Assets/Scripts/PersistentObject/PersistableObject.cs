using UnityEngine;

public class PersistableObject : MonoBehaviour
{
    public virtual void Save(ObjectDataWriter writer)
    {
        writer.WriteString(gameObject.name);
        writer.WriteVector3(transform.localPosition);
        writer.WriteVector3(transform.localEulerAngles);
        writer.WriteVector3(transform.localScale);
    }

    public virtual void Load(ObjectDataReader reader)
    {
        string name = reader.ReadString();
        transform.localPosition = reader.ReadVector3();
        transform.localEulerAngles = reader.ReadVector3();
        transform.localScale = reader.ReadVector3();
    }
}