using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using Unity.VisualScripting;

public class MeasureDepth : MonoBehaviour
{
    // Setup
    [Header("References")]
    [SerializeField] private MultiSourceManager multiSourceManager;
    [SerializeField] private ParticleSpawner particleSpawner;
    [SerializeField] private GameObject viewer;
    
    // Declarations
    private KinectSensor sensor;
    private CoordinateMapper coordinateMapper;
    private Camera mainCamera;
    private Rect validSpaceRect;
    private bool debug = true;
    
    // Cutoff values
    [Header("Wall Depth")]
    [Range(0, 10.0f)] public float depthCutoff = 0.1f;
    [Range(0, 1.0f)] public float depthSensitivity = 0.1f;
    [Range(-10f, 10f)] public float wallDepth = -10f;
    [Range(-0.02f, 0f)] public float sensitivity = -0.01f;
    
    [Header("Cutoffs")]
    [Range(-1f, 1f)] public float topCutoff = 1f;
    [Range(-1f, 1f)] public float bottomCutoff = -1f;
    [Range(-1f, 1f)] public float leftCutoff = -1f;
    [Range(-1f, 1f)] public float rightCutoff = 1f;
    
    // Depth data
    private ushort[] depthData;
    private CameraSpacePoint[] cameraSpacePoints;
    private ColorSpacePoint[] colorSpacePoints;
    private List<ValidPoint> validPoints;
    private List<Vector2> triggerPoints;

    private List<Vector3> wallPoints;
    private Dictionary<float, float> wallDepths;
    private bool once = false;
    
    // Depth Variables
    private readonly Vector2Int depthResolution = new Vector2Int(512, 424);
    public Texture2D depthTexture;
    
    private void Awake()
    {
        // Set variables
        sensor = KinectSensor.GetDefault();
        coordinateMapper = sensor.CoordinateMapper;
        mainCamera = Camera.main;
        
        // The array size is the product of the x and y resolution
        int arraySize = depthResolution.x * depthResolution.y;
        
        // Initialize the arrays
        cameraSpacePoints = new CameraSpacePoint[arraySize];
        colorSpacePoints = new ColorSpacePoint[arraySize];
        
        wallPoints = new List<Vector3>();
        wallDepths = new Dictionary<float, float>();
    }

    private void Update()
    {
        // Get the valid points
        validPoints = DepthToColor();
        
        // Filter the valid points to trigger points
        triggerPoints = FilterToTrigger(validPoints);
        
        // When space is pressed, create the validSpaceRect and texture
        if (Input.GetKeyDown(KeyCode.Space))
        {
            debug = !debug;
            viewer.SetActive(debug);
            
            if (debug)
            {
                CreateRect(validPoints);
                depthTexture = CreateTexture(validPoints);
            }
            Debug.Log(triggerPoints.Count);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            SetWallDepth();
        }
        
        if(!debug)
        {
            SpawnParticles(triggerPoints);
        }

    }

    private void SetWallDepth()
    {
        validPoints = DepthToColor(true);
        wallPoints.Clear();
        wallDepths.Clear();
        for(int i = 0; i < validPoints.Count; i++)
        {
            wallPoints.Add(new Vector3(validPoints[i].colorSpace.X, validPoints[i].colorSpace.Y, validPoints[i].z));
            wallDepths.Add(validPoints[i].pos, validPoints[i].z);
        }
    }

    private void SpawnParticles(List<Vector2> triggerPoints)
    {
        // If no trigger points, return
        if (triggerPoints.Count == 0)
        {
            return;
        }
        
        foreach (Vector2 point in triggerPoints)
        {
            particleSpawner.SpawnParticle(point);
        }
    }

    private void OnGUI()
    {
        if (!debug) return;
        
        // Draw the validSpaceRect
        GUI.Box(validSpaceRect, "");

        // Return and debug if no trigger points
        if (triggerPoints == null)
        {
            Debug.Log("No trigger points");
            return;
        }
        
        // Draw the trigger points
        foreach (Vector2 point in triggerPoints)
        {
            Rect rect = new Rect(point.x, point.y, 10, 10);
            GUI.Box(rect, "");
        }
    }

    private List<ValidPoint> DepthToColor(bool setWallDepth = false)
    {
        // Create a list of valid points
        List<ValidPoint> validPoints = new List<ValidPoint>();
        
        // Get the depth data from the MultiSourceManager
        depthData = multiSourceManager.GetDepthData();
        
        // Map the depth data to the color space
        coordinateMapper.MapDepthFrameToCameraSpace(depthData, cameraSpacePoints);
        coordinateMapper.MapDepthFrameToColorSpace(depthData, colorSpacePoints);
        
        // We don't want to check every single point, so we skip some
        int skip = 8;
        
        // Set the valid points
        for(int x = 0; x < depthResolution.x / skip; x++)
        {
            for(int y = 0; y < depthResolution.y / skip; y++)
            {
                // Get the index
                int index = (y * depthResolution.x) + x;
                index *= skip;

                if (!setWallDepth)
                {
                    // If the point is inside the cutoffs, continue
                    if (cameraSpacePoints[index].X < leftCutoff)
                        continue;
                
                    if (cameraSpacePoints[index].X > rightCutoff)
                        continue;
                
                    if (cameraSpacePoints[index].Y < bottomCutoff)
                        continue;
                
                    if (cameraSpacePoints[index].Y > topCutoff)
                        continue;
                }
                
                // Create a valid point
                ValidPoint point = new ValidPoint(colorSpacePoints[index], index, cameraSpacePoints[index].Z);

                // // // If the point is within the wall depth, set the withinWallDepth to true
                // if (cameraSpacePoints[index].Z >= wallDepth)
                // {
                //     point.withinWallDepth = true;
                // }
                
                    // if(wallDepths.TryGetValue(point.pos, out var depth))
                    // {
                    //     if(depth >= point.z)
                    //     {
                    //         point.withinWallDepth = true;
                    //     }
                    // }

                // Add the point to the list
                validPoints.Add(point);
            }
        }
        return validPoints;
    }

