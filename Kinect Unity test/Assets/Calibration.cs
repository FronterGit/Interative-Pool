using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calibration : MonoBehaviour
{
    [SerializeField] private GameObject particleSpawner;
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void FixedUpdate()
    {
        //Change the X and Y localscale of the particle spawner with the WASD keys
        if (Input.GetKey(KeyCode.W))
        {
            particleSpawner.transform.localScale -= new Vector3(0, 0.01f, 0);
        }
        if (Input.GetKey(KeyCode.S))
        {
            particleSpawner.transform.localScale += new Vector3(0, 0.01f, 0);
        }
        if (Input.GetKey(KeyCode.A))
        {
            particleSpawner.transform.localScale -= new Vector3(0.01f, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            particleSpawner.transform.localScale += new Vector3(0.01f, 0, 0);
        }
        
        //Change the X and Y location of the main camera with the arrow keys
        if (Input.GetKey(KeyCode.UpArrow))
        {
            mainCamera.transform.position += new Vector3(0, 1f, 0);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            mainCamera.transform.position += new Vector3(0, 1f, 0);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            mainCamera.transform.position -= new Vector3(1f, 0, 0);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            mainCamera.transform.position -= new Vector3(1f, 0, 0);
        }
    }
}
