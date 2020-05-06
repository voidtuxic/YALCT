using Newtonsoft.Json;

namespace YALCT
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct YALCTFilePickerItem
    {
        public bool IsUpper;
        public bool IsFolder;
        [JsonProperty]
        public string Name;
        [JsonProperty]
        public string FullPath;

        public YALCTFilePickerItem(bool isFolder, string name, string fullPath, bool isUpper = false)
        {
            IsUpper = isUpper;
            IsFolder = isFolder;
            Name = name;
            FullPath = fullPath;
        }
    }
}