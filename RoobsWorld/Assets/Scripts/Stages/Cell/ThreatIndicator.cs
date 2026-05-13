using RoobsWorld.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Stages.Cell
{
    public enum CellThreatCueType
    {
        None,
        Predator,
        Leviathan,
        Virus,
        ToxicHazard
    }

    /// <summary>
    /// Visual threat indicator system that displays danger when predators are nearby.
    /// Shows a red pulsing border and optional aura around the player cell.
    /// </summary>
    public class ThreatIndicator : MonoBehaviour
    {
        [Header("Threat Detection")]
        [SerializeField] private float _threatDetectionRadius = 18f; // Match predator threat radius
        [SerializeField] private float _threatWarningRadius = 28f; // Match predator safe radius

        [Header("Visual Feedback")]
        [SerializeField] private float _pulseSpeed = 2f; // Slower pulsing
        [SerializeField] private float _maxBorderAlpha = 0.6f; // Less intense
        [SerializeField] private float _minBorderAlpha = 0.2f; // Less intense
        [SerializeField] private float _scanInterval = 0.2f; // Scene scan cadence for threat actors

        private PlayerCell _player;
        private Image _threatBorderImage;
        private Canvas _threatCanvas;
        private float _threatIntensity; // 0 to 1
        private float _targetThreatIntensity;
        private float _scanTimer;
        private float _nearestThreatDistance = float.PositiveInfinity;
        private Vector3 _primaryThreatDirection = Vector3.zero;
        private CellThreatCueType _primaryThreatType = CellThreatCueType.None;
        private bool _isInitialized;
        private readonly List<PredatorCell> _predatorBuffer = new List<PredatorCell>(24);
        private readonly List<LeviathanCell> _leviathanBuffer = new List<LeviathanCell>(4);

        public float ThreatIntensity => _threatIntensity;
        public float NearestThreatDistance => _nearestThreatDistance;
        public Vector3 PrimaryThreatDirection => _primaryThreatDirection;
        public CellThreatCueType PrimaryThreatType => _primaryThreatType;
        public bool IsThreatDetected => _threatIntensity > 0.05f;

        public static ThreatIndicator CreateRuntime(PlayerCell player)
        {
            var go = new GameObject("ThreatIndicator");
            var indicator = go.AddComponent<ThreatIndicator>();
            indicator._player = player;
            indicator.Initialize();
            return indicator;
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // Create Canvas for threat border
            var canvasGo = new GameObject("ThreatBorderCanvas");
            canvasGo.transform.SetParent(transform, false);
            _threatCanvas = canvasGo.AddComponent<Canvas>();
            _threatCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _threatCanvas.sortingOrder = 5; // Below HUD but above game

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Create the threat border image
            var borderGo = new GameObject("ThreatBorder");
            borderGo.transform.SetParent(canvasGo.transform, false);
            _threatBorderImage = borderGo.AddComponent<Image>();

            var rectTransform = borderGo.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Create a semi-transparent red border texture
            var texture = CreateBorderTexture();
            _threatBorderImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                Vector2.one * 0.5f
            );
            _threatBorderImage.type = Image.Type.Simple;
            _threatBorderImage.color = new Color(1f, 0.2f, 0.2f, 0f);

            _scanTimer = 0f; // Force immediate first scan.
            _isInitialized = true;
        }

        private Texture2D CreateBorderTexture()
        {
            const int size = 128;
            const int borderWidth = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(new Color[size * size]);

            // Draw red border on edges
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < borderWidth; y++)
                {
                    tex.SetPixel(x, y, new Color(1f, 0.2f, 0.2f, 1f));
                    tex.SetPixel(x, size - 1 - y, new Color(1f, 0.2f, 0.2f, 1f));
                }
            }
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < borderWidth; x++)
                {
                    tex.SetPixel(x, y, new Color(1f, 0.2f, 0.2f, 1f));
                    tex.SetPixel(size - 1 - x, y, new Color(1f, 0.2f, 0.2f, 1f));
                }
            }

            tex.Apply();
            return tex;
        }

        private void Update()
        {
            if (_player == null || !_isInitialized) return;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = Mathf.Max(0.02f, _scanInterval);
                UpdateThreatDetection();
            }

            _threatIntensity = Mathf.MoveTowards(_threatIntensity, _targetThreatIntensity, Time.deltaTime * 4f);
            UpdateVisuals();
        }

        private void UpdateThreatDetection()
        {
            float targetIntensity = 0f;
            float nearestDistanceSqr = float.PositiveInfinity;
            Vector3 primaryDirection = Vector3.zero;
            CellThreatCueType primaryType = CellThreatCueType.None;

            // Prefer registry snapshots (low allocation), fallback to scene scans for compatibility.
            int predatorCount = PredatorPackCoordinator.CopyRegisteredPredators(_predatorBuffer);
            if (predatorCount == 0)
            {
                var predators = FindObjectsByType<PredatorCell>(FindObjectsInactive.Exclude);
                for (int i = 0; i < predators.Length; i++)
                {
                    if (predators[i] != null)
                        _predatorBuffer.Add(predators[i]);
                }
            }

            for (int i = 0; i < _predatorBuffer.Count; i++)
            {
                var predator = _predatorBuffer[i];
                if (predator == null) continue;
                EvaluateThreatCandidate(
                    predator.transform.position,
                    CellThreatCueType.Predator,
                    _threatDetectionRadius,
                    _threatWarningRadius,
                    0.6f,
                    ref targetIntensity,
                    ref nearestDistanceSqr,
                    ref primaryDirection,
                    ref primaryType);
            }

            // Also check for leviathan encounters (more dangerous and visible from further away).
            int leviathanCount = LeviathanCell.CopyActiveInstances(_leviathanBuffer);
            if (leviathanCount == 0)
            {
                var leviathans = FindObjectsByType<LeviathanCell>(FindObjectsInactive.Exclude);
                for (int i = 0; i < leviathans.Length; i++)
                {
                    if (leviathans[i] != null)
                        _leviathanBuffer.Add(leviathans[i]);
                }
            }

            for (int i = 0; i < _leviathanBuffer.Count; i++)
            {
                var leviathan = _leviathanBuffer[i];
                if (leviathan == null) continue;
                EvaluateThreatCandidate(
                    leviathan.transform.position,
                    CellThreatCueType.Leviathan,
                    _threatDetectionRadius * 1.4f,
                    _threatWarningRadius * 1.8f,
                    0.9f,
                    ref targetIntensity,
                    ref nearestDistanceSqr,
                    ref primaryDirection,
                    ref primaryType);
            }

            var viruses = FindObjectsByType<ViralParticle>(FindObjectsInactive.Exclude);
            for (int i = 0; i < viruses.Length; i++)
            {
                var virus = viruses[i];
                if (virus == null) continue;
                EvaluateThreatCandidate(
                    virus.transform.position,
                    CellThreatCueType.Virus,
                    _threatDetectionRadius * 0.7f,
                    _threatWarningRadius * 0.9f,
                    0.72f,
                    ref targetIntensity,
                    ref nearestDistanceSqr,
                    ref primaryDirection,
                    ref primaryType);
            }

            var toxins = FindObjectsByType<ToxinCloud>(FindObjectsInactive.Exclude);
            for (int i = 0; i < toxins.Length; i++)
            {
                var toxin = toxins[i];
                if (toxin == null) continue;
                EvaluateThreatCandidate(
                    toxin.transform.position,
                    CellThreatCueType.ToxicHazard,
                    _threatDetectionRadius * 0.45f,
                    _threatWarningRadius * 0.7f,
                    0.5f,
                    ref targetIntensity,
                    ref nearestDistanceSqr,
                    ref primaryDirection,
                    ref primaryType);
            }

            _nearestThreatDistance = float.IsPositiveInfinity(nearestDistanceSqr)
                ? float.PositiveInfinity
                : Mathf.Sqrt(nearestDistanceSqr);
            _primaryThreatDirection = primaryDirection;
            _primaryThreatType = primaryType;
            _targetThreatIntensity = targetIntensity;

            _predatorBuffer.Clear();
            _leviathanBuffer.Clear();
        }

        private void EvaluateThreatCandidate(
            Vector3 threatPosition,
            CellThreatCueType threatType,
            float detectionRadius,
            float warningRadius,
            float warningZoneMaxIntensity,
            ref float maxIntensity,
            ref float nearestDistanceSqr,
            ref Vector3 primaryDirection,
            ref CellThreatCueType primaryType)
        {
            Vector3 delta = threatPosition - _player.transform.position;
            delta.y = 0f;
            float distanceSqr = delta.sqrMagnitude;
            float warningRadiusSqr = warningRadius * warningRadius;
            if (distanceSqr >= warningRadiusSqr) return;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                primaryDirection = delta.sqrMagnitude > 0.0001f
                    ? delta.normalized
                    : Vector3.zero;
                primaryType = threatType;
            }

            float intensity = EvaluateThreatIntensityFromSqrDistance(
                distanceSqr,
                detectionRadius,
                warningRadius,
                warningZoneMaxIntensity);

            maxIntensity = Mathf.Max(maxIntensity, Mathf.Clamp01(intensity));
        }

        /// <summary>Returns normalized threat intensity for the given distance band.</summary>
        public static float EvaluateThreatIntensity(
            float distance,
            float detectionRadius,
            float warningRadius,
            float warningZoneMaxIntensity)
        {
            if (distance <= detectionRadius) return 1f;
            if (distance >= warningRadius) return 0f;
            float t = Mathf.InverseLerp(detectionRadius, warningRadius, distance);
            return Mathf.Lerp(warningZoneMaxIntensity, 0f, t);
        }

        /// <summary>
        /// Equivalent to <see cref="EvaluateThreatIntensity"/> using squared distance input.
        /// </summary>
        public static float EvaluateThreatIntensityFromSqrDistance(
            float sqrDistance,
            float detectionRadius,
            float warningRadius,
            float warningZoneMaxIntensity)
        {
            float detectionSqr = detectionRadius * detectionRadius;
            if (sqrDistance <= detectionSqr) return 1f;

            float warningSqr = warningRadius * warningRadius;
            if (sqrDistance >= warningSqr) return 0f;

            float distance = Mathf.Sqrt(Mathf.Max(0f, sqrDistance));
            return EvaluateThreatIntensity(distance, detectionRadius, warningRadius, warningZoneMaxIntensity);
        }

        public static string GetThreatCueLabel(CellThreatCueType threatType)
        {
            switch (threatType)
            {
                case CellThreatCueType.Leviathan: return "Leviathan";
                case CellThreatCueType.Virus: return "Virus";
                case CellThreatCueType.ToxicHazard: return "Toxin";
                case CellThreatCueType.Predator: return "Predator";
                default: return "Threat";
            }
        }

        private void UpdateVisuals()
        {
            if (_threatBorderImage == null) return;

            // Pulsing effect when threatened
            float pulseAlpha = _threatIntensity > 0.05f
                ? Mathf.Lerp(_minBorderAlpha, _maxBorderAlpha, (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f)
                : 0f;

            var color = _threatBorderImage.color;
            color.a = Mathf.Lerp(color.a, pulseAlpha, Time.deltaTime * 5f);
            _threatBorderImage.color = color;
        }

        private void OnDestroy()
        {
            if (_threatBorderImage != null)
                Destroy(_threatBorderImage.gameObject);
            if (_threatCanvas != null)
                Destroy(_threatCanvas.gameObject);
        }
    }
}
