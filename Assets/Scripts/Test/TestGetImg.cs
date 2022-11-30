using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wolffun.Image;
using Wolffun.StorageResource;

public class TestGetImg : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputUrl;
    [SerializeField] private Button btnLoadImg;
    [SerializeField] private Image img;


    [SerializeField] private StorageResouceConfigDataModel config;

    private void Start()
    {
        StorageResource.Initialize(config);
        
        btnLoadImg.onClick.AddListener(OnClickLoadImg);
    }

    private async void OnClickLoadImg()
    {
        var texture = await StorageResource.LoadImg(inputUrl.text);

        img.sprite = texture.ConvertToSprite();
    }
}
