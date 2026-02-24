
namespace SharpSprite.Core.Document
{
    // -------------------------------------------------------------------------
    // Tileset
    // -------------------------------------------------------------------------

    /// <summary>
    /// A named collection of tiles used by one or more <see cref="LayerTilemap"/>s.
    /// <para>
    /// Tile 0 is always reserved as the "empty" transparent tile.
    /// All other indices are valid image tiles.
    /// </para>
    /// </summary>
    public sealed class Tileset
    {
        private static int _nextId = 1;

        private readonly List<Image> _tiles = new();

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        /// <summary>
        /// Create a new tileset.
        /// </summary>
        /// <param name="tileWidth">Width of each tile in pixels.</param>
        /// <param name="tileHeight">Height of each tile in pixels.</param>
        /// <param name="colorMode">Color mode for tile images.</param>
        public Tileset(int tileWidth, int tileHeight, ColorMode colorMode = ColorMode.Rgba)
        {
            if (tileWidth <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
            if (tileHeight <= 0) throw new ArgumentOutOfRangeException(nameof(tileHeight));

            Id = _nextId++;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            ColorMode = colorMode;

            // Reserve tile 0 as the empty / transparent tile.
            _tiles.Add(CreateEmptyTile());
        }

        // ------------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------------

        /// <summary>Unique runtime identifier.</summary>
        public int Id { get; }

        /// <summary>Display name for the tileset.</summary>
        public string Name { get; set; } = "Tileset";

        public int TileWidth { get; }
        public int TileHeight { get; }
        public ColorMode ColorMode { get; }

        /// <summary>
        /// Base index offset – So the first non-empty tile
        /// can be numbered 1 instead of 0 in the tile picker UI.
        /// </summary>
        public int BaseIndex { get; set; } = 1;

        /// <summary>Total number of tiles including tile 0 (the empty tile).</summary>
        public int Count => _tiles.Count;

        /// <summary>User-defined metadata.</summary>
        public UserData UserData { get; } = new();

        // ------------------------------------------------------------------
        // Tile access
        // ------------------------------------------------------------------

        /// <summary>Get the image for tile at <paramref name="index"/>.</summary>
        public Image GetTile(int index)
        {
            if ((uint)index >= (uint)_tiles.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _tiles[index];
        }

        /// <summary>Replace the image for an existing tile.</summary>
        public void SetTile(int index, Image image)
        {
            if ((uint)index >= (uint)_tiles.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index == 0)
                throw new InvalidOperationException("Tile 0 (empty tile) cannot be replaced.");
            ValidateTileImage(image);
            _tiles[index] = image;
        }

        /// <summary>
        /// Append a new tile and return its index.
        /// The image must match <see cref="TileWidth"/> × <see cref="TileHeight"/>
        /// and <see cref="ColorMode"/>.
        /// </summary>
        public int AddTile(Image image)
        {
            ValidateTileImage(image);
            _tiles.Add(image);
            return _tiles.Count - 1;
        }

        /// <summary>Append a blank (transparent) tile and return its index.</summary>
        public int AddEmptyTile()
        {
            _tiles.Add(CreateEmptyTile());
            return _tiles.Count - 1;
        }

        /// <summary>
        /// Remove the tile at <paramref name="index"/>.
        /// Tile 0 cannot be removed.
        /// WARNING: callers are responsible for updating any tilemaps that
        /// reference tiles at or above the removed index.
        /// </summary>
        public void RemoveTile(int index)
        {
            if (index == 0)
                throw new InvalidOperationException("Tile 0 (empty tile) cannot be removed.");
            if ((uint)index >= (uint)_tiles.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _tiles.RemoveAt(index);
        }

        /// <summary>All tile images (index 0 is the empty tile).</summary>
        public IReadOnlyList<Image> Tiles => _tiles;

        // ------------------------------------------------------------------
        // Tile reference helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Decompose a raw tile cell value into its components.
        /// </summary>
        public static (uint Index, bool FlipX, bool FlipY, bool Rotate90)
            DecodeTileRef(uint tileRef)
        {
            return (
                tileRef & TileConstants.IndexMask,
                (tileRef & (uint)TileFlags.FlipX) != 0,
                (tileRef & (uint)TileFlags.FlipY) != 0,
                (tileRef & (uint)TileFlags.Rotate90) != 0
            );
        }

        /// <summary>
        /// Encode tile index + flip flags into a raw tile cell value.
        /// </summary>
        public static uint EncodeTileRef(uint index, bool flipX = false, bool flipY = false, bool rotate90 = false)
        {
            uint v = index & TileConstants.IndexMask;
            if (flipX) v |= (uint)TileFlags.FlipX;
            if (flipY) v |= (uint)TileFlags.FlipY;
            if (rotate90) v |= (uint)TileFlags.Rotate90;
            return v;
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private Image CreateEmptyTile()
        {
            var img = new Image(TileWidth, TileHeight, ColorMode);
            img.Clear();
            return img;
        }

        private void ValidateTileImage(Image image)
        {
            if (image.Width != TileWidth ||
                image.Height != TileHeight ||
                image.ColorMode != ColorMode)
            {
                throw new ArgumentException(
                    $"Tile image must be {TileWidth}×{TileHeight} in {ColorMode} mode.");
            }
        }

        public Tileset Clone()
        {
            var ts = new Tileset(TileWidth, TileHeight, ColorMode) { Name = Name, BaseIndex = BaseIndex };
            // tile 0 already created by constructor; copy it
            ts._tiles[0] = _tiles[0].Clone();
            for (int i = 1; i < _tiles.Count; i++)
                ts._tiles.Add(_tiles[i].Clone());
            ts.UserData.Text = UserData.Text;
            ts.UserData.Color = UserData.Color;
            return ts;
        }
    }
}
