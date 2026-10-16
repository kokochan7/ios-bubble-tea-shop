using BubbleTea.Extras;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Gives each drive-thru car a random body (sedan, SUV, van or pickup truck) and a random paint
    /// color when it appears, built from simple shapes and facing +Z like the car prefab. The
    /// prefab's own model is hidden; the order bubble moves up to sit above the roof.
    /// </summary>
    public class CarLook : MonoBehaviour
    {
        private enum Kind { Sedan, Suv, Van, Pickup }

        private static readonly Color[] Paints =
        {
            new(0.86f, 0.22f, 0.24f), // red
            new(0.22f, 0.42f, 0.82f), // blue
            new(0.95f, 0.95f, 0.93f), // white
            new(0.16f, 0.17f, 0.2f),  // black
            new(0.72f, 0.74f, 0.78f), // silver
            new(0.98f, 0.8f, 0.25f),  // yellow
            new(0.3f, 0.66f, 0.38f),  // green
            new(0.96f, 0.55f, 0.22f), // orange
            new(0.22f, 0.66f, 0.68f), // teal
            new(0.55f, 0.36f, 0.75f), // purple
            new(0.96f, 0.62f, 0.72f), // pink
            new(0.45f, 0.3f, 0.22f),  // brown
        };

        private static readonly Color Glass = new(0.55f, 0.75f, 0.9f);
        private static readonly Color Tire = new(0.12f, 0.12f, 0.13f);
        private static readonly Color Trim = new(0.85f, 0.86f, 0.88f);
        private static readonly Color Lamp = new(1f, 0.93f, 0.6f);
        private static readonly Color TailLamp = new(0.9f, 0.15f, 0.15f);

        private void Awake()
        {
            Transform bubble = transform.Find("OrderBubble");
            foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
            {
                if (bubble != null && part.transform.IsChildOf(bubble)) continue;
                part.enabled = false; // the prefab's own car
            }

            var look = new GameObject("Look").transform;
            look.SetParent(transform, false);
            Color paint = Paints[Random.Range(0, Paints.Length)];
            var kind = (Kind)Random.Range(0, 4);
            float height = kind switch
            {
                Kind.Suv => Suv(look, paint),
                Kind.Van => Van(look, paint),
                Kind.Pickup => Pickup(look, paint),
                _ => Sedan(look, paint),
            };
            if (bubble != null) bubble.localPosition = new Vector3(bubble.localPosition.x, Mathf.Max(bubble.localPosition.y, height + 0.55f), bubble.localPosition.z);
        }

        // Each builder returns the height of the roof.
        private static float Sedan(Transform root, Color paint)
        {
            Box(root, new Vector3(0f, 0.55f, 0f), new Vector3(1.5f, 0.5f, 2.8f), paint);
            Box(root, new Vector3(0f, 1.0f, -0.15f), new Vector3(1.3f, 0.42f, 1.35f), Glass);
            Box(root, new Vector3(0f, 1.23f, -0.15f), new Vector3(1.34f, 0.06f, 1.2f), paint);
            Box(root, new Vector3(0f, 1.0f, -0.15f), new Vector3(1.34f, 0.42f, 0.1f), paint); // middle pillar
            Ends(root, 1.5f, 2.8f, 0.5f);
            Wheels(root, 1.5f, 0.9f, 0.3f);
            return 1.26f;
        }

        private static float Suv(Transform root, Color paint)
        {
            Box(root, new Vector3(0f, 0.68f, 0f), new Vector3(1.6f, 0.7f, 2.9f), paint);
            Box(root, new Vector3(0f, 1.3f, -0.3f), new Vector3(1.48f, 0.55f, 2.0f), Glass);
            Box(root, new Vector3(0f, 1.6f, -0.3f), new Vector3(1.52f, 0.07f, 1.9f), paint);
            Box(root, new Vector3(0f, 1.3f, -0.1f), new Vector3(1.52f, 0.55f, 0.1f), paint);
            Box(root, new Vector3(-0.6f, 1.67f, -0.3f), new Vector3(0.05f, 0.05f, 1.7f), Tire); // roof rails
            Box(root, new Vector3(0.6f, 1.67f, -0.3f), new Vector3(0.05f, 0.05f, 1.7f), Tire);
            Ends(root, 1.6f, 2.9f, 0.62f);
            Wheels(root, 1.6f, 0.95f, 0.36f);
            return 1.7f;
        }

        private static float Van(Transform root, Color paint)
        {
            Box(root, new Vector3(0f, 1.05f, -0.1f), new Vector3(1.6f, 1.5f, 2.8f), paint);
            Box(root, new Vector3(0f, 0.6f, 1.38f), new Vector3(1.6f, 0.6f, 0.36f), paint); // short nose
            Box(root, new Vector3(0f, 1.35f, 1.22f), new Vector3(1.45f, 0.55f, 0.06f), Glass); // windshield
            Box(root, new Vector3(0f, 1.4f, -0.2f), new Vector3(1.62f, 0.4f, 2.1f), Glass); // side windows
            Box(root, new Vector3(0f, 0.75f, -0.1f), new Vector3(1.62f, 0.08f, 2.6f), Trim); // stripe
            Ends(root, 1.6f, 3.0f, 0.55f);
            Wheels(root, 1.6f, 0.95f, 0.33f);
            return 1.8f;
        }

        private static float Pickup(Transform root, Color paint)
        {
            Box(root, new Vector3(0f, 0.62f, 0f), new Vector3(1.6f, 0.6f, 3.0f), paint);
            Box(root, new Vector3(0f, 1.18f, 0.35f), new Vector3(1.5f, 0.55f, 1.1f), Glass); // cab
            Box(root, new Vector3(0f, 1.47f, 0.35f), new Vector3(1.54f, 0.06f, 1.0f), paint);
            Box(root, new Vector3(0f, 1.18f, -0.2f), new Vector3(1.54f, 0.55f, 0.08f), paint); // back of the cab
            // The open bed: low walls round a dark floor.
            Box(root, new Vector3(0f, 0.93f, -0.9f), new Vector3(1.4f, 0.02f, 1.1f), Tire);
            Box(root, new Vector3(-0.76f, 1.05f, -0.85f), new Vector3(0.08f, 0.26f, 1.25f), paint);
            Box(root, new Vector3(0.76f, 1.05f, -0.85f), new Vector3(0.08f, 0.26f, 1.25f), paint);
            Box(root, new Vector3(0f, 1.05f, -1.46f), new Vector3(1.6f, 0.26f, 0.08f), paint);
            Ends(root, 1.6f, 3.0f, 0.55f);
            Wheels(root, 1.6f, 1.0f, 0.36f);
            return 1.5f;
        }

        // Bumpers, headlights and tail lights.
        private static void Ends(Transform root, float width, float length, float y)
        {
            float half = length * 0.5f;
            Box(root, new Vector3(0f, y - 0.2f, half), new Vector3(width, 0.14f, 0.12f), Trim);
            Box(root, new Vector3(0f, y - 0.2f, -half), new Vector3(width, 0.14f, 0.12f), Trim);
            for (int side = -1; side <= 1; side += 2)
            {
                ModelKit.Part(root, PrimitiveType.Sphere, new Vector3(side * width * 0.32f, y, half - 0.02f), new Vector3(0.24f, 0.15f, 0.08f), Lamp, glow: true);
                Box(root, new Vector3(side * width * 0.36f, y, -half + 0.02f), new Vector3(0.22f, 0.12f, 0.06f), TailLamp);
            }
        }

        private static void Wheels(Transform root, float width, float axle, float radius)
        {
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var position = new Vector3(x * (width * 0.5f - 0.08f), radius, z * axle);
                    ModelKit.Part(root, PrimitiveType.Cylinder, position, new Vector3(radius * 2f, 0.12f, radius * 2f), Tire, new Vector3(0f, 0f, 90f));
                    ModelKit.Part(root, PrimitiveType.Cylinder, position + new Vector3(x * 0.1f, 0f, 0f), new Vector3(radius * 0.9f, 0.02f, radius * 0.9f), Trim, new Vector3(0f, 0f, 90f));
                }
            }
        }

        private static void Box(Transform root, Vector3 position, Vector3 size, Color color) =>
            ModelKit.Part(root, PrimitiveType.Cube, position, size, color);
    }
}
