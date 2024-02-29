using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using Unity.VisualScripting;
using UnityEngine.Serialization;

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
    [Header("Depth settings")] 
    [Range(1, 8)] public int accuracy = 2;
    [Range(-0.03f, 0f)] public float rangeFromSurface = -0.01f;
    
    [Header("Cutoffs")]
    [Range(-1.5f, 1.5f)] public float topCutoff = 1f;
    [Range(-1.5f, 1.5f)] public float bottomCutoff = -1f;
    [Range(-1.5f, 1.5f)] public float leftCutoff = -1f;
    [Range(-1.5f, 1.5f)] public float rightCutoff = 1f;
    [Range(0f, 3f)] public float heightCutoff = 0.5f;
    
    // Depth data
    private ushort[] depthData;
    private CameraSpacePoint[] cameraSpacePoints;
    private ColorSpacePoint[] colorSpacePoints;
    private List<ValidPoint> validPoints;
    private List<Vector2> triggerPoints;
    private Dictionary<float, float> wallDepths;
    
    // Depth Variables
    private readonly Vector2Int depthResolution = new(512, 424);
    public Texture2D depthTexture;
    
    // Custom rect variables
    private enum Corner
    {
        TopLeft,
        TopRight,
        BottomRight
    }
    private Corner corner;
    private Rect customRect;
    
    private float highestX;
    private float lowestX;
    private float highestY;
    private float lowestY;
    
    private void Awake()
    {
        // Set variables
        sensor = KinectSensor.GetDefault();
        coordinateMapper = sensor.CoordinateMapper;
        mainCamera = Camera.main;
        
        // The array size is the product of the x and y resolution
        int arraySize = depthResolution.x * depthResolution.y;
        
        // Initialize the arrays/dictionaries
        cameraSpacePoints = new CameraSpacePoint[arraySize];
        colorSpacePoints = new ColorSpacePoint[arraySize];
        wallDepths = new Dictionary<float, float>();
    }

    private void Update()
    {
        // Get the valid points
        validPoints = DepthToColor();
        
        // Filter the valid points to trigger points
        triggerPoints = FilterToTrigger(validPoints);
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Toggle debug
            debug = !debug;
            
            // Toggle the viewer
            viewer.SetActive(debug);
            
            // If debug, create the rect and texture
            if (debug)
            {
                
                depthTexture = CreateTexture(validPoints);
            }
        }
        CreateValidPointRect(validPoints);
        if (Input.GetKeyDown(KeyCode.A))
        {
            SetWallDepth();
        }
        
        //on mouse click, create custom rect
        if (Input.GetMouseButtonDown(0))
        {
            CreateCustomRect();
        }
        
        // on right mouse click, reset corner
        if (Input.GetMouseButtonDown(1))
        {
            corner = Corner.TopLeft;
            
            //Set all the cutoffs to 3
            leftCutoff = -3;
            rightCutoff = 3;
            topCutoff = 3;
            bottomCutoff = -3;
        }
        
        if(!debug)
        {
            SpawnParticles(triggerPoints);
        }

    }

    private void SetWallDepth()
    {
        // Get the valid points
        validPoints = DepthToColor(true);
        
        // Clear any previous wall depths
        wallDepths.Clear();
        
        // Set the wall depths
        for(int i = 0; i < validPoints.Count; i++)
        {
            wallDepths.Add(validPoints[i].index, validPoints[i].z);
        }
    }

    private void SpawnParticles(List<Vector2> triggerPoints)
    {
        // If no trigger points, return
        if (triggerPoints.Count == 0)
        {
            return;
        }
        
        // For each trigger point, tell the particle spawner to spawn a particle at the trigger point
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
        
        // Draw the customRect
        GUI.color = Color.blue;
        GUI.Box(customRect, "");
        GUI.color = Color.black;

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
        
        // Map the depth data to the space
        coordinateMapper.MapDepthFrameToCameraSpace(depthData, cameraSpacePoints);
        coordinateMapper.MapDepthFrameToColorSpace(depthData, colorSpacePoints);
        
        // Set the valid points
        for(int x = 0; x < depthResolution.x / accuracy; x++)
        {
            for(int y = 0; y < depthResolution.y / accuracy; y++)
            {
                // Get the index
                int index = (y * depthResolution.x) + x;
                index *= accuracy;

                // If we're not setting the wall depth, check if the point is within the cutoffs
                if (!setWallDepth)
                {
                    if (cameraSpacePoints[index].X < leftCutoff)
                        continue;
                    
                    if (cameraSpacePoints[index].X > rightCutoff)
                        continue;
                    
                    if (cameraSpacePoints[index].Y < bottomCutoff)
                        continue;
                    
                    if (cameraSpacePoints[index].Y > topCutoff)
                        continue;
                    
                    //skip infinities
                    if (float.IsInfinity(cameraSpacePoints[index].X) || float.IsInfinity(cameraSpacePoints[index].Y) || float.IsInfinity(cameraSpacePoints[index].Z))
                    {
                        continue;
                    }
                    SetHighestAndLowest(cameraSpacePoints[index]);
                }
                
                // Create a valid point
                ValidPoint point = new ValidPoint(colorSpacePoints[index], index, cameraSpacePoints[index].Z);
                
                // Add the point to the list
                validPoints.Add(point);
            }
        }
        return validPoints;
    }
    
    private void SetHighestAndLowest(CameraSpacePoint point)
    {
        // Remember the highest and lowest x and y values
        float oldHighestX = highestX;
        float oldLowestX = lowestX;
        float oldHighestY = highestY;
        float oldLowestY = lowestY;
        
        if (point.X > highestX)
        {
            highestX = point.X;
        }
        if (point.X < lowestX)
        {
            lowestX = point.X;
        }
        if (point.Y > highestY)
        {
            highestY = point.Y;
        }
        if (point.Y < lowestY)
        {
            lowestY = point.Y;
        }
    }

    private List<Vector2> FilterToTrigger(List<ValidPoint> points)
    {
        // Create a list of trigger points
        List<Vector2> triggerPoints = new List<Vector2>();
        
        // For each valid point
        foreach (ValidPoint point in points)
        {
            // If the point has a wall depth, look up its wall depth via the index
            if (wallDepths.TryGetValue(point.index, out var depth))
            {
                // If the point has a higher z value than the wall depth
                if(depth + rangeFromSurface > point.z && point.z > heightCutoff)
                {
                    // Add the point to the trigger points
                    Vector2 screenPoint = ScreenToCamera(new Vector2(point.colorSpace.X, point.colorSpace.Y));
                    triggerPoints.Add(screenPoint);
                }
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
    private Rect CreateValidPointRect(List<ValidPoint> points)
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

    private Rect CreateCustomRect()
    {
        switch(corner)
        {
            case Corner.TopLeft:
                customRect = new Rect(Input.mousePosition.x, -Input.mousePosition.y + 1080, 1, 1);
                
                //Set the left cutoff
                leftCutoff = CameraToScreen(customRect.position).x;
                
                //Set the top cutoff
                topCutoff = -CameraToScreen(customRect.position).y;
                
                corner = Corner.TopRight;
                break;
            case Corner.TopRight:
                customRect.width = Input.mousePosition.x - customRect.x;
                
                //Set the right cutoff
                rightCutoff = CameraToScreen(new Vector2(Input.mousePosition.x, customRect.position.y)).x;
                
                corner = Corner.BottomRight;
                break;
            case Corner.BottomRight:
                customRect.height = -1 * (customRect.position.y - (-Input.mousePosition.y + 1080));
                
                //Set the bottom cutoff
                bottomCutoff = -CameraToScreen(new Vector2(customRect.position.x, -Input.mousePosition.y + 1080)).y;
                
                corner = Corner.TopLeft;
                break;
        }
        //debug log all the cutoffs
        Debug.Log("Left: " + leftCutoff + " Right: " + rightCutoff + " Top: " + topCutoff + " Bottom: " + bottomCutoff);
    
        return customRect;
    }
    
    // private Rect CreateCustomRect()
    // {
    //     Vector2 mousePos;
    //     switch(corner)
    //     {
    //         case Corner.TopLeft:
    //             customRect = new Rect(Input.mousePosition.x, -Input.mousePosition.y + 1080, 1, 1);
    //             
    //             //Set the left cutoff
    //             mousePos = MouseToDepth(Input.mousePosition);
    //             leftCutoff = CameraToScreen(mousePos).x;
    //             
    //             //Set the top cutoff
    //             mousePos = MouseToDepth(Input.mousePosition);
    //             topCutoff = -CameraToScreen(mousePos).y;
    //             
    //             corner = Corner.TopRight;
    //             break;
    //         case Corner.TopRight:
    //             customRect.width = Input.mousePosition.x - customRect.x;
    //             
    //             //Set the right cutoff
    //             mousePos = MouseToDepth(Input.mousePosition);
    //             rightCutoff = CameraToScreen(mousePos).x;
    //             
    //             corner = Corner.BottomRight;
    //             break;
    //         case Corner.BottomRight:
    //             customRect.height = -1 * (customRect.position.y - (-Input.mousePosition.y + 1080));
    //             
    //             //Set the bottom cutoff
    //             mousePos = MouseToDepth(Input.mousePosition);
    //             bottomCutoff = -CameraToScreen(new Vector2(customRect.position.x, -mousePos.y + 1080)).y;
    //             
    //             corner = Corner.TopLeft;
    //             break;
    //     }
    //     //debug log all the cutoffs
    //     Debug.Log("Left: " + leftCutoff + " Right: " + rightCutoff + " Top: " + topCutoff + " Bottom: " + bottomCutoff);
    //
    //     return customRect;
    // }

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
    
    // Method that translates the mouse position on this resolution to the depth resolution
    private Vector2 MouseToDepth(Vector2 mousePosition)
    {
        Vector2 normalizedMousePoint = 
            new Vector2(Mathf.InverseLerp(0, 1920, mousePosition.x), Mathf.InverseLerp(0, 1080, mousePosition.y));
        
        Vector2 depthPoint = new Vector2(normalizedMousePoint.x * 1000, normalizedMousePoint.y * 1000);
        
        return depthPoint;
    }
    
    // Method that translates the camera position to screen position
    private Vector2 CameraToScreen(Vector2 cameraPosition)
    {
        // Highest X: 1.201952 Lowest X: -1.239277 Highest Y: 1.074169 Lowest Y: -0.9924252
        // Normalize the camera position based on the camera's pixel width and height
        Vector2 normalizedCameraPoint = new Vector2(cameraPosition.x / mainCamera.pixelWidth, cameraPosition.y / mainCamera.pixelHeight);
    
        // Convert the normalized camera position to screen coordinates
        //Vector2 screenPoint = new Vector2(Mathf.Lerp(-1.239277f, 1.201952f, normalizedCameraPoint.x), Mathf.Lerp(-0.9924252f, 1.074169f, normalizedCameraPoint.y));
        Vector2 screenPoint = new Vector2(Mathf.Lerp(-1.6f, 1.5f, normalizedCameraPoint.x), Mathf.Lerp(-.9f, .8f, normalizedCameraPoint.y));
        
        return screenPoint;
    }
    
    #endregion
    
    public class ValidPoint
    {
        public ColorSpacePoint colorSpace;
        public float z;
        public float index;
        
        public ValidPoint(ColorSpacePoint colorSpace, int index, float z)
        {
            this.colorSpace = colorSpace;
            this.z = z;
            this.index = index;
        }
    }
}
