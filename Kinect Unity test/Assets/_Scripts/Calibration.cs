using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calibration : MonoBehaviour
{
    [SerializeField] private GameObject particleSpawner;
    private Camera mainCamera;
    
    private Vector2 move;
    private float rotate;
    private float zoom;
    
    private Vector2 scaleParticlesInput;
    
    void OnEnable()
    {
        InputManager.moveEvent += MoveCamera;
        InputManager.rotateEvent += RotateCamera;
        InputManager.zoomEvent += ZoomCamera;
        InputManager.scaleParticlesEvent += ScaleParticles;
    }
    
    void OnDisable()
    {
        InputManager.moveEvent -= MoveCamera;
        InputManager.rotateEvent -= RotateCamera;
        InputManager.zoomEvent -= ZoomCamera;
        InputManager.scaleParticlesEvent -= ScaleParticles;
    }
    
    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void FixedUpdate()
    {
        mainCamera.transform.position += new Vector3(-move.x, -move.y, -zoom);
        mainCamera.transform.Rotate(new Vector3(0, 0, 0.1f * rotate));

        particleSpawner.transform.localScale += new Vector3(0.01f * scaleParticlesInput.x, 0.01f * scaleParticlesInput.y, 0);
    }

    private void MoveCamera(Vector2 move)
    {
        this.move = move;
    }
    
    private void RotateCamera(float rotate)
    {
        this.rotate = rotate;
    }
    
    private void ZoomCamera(float zoom)
    {
        this.zoom = zoom;
    }
    
    private void ScaleParticles(Vector2 scaleInput)
    {
        scaleParticlesInput = scaleInput;
    }
}
