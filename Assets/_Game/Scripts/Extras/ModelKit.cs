using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Extras
{
    /// <summary>
    /// Builds little models out of cubes, spheres and cylinders at runtime (for collectible decor and
    /// pets), with colored materials made from the shader the shop already uses.
    /// </summary>
    public static class ModelKit
    {
        private static readonly Dictionary<Color, Material> materials = new();
        private static Shader shader;

        /// <summary>A material of this color (shared between models of the same color).</summary>
        public static Material Mat(Color color, bool glow = false)
        {
            Color key = glow ? new Color(color.r, color.g, color.b, 0.5f) : color;
            if (materials.TryGetValue(key, out Material cached) && cached != null) return cached;

            Material template = BaseMaterial();
            var material = template != null ? new Material(template) : new Material(FindShader());
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (glow && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.6f);
            }
            materials[key] = material;
            return material;
        }

        /// <summary>Adds a primitive part under the parent, without its collider.</summary>
        public static Transform Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color,
            Vector3 euler = default, bool glow = false)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            Collider solid = part.GetComponent<Collider>();
            solid.enabled = false; // gone by the end of the frame; off right away so it isn't measured or hit
            Object.Destroy(solid);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = Mat(color, glow);
            return part.transform;
        }

        /// <summary>One box collider on the root, so the model can be tapped and bumped into.</summary>
        public static void Collider(GameObject root, Vector3 center, Vector3 size)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        private static Material baseMaterial;

        // The material runtime models copy: preferably a plain lit material from something already
        // showing in the shop (so its shader and keyword set are certainly in the phone build and
        // render there), otherwise the builder's Resources/ModelKitBase material.
        private static Material BaseMaterial()
        {
            if (baseMaterial != null) return baseMaterial;
#if UNITY_2023_1_OR_NEWER
            MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
#else
            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();
#endif
            foreach (MeshRenderer renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                Material material = renderer.sharedMaterial;
                if (material == null || material.shader == null || !material.shader.isSupported) continue;
                if (material.shader.name != "Universal Render Pipeline/Lit" || !material.HasProperty("_BaseColor")) continue;
                if (material.mainTexture != null || material.IsKeywordEnabled("_EMISSION")) continue;
                if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f) continue; // see-through
                baseMaterial = new Material(material) { name = "ModelKit (from " + material.name + ")" };
                break;
            }
            if (baseMaterial == null)
            {
                Material saved = Resources.Load<Material>("ModelKitBase");
                if (saved != null) baseMaterial = new Material(saved);
            }
            if (baseMaterial != null)
                Debug.Log($"Bubble Tea: runtime models use {baseMaterial.name} ({baseMaterial.shader.name}, supported: {baseMaterial.shader.isSupported}).");
            return baseMaterial;
        }

        // Fallback: the shop's own lit shader (taken from something already in the scene, so it's in the build).
        private static Shader FindShader()
        {
            if (shader != null) return shader;
#if UNITY_2023_1_OR_NEWER
            MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
#else
            MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();
#endif
            foreach (MeshRenderer renderer in renderers)
            {
                Material material = renderer.sharedMaterial;
                if (material != null && material.shader != null && material.HasProperty("_BaseColor"))
                {
                    shader = material.shader;
                    return shader;
                }
            }
            shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            materials.Clear();
            baseMaterial = null;
            shader = null;
        }
    }
}
