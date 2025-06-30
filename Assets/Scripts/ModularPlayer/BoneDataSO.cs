using UnityEngine;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using Utils.Bill.InspectorCustom;
[CreateAssetMenu(fileName = "BoneData", menuName = "ModularAsset/BoneData", order = 1)]
public class BoneDataSO : ScriptableObject
{
    // --- CÁC LỚP DỮ LIỆU ---

    [System.Serializable]
    public class BoneInfo
    {
        public string boneName;
        public string bonePath; // Lưu đường dẫn tương đối để có thể tìm lại
        public BoneType type;
        public BoneInfo[] children;
    }

    [System.Serializable]
    public class SkinMeshData
    {
        public string id;
        [ShowAssetPreview(80, 80, PreviewAlignment.Left)]
        public Mesh mesh;
        public string rootBonePath;
        // QUAN TRỌNG: Lưu đường dẫn của tất cả các xương ảnh hưởng đến mesh này
        public string[] bonePaths;
        // QUAN TRỌNG: Lưu đường dẫn đến các file material trong project
        public string[] materialPaths;
    }

    [System.Serializable]
    public class PartCategory
    {
        public string categoryName;
        public List<SkinMeshData> parts = new List<SkinMeshData>();
    }

    public enum BoneType
    {
        Hips, Spine1, Spine2,
        LeftUpLeg, LeftFoot, RightUpLeg, RightFoot,
        LeftShoulder, LeftArm, LeftForeArm, LeftHand,
        RightShoulder, RightArm, RightForeArm, RightHand,
        Head, Neck,
        Other
    }

    // --- CÁC TRƯỜNG DỮ LIỆU CỦA SCRIPTABLEOBJECT ---

    [Header("Cấu hình Hierarchy")]
    [Tooltip("GameObject cha chứa toàn bộ character")]
    [ShowAssetPreview(100, 100)]
    public GameObject parentCharacter;

    [Tooltip("Tên nhánh chứa body (ví dụ: 'Body')")]
    public string bodyBranchName = "Body";
    [Tooltip("Tên con của body (ví dụ: 'Body4')")]
    public string bodySubName = "Body4";

    [Tooltip("Tên nhánh chứa bộ xương (ví dụ: 'Bone')")]
    public string boneBranchName = "Bone";
    [Tooltip("Tên node gốc của bộ xương (ví dụ: 'QuickRigCharacter2_Reference' hoặc 'Hips')")]
    public string boneRootName = "Hips";

    [Tooltip("Tên nhánh chứa các bộ phận trang phục (ví dụ: 'Parts')")]
    public string partsBranchName = "Parts";

    [Header("Dữ liệu đã trích xuất")]
    [Tooltip("Cấu trúc phân cấp của bộ xương")]
    public BoneInfo boneHierarchy;

    [Tooltip("Thông tin mesh của Body")]
    public SkinMeshData bodyData;

    [Tooltip("Danh sách các bộ phận được phân loại")]
    public List<PartCategory> partCategories = new List<PartCategory>();

    // --- CÁC PHƯƠNG THỨC ---

