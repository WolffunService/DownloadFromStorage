using Cysharp.Text;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Wolffun.Image;
using Wolffun.StorageResource;

public class TestGetImg : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputUrl;
    [SerializeField] private Button btnLoadImg;
    [SerializeField] private Button btnLoadCosmetic;
    [SerializeField] private Button btnCleanupDownloadedImg;
    [SerializeField] private Button btnReleaseAllCache;
    [SerializeField] private Button btnSaveCacheMetaData;
    [SerializeField] private Image img;


    [SerializeField] private StorageResouceConfigDataModel config;

    [SerializeField] private List<Texture2D> listLoadedTexture;

    private void Start()
    {
        StorageResource.Initialize(config);

        btnLoadImg.onClick.AddListener(LoadImgUrl);
        btnLoadCosmetic.onClick.AddListener(LoadCosmeticAll);
        btnCleanupDownloadedImg.onClick.AddListener(CleanUpDownloadedImage);
        btnReleaseAllCache.onClick.AddListener(ReleaseAllCache);
        btnSaveCacheMetaData.onClick.AddListener(SaveCacheMetaData);
    }

    [ContextMenu("Clear Cache")]
    public void ClearCache()
    {
        foreach(var x in listLoadedTexture)
        {
            Destroy(x);
        }

        listLoadedTexture.Clear();
    }

    private async void LoadCosmeticAll()
    {
        var startTime = Time.realtimeSinceStartup;
        for (int i = 0; i <= 30; i++)
        {
            LoadCosmetic(i);
        }

        for (int i = 20001; i <= 20031; i++)
        {
            LoadCosmetic(i);
        }
        //
        for (int i = 30001; i <= 30040; i++)
        {
            LoadCosmetic(i);
        }
        //
        for (int i = 40001; i <= 40004; i++)
        {
            LoadCosmetic(i);
        }

        var endTime = Time.realtimeSinceStartup;
        Debug.Log("Load from disk " + (endTime - startTime));

        img.sprite = listLoadedTexture[0].ConvertToSprite();

        startTime = Time.realtimeSinceStartup;
        for (int i = 0; i <= 30; i++)
        {
            LoadCosmetic(i);
        }

        for (int i = 20001; i <= 20031; i++)
        {
            LoadCosmetic(i);
        }
        //
        for (int i = 30001; i <= 30040; i++)
        {
            LoadCosmetic(i);
        }
        //
        for (int i = 40001; i <= 40004; i++)
        {
            LoadCosmetic(i);
        }
        endTime = Time.realtimeSinceStartup;
        Debug.Log("Load from cache " + (endTime - startTime));

        //await UniTask.Delay(5000);
        //StorageResource.ReleaseAllCached();
    }

    private async void LoadCosmetic(int id)
    {
        listLoadedTexture.Add(await StorageResource.LoadImg(ZString.Format("/cosmetics/cosmetic_{0}.png", id)));
        img.sprite = listLoadedTexture[listLoadedTexture.Count - 1].ConvertToSprite();
    }

    private async void LoadImgUrl()
    {
        var startTime = Time.realtimeSinceStartup;
        listLoadedTexture.Add(await StorageResource.LoadImg(inputUrl.text));
        img.sprite = listLoadedTexture[listLoadedTexture.Count - 1].ConvertToSprite();
        var endTime = Time.realtimeSinceStartup;
        Debug.Log("Load from cache " + (endTime - startTime));
    }

    private void CleanUpDownloadedImage()
    {
        StorageResource.CleanUpDownloadedImg().Forget();
    }

    private void ReleaseAllCache()
    {
        StorageResource.ReleaseAllCached();
    }

    private void SaveCacheMetaData()
    {
        StorageResource.SaveCachedMetaData();
    }
}
