using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Feedback
{
    /// <summary>
    /// A burst of colorful confetti wherever a station unlocks. One particle system is
    /// built in code and reused for every burst.
    /// </summary>
    public class UnlockConfetti : MonoBehaviour
    {
        [SerializeField, Tooltip("Pieces of confetti per burst.")]
        private int pieces = 60;

        [SerializeField, Tooltip("Colors the confetti picks from.")]
        private Color[] colors =
        {
            new(1f, 0.45f, 0.55f), new(1f, 0.8f, 0.3f), new(0.45f, 0.8f, 0.5f), new(0.5f, 0.7f, 1f), new(0.8f, 0.6f, 1f),
        };

        private ParticleSystem particles;

        private void Awake() => particles = BuildParticles();

        private void OnEnable() => GameEvents.Unlocked += Burst;
        private void OnDisable() => GameEvents.Unlocked -= Burst;

        private void Burst(Vector3 position)
        {
            if (particles == null) return;
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            for (int i = 0; i < pieces; i++)
            {
                emit.position = position + Vector3.up * 0.5f;
                emit.startColor = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;
                particles.Emit(emit, 1);
            }
        }

        private ParticleSystem BuildParticles()
        {
            var go = new GameObject("Confetti Particles");
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true; // keeps simulating; emission is off, so only Burst() adds pieces
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.gravityModifier = 1.2f;
            main.maxParticles = 500;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // point the cone upward

            ParticleSystem.RotationOverLifetimeModule spin = system.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.sharedMaterial = new Material(shader);

            system.Play();
            return system;
        }
    }
}
