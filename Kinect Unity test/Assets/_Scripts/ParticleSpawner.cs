using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    private ParticleSystem particleSystem;
    [SerializeField] private int particlesPerFrame = 1;
    [SerializeField] private int maxParticles = 900;
    private ParticleSystem.MainModule main;

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        // Set main
        main = particleSystem.main;

    }

    private void Update()
    {
        // Set max particles
        // Do this in Update for easy debugging
        main.maxParticles = maxParticles;
    }

    public void SetParticles(List<Vector2> triggerPoints)
    {
        // This method will spawn particles at the trigger points. particlesPerFrame is the amount of particles to spawn per frame.
        // Recommended value is 300.
        for(int i = 0; i < particlesPerFrame; i++)
        {
            // If there are no particles to spawn, break
            if (triggerPoints.Count == 0) break;
            
            // Spawn a particle at a random trigger point. You could not do this randomly and instead spawn particles sequentially,
            // but doing it randomly will give a more interesting effect.
            Vector3 position = triggerPoints[UnityEngine.Random.Range(0, triggerPoints.Count)];
            SpawnParticle(position);
            
            // Remove the position from the list so that we don't spawn a particle at the same position
            triggerPoints.Remove(position);
        }
    }

    public void SpawnParticle(Vector3 pos)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = pos;
        particleSystem.Emit(emitParams, 1);
    }
}
