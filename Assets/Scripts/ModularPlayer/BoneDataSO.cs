using UnityEngine;
using Utils.Bill.InspectorCustom;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;

[CreateAssetMenu(fileName = "BoneData", menuName = "ModularAsset/BoneData", order = 1)]
public class BoneDataSO : ScriptableObject
{
    [System.Serializable]
    public class BoneInfo
    {
        public Transform bone;
        public enum BoneType
        {
            Hips, Spine1, Spine2,
            LeftUpLeg, LeftFoot, RightUpLeg, RightFoot,
            LeftShoulder, LeftArm, LeftForeArm, LeftHand,
            RightShoulder, RightArm, RightForeArm, RightHand,
            Head, Neck,
            Other
        }
        public BoneType type;
        public BoneInfo[] children;
    }

    [System.Serializable]
    public class SkinMeshData
    {
        public SkinnedMeshRenderer renderer;
        public string rootBonePath;
        public Mesh mesh;
        public string id;
    }

    [Tooltip("GameObject cha chứa toàn bộ character")]
    public GameObject parentCharacter;

    [Tooltip("Tên nhánh chứa body (ví dụ: 'Body')")]
    public string bodyBranchName = "Body";

    [Tooltip("Tên nhánh chứa bone (ví dụ: 'Bone')")]
    public string boneBranchName = "Bone";

    [Tooltip("Tên nhánh gốc của xương (ví dụ: 'QuickRigCharacter2_Reference' hoặc 'References')")]
    public string boneReferenceName = "QuickRigCharacter2_Reference";

    [Tooltip("Tên nhánh chứa parts (ví dụ: 'Parts')")]
    public string partsBranchName = "Parts";

    [Tooltip("Tên con của body (ví dụ: 'Body4')")]
    public string bodySubName = "Body4";

    public BoneInfo[] boneHierarchy;
    public SkinMeshData bodyData;
    public BoneInfo hipsBone;
    public System.Collections.Generic.List<SkinMeshData> partCategories;

    [CustomButton]
    public void PopulateData()
    {
        if (parentCharacter != null)
        {
            Transform characterTransform = parentCharacter.transform;
            boneHierarchy = null;
            bodyData = null;
            hipsBone = null;
            partCategories = new System.Collections.Generic.List<SkinMeshData>();

            foreach (Transform child in characterTransform)
            {
                if (child.name.ToLower() == bodyBranchName.ToLower())
                {
                    Transform bodySub = child.Find(bodySubName);
                    if (bodySub != null)
                    {
                        SkinnedMeshRenderer bodyRenderer = bodySub.GetComponent<SkinnedMeshRenderer>();
                        if (bodyRenderer != null)
                        {
                            bodyData = new SkinMeshData
                            {
                                renderer = bodyRenderer,
                                rootBonePath = bodyRenderer.rootBone != null ? GetTransformPath(bodyRenderer.rootBone) : null,
                                mesh = bodyRenderer.sharedMesh,
                                id = $"{bodySubName}"
                            };
                            Debug.Log($"Body - RootBone: {bodyRenderer.rootBone?.name}, RootBonePath: {bodyData.rootBonePath}, Bones: {string.Join(", ", bodyRenderer.bones?.Select(b => b?.name) ?? new string[] { "None" })}");
                        }
                    }
                }
                else if (child.name.ToLower() == boneBranchName.ToLower())
                {
                    Transform references = child.Find(boneReferenceName);
                    if (references == null && boneReferenceName.ToLower() != "quickrigcharacter2_reference")
                    {
                        references = child.Find("References");
                    }
                    if (references != null)
                    {
                        Debug.Log($"Tìm thấy {boneReferenceName}: {references.name}");
                        boneHierarchy = BuildBoneHierarchy(references);
                    }
                    else
                    {
                        Debug.LogWarning($"Không tìm thấy {boneReferenceName} dưới {child.name}");
                    }
                }
                else if (child.name.ToLower() == partsBranchName.ToLower())
                {
                    foreach (Transform category in child)
                    {
                        int meshIndex = 0;
                        foreach (SkinnedMeshRenderer renderer in category.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var data = new SkinMeshData
                            {
                                renderer = renderer,
                                rootBonePath = renderer.rootBone != null ? GetTransformPath(renderer.rootBone) : null,
                                mesh = renderer.sharedMesh,
                                id = $"{renderer.sharedMesh.name}{meshIndex++}"
                            };
                            partCategories.Add(data);
                            Debug.Log($"Part {data.id} - RootBone: {renderer.rootBone?.name}, RootBonePath: {data.rootBonePath}, Bones: {string.Join(", ", renderer.bones?.Select(b => b?.name) ?? new string[] { "None" })}");
                        }
                    }
                }
            }
        }
    }

