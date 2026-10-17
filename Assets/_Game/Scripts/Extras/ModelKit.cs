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

        /// <summary>Bumped with each change, so the PETS window shows whether the phone runs the newest build.</summary>
        public const string BuildTag = "17 Oct I";

        /// <summary>Which material runtime models are made from (shown at the bottom of the PETS window).</summary>
        public static string Report { get; private set; } = "no models made yet";

        /// <summary>A material of this color (shared between models of the same color).</summary>
        public static Material Mat(Color color, bool glow = false)
        {
            Color key = glow ? new Color(color.r, color.g, color.b, 0.5f) : color;
            if (materials.TryGetValue(key, out Material cached) && cached != null) return cached;

            Material template = BaseMaterial();
            Material material;
            if (template != null) material = new Material(template);
            else
            {
                material = new Material(FindShader());
                Report = $"fallback shader {material.shader.name} (supported: {material.shader.isSupported})";
                Debug.LogWarning("Bubble Tea: runtime models use " + Report);
            }
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

        /// <summary>Adds a primitive-shaped part under the parent (no collider).</summary>
        public static Transform Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color,
            Vector3 euler = default, bool glow = false)
        {
            // Not GameObject.CreatePrimitive: it also adds a collider, and on iPhone builds the
            // unused SphereCollider is stripped out, so spheres came out as magenta blobs and the
            // rest of the model never got built. These meshes are made here instead.
            var part = new GameObject(type.ToString());
            part.AddComponent<MeshFilter>().sharedMesh = Shape(type);
            part.AddComponent<MeshRenderer>().sharedMaterial = Mat(color, glow);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.transform.localScale = scale;
            return part.transform;
        }

        // ---------------------------------------------------------------- shapes (the same sizes as Unity's primitives)

        private static readonly Dictionary<PrimitiveType, Mesh> shapes = new();

        private static Mesh Shape(PrimitiveType type)
        {
            if (shapes.TryGetValue(type, out Mesh mesh) && mesh != null) return mesh;
            // Each ring is (radius, height, normal out, normal up), top to bottom.
            var rings = new List<Vector4[]>();
            int sides = 16;
            float turn = 0f;
            bool flat = false;
            switch (type)
            {
                case PrimitiveType.Cube: // a 4-sided "cylinder" turned 45 degrees: 1 x 1 x 1
                    float corner = 0.5f * Mathf.Sqrt(2f);
                    sides = 4;
                    turn = 45f;
                    flat = true;
                    rings.Add(new[] { new Vector4(0f, 0.5f, 0f, 1f), new Vector4(corner, 0.5f, 0f, 1f) });
                    rings.Add(new[] { new Vector4(corner, 0.5f, 1f, 0f), new Vector4(corner, -0.5f, 1f, 0f) });
                    rings.Add(new[] { new Vector4(corner, -0.5f, 0f, -1f), new Vector4(0f, -0.5f, 0f, -1f) });
                    break;
                case PrimitiveType.Cylinder: // radius 0.5, 2 tall
                    rings.Add(new[] { new Vector4(0f, 1f, 0f, 1f), new Vector4(0.5f, 1f, 0f, 1f) });
                    rings.Add(new[] { new Vector4(0.5f, 1f, 1f, 0f), new Vector4(0.5f, -1f, 1f, 0f) });
                    rings.Add(new[] { new Vector4(0.5f, -1f, 0f, -1f), new Vector4(0f, -1f, 0f, -1f) });
                    break;
                case PrimitiveType.Capsule: // radius 0.5, 2 tall: two half-spheres with a tube between
                    rings.Add(Arc(0f, 90f, 0.5f));
                    rings.Add(new[] { new Vector4(0.5f, 0.5f, 1f, 0f), new Vector4(0.5f, -0.5f, 1f, 0f) });
                    rings.Add(Arc(90f, 180f, -0.5f));
                    break;
                default: // sphere, radius 0.5
                    rings.Add(Arc(0f, 180f, 0f));
                    break;
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            foreach (Vector4[] profile in rings)
            {
                for (int r = 0; r + 1 < profile.Length; r++)
                {
                    Vector4 a = profile[r], b = profile[r + 1];
                    for (int j = 0; j < sides; j++)
                    {
                        float angle0 = (turn + 360f * j / sides) * Mathf.Deg2Rad;
                        float angle1 = (turn + 360f * (j + 1) / sides) * Mathf.Deg2Rad;
                        float middle = (angle0 + angle1) * 0.5f;
                        int start = vertices.Count;
                        // Corners: top-left, top-right, bottom-right, bottom-left (seen from outside).
                        foreach ((Vector4 ring, float angle) in new[] { (a, angle0), (a, angle1), (b, angle1), (b, angle0) })
                        {
                            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                            vertices.Add(new Vector3(ring.x * c, ring.y, ring.x * s));
                            float n = flat ? middle : angle;
                            normals.Add(new Vector3(ring.z * Mathf.Cos(n), ring.w, ring.z * Mathf.Sin(n)).normalized);
                        }
                        triangles.AddRange(new[] { start, start + 1, start + 3, start + 1, start + 2, start + 3 });
                    }
                }
            }
            mesh = new Mesh { name = "ModelKit " + type };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            shapes[type] = mesh;
            return mesh;
        }

        // Rings round a half-sphere of radius 0.5 from one angle to another (0 = top, 180 = bottom), raised by lift.
        private static Vector4[] Arc(float from, float to, float lift)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt((to - from) / 15f));
            var ring = new Vector4[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float t = Mathf.Lerp(from, to, (float)i / steps) * Mathf.Deg2Rad;
                ring[i] = new Vector4(0.5f * Mathf.Sin(t), 0.5f * Mathf.Cos(t) + lift, Mathf.Sin(t), Mathf.Cos(t));
            }
            return ring;
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
            {
                Report = $"{baseMaterial.name} ({baseMaterial.shader.name}, supported: {baseMaterial.shader.isSupported})";
                Debug.LogWarning("Bubble Tea: runtime models use " + Report);
            }
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
            Report = "no models made yet";
        }
    }
}
