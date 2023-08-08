using BestHTTP;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wolffun.Log;

namespace Wolffun.StorageResource
{
    public static class StorageResource
    {
        private const string DEFAULT_BUCKET_URL = "https://assets.thetanarena.com";
        private const long DEFAULT_MAX_CACHED_FOLDER_SIZE_MB = 100;
        private const int DEFAULT_MAX_CACHED_DAYS = 30;

        private static StorageResouceConfigDataModel configData;
        private static bool _isInitialized;

        private static Dictionary<string, UniTaskCompletionSource<Texture2D>> loadingProcess;
        private static Dictionary<string, Texture2D> loadedResource;

        public static void Initialize(string bucketURL = DEFAULT_BUCKET_URL,
            long maxCachedFolderSizeMB = DEFAULT_MAX_CACHED_FOLDER_SIZE_MB, 
            int maxCachedDays = DEFAULT_MAX_CACHED_DAYS)
        {
            Initialize(new StorageResouceConfigDataModel()
            {
                bucketURL = bucketURL,
                maxCachedFolderSizeMB = maxCachedFolderSizeMB,
                maxCachedDays = maxCachedDays
            });
        }

        public static void Initialize(StorageResouceConfigDataModel config)
        {
            if (string.IsNullOrEmpty(config.bucketURL))
                config.bucketURL = DEFAULT_BUCKET_URL;
            if (config.maxCachedFolderSizeMB <= 0)
                config.maxCachedFolderSizeMB = DEFAULT_MAX_CACHED_FOLDER_SIZE_MB;
            if (config.maxCachedDays < 1)
                config.maxCachedDays = DEFAULT_MAX_CACHED_DAYS;
            configData = config;

            RemoveAllFromMemoryCache();
            
            loadingProcess = new Dictionary<string, UniTaskCompletionSource<Texture2D>>();
            loadedResource = new Dictionary<string, Texture2D>();

            _isInitialized = true;
        }

        /// <summary>
        /// Load Image from loaded resource on RAM, persistent cache or cloud storage
        /// </summary>
        public static async UniTask<Texture2D> LoadImg(string relativeURL)
        {
            if (!_isInitialized)
                Initialize();

            if (loadedResource.TryGetValue(relativeURL, out Texture2D value))
            {
                return value;
            }
            else
            {
                var tex = await LoadImgHttp(relativeURL);
                if (!loadedResource.ContainsKey(relativeURL) && tex != null)
                    loadedResource.Add(relativeURL, tex);

                return tex;
            }
        }

        /// <summary>
        /// Load Image from persistent cache or cloud storage
        /// https://benedicht.github.io/BestHTTP-Documentation/pages/best_http2/protocols/http/Caching.html
        /// </summary>
        private static async UniTask<Texture2D> LoadImgHttp(string relativeURL)
        {
            Texture2D tex = null;
            var request = new HTTPRequest(new Uri(GetFullUrl(relativeURL)));

            try
            {
                var response =  await request.GetHTTPResponseAsync();
                tex = response.DataAsTexture2D;

                CommonLog.Log(String.Format(
                    "LoadImgHttp url: {0} \n" +
                    "IsFromCache: {1} \n" +
                    "ETag: {2}\n" +
                    "Expires: {3} \n" +
                    "Cache-Control: {4}\n" +
                    "CacheFileInfo GetPath {5} \n",
                    relativeURL, response.IsFromCache,
                    response.CacheFileInfo.ETag,
                    response.CacheFileInfo.Expires,
                    response.CacheFileInfo.Received,
                    response.CacheFileInfo.GetPath()));
            }
            catch (Exception ex)
            {
                CommonLog.LogWarning("LoadImgHttp Exception: " + ex.ToString());
            }

            return tex;
        }

        /// <summary>
        /// Remove downloaded resource from persistent cache
        /// This also remove loaded resource on memory cache
        /// </summary>
        public static void RemoveFromPersistentCache(string relativeURL)
        {
            RemoveFromMemoryCache(relativeURL);

            // TODO:
            // Call Best-HTTP2 to delete cached file on persistent storage
        }

        /// <summary>
        /// Remove loaded resource from memory cached on RAM
        /// This function not delete cached files saved on disk
        /// </summary>
        public static void RemoveFromMemoryCache(string relativeURL)
        {
            if (loadedResource == null)
                return;

            if (loadedResource.ContainsKey(relativeURL))
            {
                if (loadedResource[relativeURL] == null)
                    return;

                GameObject.Destroy(loadedResource[relativeURL]);
                loadedResource.Remove(relativeURL);
            }
        }

        /// <summary>
        /// Remove all loaded resource from memory cached on RAM
        /// This function not delete cached files saved on disk
        /// </summary>
        public static void RemoveAllFromMemoryCache()
        {
            if (loadedResource == null)
                return;

            foreach (var resource in loadedResource)
            {
                if (resource.Value == null)
                    continue;

                GameObject.Destroy(resource.Value);
            }

            loadedResource.Clear();
        }

        /// <summary>
        /// Get full cloud storage bucket url
        /// </summary>
        internal static string GetFullUrl(string relativeURL)
        {
            return ZString.Concat(configData.bucketURL, relativeURL);
        }
    }
}
