using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Something in the space the guide agent can see and talk about. Put it on an object with a collider.
    /// The visitor clicks it (ray or touch) to ask "what is this?"; the agent refers to it by its id.
    /// </summary>
    public class GuideThing : Interactable
    {
        public static readonly List<GuideThing> All = new List<GuideThing>();
        /// <summary>Raised when the visitor clicks a thing ("this one").</summary>
        public static event Action<GuideThing> Clicked;

        [Tooltip("Short id the brain uses as 'target', e.g. statue. Lower case, no spaces.")]
        public string id = "thing";
        [Tooltip("What the agent says about it. One or two short sentences.")]
        [TextArea(2, 4)] public string description = "";

        /// <summary>The thing the visitor clicked last.</summary>
        public static GuideThing Selected { get; private set; }

        public string Name => string.IsNullOrEmpty(DisplayName) ? id : DisplayName;

        public Vector3 Center
        {
            get
            {
                var r = GetComponentInChildren<Renderer>();
                return r != null ? r.bounds.center : transform.position;
            }
        }

        private void Reset() { DisplayName = name; id = name.ToLowerInvariant().Replace(' ', '_'); }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }

        protected override void OnDisable()
        {
            base.OnDisable();
            All.Remove(this);
            if (Selected == this) Selected = null;
        }

        protected override void OnActivate(InteractionContext ctx)
        {
            Selected = this;
            Clicked?.Invoke(this);
        }

        public static GuideThing Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var t in All) if (t != null && t.id == id) return t;
            return null;
        }

        /// <summary>For tests: act as if the visitor clicked this thing.</summary>
        public void ClickFromCode() { Selected = this; Clicked?.Invoke(this); }
    }
}
