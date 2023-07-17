using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Wolffun.StorageResource
{
    [Serializable]
    public struct StorageResouceConfigDataModel
    {
        public string storageURL;
        public string cachedFolderLocation;
        public long maxCachedFolderSizeMB;
        public int maxCachedDays;
    }
}
