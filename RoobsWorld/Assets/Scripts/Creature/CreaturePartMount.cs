using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// Metadata attached to each visual creature part so animation, editor tooling, and tests can treat parts as modular mounts.
    /// </summary>
    public class CreaturePartMount : MonoBehaviour
    {
        [SerializeField] private string _partId;
        [SerializeField] private int _partIndex;
        [SerializeField] private int _attachBone;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private float _scale = 1f;

        public string PartId => _partId;
        public int PartIndex => _partIndex;
        public int AttachBone => _attachBone;
        public Vector3 Offset => _offset;
        public float Scale => _scale;
        public bool IsLeftSide => _offset.x < 0f;

        public void Initialize(PartPlacement placement, int partIndex)
        {
            _partId = placement?.PartId ?? string.Empty;
            _partIndex = Mathf.Max(0, partIndex);
            _attachBone = placement != null ? placement.AttachBone : 0;
            _offset = placement != null ? placement.Offset : Vector3.zero;
            _scale = placement != null ? Mathf.Max(0.01f, placement.Scale) : 1f;
        }
    }
}
