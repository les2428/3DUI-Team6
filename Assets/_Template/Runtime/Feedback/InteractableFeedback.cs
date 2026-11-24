using System.Collections.Generic;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Standard visual / audio / haptic feedback for an Interactable (the "Feedback" part of Discover-Act-Feedback).
    /// Add next to any Interactable. Tints the object's renderers on hover/select, plays sounds, pulses the controller.
    /// For richer feedback, write your own component that listens to the Interactable's events the same way.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class InteractableFeedback : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private bool tintOnHover = true;
        [SerializeField] private Color hoverTint = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private Color selectTint = new Color(1f, 0.8f, 0.35f);
        [Range(0f, 1f)] [SerializeField] private float tintStrength = 0.45f;
        [Tooltip("Scale up slightly while hovered.")]
        [SerializeField] private float hoverScale = 1.0f;
        [Tooltip("Renderers to tint. Empty = all child renderers.")]
        [SerializeField] private Renderer[] targetRenderers;

        [Header("Audio")]
        [SerializeField] private bool playSounds = true;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioClip activateClip;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;

        [Header("Haptics (XR)")]
        [SerializeField] private bool haptics = true;
        [Range(0f, 1f)] [SerializeField] private float hoverHaptic = 0.1f;
        [Range(0f, 1f)] [SerializeField] private float activateHaptic = 0.6f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Interactable interactable;
        private MaterialPropertyBlock block;
        private readonly List<(Renderer renderer, int index, int propId, Color original)> slots = new List<(Renderer, int, int, Color)>();
        private Vector3 baseScale;

        private void Awake()
        {
            interactable = GetComponent<Interactable>();
            block = new MaterialPropertyBlock();
            baseScale = transform.localScale;

            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>();

            foreach (var r in targetRenderers)
            {
                if (r == null || r is LineRenderer) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    int id = m.HasProperty(BaseColorId) ? BaseColorId : m.HasProperty(ColorId) ? ColorId : -1;
                    if (id != -1) slots.Add((r, i, id, m.GetColor(id)));
                }
            }

            if (hoverClip == null) hoverClip = ToneLibrary.Blip(880f, 0.04f);
            if (selectClip == null) selectClip = ToneLibrary.Blip(660f, 0.06f);
            if (activateClip == null) activateClip = ToneLibrary.Blip(1175f, 0.1f);
        }

        private void OnEnable()
        {
            interactable.HoverEntered += OnHoverEntered;
            interactable.HoverExited += OnHoverExited;
            interactable.SelectEntered += OnSelectEntered;
            interactable.SelectExited += OnSelectExited;
            interactable.Activated += OnActivated;
        }

        private void OnDisable()
        {
            interactable.HoverEntered -= OnHoverEntered;
            interactable.HoverExited -= OnHoverExited;
            interactable.SelectEntered -= OnSelectEntered;
            interactable.SelectExited -= OnSelectExited;
            interactable.Activated -= OnActivated;
            ApplyTint(null);
        }

        private void OnHoverEntered(Interactable i, InteractionContext ctx)
        {
            if (!i.IsSelected) ApplyTint(hoverTint);
            if (hoverScale != 1f) transform.localScale = baseScale * hoverScale;
            if (playSounds) ToneLibrary.Play(hoverClip, transform.position, volume * 0.4f);
            if (haptics && ctx?.Pointer != null) ctx.Pointer.Haptic(hoverHaptic, 15f);
        }

        private void OnHoverExited(Interactable i, InteractionContext ctx)
        {
            if (!i.IsSelected) ApplyTint(null);
            transform.localScale = baseScale;
        }

        private void OnSelectEntered(Interactable i, InteractionContext ctx)
        {
            ApplyTint(selectTint);
            if (playSounds) ToneLibrary.Play(selectClip, transform.position, volume);
        }

        private void OnSelectExited(Interactable i, InteractionContext ctx)
        {
            ApplyTint(i.IsHovered ? hoverTint : (Color?)null);
            if (!i.IsHovered) transform.localScale = baseScale;
        }

        private void OnActivated(Interactable i, InteractionContext ctx)
        {
            if (playSounds) ToneLibrary.Play(activateClip, transform.position, volume);
            if (haptics && ctx?.Pointer != null) ctx.Pointer.Haptic(activateHaptic, 60f);
        }

        private void ApplyTint(Color? tint)
        {
            if (!tintOnHover) return;
            foreach (var (r, index, propId, original) in slots)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block, index);
                if (tint.HasValue) block.SetColor(propId, Color.Lerp(original, tint.Value, tintStrength));
                else block.SetColor(propId, original);
                r.SetPropertyBlock(block, index);
            }
        }
    }
}
