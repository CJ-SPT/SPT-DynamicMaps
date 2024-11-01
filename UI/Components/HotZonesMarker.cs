using DynamicMaps.Utils;
using DynamicMaps.Data;
using EFT;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    public class EnemyHotZonesMarker: MapMarker
    {
        private static float _maxCallbackTime = 0.5f;  // how often to call callback in seconds
        private static Vector2 _pivot = new Vector2(0.5f, 0.5f);

        public IPlayer Player { get; private set; }

        private float _callbackTime = _maxCallbackTime;  // make sure to start with a callback

        public static EnemyHotZonesMarker Create(GameObject parent, string text, string category, string imagePath, Color color, Vector3 position, 
                                            Vector2 size, float degreesRotation, float scale )
        {
            var mapMarker = Create<EnemyHotZonesMarker>(parent, text, category, imagePath, color, 
                                                    position, size, _pivot, degreesRotation, scale, true, false);

            return mapMarker;
        }

        public EnemyHotZonesMarker()
        {
            // Do nothing
        }

        private void LateUpdate()
        {
            if (Player?.Transform?.Original == null)
            {
                return;
            }

            // throttle callback, since that leads to a layer search which might be expensive
            _callbackTime += Time.deltaTime;
            var callback = _callbackTime >= _maxCallbackTime;
            if (callback)
            {
                _callbackTime = 0f;
            }

            MoveAndRotate(MathUtils.ConvertToMapPosition(Player.Position), -Player.Rotation.x, callback);
        }
    }
}
