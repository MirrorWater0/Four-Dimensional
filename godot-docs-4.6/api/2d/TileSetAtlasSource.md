# TileSetAtlasSource

## Meta

- Name: TileSetAtlasSource
- Source: TileSetAtlasSource.xml
- Inherits: TileSetSource
- Inheritance Chain: TileSetAtlasSource -> TileSetSource -> Resource -> RefCounted -> Object

## Brief Description

Exposes a 2D atlas texture as a set of tiles for a TileSet resource.

## Description

An atlas is a grid of tiles laid out on a texture. Each tile in the grid must be exposed using create_tile(). Those tiles are then indexed using their coordinates in the grid. Each tile can also have a size in the grid coordinates, making it more or less cells in the atlas. Alternatives version of a tile can be created using create_alternative_tile(), which are then indexed using an alternative ID. The main tile (the one in the grid), is accessed with an alternative ID equal to 0. Each tile alternate has a set of properties that is defined by the source's TileSet layers. Those properties are stored in a TileData object that can be accessed and modified using get_tile_data(). As TileData properties are stored directly in the TileSetAtlasSource resource, their properties might also be set using TileSetAtlasSource.set("<coords_x>:<coords_y>/<alternative_id>/<tile_data_property>").

## Quick Reference

```
[methods]
clear_tiles_outside_texture() -> void
create_alternative_tile(atlas_coords: Vector2i, alternative_id_override: int = -1) -> int
create_tile(atlas_coords: Vector2i, size: Vector2i = Vector2i(1, 1)) -> void
get_atlas_grid_size() -> Vector2i [const]
get_next_alternative_tile_id(atlas_coords: Vector2i) -> int [const]
get_runtime_texture() -> Texture2D [const]
get_runtime_tile_texture_region(atlas_coords: Vector2i, frame: int) -> Rect2i [const]
get_tile_animation_columns(atlas_coords: Vector2i) -> int [const]
get_tile_animation_frame_duration(atlas_coords: Vector2i, frame_index: int) -> float [const]
get_tile_animation_frames_count(atlas_coords: Vector2i) -> int [const]
get_tile_animation_mode(atlas_coords: Vector2i) -> int (TileSetAtlasSource.TileAnimationMode) [const]
get_tile_animation_separation(atlas_coords: Vector2i) -> Vector2i [const]
get_tile_animation_speed(atlas_coords: Vector2i) -> float [const]
get_tile_animation_total_duration(atlas_coords: Vector2i) -> float [const]
get_tile_at_coords(atlas_coords: Vector2i) -> Vector2i [const]
get_tile_data(atlas_coords: Vector2i, alternative_tile: int) -> TileData [const]
get_tile_size_in_atlas(atlas_coords: Vector2i) -> Vector2i [const]
get_tile_texture_region(atlas_coords: Vector2i, frame: int = 0) -> Rect2i [const]
get_tiles_to_be_removed_on_change(texture: Texture2D, margins: Vector2i, separation: Vector2i, texture_region_size: Vector2i) -> PackedVector2Array
has_room_for_tile(atlas_coords: Vector2i, size: Vector2i, animation_columns: int, animation_separation: Vector2i, frames_count: int, ignored_tile: Vector2i = Vector2i(-1, -1)) -> bool [const]
has_tiles_outside_texture() -> bool [const]
move_tile_in_atlas(atlas_coords: Vector2i, new_atlas_coords: Vector2i = Vector2i(-1, -1), new_size: Vector2i = Vector2i(-1, -1)) -> void
remove_alternative_tile(atlas_coords: Vector2i, alternative_tile: int) -> void
remove_tile(atlas_coords: Vector2i) -> void
set_alternative_tile_id(atlas_coords: Vector2i, alternative_tile: int, new_id: int) -> void
set_tile_animation_columns(atlas_coords: Vector2i, frame_columns: int) -> void
set_tile_animation_frame_duration(atlas_coords: Vector2i, frame_index: int, duration: float) -> void
set_tile_animation_frames_count(atlas_coords: Vector2i, frames_count: int) -> void
set_tile_animation_mode(atlas_coords: Vector2i, mode: int (TileSetAtlasSource.TileAnimationMode)) -> void
set_tile_animation_separation(atlas_coords: Vector2i, separation: Vector2i) -> void
set_tile_animation_speed(atlas_coords: Vector2i, speed: float) -> void

[properties]
margins: Vector2i = Vector2i(0, 0)
separation: Vector2i = Vector2i(0, 0)
texture: Texture2D
texture_region_size: Vector2i = Vector2i(16, 16)
use_texture_padding: bool = true
```

## Methods

- clear_tiles_outside_texture() -> void
  Removes all tiles that don't fit the available texture area. This method iterates over all the source's tiles, so it's advised to use has_tiles_outside_texture() beforehand.

