using System.IO;
using UnityEngine;

public class ObjectDataWriter
{
    BinaryWriter writer;

    public ObjectDataWriter(BinaryWriter writer)
    {
        this.writer = writer;
    }

    public void WriteVector3(Vector3 value)
    {
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
    }

    public void WriteQuaternion(Quaternion value)
    {
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
        writer.Write(value.w);
    }

    public void WriteString(string value)
    {
        writer.Write(value);
    }

    public void WriteFloat(float value)
    {
        writer.Write(value);
    }

    public void WriteInt(int value)
    {
        writer.Write(value);
    }

    public void WriteBool(bool value)
    {
        writer.Write(value);
    }

    public void WriteColor(Color value)
    {
        writer.Write(value.r);
        writer.Write(value.g);
        writer.Write(value.b);
        writer.Write(value.a);
    }
}

public class ObjectDataReader
{
    BinaryReader reader;

    public ObjectDataReader(BinaryReader reader)
    {
        this.reader = reader;
    }

    public Vector3 ReadVector3()
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public Quaternion ReadQuaternion()
    {
        return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public string ReadString()
    {
        return reader.ReadString();
    }

    public float ReadFloat()
    {
        return reader.ReadSingle();
    }

    public int ReadInt()
    {
        return reader.ReadInt32();
    }

    public bool ReadBool()
    {
        return reader.ReadBoolean();
    }

    public Color ReadColor()
    {
        return new Color(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle()
        );
    }
}
