using UnityEngine;

/// <summary>
/// Adds a subtle particle effect above each elemental piece, colored by element.
/// Fire = orange embers, Earth = brown-gold dust, Lightning = blue sparks.
/// Attach to each piece GameObject alongside PieceMove.
/// </summary>
public class ElementParticleUI : MonoBehaviour
{
    private ParticleSystem ps;
    private PieceMove pieceMove;
    private bool initialized = false;

    void Start()
    {
        pieceMove = GetComponent<PieceMove>();
    }

    void LateUpdate()
    {
        if (!initialized && pieceMove != null && pieceMove.elementalPiece != null)
        {
            CreateParticleEffect(pieceMove.elementalPiece.elementId);
            initialized = true;
        }
    }

    private void CreateParticleEffect(int elementId)
    {
        // Create child GameObject for particle system
        GameObject particleObj = new GameObject("ElementParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = new Vector3(0, 0.6f, 0);
        particleObj.transform.localRotation = Quaternion.identity;

        ps = particleObj.AddComponent<ParticleSystem>();

        // Stop the auto-play so we can configure first
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Color baseColor = GetElementColor(elementId);

        // Main module
        var main = ps.main;
        main.startLifetime = 1.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.maxParticles = 25;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = baseColor;
        main.gravityModifier = GetGravityForElement(elementId);
        main.playOnAwake = false;

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 6f;

        // Shape - small sphere around piece top
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        // Color over lifetime - fade out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(baseColor, 0f),
                new GradientColorKey(GetFadeColor(elementId), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Size over lifetime - shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Velocity over lifetime for element-specific movement
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        SetElementVelocity(velocityOverLifetime, elementId);

        // Renderer - use default particle material
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Create a simple particle material
        Material particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (particleMat != null)
        {
            particleMat.SetColor("_Color", baseColor);
            particleMat.SetFloat("_Mode", 1); // Additive
            renderer.material = particleMat;
        }

        // Start playing
        ps.Play();
    }

    private Color GetElementColor(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE:
                return new Color(1f, 0.45f, 0.1f, 1f);
            case ChessConstants.ELEMENT_EARTH:
                return new Color(0.7f, 0.55f, 0.25f, 1f);
            case ChessConstants.ELEMENT_LIGHTNING:
                return new Color(0.4f, 0.7f, 1f, 1f);
            default:
                return Color.white;
        }
    }

    private Color GetFadeColor(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE:
                return new Color(1f, 0.2f, 0f, 1f); // Fade to red
            case ChessConstants.ELEMENT_EARTH:
                return new Color(0.5f, 0.4f, 0.15f, 1f); // Fade to dark brown
            case ChessConstants.ELEMENT_LIGHTNING:
                return new Color(0.8f, 0.9f, 1f, 1f); // Fade to white-blue
            default:
                return Color.white;
        }
    }

    private float GetGravityForElement(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE:
                return -0.15f; // Float upward (embers rise)
            case ChessConstants.ELEMENT_EARTH:
                return 0.1f; // Settle downward (dust falls)
            case ChessConstants.ELEMENT_LIGHTNING:
                return 0f; // Float freely (sparks)
            default:
                return 0f;
        }
    }

    private void SetElementVelocity(ParticleSystem.VelocityOverLifetimeModule vel, int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE:
                // Gentle upward drift
                vel.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
                vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
                break;
            case ChessConstants.ELEMENT_EARTH:
                // Slow outward drift then settle
                vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
                vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
                break;
            case ChessConstants.ELEMENT_LIGHTNING:
                // Quick erratic movement
                vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.2f);
                vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
                break;
        }
    }
}
