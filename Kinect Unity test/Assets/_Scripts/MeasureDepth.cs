using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using Random = System.Random;

public class MeasureDepth : MonoBehaviour
{
    //Setup
    [Header("References")]
    [SerializeField] private MultiSourceManager multiSourceManager;
    [SerializeField] private ParticleSpawner particleSpawner;
    [SerializeField] private CalibrationParticleSpawner calibrationParticleSpawner;
    
    //Declarations
    private KinectSensor sensor;
    private CoordinateMapper coordinateMapper;
    private Camera mainCamera;
    private Rect validSpaceRect;
    private bool tableView = true;
    private MultiSourceFrameReader multiSourceFrameReader;
    
    //Cutoff values
    [Header("Depth settings")] 
    [Range(1, 8)] public int accuracy = 2;
    [Range(-0.04f, 0f)] public float rangeFromSurface = -0.01f;
    
    [Header("Cutoffs")]
    [Range(-1.5f, 1.5f)] public float topCutoff = 1f;
    [Range(-1.5f, 1.5f)] public float bottomCutoff = -1f;
    [Range(-1.5f, 1.5f)] public float leftCutoff = -1f;
    [Range(-1.5f, 1.5f)] public float rightCutoff = 1f;
    [Range(0f, 3f)] public float heightCutoff = 0.5f;
    
    //Depth data
    private ushort[] depthData;
    private CameraSpacePoint[] cameraSpacePoints;
    private ColorSpacePoint[] colorSpacePoints;
    private List<ValidPoint> validPoints;
    private List<Vector2> triggerPoints;
    private Dictionary<float, float> wallDepths;
    
    //Depth Variables
    private readonly Vector2Int depthResolution = new(512, 424);
    public Texture2D depthTexture;
    
    //Input Variables
    private float rangeFromSurfaceInput;
    private float heightCutoffInput;
    
    //Other Variables
    private UIManager.State currentState;

    private void OnEnable()
    {
        UIManager.onSwitchState += SwitchView;
        
        InputManager.setDepthEvent += SetWallDepth;
        InputManager.rangeFromSurfaceEvent += SetRangeFromSurface;
        InputManager.heightCutOffEvent += SetHeightCutoff;
    }
    
    private void OnDisable()
    {
        UIManager.onSwitchState -= SwitchView;
        
        InputManager.setDepthEvent -= SetWallDepth;
        InputManager.rangeFromSurfaceEvent -= SetRangeFromSurface;
        InputManager.heightCutOffEvent -= SetHeightCutoff;
        
        multiSourceFrameReader.MultiSourceFrameArrived -= DepthToColor;
    }

    private void Awake()
    {
        // Set variables
        sensor = KinectSensor.GetDefault();
        coordinateMapper = sensor.CoordinateMapper;
        mainCamera = Camera.main;
        
        // Open the multiSourceFrameReader and subscribe to the MultiSourceFrameArrived event
        multiSourceFrameReader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color);
        multiSourceFrameReader.MultiSourceFrameArrived += DepthToColor;
        
        // The array size is the product of the x and y resolution
        int arraySize = depthResolution.x * depthResolution.y;
        
