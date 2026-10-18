// Minimal stand-ins for Unity package APIs (uGUI, EventSystems, InputSystem, URP) used only by
// tools/compile-check/check.sh. Not part of the Unity project (lives outside Assets).
// When the compile check fails on a missing package member the game really uses, add it here.
using System;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    public abstract class UIBehaviour : MonoBehaviour
    {
        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void Start() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnValidate() { }
        protected virtual void OnRectTransformDimensionsChange() { }
        public virtual bool IsActive() => true;
    }
    public class BaseEventData { public GameObject selectedObject { get; set; } public void Use() { } }
    public class PointerEventData : BaseEventData
    {
        public enum InputButton { Left, Right, Middle }
        public int pointerId { get; set; }
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public Vector2 pressPosition { get; set; }
        public InputButton button { get; set; }
        public int clickCount { get; set; }
        public GameObject pointerPress { get; set; }
        public GameObject pointerEnter { get; set; }
        public RaycastResult pointerPressRaycast { get; set; }
        public RaycastResult pointerCurrentRaycast { get; set; }
        public Camera pressEventCamera => null;
        public Camera enterEventCamera => null;
    }
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData e); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData e); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData e); }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData e); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData e); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData e); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData e); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData e); }
    public struct RaycastResult { public GameObject gameObject; }
    public abstract class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
    public abstract class BaseRaycaster : UIBehaviour { }
    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; }
        public GameObject currentSelectedGameObject => null;
        public void SetSelectedGameObject(GameObject go) { }
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
        public void RaycastAll(PointerEventData e, List<RaycastResult> results) { }
    }
}

namespace UnityEngine.UI
{
    using UnityEngine.EventSystems;
    using UnityEngine.Events;

