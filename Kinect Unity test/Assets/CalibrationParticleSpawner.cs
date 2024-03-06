using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrationParticleSpawner : MonoBehaviour
{
    private ParticleSystem particleSystem;
    
    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }
        
    public void SpawnCalibrationParticles(List<Vector2> points)
    {
        //clear all particles
        particleSystem.Clear();
        
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.startLifetime = Single.PositiveInfinity;
        
        foreach(Vector2 point in points)
        {
            emitParams.position = new Vector3(point.x, point.y, 0);
            particleSystem.Emit(emitParams, 1);
        }
    }
}
