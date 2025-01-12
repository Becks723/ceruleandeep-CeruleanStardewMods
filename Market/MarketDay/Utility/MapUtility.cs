using System.Collections.Generic;
using System.Linq;
using MarketDay.Shop;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace MarketDay.Utility
{
    public static class MapUtility
    {
        // 253 254 255
        // 285 286 287 XXX
        // 317 318 319
        // 349 350 351
        // dictionary provides location of the storage chest given the ID of the tile clicked on
        // turns out it's actually faster to just scan 12 nearby tiles
        //
        private static Dictionary<int, Vector2> ChestOffsetForTile = new()
        {
            [253] = new Vector2(3, 1), [254] = new Vector2(2, 1), [255] = new Vector2(1, 1),
            [285] = new Vector2(3, 0), [286] = new Vector2(2, 0), [287] = new Vector2(1, 0),
            [317] = new Vector2(3, -1), [318] = new Vector2(2, -1), [319] = new Vector2(1, -1),
            [349] = new Vector2(3, -2), [350] = new Vector2(2, -2), [351] = new Vector2(1, -2),
        };


        /// <summary>
        /// Returns the tile property found at the given parameters
        /// </summary>
        /// <value>an instance of the the map location</value>
        /// <value>the name of the layer</value>
        /// <value>the coordinates of the tile</value>
        /// <value>The tile property if there is one, null if there isn't</value>
        public static List<Vector2> ShopTiles
        {
            get
            {
                List<Vector2> ShopLocations = new();
                var town = Game1.getLocationFromName("Town");
                if (town?.map?.Layers is null || town.map.Layers.Count < 1)
                {
                    MarketDay.Log($"ShopTiles: Town location or map not available", LogLevel.Error);
                    return ShopLocations;
                }

                var layerWidth = town.map.Layers[0].LayerWidth;
                var layerHeight = town.map.Layers[0].LayerHeight;

                for (var x = 0; x < layerWidth; x++)
                {
                    for (var y = 0; y < layerHeight; y++)
                    {
                        var v = new Vector2(x, y);
                        var tileProperty = TileUtility.GetTileProperty(town, "Back", v);
                        if (tileProperty is null) continue;
                        if (tileProperty.ContainsKey($"{MarketDay.SMod.ModManifest.UniqueID}.GrangeShop")) ShopLocations.Add(v);
                    }
                }

                return ShopLocations;
            }
        }
        
        public static Dictionary<string, GrangeShop> ShopOwners
        {
            get
            {
                var s = new Dictionary<string, GrangeShop>();
                foreach (var shop in ShopAtTile().Values)
                {
                    s[shop.Owner()] = shop;
                }

                return s;
            }
        }

        internal static Dictionary<Vector2, GrangeShop> ShopAtTile()
        {
            var town = Game1.getLocationFromName("Town");
            var shopsAtTiles = new Dictionary<Vector2, GrangeShop>();

            foreach (var tile in ShopTiles)
            {
                var signTile = tile + new Vector2(3, 3);
                if (!town.objects.TryGetValue(signTile, out var obj) || obj is not Sign sign) continue;
                if (sign.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.ShopSignKey}",
                    out var signOwner))
                {
                    shopsAtTiles[tile] = ShopManager.GrangeShops[signOwner];
                }
            }

            return shopsAtTiles;
        }

        /// <summary>
        /// Find the GrangeShop that the player clicked on
        /// </summary>
        /// <param name="tile">GrabTile that user clicked on</param>
        /// <returns>GrangeShop for tile clicked on, or null</returns>
        public static GrangeShop ShopNearTile(Vector2 tile)
        {
            for (var x = 1; x <= 3; x++)
            {
                for (var y = -2; y <= 1; y++)
                {
                    var search = tile + new Vector2(x, y);
                    MarketDay.Log($"    ShopNearTile {x} {y} {search}", LogLevel.Debug, true);
                    if (!Game1.currentLocation.objects.TryGetValue(search, out var chest) ||
                        chest is not Chest) continue;
                    chest.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.StockChestKey}",
                        out var shopOwner);
                    if (shopOwner is null) return null;
                    MarketDay.Log($"    ShopNearTile {shopOwner}", LogLevel.Debug, true);
                    if (ShopManager.GrangeShops.TryGetValue(shopOwner, out var shop)) return shop;
                }
            }

            return null;
        }

        /// <summary>
        /// this code is slower than ShopNearTile but kept just in case
        /// </summary>
        /// <param name="tile">GrabTile that user clicked on</param>
        /// <returns>GrangeShop for tile clicked on, or null</returns>
        public static GrangeShop ShopNearTileByOffset(Vector2 tile)
        {
            var bldgTileIndexAt = Game1.currentLocation.getTileIndexAt((int) tile.X, (int) tile.Y, "Buildings");
            var frontTileIndexAt = Game1.currentLocation.getTileIndexAt((int) tile.X, (int) tile.Y, "Front");
            var tileIndexAt = bldgTileIndexAt == -1 ? frontTileIndexAt : bldgTileIndexAt;

            MarketDay.Log($"ShopNearTile {tile} {tileIndexAt}", LogLevel.Debug, false);

            if (!ChestOffsetForTile.TryGetValue(tileIndexAt, out var offset)) return null;

            var search = tile + offset;
            MarketDay.Log($"    ShopNearTile {tile} -> {search}", LogLevel.Debug, false);
            if (!Game1.currentLocation.objects.TryGetValue(search, out var chest) || chest is not Chest) return null;
            chest.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.StockChestKey}",
                out var shopOwner);
            if (shopOwner is null) return null;
            MarketDay.Log($"    ShopNearTile {shopOwner}", LogLevel.Debug, true);
            return ShopManager.GrangeShops.TryGetValue(shopOwner, out var shop) ? shop : null;
        }

        internal static string Owner(Item item)
        {
            item.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.GrangeChestKey}",
                out var grangeChestOwner);
            item.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.StockChestKey}",
                out var stockChestOwner);
            item.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.ShopSignKey}",
                out var signOwner);
            string owner = null;
            if (grangeChestOwner is not null) owner = grangeChestOwner;
            if (stockChestOwner is not null) owner = stockChestOwner;
            if (signOwner is not null) owner = signOwner;
            return owner;
        }
    }
}