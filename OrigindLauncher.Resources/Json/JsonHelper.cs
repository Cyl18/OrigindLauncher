﻿using Newtonsoft.Json;

namespace OrigindLauncher.Resources.Json
{
    public static class JsonHelper
    {
        private static readonly SerializeSettings SerializeSettings = new SerializeSettings();
        
        public static string ToJsonString(this object source)
        {
            return JsonConvert.SerializeObject(source, SerializeSettings);
        }

        public static T JsonCast<T>(this string source)
        {
            return JsonConvert.DeserializeObject<T>(source, SerializeSettings);
        }

        public static string ToJsonString(this object source, JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(source, settings);
        }

        public static T JsonCast<T>(this string source, JsonSerializerSettings settings)
        {
            return JsonConvert.DeserializeObject<T>(source, settings);
        }
    }

    public class SerializeSettings : JsonSerializerSettings
    {
        public SerializeSettings()
        {
            NullValueHandling = NullValueHandling.Include;
            Formatting = Formatting.Indented;
            MissingMemberHandling = MissingMemberHandling.Ignore;
        }
    }
}