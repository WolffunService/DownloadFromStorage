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

            _isInitialized = true;
        }

        public static UniTask<Texture2D> LoadImg(string relativePathUrl)
        {
            if (!_isInitialized)
                Initialize(DEFAULT_STORAGE_URL, DEFAULT_CACHED_FOLDER_LOCATION);
            
            if (cachedMetaData.IsFileDownloaded(relativePathUrl))
            {
                return LoadImgFromCached(relativePathUrl);
            }
            else
            {
                return LoadAndCacheImgFromStorage(relativePathUrl);
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
            }
            else
            {
                cachedMetaData.MarkFileUrlInvalid(relativePath);
                LoadAndCacheImgFromStorage(relativePath);
            }
            
            return tex;
        }

        private static async UniTask<Texture2D> LoadAndCacheImgFromStorage(string relativePath)
        {
            var urlPullPath = ZString.Concat(configData.storageURL, relativePath);
            
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(urlPullPath);
            await www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("Save image fail - " + urlPullPath + " - " + www.error);
                return null;
            }
            else
            {
                var myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;


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
                            
                            if(i != 0)
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
    }
}
