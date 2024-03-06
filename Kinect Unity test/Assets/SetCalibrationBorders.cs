using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetCalibrationBorders : MonoBehaviour
{
    [SerializeField] private Image topBorder;
    [SerializeField] private Image bottomBorder;
    [SerializeField] private Image leftBorder;
    [SerializeField] private Image rightBorder;
    
    private void OnEnable()
    {
        MeasureDepth.broadcastTableCutoffsEvent += SetBorders;
    }
    
    private void OnDisable()
    {
        MeasureDepth.broadcastTableCutoffsEvent -= SetBorders;
    }

    private void SetBorders(MeasureDepth.TableCutoffs tableCutoffs)
    {
        topBorder.transform.position = new Vector3(topBorder.transform.position.x, tableCutoffs.top, topBorder.transform.position.z);
        bottomBorder.transform.position = new Vector3(bottomBorder.transform.position.x, tableCutoffs.bottom, bottomBorder.transform.position.z);
        leftBorder.transform.position = new Vector3(tableCutoffs.left, leftBorder.transform.position.y, leftBorder.transform.position.z);
        rightBorder.transform.position = new Vector3(tableCutoffs.right, rightBorder.transform.position.y, rightBorder.transform.position.z);
    }
}
