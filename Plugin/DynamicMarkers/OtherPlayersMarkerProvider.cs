using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class OtherPlayersMarkerProvider : IDynamicMarkerProvider
    {
        private const string _arrowImagePath = "Markers/arrow.png";
        private const string _starImagePath = "Markers/star.png";
        
        private const string _friendlyPlayerCategory = "Friendly Player";
        private const string _friendlyPlayerImagePath = _arrowImagePath;
        private static Color _friendlyPlayerColor = Color.Lerp(Color.blue, Color.white, 0.5f);

        private const string _enemyPlayerCategory = "Enemy Player";
        private const string _enemyPlayerImagePath = _arrowImagePath;

        private const string _scavCategory = "Scav";
        private const string _scavImagePath = _arrowImagePath;

        private const string _bossCategory = "Boss";
        private const string _bossImagePath = _starImagePath;
        
        private bool _showFriendlyPlayers = true;
        public bool ShowFriendlyPlayers
        {
            get
            {
                return _showFriendlyPlayers;
            }

            set
            {
                HandleSetBoolOption(ref _showFriendlyPlayers, value);
            }
        }

        private bool _showEnemyPlayers = false;
        public bool ShowEnemyPlayers
        {
            get
            {
                return _showEnemyPlayers;
            }

            set
            {
                HandleSetBoolOption(ref _showEnemyPlayers, value);
            }
        }

        private bool _showScavs = false;
        public bool ShowScavs
        {
            get
            {
                return _showScavs;
            }

            set
            {
                HandleSetBoolOption(ref _showScavs, value);
            }
        }

        private bool _showBosses = false;
        public bool ShowBosses
        {
            get
            {
                return _showBosses;
            }

            set
            {
                HandleSetBoolOption(ref _showBosses, value);
            }
        }

        private MapView _lastMapView;
        private Dictionary<Player, PlayerMapMarker> _playerMarkers = [];

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            
            TryAddMarkers();
            RemoveNonActivePlayers();

            // register to event to get all the new ones while map is showing
            Singleton<GameWorld>.Instance.OnPersonAdd += TryAddMarker;

            // register to events to get when players die
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer += OnUnregisterPlayer;
            PlayerOnDeadPatch.OnDead += TryRemoveMarker;
        }

        public void OnHideInRaid(MapView map)
        {
            // unregister from events while map isn't showing
            Singleton<GameWorld>.Instance.OnPersonAdd -= TryAddMarker;
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            PlayerOnDeadPatch.OnDead -= TryRemoveMarker;
        }

        public void OnRaidEnd(MapView map)
        {
            // unregister from events since map is ending
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld is not null)
            {
                gameWorld.OnPersonAdd -= TryAddMarker;
            }

            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            PlayerOnDeadPatch.OnDead -= TryRemoveMarker;

            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            foreach (var player in _playerMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
                TryAddMarker(player);
            }
        }

        public void OnDisable(MapView map)
        {
            // unregister from events since provider is being disabled
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld is not null)
            {
                gameWorld.OnPersonAdd -= TryAddMarker;
            }

            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            PlayerOnDeadPatch.OnDead -= TryRemoveMarker;

            TryRemoveMarkers();
        }

        private void TryRemoveMarkers()
        {
            foreach (var player in _playerMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
            }

            _playerMarkers.Clear();
        }

        private void TryAddMarkers()
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }
            
            // add all players that have spawned already in raid
            var gameWorld = Singleton<GameWorld>.Instance;
            foreach (var player in gameWorld.AllAlivePlayersList)
            {
                if (player.IsYourPlayer || _playerMarkers.ContainsKey(player))
                {
                    continue;
                }

                TryAddMarker(player);
            }
        }

        private void OnUnregisterPlayer(IPlayer iPlayer)
        {
            var player = iPlayer as Player;
            if (player is null)
            {
                return;
            }

            TryRemoveMarker(player);
        }

        private void RemoveNonActivePlayers()
        {
            var alivePlayers = new HashSet<Player>(Singleton<GameWorld>.Instance.AllAlivePlayersList);
            foreach (var player in _playerMarkers.Keys.ToList())
            {
                if (player.HasCorpse() || !alivePlayers.Contains(player))
                {
                    TryRemoveMarker(player);
                }
            }
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;

            foreach (var player in _playerMarkers.ToArray())
            {
                if (player.Key.IsYourPlayer) continue;
                
                TryRemoveMarker(player.Key);
                TryAddMarker(player.Key);
            }
        }
        
        private void TryAddMarker(IPlayer iPlayer)
        {
            var player = iPlayer as Player;
            if (player is null || player.IsDedicatedServer())
            {
                return;
            }

            if (_lastMapView is null || player.IsBTRShooter() || _playerMarkers.ContainsKey(player))
            {
                return;
            }
            
            // set category and color
            var category = string.Empty;
            var imagePath = string.Empty;
            var color = Color.clear;
            
            var intelLevel = GameUtils.GetIntelLevel();
            
            if (player.IsGroupedWithMainPlayer() && Settings.ShowFriendlyIntelLevel.Value <= intelLevel)
            {
                category = _friendlyPlayerCategory;
                imagePath = _friendlyPlayerImagePath;
                color = _friendlyPlayerColor;
            }
            else if (player.IsTrackedBoss() && Settings.ShowBossIntelLevel.Value <= intelLevel)
            {
                category = _bossCategory;
                imagePath = _bossImagePath;
                color = Settings.BossColor.Value;
            }
            else if (player.IsPMC() && Settings.ShowPmcIntelLevel.Value <= intelLevel)
            {
                category = _enemyPlayerCategory;
                imagePath = _enemyPlayerImagePath;
                color = player.Side == EPlayerSide.Bear
                    ? Settings.PmcBearColor.Value
                    : Settings.PmcUsecColor.Value;
                    
            }
            else if (player.IsScav() && Settings.ShowScavIntelLevel.Value <= intelLevel)
            {
                category = _scavCategory;
                imagePath = _scavImagePath;
                color = Settings.ScavColor.Value;
            }

            if (!ShouldShowCategory(category))
            {
                return;
            }

            // try adding marker
            var marker = _lastMapView.AddPlayerMarker(player, category, color, imagePath);
            _playerMarkers[player] = marker;
        }

        private void RemoveDisabledMarkers()
        {
            foreach (var player in _playerMarkers.Keys.ToList())
            {
                var marker = _playerMarkers[player];
                if (!ShouldShowCategory(marker.Category))
                {
                    TryRemoveMarker(player);
                }
            }
        }

        private void TryRemoveMarker(Player player)
        {
            if (!_playerMarkers.ContainsKey(player))
            {
                return;
            }

            _playerMarkers[player].ContainingMapView.RemoveMapMarker(_playerMarkers[player]);
            _playerMarkers.Remove(player);
        }

        private bool ShouldShowCategory(string category)
        {
            switch (category)
            {
                case _friendlyPlayerCategory:
                    return _showFriendlyPlayers;
                case _enemyPlayerCategory:
                    return _showEnemyPlayers;
                case _bossCategory:
                    return _showBosses;
                case _scavCategory:
                    return _showScavs;
                default:
                    return false;
            }
        }

        private void HandleSetBoolOption(ref bool boolOption, bool value)
        {
            if (value == boolOption)
            {
                return;
            }

            boolOption = value;

            if (boolOption)
            {
                TryAddMarkers();
            }
            else
            {
                RemoveDisabledMarkers();
            }
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}