- create_alternative_tile(atlas_coords: Vector2i, alternative_id_override: int = -1) -> int
  Creates an alternative tile for the tile at coordinates atlas_coords. If alternative_id_override is -1, give it an automatically generated unique ID, or assigns it the given ID otherwise. Returns the new alternative identifier, or -1 if the alternative could not be created with a provided alternative_id_override.

- create_tile(atlas_coords: Vector2i, size: Vector2i = Vector2i(1, 1)) -> void
  Creates a new tile at coordinates atlas_coords with the given size.

- get_atlas_grid_size() -> Vector2i [const]
  Returns the atlas grid size, which depends on how many tiles can fit in the texture. It thus depends on the texture's size, the atlas margins, and the tiles' texture_region_size.

- get_next_alternative_tile_id(atlas_coords: Vector2i) -> int [const]
  Returns the alternative ID a following call to create_alternative_tile() would return.

- get_runtime_texture() -> Texture2D [const]
  If use_texture_padding is false, returns texture. Otherwise, returns an internal ImageTexture created that includes the padding.

- get_runtime_tile_texture_region(atlas_coords: Vector2i, frame: int) -> Rect2i [const]
  Returns the region of the tile at coordinates atlas_coords for the given frame inside the texture returned by get_runtime_texture(). **Note:** If use_texture_padding is false, returns the same as get_tile_texture_region().

- get_tile_animation_columns(atlas_coords: Vector2i) -> int [const]
  Returns how many columns the tile at atlas_coords has in its animation layout.

- get_tile_animation_frame_duration(atlas_coords: Vector2i, frame_index: int) -> float [const]
  Returns the animation frame duration of frame frame_index for the tile at coordinates atlas_coords.

- get_tile_animation_frames_count(atlas_coords: Vector2i) -> int [const]
  Returns how many animation frames has the tile at coordinates atlas_coords.

- get_tile_animation_mode(atlas_coords: Vector2i) -> int (TileSetAtlasSource.TileAnimationMode) [const]
  Returns the tile animation mode of the tile at atlas_coords. See also set_tile_animation_mode().

- get_tile_animation_separation(atlas_coords: Vector2i) -> Vector2i [const]
  Returns the separation (as in the atlas grid) between each frame of an animated tile at coordinates atlas_coords.

- get_tile_animation_speed(atlas_coords: Vector2i) -> float [const]
  Returns the animation speed of the tile at coordinates atlas_coords.

- get_tile_animation_total_duration(atlas_coords: Vector2i) -> float [const]
  Returns the sum of the sum of the frame durations of the tile at coordinates atlas_coords. This value needs to be divided by the animation speed to get the actual animation loop duration.

- get_tile_at_coords(atlas_coords: Vector2i) -> Vector2i [const]
  If there is a tile covering the atlas_coords coordinates, returns the top-left coordinates of the tile (thus its coordinate ID). Returns Vector2i(-1, -1) otherwise.

- get_tile_data(atlas_coords: Vector2i, alternative_tile: int) -> TileData [const]
  Returns the TileData object for the given atlas coordinates and alternative ID.

- get_tile_size_in_atlas(atlas_coords: Vector2i) -> Vector2i [const]
  Returns the size of the tile (in the grid coordinates system) at coordinates atlas_coords.

- get_tile_texture_region(atlas_coords: Vector2i, frame: int = 0) -> Rect2i [const]
  Returns a tile's texture region in the atlas texture. For animated tiles, a frame argument might be provided for the different frames of the animation.

- get_tiles_to_be_removed_on_change(texture: Texture2D, margins: Vector2i, separation: Vector2i, texture_region_size: Vector2i) -> PackedVector2Array
  Returns an array of tiles coordinates ID that will be automatically removed when modifying one or several of those properties: texture, margins, separation or texture_region_size. This can be used to undo changes that would have caused tiles data loss.

- has_room_for_tile(atlas_coords: Vector2i, size: Vector2i, animation_columns: int, animation_separation: Vector2i, frames_count: int, ignored_tile: Vector2i = Vector2i(-1, -1)) -> bool [const]
  Returns whether there is enough room in an atlas to create/modify a tile with the given properties. If ignored_tile is provided, act as is the given tile was not present in the atlas. This may be used when you want to modify a tile's properties.

- has_tiles_outside_texture() -> bool [const]
  Checks if the source has any tiles that don't fit the texture area (either partially or completely).

- move_tile_in_atlas(atlas_coords: Vector2i, new_atlas_coords: Vector2i = Vector2i(-1, -1), new_size: Vector2i = Vector2i(-1, -1)) -> void
  Move the tile and its alternatives at the atlas_coords coordinates to the new_atlas_coords coordinates with the new_size size. This functions will fail if a tile is already present in the given area. If new_atlas_coords is Vector2i(-1, -1), keeps the tile's coordinates. If new_size is Vector2i(-1, -1), keeps the tile's size. To avoid an error, first check if a move is possible using has_room_for_tile().

