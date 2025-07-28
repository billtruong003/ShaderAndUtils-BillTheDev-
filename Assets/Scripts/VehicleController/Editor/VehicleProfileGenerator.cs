// VehicleProfileGenerator.cs
// Đặt file này trong một thư mục tên "Editor".

using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

public class VehicleProfileGenerator : OdinEditorWindow
{
    public enum VehicleArchetype
    {
        Balanced,
        ArcadeRacer,
        DriftKing,
        RealisticGT
    }

    [Title("GENERATOR SETTINGS")]
    [EnumToggleButtons()]
    [SerializeField] private VehicleArchetype archetypeToGenerate;

    [Required("Phải đặt tên cho file profile!")]
    [SerializeField] private string profileName;

    [FolderPath(ParentFolder = "Assets")]
    [SerializeField] private string savePath = "Assets/VehicleProfiles";

    [MenuItem("Tools/Vehicle System/Vehicle Profile Generator")]
    private static void OpenWindow()
    {
        GetWindow<VehicleProfileGenerator>().Show();
    }

    [Button(ButtonSizes.Large), GUIColor(0.2f, 1f, 0.2f)]
    private void GenerateVehicleProfile()
    {
        if (string.IsNullOrEmpty(profileName)) return;

        VehicleProfileSO profile = CreateInstance<VehicleProfileSO>();
        profile.vehicleName = profileName;

        switch (archetypeToGenerate)
        {
            case VehicleArchetype.Balanced:
                // Các giá trị mặc định đã là Balanced
                break;
            case VehicleArchetype.ArcadeRacer:
                profile.maxEnginePower = 55000f;
                profile.baseLateralGrip = 20f;
                profile.driftGripMultiplier = 0.6f;
                profile.turboForce = 40000f;
                profile.downforceCoefficient = 70f;
                profile.turnTorque = 60000f;
                break;
            case VehicleArchetype.DriftKing:
                profile.maxEnginePower = 65000f;
                profile.baseLateralGrip = 9f;
                profile.driftGripMultiplier = 0.25f;
                profile.turboForce = 30000f;
                profile.downforceCoefficient = 30f;
                profile.turnTorque = 45000f;
                profile.driftInitiationBoost = 50000f;
                break;
            case VehicleArchetype.RealisticGT:
                profile.maxEnginePower = 40000f;
                profile.baseLateralGrip = 15f;
                profile.driftGripMultiplier = 0.75f;
                profile.turboForce = 20000f;
                profile.downforceCoefficient = 100f;
                profile.turnTorque = 35000f;
                break;
        }

        if (!AssetDatabase.IsValidFolder(savePath))
        {
            AssetDatabase.CreateFolder("Assets", "VehicleProfiles");
        }

        string fullPath = $"{savePath}/{profileName}.asset";
        AssetDatabase.CreateAsset(profile, fullPath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = profile;
    }
}