    public class VertexHelper
    {
        public int currentVertCount => 0;
        public void PopulateUIVertex(ref UIVertex v, int i) { }
        public void SetUIVertex(UIVertex v, int i) { }
        public void GetUIVertexStream(List<UIVertex> stream) { }
        public void AddUIVertexTriangleStream(List<UIVertex> stream) { }
        public void Clear() { }
        public void AddVert(Vector3 position, Color32 color, Vector4 uv0) { }
        public void AddVert(UIVertex v) { }
        public void AddTriangle(int a, int b, int c) { }
    }
    public interface IMeshModifier { void ModifyMesh(VertexHelper vh); }
    public abstract class BaseMeshEffect : UIBehaviour, IMeshModifier
    {
        protected Graphic graphic => null;
        public abstract void ModifyMesh(VertexHelper vh);
    }
    public class Shadow : BaseMeshEffect
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
        public bool useGraphicAlpha { get; set; }
        public override void ModifyMesh(VertexHelper vh) { }
    }
    public class Outline : Shadow { }

    public abstract class Graphic : UIBehaviour
    {
        public virtual Color color { get; set; }
        public virtual Material material { get; set; }
        public virtual Texture mainTexture => null;
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform => null;
        public CanvasRenderer canvasRenderer => null;
        public Canvas canvas => null;
        public virtual void SetAllDirty() { }
        public virtual void SetVerticesDirty() { }
        public virtual void SetMaterialDirty() { }
        public virtual void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }
        public virtual void CrossFadeColor(Color c, float duration, bool ignoreTimeScale, bool useAlpha) { }
        protected virtual void OnPopulateMesh(VertexHelper vh) { }
    }
    public abstract class MaskableGraphic : Graphic { public bool maskable { get; set; } }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public enum OriginHorizontal { Left, Right }
        public enum OriginVertical { Bottom, Top }
        public enum Origin90 { BottomLeft, TopLeft, TopRight, BottomRight }
        public enum Origin180 { Bottom, Left, Top, Right }
        public enum Origin360 { Bottom, Right, Top, Left }
        public Sprite sprite { get; set; }
        public Sprite overrideSprite { get; set; }
        public Type type { get; set; }
        public FillMethod fillMethod { get; set; }
        public int fillOrigin { get; set; }
        public float fillAmount { get; set; }
        public bool fillClockwise { get; set; }
        public bool fillCenter { get; set; }
        public bool preserveAspect { get; set; }
        public float pixelsPerUnitMultiplier { get; set; }
        public float alphaHitTestMinimumThreshold { get; set; }
        public void SetNativeSize() { }
    }
    public class RawImage : MaskableGraphic { public Texture texture { get; set; } public Rect uvRect { get; set; } }

    public class Text : MaskableGraphic
    {
        public virtual string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool alignByGeometry { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; }
        public int resizeTextMaxSize { get; set; }
        public float lineSpacing { get; set; }
        public bool supportRichText { get; set; }
        public float preferredWidth => 0;
        public float preferredHeight => 0;
    }

    public struct ColorBlock
    {
        public static ColorBlock defaultColorBlock => default;
        public Color normalColor, highlightedColor, pressedColor, selectedColor, disabledColor;
        public float colorMultiplier, fadeDuration;
    }
    public struct Navigation
    {
        public enum Mode { None, Horizontal, Vertical, Automatic, Explicit }
        public Mode mode;
        public static Navigation defaultNavigation => default;
    }
    public class Selectable : UIBehaviour
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
        public Image image { get; set; }
        public Transition transition { get; set; }
        public ColorBlock colors { get; set; }
        public Navigation navigation { get; set; }
        public virtual void Select() { }
    }
    public class Button : Selectable, IPointerClickHandler
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent();
        public virtual void OnPointerClick(PointerEventData e) { }
    }
    public class Toggle : Selectable
    {
        public class ToggleEvent : UnityEvent<bool> { }
        public bool isOn { get; set; }
        public ToggleEvent onValueChanged { get; set; } = new ToggleEvent();
    }
    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public class SliderEvent : UnityEvent<float> { }
        public float value { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public bool wholeNumbers { get; set; }
        public Direction direction { get; set; }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public SliderEvent onValueChanged { get; set; } = new SliderEvent();
        public void SetValueWithoutNotify(float v) { }
    }
    public class Scrollbar : Selectable { public float value { get; set; } }
    public class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public class ScrollRectEvent : UnityEvent<Vector2> { }
        public RectTransform content { get; set; }
        public RectTransform viewport { get; set; }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public MovementType movementType { get; set; }
        public float scrollSensitivity { get; set; }
        public float elasticity { get; set; }
        public bool inertia { get; set; }
        public float decelerationRate { get; set; }
        public Vector2 normalizedPosition { get; set; }
        public float verticalNormalizedPosition { get; set; }
        public float horizontalNormalizedPosition { get; set; }
        public Vector2 velocity { get; set; }
        public Scrollbar verticalScrollbar { get; set; }
        public ScrollRectEvent onValueChanged { get; set; } = new ScrollRectEvent();
        public void StopMovement() { }
    }
    public class Mask : UIBehaviour { public bool showMaskGraphic { get; set; } }
    public class RectMask2D : UIBehaviour { }

    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float scaleFactor { get; set; }
        public float referencePixelsPerUnit { get; set; }
        public float dynamicPixelsPerUnit { get; set; }
    }
    public class GraphicRaycaster : BaseRaycaster { }

    public abstract class LayoutGroup : UIBehaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
        public bool childScaleWidth { get; set; }
        public bool childScaleHeight { get; set; }
    }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class GridLayoutGroup : LayoutGroup
    {
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Vector2 cellSize { get; set; }
        public Vector2 spacing { get; set; }
        public Constraint constraint { get; set; }
        public int constraintCount { get; set; }
    }
    public class LayoutElement : UIBehaviour
    {
        public bool ignoreLayout { get; set; }
        public float minWidth { get; set; }
        public float minHeight { get; set; }
        public float preferredWidth { get; set; }
        public float preferredHeight { get; set; }
        public float flexibleWidth { get; set; }
        public float flexibleHeight { get; set; }
    }
    public class ContentSizeFitter : UIBehaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }
    public static class LayoutRebuilder
    {
        public static void ForceRebuildLayoutImmediate(RectTransform rect) { }
        public static void MarkLayoutForRebuild(RectTransform rect) { }
    }
}