    [Utils.Bill.InspectorCustom.CustomButton]
    public void PopulateData()
    {
        if (parentCharacter == null)
        {
            Debug.LogError("Vui lòng gán 'Parent Character' trước khi Populate Data!");
            return;
        }

        ClearAll();

        Transform characterTransform = parentCharacter.transform;

        // 1. Tìm và xử lý bộ xương (Bone)
        Transform boneRootParent = characterTransform.Find(boneBranchName);
        if (boneRootParent == null)
        {
            Debug.LogWarning($"Không tìm thấy nhánh xương '{boneBranchName}'.");
            return;
        }

        Transform boneRoot = FindDeepChild(boneRootParent, boneRootName);
        if (boneRoot != null)
        {
            Debug.Log($"Tìm thấy gốc xương '{boneRootName}' tại đường dẫn: {GetRelativeTransformPath(boneRoot, characterTransform)}");
            boneHierarchy = BuildBoneHierarchy(boneRoot, characterTransform);
        }
        else
        {
            Debug.LogError($"Không thể tìm thấy gốc xương '{boneRootName}' bên trong '{boneBranchName}'. Vui lòng kiểm tra lại cấu hình.");
            return;
        }

        // 2. Tìm và xử lý Body
        Transform bodyBranch = characterTransform.Find(bodyBranchName);
        if (bodyBranch != null)
        {
            Transform bodySub = bodyBranch.Find(bodySubName);
            if (bodySub != null)
            {
                SkinnedMeshRenderer bodyRenderer = bodySub.GetComponent<SkinnedMeshRenderer>();
                if (bodyRenderer != null)
                {
                    bodyData = CreateSkinMeshData(bodyRenderer, $"{bodySubName}", characterTransform);
                    Debug.Log($"Đã xử lý Body: {bodyData.id}. Xương: {bodyData.bonePaths.Length}, Materials: {bodyData.materialPaths.Length}");
                }
            }
        }

        // 3. Tìm và xử lý các bộ phận (Parts) theo danh mục
        Transform partsBranch = characterTransform.Find(partsBranchName);
        if (partsBranch != null)
        {
            foreach (Transform categoryTransform in partsBranch)
            {
                PartCategory newCategory = new PartCategory { categoryName = categoryTransform.name };
                foreach (SkinnedMeshRenderer renderer in categoryTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    string partId = $"{categoryTransform.name}_{renderer.gameObject.name}";
                    SkinMeshData partData = CreateSkinMeshData(renderer, partId, characterTransform);
                    newCategory.parts.Add(partData);
                }

                if (newCategory.parts.Count > 0)
                {
                    partCategories.Add(newCategory);
                    Debug.Log($"Đã xử lý danh mục '{newCategory.categoryName}' với {newCategory.parts.Count} bộ phận.");
                }
            }
        }

        Debug.Log("PopulateData hoàn tất!");
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [Utils.Bill.InspectorCustom.CustomButton]
    public void ClearAll()
    {
        boneHierarchy = null;
        bodyData = null;
        partCategories.Clear();
        Debug.Log("Đã xóa tất cả dữ liệu.");
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // --- CÁC HÀM HỖ TRỢ ---

    private SkinMeshData CreateSkinMeshData(SkinnedMeshRenderer renderer, string id, Transform root)
    {
        if (renderer == null) return null;

        string[] bonePaths = renderer.bones
            .Select(b => GetRelativeTransformPath(b, root))
            .ToArray();

        string[] materialPaths = new string[0];
#if UNITY_EDITOR
        materialPaths = renderer.sharedMaterials
            .Select(mat => mat != null ? AssetDatabase.GetAssetPath(mat) : null)
            .ToArray();
#endif

        return new SkinMeshData
        {
            id = id,
            mesh = renderer.sharedMesh,
            rootBonePath = GetRelativeTransformPath(renderer.rootBone, root),
            bonePaths = bonePaths,
            materialPaths = materialPaths
        };
    }

    private BoneInfo BuildBoneHierarchy(Transform currentBone, Transform characterRoot)
    {
        if (currentBone == null) return null;

        var info = new BoneInfo
        {
            boneName = currentBone.name,
            bonePath = GetRelativeTransformPath(currentBone, characterRoot),
            type = AssignBoneType(currentBone.name),
            children = new BoneInfo[currentBone.childCount]
        };

        for (int i = 0; i < currentBone.childCount; i++)
        {
            info.children[i] = BuildBoneHierarchy(currentBone.GetChild(i), characterRoot);
        }

        return info;
    }

    private string GetRelativeTransformPath(Transform target, Transform root)
    {
        if (target == null || root == null) return null;
        if (target == root) return "";

        var pathParts = new List<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            pathParts.Add(current.name);
            current = current.parent;
        }

        if (current == null) return target.name;

        pathParts.Reverse();
        return string.Join("/", pathParts);
    }

    public static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private BoneType AssignBoneType(string name)
    {
        name = name.ToLower();
        if (name.Contains("hips")) return BoneType.Hips;
        if (name.Contains("spine1")) return BoneType.Spine1;
        if (name.Contains("spine2")) return BoneType.Spine2;
        if (name.Contains("leftupleg")) return BoneType.LeftUpLeg;
        if (name.Contains("leftfoot")) return BoneType.LeftFoot;
        if (name.Contains("rightupleg")) return BoneType.RightUpLeg;
        if (name.Contains("rightfoot")) return BoneType.RightFoot;
        if (name.Contains("leftshoulder")) return BoneType.LeftShoulder;
        if (name.Contains("leftarm")) return BoneType.LeftArm;
        if (name.Contains("leftforearm")) return BoneType.LeftForeArm;
        if (name.Contains("lefthand")) return BoneType.LeftHand;
        if (name.Contains("rightshoulder")) return BoneType.RightShoulder;
        if (name.Contains("rightarm")) return BoneType.RightArm;
        if (name.Contains("rightforearm")) return BoneType.RightForeArm;
        if (name.Contains("righthand")) return BoneType.RightHand;
        if (name.Contains("head")) return BoneType.Head;
        if (name.Contains("neck")) return BoneType.Neck;
        return BoneType.Other;
    }
}