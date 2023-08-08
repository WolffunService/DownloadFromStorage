using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Wolffun.StorageResource
{
    [Serializable]
    public struct StorageResouceConfigDataModel
    {
        public string bucketURL; // Cloud storage bucket URL
        public long maxCachedFolderSizeMB;
        public int maxCachedDays;
    }
}
