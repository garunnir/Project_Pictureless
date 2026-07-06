// ============================================================
// ContainerDefinitionSO — 인벤 컨테이너(몸통·상자·가방 내부) 정의
// ============================================================

using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Item
{
    [CreateAssetMenu(fileName = "Container", menuName = "GameDataAsset/Item/Container Definition")]
    public sealed class ContainerDefinitionSO : UnityEngine.ScriptableObject
    {
        [SerializeField] string _locKey;
        [SerializeField] Sprite _icon;
        [SerializeField, Min(0f)] float _maxWeight = 50f;
        [SerializeField, Min(0f)] float _maxVolume = 30f;

        public string LocKey => _locKey;
        public Sprite Icon => _icon;
        public float MaxWeight => _maxWeight;
        public float MaxVolume => _maxVolume;
    }
}