    private List<Vector2> FilterToTrigger(List<ValidPoint> points)
    {
        // Create a list of trigger points
        List<Vector2> triggerPoints = new List<Vector2>();
        
        // For each valid point
        foreach (ValidPoint point in points)
        {
            //if(point.withinWallDepth) Debug.Log("Within wall depth");
            // If the point is not within the wall depth
            if (!point.withinWallDepth)
            {
                // // If the point is within the wall depth sensitivity
                // if(point.z < wallDepth * depthSensitivity && point.z > depthCutoff)
                // {
                //     // Add the point to the trigger points
                //     Vector2 screenPoint = ScreenToCamera(new Vector2(point.colorSpace.X, point.colorSpace.Y));
                //     triggerPoints.Add(screenPoint);
                // }


                //else Debug.Log(point.pos);
            }
            
            if (wallDepths.TryGetValue(point.pos, out var depth))
            {
                if(depth + sensitivity > point.z)
                {
                    Vector2 screenPoint = ScreenToCamera(new Vector2(point.colorSpace.X, point.colorSpace.Y));
                    triggerPoints.Add(screenPoint);
                }
                //else Debug.Log("Depth: " + depth + " Point: " + point.z);
            }
        }
        return triggerPoints;
    }

    private Texture2D CreateTexture(List<ValidPoint> validPoints)
    {
        // Create a texture
        Texture2D texture = new Texture2D(1920, 1080, TextureFormat.Alpha8, false);
        
        // Set the texture to clear
        for(int x = 0; x < 1920; x++)
        {
            for(int y = 0; y < 1080; y++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        // Set the valid points to black
        foreach (ValidPoint point in validPoints)
        {
            texture.SetPixel((int)point.colorSpace.X, (int)point.colorSpace.Y, Color.black);
        }
        
        // Apply the texture
        texture.Apply();
        return texture;
    }

    #region Rect Creation
    private Rect CreateRect(List<ValidPoint> points)
    {
        // If no points, return empty validSpaceRect
        if (points.Count == 0)
        {
            Debug.Log("No points");
            return new Rect();
        }
        
        // Get top left and bottom right
        Vector2 topLeft = GetTopLeft(points);
        Vector2 bottomRight = GetBottomRight(points);
        
        // Translate to viewport
        Vector2 screenTopLeft = ScreenToCamera(topLeft);
        Vector2 screenBottomRight = ScreenToCamera(bottomRight);
        
        // Rect dimensions
        int width = (int)(screenBottomRight.x - screenTopLeft.x);
        int height = (int)(screenBottomRight.y - screenTopLeft.y);
        
        // Create the validSpaceRect
        Vector2 size = new Vector2(width, height);
        validSpaceRect = new Rect(screenTopLeft, size);
        
        return validSpaceRect;
    }

    // Method that gets the top left point of the valid points
    private Vector2 GetTopLeft(List<ValidPoint> points)
    {
        Vector2 topLeft = new Vector2(float.MaxValue, float.MaxValue);
        foreach(ValidPoint point in points)
        {
            // Top most left
            if (point.colorSpace.X < topLeft.x)
            {
                topLeft.x = point.colorSpace.X;
            }
            
            // Left most top
            if (point.colorSpace.Y < topLeft.y)
            {
                topLeft.y = point.colorSpace.Y;
            }
            
        }
        return topLeft;
    }
    
    // Method that gets the bottom right point of the valid points
    private Vector2 GetBottomRight(List<ValidPoint> points)
    {
        Vector2 bottomRight = new Vector2(float.MinValue, float.MinValue);
        foreach(ValidPoint point in points)
        {
            // Bottom most right
            if (point.colorSpace.X > bottomRight.x)
            {
                bottomRight.x = point.colorSpace.X;
            }
            
            // Right most bottom
            if (point.colorSpace.Y > bottomRight.y)
            {
                bottomRight.y = point.colorSpace.Y;
            }
        }
        return bottomRight;
    }
    
    // Method that translates the screen position to camera position
    private Vector2 ScreenToCamera(Vector2 screenPosition)
    {
        Vector2 normalizedScreenPoint = 
            new Vector2(Mathf.InverseLerp(0, 1920, screenPosition.x), Mathf.InverseLerp(0, 1080, screenPosition.y));
        
        Vector2 screenPoint = new Vector2(normalizedScreenPoint.x * mainCamera.pixelWidth, normalizedScreenPoint.y * mainCamera.pixelHeight);
        
        return screenPoint;
    }
    
    #endregion
    
    public class ValidPoint
    {
        public ColorSpacePoint colorSpace;
        public float z;
        public float wallDepth;
        public bool withinWallDepth;
        
        public int screenX;
        public int screenY;
        public float pos;
        
        public ValidPoint(ColorSpacePoint colorSpace, int index, float z)
        {
            this.colorSpace = colorSpace;
            this.z = z;
            this.pos = index;
        }
    }
}
