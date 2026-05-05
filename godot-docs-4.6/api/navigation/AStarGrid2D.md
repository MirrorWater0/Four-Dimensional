# AStarGrid2D

## Meta

- Name: AStarGrid2D
- Source: AStarGrid2D.xml
- Inherits: RefCounted
- Inheritance Chain: AStarGrid2D -> RefCounted -> Object

## Brief Description

An implementation of A* for finding the shortest path between two points on a partial 2D grid.

## Description

AStarGrid2D is a variant of AStar2D that is specialized for partial 2D grids. It is simpler to use because it doesn't require you to manually create points and connect them together. This class also supports multiple types of heuristics, modes for diagonal movement, and a jumping mode to speed up calculations. To use AStarGrid2D, you only need to set the region of the grid, optionally set the cell_size, and then call the update() method:

```
var astar_grid = AStarGrid2D.new()
astar_grid.region = Rect2i(0, 0, 32, 32)
astar_grid.cell_size = Vector2(16, 16)
astar_grid.update()
print(astar_grid.get_id_path(Vector2i(0, 0), Vector2i(3, 4))) # Prints [(0, 0), (1, 1), (2, 2), (3, 3), (3, 4)]
print(astar_grid.get_point_path(Vector2i(0, 0), Vector2i(3, 4))) # Prints [(0, 0), (16, 16), (32, 32), (48, 48), (48, 64)]
```

```
AStarGrid2D astarGrid = new AStarGrid2D();
astarGrid.Region = new Rect2I(0, 0, 32, 32);
astarGrid.CellSize = new Vector2I(16, 16);
astarGrid.Update();
GD.Print(astarGrid.GetIdPath(Vector2I.Zero, new Vector2I(3, 4))); // Prints [(0, 0), (1, 1), (2, 2), (3, 3), (3, 4)]
GD.Print(astarGrid.GetPointPath(Vector2I.Zero, new Vector2I(3, 4))); // Prints [(0, 0), (16, 16), (32, 32), (48, 48), (48, 64)]
```

To remove a point from the pathfinding grid, it must be set as "solid" with set_point_solid().

## Quick Reference

```
[methods]
_compute_cost(from_id: Vector2i, to_id: Vector2i) -> float [virtual const]
_estimate_cost(from_id: Vector2i, end_id: Vector2i) -> float [virtual const]
clear() -> void
fill_solid_region(region: Rect2i, solid: bool = true) -> void
fill_weight_scale_region(region: Rect2i, weight_scale: float) -> void
get_id_path(from_id: Vector2i, to_id: Vector2i, allow_partial_path: bool = false) -> Vector2i[]
get_point_data_in_region(region: Rect2i) -> Dictionary[] [const]
get_point_path(from_id: Vector2i, to_id: Vector2i, allow_partial_path: bool = false) -> PackedVector2Array
get_point_position(id: Vector2i) -> Vector2 [const]
get_point_weight_scale(id: Vector2i) -> float [const]
is_dirty() -> bool [const]
is_in_bounds(x: int, y: int) -> bool [const]
is_in_boundsv(id: Vector2i) -> bool [const]
is_point_solid(id: Vector2i) -> bool [const]
set_point_solid(id: Vector2i, solid: bool = true) -> void
set_point_weight_scale(id: Vector2i, weight_scale: float) -> void
update() -> void

[properties]
cell_shape: int (AStarGrid2D.CellShape) = 0
cell_size: Vector2 = Vector2(1, 1)
default_compute_heuristic: int (AStarGrid2D.Heuristic) = 0
default_estimate_heuristic: int (AStarGrid2D.Heuristic) = 0
diagonal_mode: int (AStarGrid2D.DiagonalMode) = 0
jumping_enabled: bool = false
offset: Vector2 = Vector2(0, 0)
region: Rect2i = Rect2i(0, 0, 0, 0)
size: Vector2i = Vector2i(0, 0)
```

## Tutorials

