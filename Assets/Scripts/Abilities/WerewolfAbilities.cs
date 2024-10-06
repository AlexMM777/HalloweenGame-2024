using System.Collections;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class WerewolfAbilities : MonoBehaviour
{
    public PostProcessVolume postProcessingVolume;
    public float transitionDuration = 0.1f;
    private bool enableScent;
    private Coroutine currentCoroutine = null;

    public Transform keyItem; // Reference to the key item Transform
    public LineRenderer lineRenderer;
    public ParticleSystem smokeParticleSystem; // Reference to the Smoke Particle System
    public float emissionRate = 1;

    void Start()
    {
        postProcessingVolume.weight = 0;
        postProcessingVolume.enabled = false;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2; // Two points: start and end
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Enables Scent vision
        if (Input.GetKeyDown(KeyCode.Z))
        {
            enableScent = !enableScent;
            postProcessingVolume.enabled = enableScent;
            lineRenderer.enabled = !lineRenderer.enabled;

            // Stop any existing transition
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(TransitionPostProcessWeight(enableScent ? 1 : 0));
        }

        if (enableScent)
        {
            // Set the start point to the position of the GameObject this script is attached to
            lineRenderer.SetPosition(0, transform.GetChild(0).position);
            // Set the end point to the key item's position
            lineRenderer.SetPosition(1, keyItem.position);
            EmitSmokeAlongLine();
        }
    }

    void EmitSmokeAlongLine()
    {
        if (lineRenderer.positionCount < 2) return; // Ensure there are at least 2 points

        // Emit particles along the length of the line
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            Vector3 start = lineRenderer.GetPosition(i);
            Vector3 end = lineRenderer.GetPosition(i + 1);
            float distance = Vector3.Distance(start, end);

            // Calculate how many particles to emit based on the distance and desired emission rate
            int particleCount = Mathf.RoundToInt(distance * emissionRate * 2.0f * Time.deltaTime);

            // Emit the particles evenly along the segment
            for (int j = 0; j < particleCount; j++)
            {
                // Calculate the position for the emitted particle along the line
                float lerpFactor = (j +0.5f) / (float)(particleCount); // Avoid emitting at start and end
                Debug.Log("Position:" + start + "|||" + end);
                Vector3 position = Vector3.Lerp(start, end, lerpFactor);
                EmitSmokeParticle(position);
            }
        }
    }

    void EmitSmokeParticle(Vector3 position)
    {
        // Create a temporary particle instance and set its position
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = position;
        emitParams.velocity = Vector3.zero; // Adjust velocity as needed

        // Emit the particle
        smokeParticleSystem.Emit(emitParams, 1);
    }

    IEnumerator TransitionPostProcessWeight(float targetWeight)
    {
        float startWeight = postProcessingVolume.weight;
        float elapsedTime = 0;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            // Lerp the weight between start and target values
            postProcessingVolume.weight = Mathf.Lerp(startWeight, targetWeight, (elapsedTime / transitionDuration)*5);
            yield return null; // Wait for the next frame
        }

        // Ensure the final weight is exactly the target weight
        postProcessingVolume.weight = targetWeight;
    }
}
