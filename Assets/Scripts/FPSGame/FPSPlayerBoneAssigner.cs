// Designed by KINEMATION, 2025.
// Bone Assigner Tool modified to work with the provided FPSPlayer script.

// Thêm các thư viện cần thiết
using UnityEngine;
using Sirenix.OdinInspector;
using System.Text;
using System.Reflection; // Quan trọng: Thêm thư viện Reflection
using KINEMATION.FPSAnimationPack.Scripts.Player; // Namespace của FPSPlayer
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(FPSPlayer), typeof(Animator))]
public class FPSPlayerBoneAssigner : MonoBehaviour
{
    private Animator _animator;
    private FPSPlayer _fpsPlayer;

    [Title("Tự động Gán xương cho FPS Player")]
    [InfoBox("Công cụ này sẽ tự động gán các xương từ Avatar của Animator vào component FPS Player.\n" +
             "Nó sử dụng cấu trúc xương Humanoid cho các xương tay/vai và tìm kiếm các xương tùy chỉnh (ví dụ: 'ik_hand_gun') theo tên. " +
             "Hãy đảm bảo model của bạn đã được cấu hình là Humanoid.")]
    [Button("Gán xương từ Avatar", ButtonSizes.Large)]
    private void AssignBones()
    {
        _animator = GetComponent<Animator>();
        _fpsPlayer = GetComponent<FPSPlayer>();

        if (_animator.avatar == null || !_animator.avatar.isHuman)
        {
            Debug.LogError("Lỗi: Animator chưa có Avatar hoặc Avatar không phải là Humanoid. Vui lòng kiểm tra lại trong phần cài đặt Rig của model.", this);
            return;
        }

        StringBuilder logBuilder = new StringBuilder("<b>--- Kết quả gán xương ---</b>\n");
        Type fpsPlayerType = typeof(FPSPlayer);

        // --- Gán các Transform cơ bản ---
        // Sử dụng Reflection để set các private field
        SetPrivateField(fpsPlayerType, "skeletonRoot", _animator.transform, "Skeleton Root", logBuilder);
        SetPrivateField(fpsPlayerType, "weaponBone", FindDeepChild(_animator.transform, "ik_hand_gun"), "Weapon Bone (ik_hand_gun)", logBuilder);
        SetPrivateField(fpsPlayerType, "weaponBoneAdditive", FindDeepChild(_animator.transform, "ik_hand_gun_additive"), "Weapon Bone Additive (ik_hand_gun_additive)", logBuilder);

        // Tìm Camera component là con của object này
        Camera playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            SetPrivateField(fpsPlayerType, "cameraPoint", playerCamera.transform, "Camera Point", logBuilder);
        }
        else
        {
            LogResult(logBuilder, "Camera Point", null);
        }

        // --- Gán xương cho IKTransforms (Right Hand) ---
        logBuilder.Append("<b>Right Hand:</b>\n");
        AssignIKTransforms(fpsPlayerType, "rightHand", HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm, logBuilder);

        // --- Gán xương cho IKTransforms (Left Hand) ---
        logBuilder.Append("<b>Left Hand:</b>\n");
        AssignIKTransforms(fpsPlayerType, "leftHand", HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm, logBuilder);

        Debug.Log(logBuilder.ToString(), this);

#if UNITY_EDITOR
        // Đánh dấu object đã thay đổi để lưu lại scene
        EditorUtility.SetDirty(_fpsPlayer);
#endif
        Debug.Log("Gán xương hoàn tất! Vui lòng kiểm tra lại các trường trong Inspector của FPSPlayer.");
    }

    /// <summary>
    /// Sử dụng Reflection để gán giá trị cho một private field kiểu Transform.
    /// </summary>
    private void SetPrivateField(Type targetType, string fieldName, Transform value, string logName, StringBuilder logger)
    {
        FieldInfo field = targetType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(_fpsPlayer, value);
            LogResult(logger, logName, value);
        }
        else
        {
            logger.AppendLine($"<color=orange>⚠</color> Không tìm thấy field '{fieldName}' trong FPSPlayer.cs.");
        }
    }

    /// <summary>
    /// Sử dụng Reflection để gán giá trị cho các xương trong struct IKTransforms.
    /// </summary>
    private void AssignIKTransforms(Type playerType, string fieldName, HumanBodyBones tipBone, HumanBodyBones midBone, HumanBodyBones rootBone, StringBuilder logger)
    {
        FieldInfo ikField = playerType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (ikField == null)
        {
            logger.AppendLine($"<color=orange>⚠</color> Không tìm thấy field '{fieldName}' trong FPSPlayer.cs.");
            return;
        }

        // Vì IKTransforms là struct, ta phải lấy một bản copy, sửa nó, rồi gán lại.
        object ikStruct = ikField.GetValue(_fpsPlayer);
        Type ikType = typeof(IKTransforms);

        // Lấy thông tin các field bên trong struct
        FieldInfo tipField = ikType.GetField("tip");
        FieldInfo midField = ikType.GetField("mid");
        FieldInfo rootField = ikType.GetField("root");

        // Tìm các transform của xương
        Transform tipTransform = _animator.GetBoneTransform(tipBone);
        Transform midTransform = _animator.GetBoneTransform(midBone);
        Transform rootTransform = _animator.GetBoneTransform(rootBone);

        // Gán giá trị vào bản copy của struct
        tipField.SetValue(ikStruct, tipTransform);
        midField.SetValue(ikStruct, midTransform);
        rootField.SetValue(ikStruct, rootTransform);

        LogResult(logger, "  - Tip", tipTransform);
        LogResult(logger, "  - Mid", midTransform);
        LogResult(logger, "  - Root", rootTransform);

        // Gán struct đã được sửa đổi trở lại vào component FPSPlayer
        ikField.SetValue(_fpsPlayer, ikStruct);
    }

    /// <summary>
    /// Hàm trợ giúp để ghi log kết quả
    /// </summary>
    private void LogResult(StringBuilder logger, string boneName, Transform boneTransform)
    {
        if (boneTransform != null)
        {
            logger.AppendLine($"<color=green>✓</color> {boneName}: Đã gán '{boneTransform.name}'.");
        }
        else
        {
            logger.AppendLine($"<color=red>✗</color> {boneName}: Không tìm thấy!");
        }
    }

    /// <summary>
    /// Tìm kiếm đệ quy một Transform con theo tên
    /// </summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}