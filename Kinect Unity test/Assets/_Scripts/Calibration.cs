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
    
    void OnEnable()
    {
        InputManager.moveEvent += MoveCamera;
        InputManager.rotateEvent += RotateCamera;
        InputManager.zoomEvent += ZoomCamera;
    }
    
    void OnDisable()
    {
        InputManager.moveEvent -= MoveCamera;
        InputManager.rotateEvent -= RotateCamera;
        InputManager.zoomEvent -= ZoomCamera;
    }
    
    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void FixedUpdate()
    {
        mainCamera.transform.position += new Vector3(-move.x, -move.y, -zoom);
        mainCamera.transform.Rotate(new Vector3(0, 0, 0.1f * rotate));
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
}
