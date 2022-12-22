using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Cysharp.Threading.Tasks;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.Networking;


namespace Wolffun.StorageResource
{
    public class StorageResource
    {
        private static StorageResouceConfigDataModel configData;
        
        private const string DEFAULT_CACHED_FOLDER_LOCATION = "/StorageResource";
        private const string DEFAULT_STORAGE_URL = "https://assets.thetanarena.com";

        private static StorageCachedMetaData cachedMetaData;

        private static bool _isInitialized;

        private static Dictionary<string, UniTaskCompletionSource<Texture2D>> loadingProcess;
        private static Dictionary<string, Texture2D> loadedResource;


        public static void Initialize(string storageURL, string cachedFolderLocation = null)
        {
            Initialize(new StorageResouceConfigDataModel()
            {
                storageURL = storageURL,
                cachedFolderLocation = cachedFolderLocation,
            });
        }

        public static void Initialize(StorageResouceConfigDataModel config)
        {
            if (string.IsNullOrEmpty(config.storageURL))
                config.storageURL = DEFAULT_STORAGE_URL;

            if (string.IsNullOrEmpty(config.cachedFolderLocation))
                config.cachedFolderLocation = DEFAULT_CACHED_FOLDER_LOCATION;
            
            configData = config;
            cachedMetaData = new StorageCachedMetaData();
            cachedMetaData.Init(configData.cachedFolderLocation);

            ReleaseAllCached();
            
            loadingProcess = new Dictionary<string, UniTaskCompletionSource<Texture2D>>();
            loadedResource = new Dictionary<string, Texture2D>();

            _isInitialized = true;
        }

        public static async UniTask<Texture2D> LoadImg(string relativePathUrl)
        {
            if (!_isInitialized)
                Initialize(DEFAULT_STORAGE_URL, DEFAULT_CACHED_FOLDER_LOCATION);

            if (loadedResource.TryGetValue(relativePathUrl, out var texture))
            {
                return texture;
            }
            
            if (cachedMetaData.IsFileDownloaded(relativePathUrl))
            {
                return await LoadImgFromCached(relativePathUrl);
            }
            else
            {
                return await LoadAndCacheImgFromStorage(relativePathUrl);
            }
        }

        private static async UniTask<Texture2D> LoadImgFromCached(string relativePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            var imgAbsolutePath = ZString.Concat(Application.persistentDataPath,
                configData.cachedFolderLocation, relativePath);
            
            if (File.Exists(imgAbsolutePath))
            {
                fileData = File.ReadAllBytes(imgAbsolutePath);
#if UNITY_EDITOR
                tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
#elif UNITY_IOS
                tex = new Texture2D(2, 2, TextureFormat.ASTC_5x5, false);
#else
                // android
                tex = new Texture2D(2, 2, TextureFormat.ETC2_RGBA8, false);
#endif
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                tex.Compress(false);

                loadedResource[relativePath] = tex;
            }
            else
            {
                cachedMetaData.MarkFileUrlInvalid(relativePath);
                tex = await LoadAndCacheImgFromStorage(relativePath);
            }

            return tex;
        }

        private static async UniTask<Texture2D> LoadAndCacheImgFromStorage(string relativePath)
        {
            if (loadingProcess.TryGetValue(relativePath, out var completeSource))
            {
                
                return await completeSource.Task;
            }

            var urlPullPath = ZString.Concat(configData.storageURL, relativePath);
            
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(urlPullPath);

            loadingProcess.Add(relativePath, new UniTaskCompletionSource<Texture2D>());

            try
            {
                var operation = www.SendWebRequest();

                await operation;

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError("Save image fail - " + urlPullPath + " - " + www.error);
                    loadingProcess[relativePath].TrySetResult(null);
                    loadingProcess.Remove(relativePath);
                    return null;
                }
                else
                {
                    var myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;

                    if(loadingProcess.TryGetValue(relativePath, out var loading))
                    {
                        loading.TrySetResult(myTexture);
                        loadingProcess.Remove(relativePath);
                    }

                    if(!loadedResource.ContainsKey(relativePath))
                    {
                        loadedResource.Add(relativePath, myTexture);
                    }

                    byte[] imageBytes = myTexture.EncodeToPNG();

                    var localFullPath = ZString.Concat(Application.persistentDataPath, configData.cachedFolderLocation, relativePath);
                    string[] folderName = localFullPath.Split('/');
                    string PathFolder = string.Empty;

                    using (var strBuilder = ZString.CreateStringBuilder())
                    {
                        if (folderName.Length > 0)
                        {
                            for (int i = 0; i < folderName.Length - 1; i++)
                            {
                                var pathname = folderName[i];
                                if (string.IsNullOrEmpty(pathname))
                                {
                                    continue;
                                }

                                if (i != 0)
                                    strBuilder.Append("/");

                                strBuilder.Append(folderName[i]);
                            }
                        }

                        PathFolder = strBuilder.ToString();
                    }


                    Directory.CreateDirectory(PathFolder);

                    File.WriteAllBytes(localFullPath, imageBytes);

                    cachedMetaData.MarkFileUrlDownloaded(relativePath);

                    return myTexture;


                }
            }
            catch(Exception ex)
            {
                Debug.LogError("Download Image fail - " + urlPullPath + " - " + ex.Message);
                loadingProcess[relativePath].TrySetResult(null);
                loadingProcess.Remove(relativePath);
                return null;
            }
        }

        public static void ReleaseAllCached()
        {
            if (loadedResource == null)
                return;
            
            foreach(var resource in loadedResource)
            {
                if(resource.Value == null)
                    continue;
                
                GameObject.Destroy(resource.Value);
            }

            loadedResource.Clear();
        }
    }
}
