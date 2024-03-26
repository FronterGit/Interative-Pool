using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // UI MANAGER //
    // 1. Listen to the InputManager for input events
    // 2. Set the UI elements based on the input events
    // 3. Track the state of the UI elements
    // 4. Raise the appropriate events when the state of the UI elements change


    [SerializeField] private GameObject particleView;
    [SerializeField] private GameObject calibrationView;
    [SerializeField] private GameObject controls;
    [SerializeField] private GameObject tableView;
    public enum State { Particles, Calibration, Table }
    public static State currentState;
    
    public static event System.Action onSwitchState;
    public static event System.Action<bool> onToggleControlsMenu;
    
    private void OnEnable()
    {
        InputManager.switchEvent += OnSwitch;
        InputManager.toggleControlsMenuEvent += OnToggleControlsMenu;
    }
    
    private void OnDisable()
    {
        InputManager.switchEvent -= OnSwitch;
        InputManager.toggleControlsMenuEvent -= OnToggleControlsMenu;
    }
    
    void Start()
    {
        if (particleView.activeSelf)
        {
            currentState = State.Particles;
        }
        else if (calibrationView.activeSelf)
        {
            currentState = State.Calibration;
        }
        else if (tableView.activeSelf)
        {
            currentState = State.Table;
        }
        
        onSwitchState?.Invoke();
    }
    
    private void OnSwitch()
    {
        switch (currentState)
        {
            case State.Particles:
                calibrationView.SetActive(true);
                particleView.SetActive(false);
                currentState = State.Calibration;
                break;
            case State.Calibration:
                tableView.SetActive(true);
                calibrationView.SetActive(false);
                currentState = State.Table;
                break;
            case State.Table:
                particleView.SetActive(true);
                tableView.SetActive(false);
                currentState = State.Particles;
                break;
        }
        onSwitchState?.Invoke();
    }
    
    private void OnToggleControlsMenu()
    {
        controls.SetActive(!controls.activeSelf);
        onToggleControlsMenu?.Invoke(controls.activeSelf);
    }
}