- [Grid-based Navigation with AStarGrid2D Demo](https://godotengine.org/asset-library/asset/2723)

## Methods

- _compute_cost(from_id: Vector2i, to_id: Vector2i) -> float [virtual const]
  Called when computing the cost between two connected points. Note that this function is hidden in the default AStarGrid2D class.

- _estimate_cost(from_id: Vector2i, end_id: Vector2i) -> float [virtual const]
  Called when estimating the cost between a point and the path's ending point. Note that this function is hidden in the default AStarGrid2D class.

- clear() -> void
  Clears the grid and sets the region to Rect2i(0, 0, 0, 0).

- fill_solid_region(region: Rect2i, solid: bool = true) -> void
  Fills the given region on the grid with the specified value for the solid flag. **Note:** Calling update() is not needed after the call of this function.

- fill_weight_scale_region(region: Rect2i, weight_scale: float) -> void
  Fills the given region on the grid with the specified value for the weight scale. **Note:** Calling update() is not needed after the call of this function.

- get_id_path(from_id: Vector2i, to_id: Vector2i, allow_partial_path: bool = false) -> Vector2i[]
  Returns an array with the IDs of the points that form the path found by AStar2D between the given points. The array is ordered from the starting point to the ending point of the path. If from_id point is disabled, returns an empty array (even if from_id == to_id). If from_id point is not disabled, there is no valid path to the target, and allow_partial_path is true, returns a path to the point closest to the target that can be reached. **Note:** When allow_partial_path is true and to_id is solid the search may take an unusually long time to finish.

- get_point_data_in_region(region: Rect2i) -> Dictionary[] [const]
  Returns an array of dictionaries with point data (id: Vector2i, position: Vector2, solid: bool, weight_scale: float) within a region.

- get_point_path(from_id: Vector2i, to_id: Vector2i, allow_partial_path: bool = false) -> PackedVector2Array
  Returns an array with the points that are in the path found by AStarGrid2D between the given points. The array is ordered from the starting point to the ending point of the path. If from_id point is disabled, returns an empty array (even if from_id == to_id). If from_id point is not disabled, there is no valid path to the target, and allow_partial_path is true, returns a path to the point closest to the target that can be reached. **Note:** This method is not thread-safe; it can only be used from a single Thread at a given time. Consider using Mutex to ensure exclusive access to one thread to avoid race conditions. Additionally, when allow_partial_path is true and to_id is solid the search may take an unusually long time to finish.

- get_point_position(id: Vector2i) -> Vector2 [const]
  Returns the position of the point associated with the given id.

- get_point_weight_scale(id: Vector2i) -> float [const]
  Returns the weight scale of the point associated with the given id.

- is_dirty() -> bool [const]
  Indicates that the grid parameters were changed and update() needs to be called.

- is_in_bounds(x: int, y: int) -> bool [const]
  Returns true if the x and y is a valid grid coordinate (id), i.e. if it is inside region. Equivalent to region.has_point(Vector2i(x, y)).

- is_in_boundsv(id: Vector2i) -> bool [const]
  Returns true if the id vector is a valid grid coordinate, i.e. if it is inside region. Equivalent to region.has_point(id).

- is_point_solid(id: Vector2i) -> bool [const]
  Returns true if a point is disabled for pathfinding. By default, all points are enabled.

- set_point_solid(id: Vector2i, solid: bool = true) -> void
  Disables or enables the specified point for pathfinding. Useful for making an obstacle. By default, all points are enabled. **Note:** Calling update() is not needed after the call of this function.

- set_point_weight_scale(id: Vector2i, weight_scale: float) -> void
  Sets the weight_scale for the point with the given id. The weight_scale is multiplied by the result of _compute_cost() when determining the overall cost of traveling across a segment from a neighboring point to this point. **Note:** Calling update() is not needed after the call of this function.

- update() -> void
  Updates the internal state of the grid according to the parameters to prepare it to search the path. Needs to be called if parameters like region, cell_size or offset are changed. is_dirty() will return true if this is the case and this needs to be called. **Note:** All point data (solidity and weight scale) will be cleared.

## Properties

- cell_shape: int (AStarGrid2D.CellShape) = 0 [set set_cell_shape; get get_cell_shape]
  The cell shape. Affects how the positions are placed in the grid. If changed, update() needs to be called before finding the next path.

- cell_size: Vector2 = Vector2(1, 1) [set set_cell_size; get get_cell_size]
  The size of the point cell which will be applied to calculate the resulting point position returned by get_point_path(). If changed, update() needs to be called before finding the next path.

- default_compute_heuristic: int (AStarGrid2D.Heuristic) = 0 [set set_default_compute_heuristic; get get_default_compute_heuristic]
  The default Heuristic which will be used to calculate the cost between two points if _compute_cost() was not overridden.

- default_estimate_heuristic: int (AStarGrid2D.Heuristic) = 0 [set set_default_estimate_heuristic; get get_default_estimate_heuristic]
  The default Heuristic which will be used to calculate the cost between the point and the end point if _estimate_cost() was not overridden.

- diagonal_mode: int (AStarGrid2D.DiagonalMode) = 0 [set set_diagonal_mode; get get_diagonal_mode]
  A specific DiagonalMode mode which will force the path to avoid or accept the specified diagonals.

- jumping_enabled: bool = false [set set_jumping_enabled; get is_jumping_enabled]
  Enables or disables jumping to skip up the intermediate points and speeds up the searching algorithm. **Note:** Currently, toggling it on disables the consideration of weight scaling in pathfinding.

- offset: Vector2 = Vector2(0, 0) [set set_offset; get get_offset]
  The offset of the grid which will be applied to calculate the resulting point position returned by get_point_path(). If changed, update() needs to be called before finding the next path.

- region: Rect2i = Rect2i(0, 0, 0, 0) [set set_region; get get_region]
  The region of grid cells available for pathfinding. If changed, update() needs to be called before finding the next path.

- size: Vector2i = Vector2i(0, 0) [set set_size; get get_size]
  The size of the grid (number of cells of size cell_size on each axis). If changed, update() needs to be called before finding the next path.

## Constants

### Enum Heuristic

- HEURISTIC_EUCLIDEAN = 0
  The [Euclidean heuristic](https://en.wikipedia.org/wiki/Euclidean_distance) to be used for the pathfinding using the following formula:

```
dx = abs(to_id.x - from_id.x)
dy = abs(to_id.y - from_id.y)
result = sqrt(dx * dx + dy * dy)
```

**Note:** This is also the internal heuristic used in AStar3D and AStar2D by default (with the inclusion of possible z-axis coordinate).

- HEURISTIC_MANHATTAN = 1
  The [Manhattan heuristic](https://en.wikipedia.org/wiki/Taxicab_geometry) to be used for the pathfinding using the following formula:

```
dx = abs(to_id.x - from_id.x)
dy = abs(to_id.y - from_id.y)
result = dx + dy
```

**Note:** This heuristic is intended to be used with 4-side orthogonal movements, provided by setting the diagonal_mode to DIAGONAL_MODE_NEVER.

- HEURISTIC_OCTILE = 2
  The Octile heuristic to be used for the pathfinding using the following formula:

```
dx = abs(to_id.x - from_id.x)
dy = abs(to_id.y - from_id.y)
f = sqrt(2) - 1
result = (dx < dy) ? f * dx + dy : f * dy + dx;
```

- HEURISTIC_CHEBYSHEV = 3
  The [Chebyshev heuristic](https://en.wikipedia.org/wiki/Chebyshev_distance) to be used for the pathfinding using the following formula:

```
dx = abs(to_id.x - from_id.x)
dy = abs(to_id.y - from_id.y)
result = max(dx, dy)
```

- HEURISTIC_MAX = 4
  Represents the size of the Heuristic enum.

### Enum DiagonalMode

- DIAGONAL_MODE_ALWAYS = 0
  The pathfinding algorithm will ignore solid neighbors around the target cell and allow passing using diagonals.

- DIAGONAL_MODE_NEVER = 1
  The pathfinding algorithm will ignore all diagonals and the way will be always orthogonal.

- DIAGONAL_MODE_AT_LEAST_ONE_WALKABLE = 2
  The pathfinding algorithm will avoid using diagonals if at least two obstacles have been placed around the neighboring cells of the specific path segment.

- DIAGONAL_MODE_ONLY_IF_NO_OBSTACLES = 3
  The pathfinding algorithm will avoid using diagonals if any obstacle has been placed around the neighboring cells of the specific path segment.

- DIAGONAL_MODE_MAX = 4
  Represents the size of the DiagonalMode enum.

### Enum CellShape

- CELL_SHAPE_SQUARE = 0
  Rectangular cell shape.

- CELL_SHAPE_ISOMETRIC_RIGHT = 1
  Diamond cell shape (for isometric look). Cell coordinates layout where the horizontal axis goes up-right, and the vertical one goes down-right.

- CELL_SHAPE_ISOMETRIC_DOWN = 2
  Diamond cell shape (for isometric look). Cell coordinates layout where the horizontal axis goes down-right, and the vertical one goes down-left.

- CELL_SHAPE_MAX = 3
  Represents the size of the CellShape enum.
