using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using UnityEngine.UI;

public class ImageViewer : MonoBehaviour
{
    [SerializeField] private MultiSourceManager multiSourceManager;
    [SerializeField] private MeasureDepth measureDepth;
    
    [SerializeField] private RawImage rawImage;
    [SerializeField] private RawImage rawDepthImage;

    private void Update()
    {
        rawImage.texture = multiSourceManager.GetColorTexture();
        rawDepthImage.texture = measureDepth.depthTexture;
    }
}
