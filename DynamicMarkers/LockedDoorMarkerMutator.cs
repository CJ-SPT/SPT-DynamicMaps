using System.IO;
using System.Linq;
using InGameMap.Data;
using InGameMap.UI.Components;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.DynamicMarkers
{
    public class LockedDoorMarkerMutator : IDynamicMarkerProvider
    {
        // FIXME: move to configuration somehow
        private static string doorWithKeyPath = Path.Combine(Plugin.Path, "Markers/door_with_key.png");
        private static string doorWithLockPath = Path.Combine(Plugin.Path, "Markers/door_with_lock.png");

        public void OnShowInRaid(MapView map, string mapInternalName)
        {
            var player = GameUtils.GetMainPlayer();
            var markers = map.GetMapMarkersByCategory("LockedDoor");

            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                // TODO: GetAllItems is a BSG extension method under GClass
                var hasKey = player.Inventory.Equipment.GetAllItems()
                    .FirstOrDefault(i => i.TemplateId == marker.AssociatedItemId) != null;

                marker.Image.sprite = hasKey
                    ? TextureUtils.GetOrLoadCachedSprite(doorWithKeyPath)
                    : TextureUtils.GetOrLoadCachedSprite(doorWithLockPath);

                marker.Color = hasKey
                    ? Color.green
                    : Color.red;
            }
        }

        public void OnShowOutOfRaid(MapView map)
        {
            var profile = GameUtils.GetPlayerProfile();
            var markers = map.GetMapMarkersByCategory("LockedDoor");

            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                // TODO: GetAllItems is a BSG extension method under GClass
                var keyInStash = profile.Inventory.Stash.GetAllItems()
                    .FirstOrDefault(i => i.TemplateId == marker.AssociatedItemId) != null;
                var keyInEquipment = profile.Inventory.Equipment.GetAllItems()
                    .FirstOrDefault(i => i.TemplateId == marker.AssociatedItemId) != null;

                // change icon
                marker.Image.sprite = (keyInStash || keyInEquipment)
                    ? TextureUtils.GetOrLoadCachedSprite(doorWithKeyPath)
                    : TextureUtils.GetOrLoadCachedSprite(doorWithLockPath);

                // change color
                marker.Color = keyInEquipment
                    ? Color.green
                    : keyInStash
                        ? Color.yellow
                        : Color.red;
            }
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            if (GameUtils.IsInRaid())
            {
                OnShowInRaid(map, GameUtils.GetCurrentMapInternalName());
            }
            else
            {
                OnShowOutOfRaid(map);
            }
        }

        public void OnRaidEnd(MapView map)
        {
            // do nothing
        }

        public void OnHideInRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}
