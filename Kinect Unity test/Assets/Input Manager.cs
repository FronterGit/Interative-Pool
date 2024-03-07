using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // INPUT MANAGER //
    // 1. Listen for input events
    // 2. Raise events when input is detected
    
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference rotateAction;
    [SerializeField] private InputActionReference zoomAction;
    [SerializeField] private InputActionReference switchAction;
    [SerializeField] private InputActionReference setDepthAction;
    [SerializeField] private InputActionReference rangeFromSurfaceAction;
    [SerializeField] private InputActionReference heightCutOffAction;
    [SerializeField] private InputActionReference showControlsAction;
    [SerializeField] private InputActionReference scaleParticlesAction;
    
    private Vector2 move;
    private float rotate;
    private float zoom;
    private float rangeFromSurface;
    private float heightCutOff;
    private Vector2 scaleParticles;

    public static event System.Action<Vector2> moveEvent;
    public static event System.Action<float> rotateEvent;
    public static event System.Action<float> zoomEvent;
    public static event System.Action<float> rangeFromSurfaceEvent;
    public static event System.Action<float> heightCutOffEvent;
    
    public static event System.Action<Vector2> scaleParticlesEvent;
    
    
    public static event System.Action switchEvent;
    public static event System.Action setDepthEvent;
    public static event System.Action showControlsEvent;
    
    private void OnEnable()
    {
        moveAction.action.performed += OnMove;
        moveAction.action.canceled += OnMove;
        
        rotateAction.action.performed += OnRotate;
        rotateAction.action.canceled += OnRotate;
        
        zoomAction.action.performed += OnZoom;
        zoomAction.action.canceled += OnZoom;
        
        rangeFromSurfaceAction.action.performed += OnRangeFromSurface;
        rangeFromSurfaceAction.action.canceled += OnRangeFromSurface;
        
        heightCutOffAction.action.performed += OnHeightCutoff;
        heightCutOffAction.action.canceled += OnHeightCutoff;
        
        scaleParticlesAction.action.performed += OnScaleParticles;
        scaleParticlesAction.action.canceled += OnScaleParticles;
        
        switchAction.action.performed += OnSwitch;
        
        setDepthAction.action.performed += OnSetDepth;
        
        showControlsAction.action.performed += OnShowControls;
    }
    
    private void OnDisable()
    {
        moveAction.action.performed -= OnMove;
        moveAction.action.canceled -= OnMove;
        
        rotateAction.action.performed -= OnRotate;
        rotateAction.action.canceled -= OnRotate;
        
        zoomAction.action.performed -= OnZoom;
        zoomAction.action.canceled -= OnZoom;
        
        rangeFromSurfaceAction.action.performed -= OnRangeFromSurface;
        rangeFromSurfaceAction.action.canceled -= OnRangeFromSurface;
        
        heightCutOffAction.action.performed -= OnHeightCutoff;
        heightCutOffAction.action.canceled -= OnHeightCutoff;
        
        scaleParticlesAction.action.performed -= OnScaleParticles;
        scaleParticlesAction.action.canceled -= OnScaleParticles;
        
        switchAction.action.performed -= OnSwitch;
        
        setDepthAction.action.performed -= OnSetDepth;
        
        showControlsAction.action.performed -= OnShowControls;
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        //Read the value of the input
        move = context.ReadValue<Vector2>();
        
        //Raise the event
        moveEvent?.Invoke(move);
    }
    
    private void OnRotate(InputAction.CallbackContext context)
    {
        //Read the value of the input
        rotate = context.ReadValue<float>();
        
        //Raise the event
        rotateEvent?.Invoke(rotate);
    }
    
    private void OnZoom(InputAction.CallbackContext context)
    {
        //Read the value of the input
        zoom = context.ReadValue<float>();
        
        //Raise the event
        zoomEvent?.Invoke(zoom);
    }
    
    private void OnRangeFromSurface(InputAction.CallbackContext context)
    {
        //Read the value of the input
        rangeFromSurface = context.ReadValue<float>();
        
        //Raise the event
        rangeFromSurfaceEvent?.Invoke(rangeFromSurface);
    }
    
    private void OnHeightCutoff(InputAction.CallbackContext context)
    {
        //Read the value of the input
        heightCutOff = context.ReadValue<float>();
        
        //Raise the event
        heightCutOffEvent?.Invoke(heightCutOff);
    }
    
    private void OnSwitch(InputAction.CallbackContext context)
    {
        //Raise the event
        switchEvent?.Invoke();
    }
    
    private void OnSetDepth(InputAction.CallbackContext context)
    {
        //Raise the event
        setDepthEvent?.Invoke();
    }
    
    private void OnShowControls(InputAction.CallbackContext context)
    {
        //Raise the event
        showControlsEvent?.Invoke();
    }
    
    private void OnScaleParticles(InputAction.CallbackContext context)
    {
        //Read the value of the input
        scaleParticles = context.ReadValue<Vector2>();
        
        //Raise the event
        scaleParticlesEvent?.Invoke(scaleParticles);
    }
}
