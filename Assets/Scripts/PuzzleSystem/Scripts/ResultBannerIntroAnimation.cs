using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace AvoidTheCold
{
    /// <summary>
    /// Plays a one-shot DOTween entrance animation every time this
    /// component's GameObject is enabled (e.g. WinBanner/LoseBanner, right
    /// when ResultScreenUI activates it): the Panel slides up from below
    /// into its resting position, then Daya and Tappu slide in from the
    /// right and left at the same time. Purely positional - the existing
    /// CanvasGroupFader on the same object still owns the alpha fade in/out.
    /// </summary>
    public class ResultBannerIntroAnimation : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform tappu;
        [SerializeField] private RectTransform daya;

        [SerializeField] Button retry, next;

        [SerializeField] private float panelDuration = 0.45f;
        [SerializeField] private float sideDuration = 0.4f;
        [SerializeField] private Ease panelEase = Ease.OutCubic;
        [SerializeField] private Ease sideEase = Ease.OutBack;

        private Vector2 _panelRestPos, _tappuRestPos, _dayaRestPos;
        private float _panelTravel, _tappuTravel, _dayaTravel;
        private Sequence _sequence;

        private void Awake()
        {
            CacheInitialTransforms();
        }

        private void CacheInitialTransforms()
        {
            if (panel != null)
            {
                _panelRestPos = panel.anchoredPosition;
                _panelTravel = panel.rect.height;
            }
            if (tappu != null)
            {
                _tappuRestPos = tappu.anchoredPosition;
                _tappuTravel = tappu.rect.width;
            }
            if (daya != null)
            {
                _dayaRestPos = daya.anchoredPosition;
                _dayaTravel = daya.rect.width;
            }
        }

        private void OnEnable()
        {
            Debug.Log($"[ResultBannerIntroAnimation] Playing intro for {name}");
            _sequence?.Kill();

            // Snap everything to its off-screen starting spot before playing.
            if (panel != null) panel.anchoredPosition = _panelRestPos + Vector2.down * _panelTravel;
            if (tappu != null) tappu.anchoredPosition = _tappuRestPos + Vector2.left * _tappuTravel;
            if (daya != null) daya.anchoredPosition = _dayaRestPos + Vector2.right * _dayaTravel;

            _sequence = DOTween.Sequence().SetTarget(this);

            if (panel != null)
                _sequence.Append(panel.DOAnchorPos(_panelRestPos, panelDuration).SetEase(panelEase));

            bool sideStepStarted = false;
            if (tappu != null)
            {
                _sequence.Append(tappu.DOAnchorPos(_tappuRestPos, sideDuration).SetEase(sideEase));
                sideStepStarted = true;
            }
            if (daya != null)
            {
                var dayaTween = daya.DOAnchorPos(_dayaRestPos, sideDuration).SetEase(sideEase);
                if (sideStepStarted) _sequence.Join(dayaTween);
                else _sequence.Append(dayaTween);
            }

            _sequence.OnComplete(()=>{
                if (next != null) next.gameObject.SetActive(true);
                if(retry != null) retry.gameObject.SetActive(true); 
            });
        }

        private void OnDisable()
        {
            ResetPositions();
        }

        /// <summary>
        /// Call this manually from an external script (like CanvasGroupFader) 
        /// right *before* disabling the GameObject to guarantee a clean reset.
        /// </summary>
        public void ResetPositions()
        {
            _sequence?.Kill();
            _sequence = null;

            // Snap back to resting position so the next OnEnable's animation
            // always starts from a clean, known state instead of wherever
            // the tween was interrupted.
            if (panel != null) panel.anchoredPosition = _panelRestPos;
            if (tappu != null) tappu.anchoredPosition = _tappuRestPos;
            if (daya != null) daya.anchoredPosition = _dayaRestPos;

            if (next != null) next.gameObject.SetActive(false);
            if (retry != null) retry.gameObject.SetActive(false);
           
        }
    }
}