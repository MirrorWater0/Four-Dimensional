# AStar3D

## Meta

- Name: AStar3D
- Source: AStar3D.xml
- Inherits: RefCounted
- Inheritance Chain: AStar3D -> RefCounted -> Object

## Brief Description

An implementation of A* for finding the shortest path between two vertices on a connected graph in 3D space.

## Description

A* (A star) is a computer algorithm used in pathfinding and graph traversal, the process of plotting short paths among vertices (points), passing through a given set of edges (segments). It enjoys widespread use due to its performance and accuracy. Godot's A* implementation uses points in 3D space and Euclidean distances by default. You must add points manually with add_point() and create segments manually with connect_points(). Once done, you can test if there is a path between two points with the are_points_connected() function, get a path containing indices by get_id_path(), or one containing actual coordinates with get_point_path(). It is also possible to use non-Euclidean distances. To do so, create a script that extends AStar3D and override the methods _compute_cost() and _estimate_cost(). Both should take two point IDs and return the distance between the corresponding points. **Example:** Use Manhattan distance instead of Euclidean distance:

```
class_name MyAStar3D
extends AStar3D

func _compute_cost(u, v):
    var u_pos = get_point_position(u)
    var v_pos = get_point_position(v)
    return abs(u_pos.x - v_pos.x) + abs(u_pos.y - v_pos.y) + abs(u_pos.z - v_pos.z)

func _estimate_cost(u, v):
    var u_pos = get_point_position(u)
    var v_pos = get_point_position(v)
    return abs(u_pos.x - v_pos.x) + abs(u_pos.y - v_pos.y) + abs(u_pos.z - v_pos.z)
```

```
using Godot;

GlobalClass
public partial class MyAStar3D : AStar3D
{
    public override float _ComputeCost(long fromId, long toId)
    {
        Vector3 fromPoint = GetPointPosition(fromId);
        Vector3 toPoint = GetPointPosition(toId);

        return Mathf.Abs(fromPoint.X - toPoint.X) + Mathf.Abs(fromPoint.Y - toPoint.Y) + Mathf.Abs(fromPoint.Z - toPoint.Z);
    }

    public override float _EstimateCost(long fromId, long toId)
    {
        Vector3 fromPoint = GetPointPosition(fromId);
        Vector3 toPoint = GetPointPosition(toId);
        return Mathf.Abs(fromPoint.X - toPoint.X) + Mathf.Abs(fromPoint.Y - toPoint.Y) + Mathf.Abs(fromPoint.Z - toPoint.Z);
    }
}
```

_estimate_cost() should return a lower bound of the distance, i.e. _estimate_cost(u, v) <= _compute_cost(u, v). This serves as a hint to the algorithm because the custom _compute_cost() might be computation-heavy. If this is not the case, make _estimate_cost() return the same value as _compute_cost() to provide the algorithm with the most accurate information. If the default _estimate_cost() and _compute_cost() methods are used, or if the supplied _estimate_cost() method returns a lower bound of the cost, then the paths returned by A* will be the lowest-cost paths. Here, the cost of a path equals the sum of the _compute_cost() results of all segments in the path multiplied by the weight_scales of the endpoints of the respective segments. If the default methods are used and the weight_scales of all points are set to 1.0, then this equals the sum of Euclidean distances of all segments in the path.

## Quick Reference

```
[methods]
_compute_cost(from_id: int, to_id: int) -> float [virtual const]
_estimate_cost(from_id: int, end_id: int) -> float [virtual const]
_filter_neighbor(from_id: int, neighbor_id: int) -> bool [virtual const]
add_point(id: int, position: Vector3, weight_scale: float = 1.0) -> void
are_points_connected(id: int, to_id: int, bidirectional: bool = true) -> bool [const]
clear() -> void
connect_points(id: int, to_id: int, bidirectional: bool = true) -> void
disconnect_points(id: int, to_id: int, bidirectional: bool = true) -> void
get_available_point_id() -> int [const]
get_closest_point(to_position: Vector3, include_disabled: bool = false) -> int [const]
get_closest_position_in_segment(to_position: Vector3) -> Vector3 [const]
get_id_path(from_id: int, to_id: int, allow_partial_path: bool = false) -> PackedInt64Array
get_point_capacity() -> int [const]
get_point_connections(id: int) -> PackedInt64Array
get_point_count() -> int [const]
get_point_ids() -> PackedInt64Array
get_point_path(from_id: int, to_id: int, allow_partial_path: bool = false) -> PackedVector3Array
get_point_position(id: int) -> Vector3 [const]
get_point_weight_scale(id: int) -> float [const]
has_point(id: int) -> bool [const]
is_point_disabled(id: int) -> bool [const]
remove_point(id: int) -> void
reserve_space(num_nodes: int) -> void
set_point_disabled(id: int, disabled: bool = true) -> void
set_point_position(id: int, position: Vector3) -> void
set_point_weight_scale(id: int, weight_scale: float) -> void

[properties]
neighbor_filter_enabled: bool = false
```

