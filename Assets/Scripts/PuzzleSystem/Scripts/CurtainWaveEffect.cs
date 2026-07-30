using UnityEngine;

namespace AvoidTheCold
{
    /// <summary>
    /// Purely cosmetic idle animation for the Curtains UI image - sways its
    /// rotation around the pivot (like wind blowing a curtain hanging from a
    /// rod) and gently billows its width. No gameplay coupling; runs for as
    /// long as this component is enabled.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CurtainWaveEffect : MonoBehaviour
    {
        private const float TwoPi = Mathf.PI * 2f;
        private const float RandomOffsetRange = 100f;

        [Header("Primary Sway (slow, wide)")]
        [SerializeField] private float swayAngleDegrees = 4f;
        [SerializeField] private float swayCyclesPerSecond = 0.5f;

        [Header("Flutter (fast, subtle - breaks up the motion)")]
        [SerializeField] private float flutterAngleDegrees = 1.5f;
        [SerializeField] private float flutterCyclesPerSecond = 1.7f;

        [Header("Billow (horizontal puff in/out)")]
        [SerializeField] private float billowScaleFraction = 0.02f;
        [SerializeField] private float billowCyclesPerSecond = 0.8f;

        private RectTransform _rectTransform;
        private Quaternion _baseRotation;
        private Vector3 _baseScale;
        private Vector3 _scratchScale;
        private float _timeOffset;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                Debug.Log("[CurtainWaveEffect] No RectTransform found - disabling effect");
                enabled = false;
                return;
            }

            _baseRotation = _rectTransform.localRotation;
            _baseScale = _rectTransform.localScale;
            _scratchScale = _baseScale;
            _timeOffset = Random.Range(0f, RandomOffsetRange); // avoid every curtain instance syncing up
        }

        private void OnEnable()
        {
            Debug.Log("[CurtainWaveEffect] Curtain wave effect started");
        }

        private void OnDisable()
        {
            if (_rectTransform == null) return;
            _rectTransform.localRotation = _baseRotation;
            _rectTransform.localScale = _baseScale;
        }

        private void Update()
        {
            if (_rectTransform == null) return;

            float t = Time.time + _timeOffset;
            float sway = Mathf.Sin(t * swayCyclesPerSecond * TwoPi) * swayAngleDegrees;
            float flutter = Mathf.Sin(t * flutterCyclesPerSecond * TwoPi) * flutterAngleDegrees;
            float billow = Mathf.Sin(t * billowCyclesPerSecond * TwoPi) * billowScaleFraction;

            _rectTransform.localRotation = _baseRotation * Quaternion.AngleAxis(sway + flutter, Vector3.forward);

            _scratchScale.Set(_baseScale.x * (1f + billow), _baseScale.y, _baseScale.z);
            _rectTransform.localScale = _scratchScale;
        }
    }
}