        // Initialize the arrays/dictionaries
        cameraSpacePoints = new CameraSpacePoint[arraySize];
        colorSpacePoints = new ColorSpacePoint[arraySize];
        wallDepths = new Dictionary<float, float>();
    }

    private void FixedUpdate()
    {
        SetSettings();
    }

    private void Update()
    {
        //If we're not in tableView mode, give the trigger points to the particle spawner
        if(!tableView) particleSpawner.SetParticles(triggerPoints);
        
        //If we're in tableView mode, create the valid space rect to show the table cutoffs
        if(tableView) CreateValidPointRect(validPoints);
    }
        
    private void SetRangeFromSurface(float rangeFromSurface)
    {
        //This method listens to the InputManager for the range from surface input
        rangeFromSurfaceInput = rangeFromSurface;
    }
    
    private void SetHeightCutoff(float heightCutoff)
    {
        //This method listens to the InputManager for the height cutoff input
        heightCutoffInput = heightCutoff;
    }

    
    private void SetSettings()
    {
        //This method is called in fixed update and sets the range from surface and height cutoff when there is input
        heightCutoff += 0.01f * heightCutoffInput;
        rangeFromSurface += 0.0002f * rangeFromSurfaceInput;
    }

    private void SwitchView(UIManager.State state)
    {
        //This method listens to the UIManager's state and acts accordingly
        currentState = state;
        switch (currentState)
        {
            case UIManager.State.Particles:
                //We're no longer in tableView mode
                tableView = false;
                break;
            
            case UIManager.State.Calibration:
                //We want to set the calibration particles. These particles are at the cutoffs of the table.
                SetCalibrationParticles();
                break;
            
            case UIManager.State.Table:
                //We want to clear the calibration particles
                calibrationParticleSpawner.ClearCalibrationParticles();
                
                //We want to create a texture of the valid points
                depthTexture = CreateTexture(validPoints);
                
                //We're now in tableView mode
                tableView = true;
                break;
        }
    }
    
    private void SetCalibrationParticles()
    {
        // Create a list of valid points
        List<Vector2> edgePoints = new List<Vector2>();
        
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

                //skip infinities
                if (float.IsInfinity(cameraSpacePoints[index].X) || float.IsInfinity(cameraSpacePoints[index].Y) || float.IsInfinity(cameraSpacePoints[index].Z))
                {
                    continue;
                }

                //If the point is within one of the cutoffs, add it to the edgePoints list
                if (cameraSpacePoints[index].X > leftCutoff && cameraSpacePoints[index].X < leftCutoff + 0.01f)
                {
                    edgePoints.Add(new Vector2(colorSpacePoints[index].X, colorSpacePoints[index].Y));
                    continue;
                }

                if (cameraSpacePoints[index].X > rightCutoff && cameraSpacePoints[index].X < rightCutoff + 0.01f)
                {
                    edgePoints.Add(new Vector2(colorSpacePoints[index].X, colorSpacePoints[index].Y));
                    continue;
                }
                
                if (cameraSpacePoints[index].Y < bottomCutoff && cameraSpacePoints[index].Y > bottomCutoff - 0.01f)
                {
                    edgePoints.Add(new Vector2(colorSpacePoints[index].X, colorSpacePoints[index].Y));
                    continue;
                }

                if (cameraSpacePoints[index].Y > topCutoff && cameraSpacePoints[index].Y < topCutoff + 0.01f)
                {
                    edgePoints.Add(new Vector2(colorSpacePoints[index].X, colorSpacePoints[index].Y));
                }
            }
        }
        //Spawn the calibration particles
        calibrationParticleSpawner.SpawnCalibrationParticles(edgePoints);
    }
    
    private void SetWallDepth()
    {
        if (validPoints == null)
        {
            Debug.Log("SetWallDepth: No valid points");
            return;
        }
        
        // Clear any previous wall depths
        wallDepths.Clear();
        
        // Set the wall depths
        for(int i = 0; i < validPoints.Count; i++)
        {
            wallDepths.Add(validPoints[i].index, validPoints[i].z);
        }
    }

    private void OnGUI()
    {
        if (!tableView) return;
        // Draw the validSpaceRect
        GUI.Box(validSpaceRect, "");
        GUI.color = Color.black;

        // Return and tableView if no trigger points
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

    private void DepthToColor(object sender, MultiSourceFrameArrivedEventArgs e)
    {
        // Create a list of valid points
        List<ValidPoint> validPoints = new List<ValidPoint>();
        List<Vector2> filteredPoints = new List<Vector2>();
        
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

                //skip infinities
                if (float.IsInfinity(cameraSpacePoints[index].X) || float.IsInfinity(cameraSpacePoints[index].Y) || float.IsInfinity(cameraSpacePoints[index].Z))
                    continue;

                //filter out the points that are not within the cutoffs
                if (cameraSpacePoints[index].X < leftCutoff)
                    continue;
                
                if (cameraSpacePoints[index].X > rightCutoff)
                    continue;
                
                if (cameraSpacePoints[index].Y < bottomCutoff)
                    continue;
                
                if (cameraSpacePoints[index].Y > topCutoff)
                    continue;
                
                // Create a valid point
                ValidPoint point = new ValidPoint(colorSpacePoints[index], index, cameraSpacePoints[index].Z);
                
                // Add the point to the list
                validPoints.Add(point);
            }
        }
        this.validPoints = validPoints;
        
        // Filter the valid points to trigger points
        triggerPoints = FilterToTrigger(validPoints);

    }

    private List<Vector2> FilterToTrigger(List<ValidPoint> points)
    {
        // Create a list of trigger points
        List<Vector2> triggerPoints = new List<Vector2>();
        
        //Null check
        if (points == null)
        {
            Debug.Log("FilterToTrigger: No points");
            return triggerPoints;
        }
        
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
        
        //Null check
        if (validPoints == null)
        {
            Debug.Log("CreateTexture: validPoints is null");
            return texture;
        }
        
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
    private void CreateValidPointRect(List<ValidPoint> points)
    {
        if (points == null)
        {
            Debug.Log("Points are null");
            return;
        }
        // If no points, return empty validSpaceRect
        if (points.Count == 0)
        {
            Debug.Log("No points");
            return;
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
        public float index;
        
        public ValidPoint(ColorSpacePoint colorSpace, int index, float z)
        {
            this.colorSpace = colorSpace;
            this.z = z;
            this.index = index;
        }
    }
}