## Methods

- _compute_cost(from_id: int, to_id: int) -> float [virtual const]
  Called when computing the cost between two connected points. Note that this function is hidden in the default AStar3D class.

- _estimate_cost(from_id: int, end_id: int) -> float [virtual const]
  Called when estimating the cost between a point and the path's ending point. Note that this function is hidden in the default AStar3D class.

- _filter_neighbor(from_id: int, neighbor_id: int) -> bool [virtual const]
  Called when neighboring point enters processing and if neighbor_filter_enabled is true. If true is returned the point will not be processed. Note that this function is hidden in the default AStar3D class.

- add_point(id: int, position: Vector3, weight_scale: float = 1.0) -> void
  Adds a new point at the given position with the given identifier. The id must be 0 or larger, and the weight_scale must be 0.0 or greater. The weight_scale is multiplied by the result of _compute_cost() when determining the overall cost of traveling across a segment from a neighboring point to this point. Thus, all else being equal, the algorithm prefers points with lower weight_scales to form a path.


```
  var astar = AStar3D.new()
  astar.add_point(1, Vector3(1, 0, 0), 4) # Adds the point (1, 0, 0) with weight_scale 4 and id 1

```

```
  var astar = new AStar3D();
  astar.AddPoint(1, new Vector3(1, 0, 0), 4); // Adds the point (1, 0, 0) with weight_scale 4 and id 1

```
  If there already exists a point for the given id, its position and weight scale are updated to the given values.

- are_points_connected(id: int, to_id: int, bidirectional: bool = true) -> bool [const]
  Returns whether the two given points are directly connected by a segment. If bidirectional is false, returns whether movement from id to to_id is possible through this segment.

- clear() -> void
  Clears all the points and segments.

- connect_points(id: int, to_id: int, bidirectional: bool = true) -> void
  Creates a segment between the given points. If bidirectional is false, only movement from id to to_id is allowed, not the reverse direction.


```
  var astar = AStar3D.new()
  astar.add_point(1, Vector3(1, 1, 0))
  astar.add_point(2, Vector3(0, 5, 0))
  astar.connect_points(1, 2, false)

```

```
  var astar = new AStar3D();
  astar.AddPoint(1, new Vector3(1, 1, 0));
  astar.AddPoint(2, new Vector3(0, 5, 0));
  astar.ConnectPoints(1, 2, false);

```

- disconnect_points(id: int, to_id: int, bidirectional: bool = true) -> void
  Deletes the segment between the given points. If bidirectional is false, only movement from id to to_id is prevented, and a unidirectional segment possibly remains.

- get_available_point_id() -> int [const]
  Returns the next available point ID with no point associated to it.

- get_closest_point(to_position: Vector3, include_disabled: bool = false) -> int [const]
  Returns the ID of the closest point to to_position, optionally taking disabled points into account. Returns -1 if there are no points in the points pool. **Note:** If several points are the closest to to_position, the one with the smallest ID will be returned, ensuring a deterministic result.

- get_closest_position_in_segment(to_position: Vector3) -> Vector3 [const]
  Returns the closest position to to_position that resides inside a segment between two connected points.


```
  var astar = AStar3D.new()
  astar.add_point(1, Vector3(0, 0, 0))
  astar.add_point(2, Vector3(0, 5, 0))
  astar.connect_points(1, 2)
  var res = astar.get_closest_position_in_segment(Vector3(3, 3, 0)) # Returns (0, 3, 0)

```

```
  var astar = new AStar3D();
  astar.AddPoint(1, new Vector3(0, 0, 0));
  astar.AddPoint(2, new Vector3(0, 5, 0));
  astar.ConnectPoints(1, 2);
  Vector3 res = astar.GetClosestPositionInSegment(new Vector3(3, 3, 0)); // Returns (0, 3, 0)

```
  The result is in the segment that goes from y = 0 to y = 5. It's the closest position in the segment to the given point.

- get_id_path(from_id: int, to_id: int, allow_partial_path: bool = false) -> PackedInt64Array
  Returns an array with the IDs of the points that form the path found by AStar3D between the given points. The array is ordered from the starting point to the ending point of the path. If from_id point is disabled, returns an empty array (even if from_id == to_id). If from_id point is not disabled, there is no valid path to the target, and allow_partial_path is true, returns a path to the point closest to the target that can be reached. **Note:** When allow_partial_path is true and to_id is disabled the search may take an unusually long time to finish.


