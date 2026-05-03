using UnityEngine;
using RoobsWorld.Utils;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// Visual ghost representing a remote player's cell in the shared ocean.
    ///
    /// Driven entirely by received <see cref="PlayerNetState"/> snapshots — no physics, no input.
    /// Position is smoothly interpolated toward the last known server position to hide jitter.
    ///
    /// Lifecycle: created/destroyed by <see cref="NetworkTickManager"/> when players join/leave.
    /// </summary>
    public sealed class RemotePlayerCell : MonoBehaviour
    {
        // Remote player colour palette — index 0 = player 1, 1 = player 2, 2 = player 3.
        // Local player (id 0) keeps the default green assigned by CellStage.SpawnPlayer().
        private static readonly Color[] RemoteColors =
        {
            new Color(0.20f, 0.85f, 1.00f, 1f),  // Player 1 — cyan
            new Color(1.00f, 0.50f, 0.10f, 1f),  // Player 2 — orange
            new Color(0.90f, 0.20f, 0.80f, 1f),  // Player 3 — pink
        };

        /// <summary>Smooth-follow speed (units/s × lerp factor per frame). Higher = snappier.</summary>
        public const float LerpSpeed = 12f;

        private int     _playerId;
        private Vector3 _targetPosition;
        private float   _targetScale = 1f;
        private bool    _isAlive = true;

        public int PlayerId => _playerId;

        // ---- factory ----

        /// <summary>
        /// Spawns a visual ghost cell for the given remote player at <paramref name="startPos"/>.
        /// </summary>
        public static RemotePlayerCell Create(int playerId, Vector3 startPos)
        {
            Color color = GetColorForPlayer(playerId);
            var go = ModelFactory.CreateCell(startPos, color);
            go.name = $"RemotePlayer_{playerId}";

            // No physics — ghost representation only
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;

            var comp = go.AddComponent<RemotePlayerCell>();
            comp._playerId       = playerId;
            comp._targetPosition = startPos;
            return comp;
        }

        /// <summary>Returns the display colour for the given player ID (0 = local/green, 1–3 = remote palette).</summary>
        public static Color GetColorForPlayer(int playerId)
        {
            if (playerId <= 0) return new Color(0.3f, 1f, 0.5f); // local player green
            int idx = Mathf.Clamp(playerId - 1, 0, RemoteColors.Length - 1);
            return RemoteColors[idx];
        }

        // ---- state application ----

        /// <summary>
        /// Updates the ghost with the latest host snapshot for this player.
        /// Call from <see cref="NetworkTickManager"/> whenever a new <see cref="PlayerNetState"/> arrives.
        /// </summary>
        public void ApplyState(in PlayerNetState state)
        {
            _targetPosition = new Vector3(state.PosX, transform.position.y, state.PosY);
            _targetScale    = Mathf.Clamp(1f + state.Mass * 0.05f, 0.3f, 4f);
            _isAlive        = state.IsAlive;
            gameObject.SetActive(_isAlive);
        }

        /// <summary>
        /// Moves the host-side ghost immediately from validated remote input.
        /// </summary>
        public void MoveAuthoritative(Vector3 delta)
        {
            transform.position += delta;
            _targetPosition = transform.position;
        }

        // ---- MonoBehaviour ----

        private void Update()
        {
            if (!_isAlive) return;
            float t = LerpSpeed * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, _targetPosition, t);
            float s = Mathf.Lerp(transform.localScale.x, _targetScale, t);
            transform.localScale = new Vector3(s, s, s);
        }
    }
}
