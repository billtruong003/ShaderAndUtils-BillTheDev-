using UnityEngine;
using System.Collections.Generic;

namespace ZombieAI.VAT
{
    /// <summary>
    /// Quản lý, culling và render tất cả các VAT_Zombie bằng GPU Instancing.
    /// Đây là thành phần cốt lõi để tối ưu hóa hiệu suất.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class VAT_ZombieDirector : MonoBehaviour
    {
        private struct ZombieInstance
        {
            public VAT_Zombie Logic;
            public VAT_Animator_Instanced Animator; // SỬA: Dùng phiên bản Animator mới
            public Renderer Renderer;
        }

        private static VAT_ZombieDirector _instance;
        public static VAT_ZombieDirector Instance
        {
            get
            {
                if (_instance == null)
                    // SỬA LỖI CS0618: Dùng API mới nhất, hiệu quả hơn
                    _instance = FindFirstObjectByType<VAT_ZombieDirector>();
                return _instance;
            }
        }

        private Camera _cullingCamera;
        private readonly List<ZombieInstance> _allActiveZombies = new List<ZombieInstance>(1024);
        private readonly Plane[] _frustumPlanes = new Plane[6];

        private readonly Dictionary<VAT_AnimationData, List<ZombieInstance>> _visibleZombiesByData = new Dictionary<VAT_AnimationData, List<ZombieInstance>>();

        private readonly Matrix4x4[] _instanceMatrices = new Matrix4x4[1023];
        private readonly Vector4[] _instanceAnimationData = new Vector4[1023];
        private MaterialPropertyBlock _propertyBlock;
        private static readonly int AnimationDataID = Shader.PropertyToID("_AnimationData");

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _cullingCamera = GetComponent<Camera>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        public void Register(VAT_Zombie zombie)
        {
            // SỬA: Dùng Animator phiên bản mới
            var animator = zombie.GetComponent<VAT_Animator_Instanced>();
            if (animator == null || animator.animationData == null) return;

            var instance = new ZombieInstance
            {
                Logic = zombie,
                Animator = animator,
                Renderer = zombie.GetComponent<Renderer>()
            };

            instance.Renderer.enabled = false;
            _allActiveZombies.Add(instance);
        }

        public void Unregister(VAT_Zombie zombie)
        {
            _allActiveZombies.RemoveAll(z => z.Logic == zombie);
        }

        private void LateUpdate()
        {
            UpdateAndCullZombies();
            RenderVisibleZombies();
        }

        private void UpdateAndCullZombies()
        {
            // Xóa danh sách render cũ
            foreach (var list in _visibleZombiesByData.Values) list.Clear();

            GeometryUtility.CalculateFrustumPlanes(_cullingCamera, _frustumPlanes);

            for (int i = 0; i < _allActiveZombies.Count; i++)
            {
                var zombie = _allActiveZombies[i];
                if (zombie.Logic == null || !zombie.Logic.gameObject.activeInHierarchy) continue;

                if (GeometryUtility.TestPlanesAABB(_frustumPlanes, zombie.Renderer.bounds))
                {
                    var data = zombie.Animator.animationData;
                    if (!_visibleZombiesByData.ContainsKey(data))
                    {
                        _visibleZombiesByData[data] = new List<ZombieInstance>(512);
                    }
                    _visibleZombiesByData[data].Add(zombie);
                }
            }
        }

        private void RenderVisibleZombies()
        {
            foreach (var pair in _visibleZombiesByData)
            {
                var animationDataAsset = pair.Key;
                var visibleInstances = pair.Value;

                if (visibleInstances.Count == 0 || animationDataAsset.instancedMaterial == null) continue;

                int drawnCount = 0;
                while (drawnCount < visibleInstances.Count)
                {
                    int batchSize = Mathf.Min(visibleInstances.Count - drawnCount, 1023);

                    for (int i = 0; i < batchSize; i++)
                    {
                        var zombieInstance = visibleInstances[drawnCount + i];
                        _instanceMatrices[i] = zombieInstance.Logic.transform.localToWorldMatrix;
                        // SỬA LỖI CS1061: Gọi hàm mới đã được định nghĩa
                        _instanceAnimationData[i] = zombieInstance.Animator.GetAnimationDataForInstancing();
                    }

                    _propertyBlock.SetVectorArray(AnimationDataID, _instanceAnimationData);

                    Graphics.DrawMeshInstanced(
                        animationDataAsset.bakedMesh,
                        0,
                        // SỬA LỖI CS1061: Lấy material từ data asset, không phải từ mesh
                        animationDataAsset.instancedMaterial,
                        _instanceMatrices,
                        batchSize,
                        _propertyBlock
                    );

                    drawnCount += batchSize;
                }
            }
        }
    }
}