namespace UnityEngine.InputSystem
{
    namespace Controls
    {
        public class ButtonControl
        {
            public bool isPressed => false;
            public bool wasPressedThisFrame => false;
            public bool wasReleasedThisFrame => false;
        }
        public class KeyControl : ButtonControl { }
    }
    public enum Key { None, Space, Enter, Escape, Tab, Backspace, LeftShift, RightShift, LeftCtrl, RightCtrl,
        LeftArrow, RightArrow, UpArrow, DownArrow,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit0, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9 }
    public class Keyboard
    {
        public static Keyboard current => null;
        public Controls.KeyControl this[Key key] => null;
        public Controls.KeyControl wKey, aKey, sKey, dKey, spaceKey, escapeKey, enterKey, tabKey,
            leftArrowKey, rightArrowKey, upArrowKey, downArrowKey, leftShiftKey, gKey, mKey, pKey, rKey;
    }
    namespace UI
    {
        public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { }
    }
}

namespace UnityEngine.Rendering
{
    public class VolumeComponent : ScriptableObject { public bool active { get; set; } }
    public class VolumeParameter<T>
    {
        public T value { get; set; }
        public bool overrideState { get; set; }
        public void Override(T v) { value = v; overrideState = true; }
    }
    public class ClampedFloatParameter : VolumeParameter<float> { public ClampedFloatParameter(float v, float min, float max, bool o = false) { } }
    public class MinFloatParameter : VolumeParameter<float> { public MinFloatParameter(float v, float min, bool o = false) { } }
    public class FloatParameter : VolumeParameter<float> { public FloatParameter(float v, bool o = false) { } }
    public class ColorParameter : VolumeParameter<Color> { public ColorParameter(Color v, bool o = false) { } }
    public class BoolParameter : VolumeParameter<bool> { public BoolParameter(bool v, bool o = false) { } }
    public class Vector2Parameter : VolumeParameter<Vector2> { public Vector2Parameter(Vector2 v, bool o = false) { } }
    public class VolumeProfile : ScriptableObject
    {
        public List<VolumeComponent> components = new List<VolumeComponent>();
        public T Add<T>(bool overrides = false) where T : VolumeComponent => null;
        public bool TryGet<T>(out T component) where T : VolumeComponent { component = null; return false; }
        public bool Has<T>() where T : VolumeComponent => false;
        public void Remove<T>() where T : VolumeComponent { }
    }
    public class Volume : MonoBehaviour
    {
        public bool isGlobal;
        public float priority;
        public float weight;
        public float blendDistance;
        public VolumeProfile sharedProfile;
        public VolumeProfile profile { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    using UnityEngine.Rendering;
    public enum TonemappingMode { None, Neutral, ACES }
    public class TonemappingModeParameter : VolumeParameter<TonemappingMode> { public TonemappingModeParameter(TonemappingMode v, bool o = false) { } }
    public class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold, intensity;
        public ClampedFloatParameter scatter;
        public ColorParameter tint;
    }
    public class Vignette : VolumeComponent
    {
        public ColorParameter color;
        public Vector2Parameter center;
        public ClampedFloatParameter intensity, smoothness;
        public BoolParameter rounded;
    }
    public class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure;
        public ClampedFloatParameter contrast, hueShift, saturation;
        public ColorParameter colorFilter;
    }
    public class Tonemapping : VolumeComponent { public TonemappingModeParameter mode; }
    public class WhiteBalance : VolumeComponent { public ClampedFloatParameter temperature, tint; }
    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get; set; }
        public AntialiasingMode antialiasing { get; set; }
    }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public static class CameraExtensions
    {
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera) => null;
    }
}