    [CustomButton]
    public void ClearAll()
    {
        boneHierarchy = null;
        bodyData = null;
        hipsBone = null;
        if (partCategories != null) partCategories.Clear();
    }

    [CustomButton]
    public Transform GetRootBoneTransform(SkinMeshData data)
    {
        if (data == null || string.IsNullOrEmpty(data.rootBonePath) || parentCharacter == null) return null;
        return parentCharacter.transform.Find(data.rootBonePath);
    }

    [CustomButton]
    public void PrintToJson()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        // FIXED: Wrapped ternary operator in parentheses
        sb.AppendLine($"  \"ParentCharacter\": \"{(parentCharacter != null ? parentCharacter.name : "None")}\",");
        sb.AppendLine($"  \"BodyBranchName\": \"{bodyBranchName}\",");
        sb.AppendLine($"  \"BoneBranchName\": \"{boneBranchName}\",");
        sb.AppendLine($"  \"BoneReferenceName\": \"{boneReferenceName}\",");
        sb.AppendLine($"  \"PartsBranchName\": \"{partsBranchName}\",");
        sb.AppendLine($"  \"BodySubName\": \"{bodySubName}\",");

        // BoneHierarchy
        sb.AppendLine("  \"BoneHierarchy\": [");
        if (boneHierarchy != null)
        {
            for (int i = 0; i < boneHierarchy.Length; i++)
            {
                var b = boneHierarchy[i];
                sb.AppendLine($"    {{");
                // FIXED: Wrapped ternary operator in parentheses
                sb.AppendLine($"      \"BoneName\": \"{(b.bone != null ? b.bone.name : "None")}\",");
                sb.AppendLine($"      \"Type\": \"{b.type}\",");
                sb.AppendLine($"      \"Children\": [");
                if (b.children != null)
                {
                    for (int j = 0; j < b.children.Length; j++)
                    {
                        var c = b.children[j];
                        // FIXED: Wrapped ternary operators in parentheses
                        sb.AppendLine($"        {{ \"BoneName\": \"{(c.bone != null ? c.bone.name : "None")}\", \"Type\": \"{c.type}\" }}{(j < b.children.Length - 1 ? "," : "")}");
                    }
                }
                sb.AppendLine("        ]");
                // FIXED: Wrapped ternary operator in parentheses
                sb.AppendLine($"    }}{(i < boneHierarchy.Length - 1 ? "," : "")}");
            }
        }
        sb.AppendLine("  ],");

        // BodyData
        sb.AppendLine("  \"BodyData\": {");
        // FIXED & SIMPLIFIED: Used ?. and ?? operators which are cleaner and avoid the issue
        sb.AppendLine($"    \"Id\": \"{(bodyData?.id ?? "None")}\",");
        sb.AppendLine($"    \"RootBonePath\": \"{(bodyData?.rootBonePath ?? "None")}\",");
        sb.AppendLine($"    \"MeshName\": \"{(bodyData?.mesh?.name ?? "None")}\"");
        sb.AppendLine("  },");

        // HipsBone
        sb.AppendLine("  \"HipsBone\": {");
        // FIXED: Used ?. operator and wrapped the other ternary
        sb.AppendLine($"    \"BoneName\": \"{(hipsBone?.bone?.name ?? "None")}\",");
        sb.AppendLine($"    \"Type\": \"{(hipsBone != null ? hipsBone.type.ToString() : "None")}\"");
        sb.AppendLine("  },");

