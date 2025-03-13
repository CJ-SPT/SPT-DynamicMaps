using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static (Sprite, SVGParser.SceneInfo) LoadSpriteFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath) || !absolutePath.EndsWith(".svg"))
            {
                return default;
            }

            var text = File.ReadAllText(absolutePath);
            var sceneInfo = SVGParser.ImportSVG(new StringReader(text));
            
            var tessOptions = new VectorUtils.TessellationOptions
            {
                StepDistance = 10.0f,
                MaxCordDeviation = float.MaxValue,      // Disables constraints
                MaxTanAngleDeviation = Mathf.PI/2.0f,   // Disables constraints
                SamplingStepSize = 0.01f,
            };
            
            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);
            
            var sprite = VectorUtils.BuildSprite(
                geometries,
                sceneInfo.SceneViewport.width / sceneInfo.SceneViewport.height, 
                VectorUtils.Alignment.Center,
                new Vector2(0, 1), 
                128);
            
            return (sprite, sceneInfo);
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (_spriteCache.ContainsKey(path))
            {
                return _spriteCache[path];
            }

            var absolutePath = Path.Combine(Plugin.Path, path);
            var scene = LoadSpriteFromPath(absolutePath);

            _spriteCache[path] = scene.Item1;
            return _spriteCache[path];
        }
    }
}
