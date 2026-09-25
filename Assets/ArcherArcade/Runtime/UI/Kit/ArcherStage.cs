using ArcherArcade.Archers;
using ArcherArcade.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// A live 2.5D archer inside the UI (Home stage, Archers turntable, shop preview): the same ArcherView as in
    /// matches, rendered by a private orthographic camera into a transparent RenderTexture shown by a RawImage.
    /// Tap turns the archer around (design "Tap to turn"); <see cref="Demo"/> plays draw → release.
    /// </summary>
    public sealed class ArcherStage : MonoBehaviour, IPointerClickHandler
    {
        const int PreviewLayer = 31;
        static int _slot;

        RawImage _image;
        RenderTexture _rt;
        Camera _cam;
        GameObject _world;
        ArcherView _view;
        string _look;
        int _facing = 1;
        bool _tapToTurn;
        float _demoTime = -1f;

        public ArcherView View => _view;

        public static ArcherStage Create(RectTransform parent, string look, bool tapToTurn, int facing = 1)
        {
            RectTransform rt = UiKit.Rect(parent, "ArcherStage");
            UiKit.Stretch(rt);
            var s = rt.gameObject.AddComponent<ArcherStage>();
            s._image = rt.gameObject.AddComponent<RawImage>();
            s._image.raycastTarget = tapToTurn;
            s._tapToTurn = tapToTurn;
            s._facing = facing;
            s.BuildWorld();
            s.SetLook(look);
            return s;
        }

        void BuildWorld()
        {
            _slot = (_slot + 1) % 16;
            _world = new GameObject("PreviewStage");
            _world.transform.position = new Vector3(1000f + _slot * 40f, 1000f, 0f);
            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(_world.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.1f, -10f);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = 1.3f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _cam.cullingMask = 1 << PreviewLayer;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 50f;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
        }

        public void SetLook(string look)
        {
            if (look == _look && _view) return;
            _look = look;
            if (_view) Destroy(_view.gameObject);
            _view = ArcherView.Create(_world.transform, look, _facing, 0);
            SetLayer(_view.gameObject, PreviewLayer);
            float scale = _view.Rig != null ? _view.Rig.Scale : 1f;
            _cam.orthographicSize = 1.3f * scale;
            _cam.transform.localPosition = new Vector3(0f, 1.08f * scale, -10f);
            if (_demoTime >= 0f) _demoTime = -1f;
        }

        static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayer(t.gameObject, layer);
        }

        void LateUpdate()
        {
            if (_view) SetLayer(_view.gameObject, PreviewLayer);
            EnsureTexture();
            if (_demoTime >= 0f) RunDemo(Time.deltaTime);
        }

        void EnsureTexture()
        {
            Rect r = ((RectTransform)transform).rect;
            Canvas c = GetComponentInParent<Canvas>();
            float scale = c ? c.scaleFactor : 1f;
            int w = Mathf.Clamp(Mathf.RoundToInt(r.width * scale), 64, 1024);
            int h = Mathf.Clamp(Mathf.RoundToInt(r.height * scale), 64, 1024);
            if (_rt && _rt.width == w && _rt.height == h) return;
            if (_rt)
            {
                _cam.targetTexture = null;
                _rt.Release();
                Destroy(_rt);
            }
            _rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { name = "ArcherStage", antiAliasing = 1 };
            _rt.Create();
            _cam.targetTexture = _rt;
            _cam.aspect = (float)w / h;
            _image.texture = _rt;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!_tapToTurn) return;
            Turn();
        }

        /// <summary>Turns around with a squash (scaleX 1 → 0 → −1, overshoot) like the design's turntable.</summary>
        public void Turn()
        {
            _facing = -_facing;
            Transform t = _view ? _view.transform : null;
            if (!t) return;
            ArcherView v = _view;
            int f = _facing;
            Core.ServiceLocator.Audio?.Play(Feel.SoundId.UiTap);
            Tween.Value(1f, -1f, 0.3f, k =>
            {
                if (!t) return;
                t.localScale = new Vector3(Mathf.Abs(k) < 0.05f ? 0.05f : Mathf.Abs(k), 1f, 1f);
                if (k < 0f && v.Facing != f) v.SetFacing(f);
            }, EaseType.OutBack, 0f, true, null, t);
        }

        /// <summary>Ability demo: the archer draws, holds, releases and cheers.</summary>
        public void Demo() => _demoTime = 0f;

        void RunDemo(float dt)
        {
            _demoTime += dt;
            if (!_view) return;
            if (_demoTime < 0.9f) _view.Aim(22f, Mathf.Clamp01(_demoTime / 0.7f));
            else if (_demoTime < 0.95f) _view.Release();
            else if (_demoTime > 1.5f && _demoTime < 1.55f) _view.Victory();
            else if (_demoTime > 2.8f)
            {
                _view.Idle();
                _demoTime = -1f;
            }
        }

        void OnDestroy()
        {
            Tween.KillTarget(this);
            if (_view) Tween.KillTarget(_view.transform);
            if (_cam) _cam.targetTexture = null;
            if (_rt)
            {
                _rt.Release();
                Destroy(_rt);
            }
            if (_world) Destroy(_world);
        }
    }
}
