using System.Collections.Generic;
using System.IO;
using Cysharp.Text;
using Unity.VectorGraphics;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            var text = File.ReadAllText(absolutePath);
            var sceneInfo = SVGParser.ImportSVG(new StringReader(text));
            
            var tessOptions = new VectorUtils.TessellationOptions
            {
                StepDistance = 1.0f,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f,
                SamplingStepSize = 0.01f
            };
            
            var sprite = VectorUtils.BuildSprite(
                VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions), 
                24f, 
                VectorUtils.Alignment.Center,
                Vector2.zero, 
                16);
            
            var mat = new Material(Shader.Find("Unlit/Texture"));
            var texture = VectorUtils.RenderSpriteToTexture2D(
                sprite, 
                (int)sceneInfo.SceneViewport.width, 
                (int)sceneInfo.SceneViewport.height,
                mat,
                1, 
                false);
            
            return texture;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (_spriteCache.ContainsKey(path))
            {
                return _spriteCache[path];
            }

            var absolutePath = Path.Combine(Plugin.Path, path);
            var texture = LoadTexture2DFromPath(absolutePath);
            _spriteCache[path] = Sprite.Create(texture,
                                               new Rect(0f, 0f, texture.width, texture.height),
                                               new Vector2(texture.width / 2, texture.height / 2));

            return _spriteCache[path];
        }
    }
}
