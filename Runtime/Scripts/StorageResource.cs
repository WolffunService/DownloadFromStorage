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
        private const long DEFAULT_MAX_CACHED_FOLDER_SIZE_MB = 100;
        private const int DEFAULT_MAX_CACHED_DAYS = 30;

        private static StorageCachedMetaData cachedMetaData;

        private static bool _isInitialized;

        private static Dictionary<string, UniTaskCompletionSource<Texture2D>> loadingProcess;
        private static Dictionary<string, Texture2D> loadedResource;


        public static void Initialize(string storageURL, string cachedFolderLocation = null,
            long maxCachedFolderSizeMB = 100, int maxCachedDays = 30)
        {
            Initialize(new StorageResouceConfigDataModel()
            {
                storageURL = storageURL,
                cachedFolderLocation = cachedFolderLocation,
                maxCachedFolderSizeMB = maxCachedFolderSizeMB,
                maxCachedDays = maxCachedDays
            });
        }

        public static void Initialize(StorageResouceConfigDataModel config)
        {
            if (string.IsNullOrEmpty(config.storageURL))
                config.storageURL = DEFAULT_STORAGE_URL;

            if (string.IsNullOrEmpty(config.cachedFolderLocation))
                config.cachedFolderLocation = DEFAULT_CACHED_FOLDER_LOCATION;

            if (config.maxCachedFolderSizeMB <= 0)
                config.maxCachedFolderSizeMB = DEFAULT_MAX_CACHED_FOLDER_SIZE_MB;

            if (config.maxCachedDays < 1)
                config.maxCachedDays = DEFAULT_MAX_CACHED_DAYS;


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
                Initialize(DEFAULT_STORAGE_URL, DEFAULT_CACHED_FOLDER_LOCATION, 
                    DEFAULT_MAX_CACHED_FOLDER_SIZE_MB, DEFAULT_MAX_CACHED_DAYS);

            if (loadedResource.TryGetValue(relativePathUrl, out var texture))
            {
                cachedMetaData.MarkFileBeingUsed(relativePathUrl);
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

            var imgAbsolutePath = GetAbsolutePath(relativePath);
            
            if (File.Exists(imgAbsolutePath))
            {
                fileData = File.ReadAllBytes(imgAbsolutePath);
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
#elif UNITY_IOS
                tex = new Texture2D(2, 2, TextureFormat.ASTC_5x5, false);
#else
                // android
                tex = new Texture2D(4, 4, TextureFormat.ETC2_RGBA8, false);
#endif
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                tex.Compress(false);

                cachedMetaData.MarkFileBeingUsed(relativePath);
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

                    var localFullPath = GetAbsolutePath(relativePath);
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

        public static void ReleaseCached(string relativeUrl)
        {
            if (loadedResource == null)
                return;

            if (loadedResource.ContainsKey(relativeUrl))
            {
                if (loadedResource[relativeUrl] == null)
                    return;
                GameObject.Destroy(loadedResource[relativeUrl]);
                loadedResource.Remove(relativeUrl);
            }
        }

        public static async UniTaskVoid CleanUpDownloadedImg()
        {
#if DEBUG
            long currentCacheFolderSizeBytes = LocalFileManager.GetDirectorySizeBytes(GetCachedFolderPath());
            Debug.Log("cacheFolderSize before cleanup: " + currentCacheFolderSizeBytes.ToString());
            var startTime = Time.realtimeSinceStartup;
#endif

            CleanUpDownloadedImgByTotalCachedFolderSize();
            CleanUpDownloadedImgByLastAccessTime();

#if DEBUG
            var endTime = Time.realtimeSinceStartup;
            currentCacheFolderSizeBytes = LocalFileManager.GetDirectorySizeBytes(GetCachedFolderPath());
            Debug.Log("cacheFolderSize after cleanup: " + currentCacheFolderSizeBytes.ToString());
            Debug.Log("CleanUpDownloadedImg total time: " + (endTime - startTime));
#endif

            await UniTask.Yield();
        }


        // Should we cleanup by folder size when running LoadImg instead
        // At this time I want LoadImg run as fast as posible so I do it this way
        private static void CleanUpDownloadedImgByTotalCachedFolderSize()
        {
            long maxCachedFolderSizeBytes = configData.maxCachedFolderSizeMB * 1024 * 1024;
            var cacheFolder = ZString.Concat(Application.persistentDataPath, configData.cachedFolderLocation);
            long currentCacheFolderSizeBytes = LocalFileManager.GetDirectorySizeBytes(cacheFolder);

            // oldest files on top of list
            var currentDownloadedImg = cachedMetaData.listLinkDownloaded.First;
            while (currentCacheFolderSizeBytes > maxCachedFolderSizeBytes)
            {               
                if (currentDownloadedImg == null)
                    break; // end of list

                var nextNode = currentDownloadedImg.Next;

                var imgAbsolutePath = GetAbsolutePath(currentDownloadedImg.Value);
                if (File.Exists(imgAbsolutePath))
                {
                    currentCacheFolderSizeBytes -= new FileInfo(imgAbsolutePath).Length;
                    File.Delete(imgAbsolutePath);
                }

                cachedMetaData.MarkFileUrlInvalid(currentDownloadedImg.Value, currentDownloadedImg);
                currentDownloadedImg = nextNode;
            }
        }

        private static void CleanUpDownloadedImgByLastAccessTime()
        {
            // oldest files on top of list
            var currentDownloadedImg = cachedMetaData.listLinkDownloaded.First;
            while (currentDownloadedImg != null)
            {
                var nextNode = currentDownloadedImg.Next;

                var imgAbsolutePath = GetAbsolutePath(currentDownloadedImg.Value);
                if (!File.Exists(imgAbsolutePath))
                {
                    cachedMetaData.MarkFileUrlInvalid(currentDownloadedImg.Value, currentDownloadedImg);
                    currentDownloadedImg = nextNode;
                    continue;
                }

                DateTime fileLastAccessTime = new FileInfo(imgAbsolutePath).LastAccessTime;
                if (fileLastAccessTime.AddDays(configData.maxCachedDays).CompareTo(DateTime.Now) < 0)
                {
                    File.Delete(imgAbsolutePath);
                    cachedMetaData.MarkFileUrlInvalid(currentDownloadedImg.Value, currentDownloadedImg);
                } 

                currentDownloadedImg = nextNode;
            }
        }

        private static string GetAbsolutePath(string relativePathUrl)
        {
            return ZString.Concat(Application.persistentDataPath,
                configData.cachedFolderLocation, relativePathUrl);
        }

        private static string GetCachedFolderPath()
        {
            return ZString.Concat(Application.persistentDataPath, configData.cachedFolderLocation);
        }
    }
}
