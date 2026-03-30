using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    public static class ConfigLoader
    {
        public static EnvironmentConfig LoadFromJson(string json)
        {
            return JsonUtility.FromJson<EnvironmentConfig>(json);
        }

        public static EnvironmentConfig LoadFromTextAsset(TextAsset asset)
        {
            return LoadFromJson(asset.text);
        }

        public static EnvironmentConfig LoadFromFile(string filePath)
        {
            string json = System.IO.File.ReadAllText(filePath);
            return LoadFromJson(json);
        }
    }
}