        // PartCategories
        sb.AppendLine("  \"PartCategories\": [");
        if (partCategories != null)
        {
            for (int i = 0; i < partCategories.Count; i++)
            {
                var p = partCategories[i];
                sb.AppendLine($"    {{");
                // FIXED & SIMPLIFIED: Corrected 'None' to "None" and used ?. and ?? operators
                sb.AppendLine($"      \"Id\": \"{(p?.id ?? "None")}\",");
                sb.AppendLine($"      \"RootBonePath\": \"{(p?.rootBonePath ?? "None")}\",");
                sb.AppendLine($"      \"MeshName\": \"{(p?.mesh?.name ?? "None")}\"");
                // FIXED: Wrapped ternary operator in parentheses
                sb.AppendLine($"    }}{(i < partCategories.Count - 1 ? "," : "")}");
            }
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");

        string json = sb.ToString();
        Debug.Log("BoneDataSO JSON:\n" + json);

#if UNITY_EDITOR
        string path = EditorUtility.SaveFilePanel("Save BoneDataSO JSON", "Assets", "BoneData_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"JSON saved to: {path}");
        }
#endif
    }

    private string GetTransformPath(Transform transform)
    {
        if (transform == null) return null;
        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private BoneInfo[] BuildBoneHierarchy(Transform parent)
    {
        var bones = new System.Collections.Generic.List<BoneInfo>();
        foreach (Transform child in parent)
        {
            var info = new BoneInfo
            {
                bone = child,
                type = AssignBoneType(child.name),
                children = child.childCount > 0 ? BuildBoneHierarchy(child) : null
            };
            bones.Add(info);
            if (child.name.ToLower().Contains("hips")) hipsBone = info;
        }
        return bones.ToArray();
    }

    private BoneInfo.BoneType AssignBoneType(string name)
    {
        name = name.ToLower();
        if (name.Contains("hips")) return BoneInfo.BoneType.Hips;
        if (name.Contains("spine1")) return BoneInfo.BoneType.Spine1;
        if (name.Contains("spine2")) return BoneInfo.BoneType.Spine2;
        if (name.Contains("leftupleg")) return BoneInfo.BoneType.LeftUpLeg;
        if (name.Contains("leftfoot")) return BoneInfo.BoneType.LeftFoot;
        if (name.Contains("rightupleg")) return BoneInfo.BoneType.RightUpLeg;
        if (name.Contains("rightfoot")) return BoneInfo.BoneType.RightFoot;
        if (name.Contains("leftshoulder")) return BoneInfo.BoneType.LeftShoulder;
        if (name.Contains("leftarm")) return BoneInfo.BoneType.LeftArm;
        if (name.Contains("leftforearm")) return BoneInfo.BoneType.LeftForeArm;
        if (name.Contains("lefthand")) return BoneInfo.BoneType.LeftHand;
        if (name.Contains("rightshoulder")) return BoneInfo.BoneType.RightShoulder;
        if (name.Contains("rightarm")) return BoneInfo.BoneType.RightArm;
        if (name.Contains("rightforearm")) return BoneInfo.BoneType.RightForeArm;
        if (name.Contains("righthand")) return BoneInfo.BoneType.RightHand;
        if (name.Contains("head")) return BoneInfo.BoneType.Head;
        if (name.Contains("neck")) return BoneInfo.BoneType.Neck;
        return BoneInfo.BoneType.Other;
    }
}

[System.Serializable]
public class BoneInfo
{
    public Transform bone;
    public enum BoneType
    {
        Hips, Spine1, Spine2,
        LeftUpLeg, LeftFoot, RightUpLeg, RightFoot,
        LeftShoulder, LeftArm, LeftForeArm, LeftHand,
        RightShoulder, RightArm, RightForeArm, RightHand,
        Head, Neck,
        Other
    }
    public BoneType type;
    public BoneInfo[] children;
}

[System.Serializable]
public class SkinMeshData
{
    public SkinnedMeshRenderer renderer;
    public string rootBonePath;
    public Mesh mesh;
    public string id;
}