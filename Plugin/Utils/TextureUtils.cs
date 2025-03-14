using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static Sprite LoadSpriteFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath) || !absolutePath.EndsWith(".svg"))
            {
                return default;
            }

            var text = File.ReadAllText(absolutePath);
            var sceneInfo = SVGParser.ImportSVG(new StringReader(text));
            
            var tessOptions = new VectorUtils.TessellationOptions
            {
                StepDistance = 10f,
                SamplingStepSize = 100f,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f,
            };
            
            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);
            
            var sprite = VectorUtils.BuildSprite(
                geometries,
                300, 
                VectorUtils.Alignment.Custom,
                Vector2.zero, 
                128);
            
            return sprite;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (_spriteCache.TryGetValue(path, out var sprite))
            {
                return sprite;
            }

            var absolutePath = Path.Combine(Plugin.Path, path);
            
            _spriteCache[path] = LoadSpriteFromPath(absolutePath);
            return _spriteCache[path];
        }
    }
}