- remove_alternative_tile(atlas_coords: Vector2i, alternative_tile: int) -> void
  Remove a tile's alternative with alternative ID alternative_tile. Calling this function with alternative_tile equals to 0 will fail, as the base tile alternative cannot be removed.

- remove_tile(atlas_coords: Vector2i) -> void
  Remove a tile and its alternative at coordinates atlas_coords.

- set_alternative_tile_id(atlas_coords: Vector2i, alternative_tile: int, new_id: int) -> void
  Change a tile's alternative ID from alternative_tile to new_id. Calling this function with new_id of 0 will fail, as the base tile alternative cannot be moved.

- set_tile_animation_columns(atlas_coords: Vector2i, frame_columns: int) -> void
  Sets the number of columns in the animation layout of the tile at coordinates atlas_coords. If set to 0, then the different frames of the animation are laid out as a single horizontal line in the atlas.

- set_tile_animation_frame_duration(atlas_coords: Vector2i, frame_index: int, duration: float) -> void
  Sets the animation frame duration of frame frame_index for the tile at coordinates atlas_coords.

- set_tile_animation_frames_count(atlas_coords: Vector2i, frames_count: int) -> void
  Sets how many animation frames the tile at coordinates atlas_coords has.

- set_tile_animation_mode(atlas_coords: Vector2i, mode: int (TileSetAtlasSource.TileAnimationMode)) -> void
  Sets the tile animation mode of the tile at atlas_coords to mode. See also get_tile_animation_mode().

- set_tile_animation_separation(atlas_coords: Vector2i, separation: Vector2i) -> void
  Sets the margin (in grid tiles) between each tile in the animation layout of the tile at coordinates atlas_coords has.

- set_tile_animation_speed(atlas_coords: Vector2i, speed: float) -> void
  Sets the animation speed of the tile at coordinates atlas_coords has.

## Properties

- margins: Vector2i = Vector2i(0, 0) [set set_margins; get get_margins]
  Margins, in pixels, to offset the origin of the grid in the texture.

- separation: Vector2i = Vector2i(0, 0) [set set_separation; get get_separation]
  Separation, in pixels, between each tile texture region of the grid.

- texture: Texture2D [set set_texture; get get_texture]
  The atlas texture.

- texture_region_size: Vector2i = Vector2i(16, 16) [set set_texture_region_size; get get_texture_region_size]
  The base tile size in the texture (in pixel). This size must be bigger than or equal to the TileSet's tile_size value.

- use_texture_padding: bool = true [set set_use_texture_padding; get get_use_texture_padding]
  If true, generates an internal texture with an additional one pixel padding around each tile. Texture padding avoids a common artifact where lines appear between tiles. Disabling this setting might lead a small performance improvement, as generating the internal texture requires both memory and processing time when the TileSetAtlasSource resource is modified.

## Constants

### Enum TileAnimationMode

- TILE_ANIMATION_MODE_DEFAULT = 0
  Tile animations start at same time, looking identical.

- TILE_ANIMATION_MODE_RANDOM_START_TIMES = 1
  Tile animations start at random times, looking varied.

- TILE_ANIMATION_MODE_MAX = 2
  Represents the size of the TileAnimationMode enum.

- TRANSFORM_FLIP_H = 4096
  Represents cell's horizontal flip flag. Should be used directly with TileMapLayer to flip placed tiles by altering their alternative IDs.

```
var alternate_id = $TileMapLayer.get_cell_alternative_tile(Vector2i(2, 2))
if not alternate_id & TileSetAtlasSource.TRANSFORM_FLIP_H:
    # If tile is not already flipped, flip it.
    $TileMapLayer.set_cell(Vector2i(2, 2), source_id, atlas_coords, alternate_id | TileSetAtlasSource.TRANSFORM_FLIP_H)
```

**Note:** These transformations can be combined to do the equivalent of 0, 90, 180, and 270 degree rotations, as shown below:

```
enum TileTransform {
    ROTATE_0 = 0,
    ROTATE_90 = TileSetAtlasSource.TRANSFORM_TRANSPOSE | TileSetAtlasSource.TRANSFORM_FLIP_H,
    ROTATE_180 = TileSetAtlasSource.TRANSFORM_FLIP_H | TileSetAtlasSource.TRANSFORM_FLIP_V,
    ROTATE_270 = TileSetAtlasSource.TRANSFORM_TRANSPOSE | TileSetAtlasSource.TRANSFORM_FLIP_V,
}
```

- TRANSFORM_FLIP_V = 8192
  Represents cell's vertical flip flag. See TRANSFORM_FLIP_H for usage.

- TRANSFORM_TRANSPOSE = 16384
  Represents cell's transposed flag. See TRANSFORM_FLIP_H for usage.
