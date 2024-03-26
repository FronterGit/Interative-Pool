using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CalibrationMenu : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject controls;
    [SerializeField] private GameObject[] tutorials;
    
    private int tutorialIndex = 0;

    private void OnEnable()
    {
        InputManager.menuChoiceEvent += OnMenu;
        UIManager.onToggleControlsMenu += OnControlsMenuToggle;
    }
    
    private void OnDisable()
    {
        InputManager.menuChoiceEvent -= OnMenu;
        UIManager.onToggleControlsMenu -= OnControlsMenuToggle;
    }

    public void OnOpen()
    {
        //Close all menus except the main menu
        menu.SetActive(true);
        controls.SetActive(false);
        foreach (var tutorial in tutorials)
        {
            tutorial.SetActive(false);
        }
    }

    void OnMenu(float choice)
    {
        //If the menu is active...
        if (menu.activeSelf)
        {
            switch (choice)
            {
                case 1:
                    controls.SetActive(true);
                    break;
                case 2:
                    tutorials[0].SetActive(true);
                    break;
            }
            menu.SetActive(false);
            return;
        }
        
        //If the controls are active...
        if(controls.activeSelf)
        {
            controls.SetActive(false);
            menu.SetActive(true);
            return;
        }
        
        //If a tutorial is active...
        if (tutorials[tutorialIndex].activeSelf)
        {
            switch (choice)
            {
                case 1:
                    if(tutorialIndex > 0)
                    {
                        tutorials[tutorialIndex].SetActive(false);
                        tutorialIndex--;
                        tutorials[tutorialIndex].SetActive(true);
                    }
                    else
                    {
                        tutorials[tutorialIndex].SetActive(false);
                        menu.SetActive(true);
                    }

                    break;
                case 2:
                    if(tutorialIndex < tutorials.Length - 1)
                    {
                        tutorials[tutorialIndex].SetActive(false);
                        tutorialIndex++;
                        tutorials[tutorialIndex].SetActive(true);
                    }

                    break;
                
            }
            return;
        }

    }
    
    public void OnClose()
    {
        //Close all menus
        menu.SetActive(false);
        controls.SetActive(false);
        foreach (var tutorial in tutorials)
        {
            tutorial.SetActive(false);
        }
        tutorialIndex = 0;
    }

    public void OnControlsMenuToggle(bool open)
    {
        if(open) OnOpen();
        else OnClose();
    }
}
