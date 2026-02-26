namespace SharpSprite.Infrastructure.Ase
{
    /// <summary>
    /// Magic numbers, chunk-type codes, and format flags used in the
    /// .ase / .aseprite binary format.
    /// </summary>
    internal static class AseConstants
    {
        // ── Magic numbers ─────────────────────────────────────────────────

        public const ushort FileMagic = 0xA5E0;
        public const ushort FrameMagic = 0xF1FA;

        // ── Header flags ──────────────────────────────────────────────────

        public const uint HeaderFlag_LayerOpacityValid = 1;
        public const uint HeaderFlag_GroupOpacityValid = 2;
        public const uint HeaderFlag_LayersHaveUUID = 4;

        // ── Color depth constants ─────────────────────────────────────────

        public const ushort ColorDepth_RGBA = 32;
        public const ushort ColorDepth_Grayscale = 16;
        public const ushort ColorDepth_Indexed = 8;

        // ── Chunk types ───────────────────────────────────────────────────

        public const ushort Chunk_OldPalette0004 = 0x0004;
        public const ushort Chunk_OldPalette0011 = 0x0011;
        public const ushort Chunk_Layer = 0x2004;
        public const ushort Chunk_Cel = 0x2005;
        public const ushort Chunk_CelExtra = 0x2006;
        public const ushort Chunk_ColorProfile = 0x2007;
        public const ushort Chunk_ExternalFiles = 0x2008;
        public const ushort Chunk_Tags = 0x2018;
        public const ushort Chunk_Palette = 0x2019;
        public const ushort Chunk_UserData = 0x2020;
        public const ushort Chunk_Slice = 0x2022;
        public const ushort Chunk_Tileset = 0x2023;

        // ── Layer types ───────────────────────────────────────────────────

        public const ushort LayerType_Image = 0;
        public const ushort LayerType_Group = 1;
        public const ushort LayerType_Tilemap = 2;

        // ── Cel types ─────────────────────────────────────────────────────

        public const ushort CelType_RawImage = 0;
        public const ushort CelType_Linked = 1;
        public const ushort CelType_CompressedImage = 2;
        public const ushort CelType_CompressedTilemap = 3;

        // ── Tileset flags ─────────────────────────────────────────────────

        public const uint TilesetFlag_ExternalLink = 1;
        public const uint TilesetFlag_EmbedTiles = 2;
        public const uint TilesetFlag_EmptyTileIs0 = 4;

        // ── User data flags ───────────────────────────────────────────────

        public const uint UserDataFlag_HasText = 1;
        public const uint UserDataFlag_HasColor = 2;
        public const uint UserDataFlag_HasProperties = 4;

        // ── Slice flags ───────────────────────────────────────────────────

        public const uint SliceFlag_9Patch = 1;
        public const uint SliceFlag_Pivot = 2;

        // ── Palette entry flags ───────────────────────────────────────────

        public const ushort PaletteEntryFlag_HasName = 1;

        // ── Header size ───────────────────────────────────────────────────

        public const int HeaderSize = 128;
        public const int FrameHeaderSize = 16;
        public const int ChunkHeaderSize = 6; // DWORD size + WORD type
    }
}