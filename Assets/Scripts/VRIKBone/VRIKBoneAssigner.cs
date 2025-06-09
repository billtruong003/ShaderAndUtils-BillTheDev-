using UnityEngine;
using RootMotion.FinalIK;
using Sirenix.OdinInspector;

public class VRIKBoneAssigner : MonoBehaviour
{
    public VRIK vrik; // Tham chiếu đến VRIK component
    public VRIKBoneNames boneNamesSO; // Tham chiếu đến ScriptableObject (tùy chọn)

    [Button("Assign Bones")]
    public void AssignBones()
    {
        // Kiểm tra xem VRIK đã được gán chưa
        if (vrik == null)
        {
            Debug.LogError("VRIK reference is not set.");
            return;
        }

        Transform rootTransform = vrik.transform; // Root của VRIK, thường là transform có component VRIK

        // Gán root
        vrik.references.root = rootTransform;

        // Sử dụng tên từ ScriptableObject nếu có, nếu không thì dùng tên từ debugger
        string pelvisName = boneNamesSO != null ? boneNamesSO.pelvis : "Root";
        string spineName = boneNamesSO != null ? boneNamesSO.spine : "J_Bip_C_Spine";
        string chestName = boneNamesSO != null ? boneNamesSO.chest : "J_Bip_C_Chest";
        string neckName = boneNamesSO != null ? boneNamesSO.neck : "J_Bip_C_Neck";
        string headName = boneNamesSO != null ? boneNamesSO.head : "J_Bip_C_Head";
        string leftShoulderName = boneNamesSO != null ? boneNamesSO.leftShoulder : "J_Bip_L_Shoulder";
        string leftUpperArmName = boneNamesSO != null ? boneNamesSO.leftUpperArm : "J_Bip_L_UpperArm";
        string leftForearmName = boneNamesSO != null ? boneNamesSO.leftForearm : "J_Bip_L_LowerArm";
        string leftHandName = boneNamesSO != null ? boneNamesSO.leftHand : "J_Bip_L_Hand";
        string rightShoulderName = boneNamesSO != null ? boneNamesSO.rightShoulder : "J_Bip_R_Shoulder";
        string rightUpperArmName = boneNamesSO != null ? boneNamesSO.rightUpperArm : "J_Bip_R_UpperArm";
        string rightForearmName = boneNamesSO != null ? boneNamesSO.rightForearm : "J_Bip_R_LowerArm";
        string rightHandName = boneNamesSO != null ? boneNamesSO.rightHand : "J_Bip_R_Hand";
        string leftThighName = boneNamesSO != null ? boneNamesSO.leftThigh : "J_Bip_L_UpperLeg";
        string leftCalfName = boneNamesSO != null ? boneNamesSO.leftCalf : "J_Bip_L_LowerLeg";
        string leftFootName = boneNamesSO != null ? boneNamesSO.leftFoot : "J_Bip_L_Foot";
        string leftToesName = boneNamesSO != null ? boneNamesSO.leftToes : "J_Bip_L_ToeBase";
        string rightThighName = boneNamesSO != null ? boneNamesSO.rightThigh : "J_Bip_R_UpperLeg";
        string rightCalfName = boneNamesSO != null ? boneNamesSO.rightCalf : "J_Bip_R_LowerLeg";
        string rightFootName = boneNamesSO != null ? boneNamesSO.rightFoot : "J_Bip_R_Foot";
        string rightToesName = boneNamesSO != null ? boneNamesSO.rightToes : "J_Bip_R_Foot";

        // Gán các transform vào VRIK References
        vrik.references.pelvis = FindTransformByName(rootTransform, pelvisName);
        vrik.references.spine = FindTransformByName(rootTransform, spineName);
        vrik.references.chest = FindTransformByName(rootTransform, chestName);
        vrik.references.neck = FindTransformByName(rootTransform, neckName);
        vrik.references.head = FindTransformByName(rootTransform, headName);
        vrik.references.leftShoulder = FindTransformByName(rootTransform, leftShoulderName);
        vrik.references.leftUpperArm = FindTransformByName(rootTransform, leftUpperArmName);
        vrik.references.leftForearm = FindTransformByName(rootTransform, leftForearmName);
        vrik.references.leftHand = FindTransformByName(rootTransform, leftHandName);
        vrik.references.rightShoulder = FindTransformByName(rootTransform, rightShoulderName);
        vrik.references.rightUpperArm = FindTransformByName(rootTransform, rightUpperArmName);
        vrik.references.rightForearm = FindTransformByName(rootTransform, rightForearmName);
        vrik.references.rightHand = FindTransformByName(rootTransform, rightHandName);
        vrik.references.leftThigh = FindTransformByName(rootTransform, leftThighName);
        vrik.references.leftCalf = FindTransformByName(rootTransform, leftCalfName);
        vrik.references.leftFoot = FindTransformByName(rootTransform, leftFootName);
        vrik.references.leftToes = FindTransformByName(rootTransform, leftToesName);
        vrik.references.rightThigh = FindTransformByName(rootTransform, rightThighName);
        vrik.references.rightCalf = FindTransformByName(rootTransform, rightCalfName);
        vrik.references.rightFoot = FindTransformByName(rootTransform, rightFootName);
        vrik.references.rightToes = FindTransformByName(rootTransform, rightToesName);

        // Kiểm tra xem tất cả các transform bắt buộc đã được gán chưa
        if (!vrik.references.isFilled)
        {
            Debug.LogWarning("Some required bones are missing. Please check the bone names or assign them manually.");
        }
        else
        {
            Debug.Log("All bones assigned successfully!");
        }
    }

    // Hàm tìm transform theo tên trong hierarchy
    private Transform FindTransformByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindTransformByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
}