```
  var astar = AStar3D.new()
  astar.add_point(1, Vector3(0, 0, 0))
  astar.add_point(2, Vector3(0, 1, 0), 1) # Default weight is 1
  astar.add_point(3, Vector3(1, 1, 0))
  astar.add_point(4, Vector3(2, 0, 0))

  astar.connect_points(1, 2, false)
  astar.connect_points(2, 3, false)
  astar.connect_points(4, 3, false)
  astar.connect_points(1, 4, false)

  var res = astar.get_id_path(1, 3) # Returns [1, 2, 3]

```

```
  var astar = new AStar3D();
  astar.AddPoint(1, new Vector3(0, 0, 0));
  astar.AddPoint(2, new Vector3(0, 1, 0), 1); // Default weight is 1
  astar.AddPoint(3, new Vector3(1, 1, 0));
  astar.AddPoint(4, new Vector3(2, 0, 0));
  astar.ConnectPoints(1, 2, false);
  astar.ConnectPoints(2, 3, false);
  astar.ConnectPoints(4, 3, false);
  astar.ConnectPoints(1, 4, false);
  long[] res = astar.GetIdPath(1, 3); // Returns [1, 2, 3]

```
  If you change the 2nd point's weight to 3, then the result will be [1, 4, 3] instead, because now even though the distance is longer, it's "easier" to get through point 4 than through point 2.

- get_point_capacity() -> int [const]
  Returns the capacity of the structure backing the points, useful in conjunction with reserve_space().

- get_point_connections(id: int) -> PackedInt64Array
  Returns an array with the IDs of the points that form the connection with the given point.


```
  var astar = AStar3D.new()
  astar.add_point(1, Vector3(0, 0, 0))
  astar.add_point(2, Vector3(0, 1, 0))
  astar.add_point(3, Vector3(1, 1, 0))
  astar.add_point(4, Vector3(2, 0, 0))

  astar.connect_points(1, 2, true)
  astar.connect_points(1, 3, true)

  var neighbors = astar.get_point_connections(1) # Returns [2, 3]

```

```
  var astar = new AStar3D();
  astar.AddPoint(1, new Vector3(0, 0, 0));
  astar.AddPoint(2, new Vector3(0, 1, 0));
  astar.AddPoint(3, new Vector3(1, 1, 0));
  astar.AddPoint(4, new Vector3(2, 0, 0));
  astar.ConnectPoints(1, 2, true);
  astar.ConnectPoints(1, 3, true);

  long[] neighbors = astar.GetPointConnections(1); // Returns [2, 3]

```

- get_point_count() -> int [const]
  Returns the number of points currently in the points pool.

- get_point_ids() -> PackedInt64Array
  Returns an array of all point IDs.

- get_point_path(from_id: int, to_id: int, allow_partial_path: bool = false) -> PackedVector3Array
  Returns an array with the points that are in the path found by AStar3D between the given points. The array is ordered from the starting point to the ending point of the path. If from_id point is disabled, returns an empty array (even if from_id == to_id). If from_id point is not disabled, there is no valid path to the target, and allow_partial_path is true, returns a path to the point closest to the target that can be reached. **Note:** This method is not thread-safe; it can only be used from a single Thread at a given time. Consider using Mutex to ensure exclusive access to one thread to avoid race conditions. Additionally, when allow_partial_path is true and to_id is disabled the search may take an unusually long time to finish.

- get_point_position(id: int) -> Vector3 [const]
  Returns the position of the point associated with the given id.

- get_point_weight_scale(id: int) -> float [const]
  Returns the weight scale of the point associated with the given id.

- has_point(id: int) -> bool [const]
  Returns whether a point associated with the given id exists.

- is_point_disabled(id: int) -> bool [const]
  Returns whether a point is disabled or not for pathfinding. By default, all points are enabled.

- remove_point(id: int) -> void
  Removes the point associated with the given id from the points pool.

- reserve_space(num_nodes: int) -> void
  Reserves space internally for num_nodes points. Useful if you're adding a known large number of points at once, such as points on a grid.

- set_point_disabled(id: int, disabled: bool = true) -> void
  Disables or enables the specified point for pathfinding. Useful for making a temporary obstacle.

- set_point_position(id: int, position: Vector3) -> void
  Sets the position for the point with the given id.

- set_point_weight_scale(id: int, weight_scale: float) -> void
  Sets the weight_scale for the point with the given id. The weight_scale is multiplied by the result of _compute_cost() when determining the overall cost of traveling across a segment from a neighboring point to this point.

## Properties

- neighbor_filter_enabled: bool = false [set set_neighbor_filter_enabled; get is_neighbor_filter_enabled]
  If true enables the filtering of neighbors via _filter_neighbor().
