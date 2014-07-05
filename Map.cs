﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TiledSharp;

namespace Abolished {
    // <summary>
    // Map component which draws a Map on the screen.
    // </summary>
    public class MapRenderer {
        private Map Map;

        public MapRenderer(Map map) {
            Map = map;
        }

        // <summary>
        // Draws the map.
        // <param name="batch">SpriteBatch on which to draw.</param>
        // <param name="viewport">Rectangle of the map to draw. Specifed in pixel, not tile coordinates, to enable smooth scrolling.</param>
        // </summary>
        public void Draw(SpriteBatch batch, Rectangle viewport) {

            // Convert pixel coordinates to the corresponding tiles
            var iStart = (int)Math.Floor((double)viewport.X / Map.TileWidth);
            var iEnd = iStart + (int)Math.Floor((double)viewport.Width / Map.TileWidth) + 1;

            var jStart = (int)Math.Floor((double)viewport.Y / Map.TileHeight);
            var jEnd = jStart + (int)Math.Floor((double)viewport.Height / Map.TileHeight) + 1;

            // Ensure we're actually drawing stuff that's inside the map
            iEnd = Math.Min(iEnd, Map.Width);
            jEnd = Math.Min(jEnd, Map.Height);
            
            var xRemainder = viewport.X % Map.TileWidth;
            var yRemainder = viewport.Y % Map.TileHeight;

            // Draw tiles inside canvas
            foreach (var layer in Map.Layers) {
                for (var i = iStart; i < iEnd; i++) {
                    for (var j = jStart; j < jEnd; j++) {
                        var tile = layer[i,j];

                        // Empty tiles have no graphical information                       
                        if (tile.TileSheet == null) {
                            continue;
                        }

                        var position = new Vector2(
                            Map.TileWidth * (i - iStart) - xRemainder,
                            Map.TileHeight * (j - jStart) - yRemainder);
                        
                        batch.Draw(tile.TileSheet, position,
                                   tile.Rectangle, Color.White, 0.0f, new Vector2(0, 0),
                                   1, SpriteEffects.None, 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a single kind of tile on a tilesheet.
    /// </summary>
    public class Tile {
        public Texture2D TileSheet; // Tilesheet containing this tile
        public Rectangle Rectangle; // Where on the tilesheet it is
        public PropertyDict Properties; // TMX tile properties

        public Tile() {
        }
    }


    // <summary>
    // This class represents a game map, providing a common interface to
    // the underlying Tiled map data. Gameplay-related functionality such
    // as tile passability should be calculated here; graphics stuff goes
    // in MapRenderer.
    // </summary>
    public class Map {
        public Dictionary<int, PropertyDict> TileProps; // tile id => tile properties

        public MapRenderer Renderer;

        public List<Tile[,]> Layers; // Tile layers, from bottom to top
        public Tile[,] Tiles; // Combined "topmost" layer (i.e. each tile from the highest layer at that position)

        // Width and height, in number of tiles
        public int Width;
        public int Height;

        // Width and height of each tile, in pixels
        public int TileWidth;
        public int TileHeight;

        public Map() {
            Renderer = new MapRenderer(this);
        }

        public void Initialize(TmxMap tmx) {
            Width = tmx.Width;
            Height = tmx.Height;
            TileWidth = tmx.TileWidth;
            TileHeight = tmx.TileHeight;

            // Temporary tmx tile id => TileType
            var tileTypes = new Dictionary<int, Tile>();

            // tileTypes[0] is the default "air" tile
            // We currently convey this by setting the TileSheet to null
            var emptyTile = new Tile();
            emptyTile.TileSheet = null;
            tileTypes[0] = emptyTile;

            foreach (TmxTileset ts in tmx.Tilesets) {
                var tileSheet = GetTileSheet(ts.Image.Source);

                // Loop over the tilesheet and calculate rectangles indicating where
                // each Tile resides. Note that we need to account for Tiled's margin
                // and spacing settings
                var wStart = ts.Margin;
                var wInc = ts.TileWidth + ts.Spacing;
                var wEnd = ts.Image.Width - (ts.Image.Width % (ts.TileWidth + ts.Spacing));

                var hStart = ts.Margin;
                var hInc = ts.TileHeight + ts.Spacing;
                var hEnd = ts.Image.Height - (ts.Image.Height % (ts.TileHeight + ts.Spacing));

                var id = ts.FirstGid;
                for (var h = hStart; h < hEnd; h += hInc) {
                    for (var w = wStart; w < wEnd; w += wInc) {                        
                        var tileType = new Tile();
                        tileType.TileSheet = tileSheet;
                        tileType.Rectangle = new Rectangle(w, h, ts.TileWidth, ts.TileHeight);                        
                        tileTypes[id] = tileType;
                        id += 1;
                    }
                }
   
                foreach (TmxTilesetTile tile in ts.Tiles) {
                    tileTypes[tile.Id].Properties = tile.Properties;
                }
            }

            // Compute map structure and gameplay tiles
            // Individual layers are used for rendering
            // The combined layer represents the topmost tiles (i.e. those with no other tiles in front of them)

            Layers = new List<Tile[,]>();
            Tiles = new Tile[Width, Height]; // Combined layer

            foreach (TmxLayer tmxLayer in tmx.Layers) {
                var layer = new Tile[Width, Height];

                foreach (TmxLayerTile tile in tmxLayer.Tiles) {
                    var tileType = tileTypes[tile.Gid];
                    layer[tile.X, tile.Y] = tileType;
                    Tiles[tile.X, tile.Y] = tileType;
                }

                Layers.Add(layer);
            }
        }

        public Texture2D GetTileSheet(string filepath) {
            Texture2D newSheet;
            Stream imgStream;

            imgStream = File.OpenRead(filepath);

            newSheet = Texture2D.FromStream(Game1.current.Graphics, imgStream);
            return newSheet;
        }
    }
}