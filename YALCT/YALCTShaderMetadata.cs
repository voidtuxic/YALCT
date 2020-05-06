using System.Collections.Generic;

namespace YALCT
{
    public struct YALCTShaderMetadata
    {
        public string Name;
        public string Description;
        public string Credit;
        public string Version;
        public List<string> Categories;
        public string[] ResourcePaths;

        public static YALCTShaderMetadata Default()
        {
            return new YALCTShaderMetadata
            {
                Name = "",
                Description = "",
                Credit = "",
                Version = "",
                Categories = new List<string>(),
            };
        }
    }
}