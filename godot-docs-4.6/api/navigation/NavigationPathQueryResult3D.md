# NavigationPathQueryResult3D

## Meta

- Name: NavigationPathQueryResult3D
- Source: NavigationPathQueryResult3D.xml
- Inherits: RefCounted
- Inheritance Chain: NavigationPathQueryResult3D -> RefCounted -> Object

## Brief Description

Represents the result of a 3D pathfinding query.

## Description

This class stores the result of a 3D navigation path query from the NavigationServer3D.

## Quick Reference

```
[methods]
reset() -> void

[properties]
path: PackedVector3Array = PackedVector3Array()
path_length: float = 0.0
path_owner_ids: PackedInt64Array = PackedInt64Array()
path_rids: RID[] = []
path_types: PackedInt32Array = PackedInt32Array()
```

## Tutorials

- [Using NavigationPathQueryObjects]($DOCS_URL/tutorials/navigation/navigation_using_navigationpathqueryobjects.html)

## Methods

- reset() -> void
  Reset the result object to its initial state. This is useful to reuse the object across multiple queries.

## Properties

- path: PackedVector3Array = PackedVector3Array() [set set_path; get get_path]
  The resulting path array from the navigation query. All path array positions are in global coordinates. Without customized query parameters this is the same path as returned by NavigationServer3D.map_get_path().

- path_length: float = 0.0 [set set_path_length; get get_path_length]
  Returns the length of the path.

- path_owner_ids: PackedInt64Array = PackedInt64Array() [set set_path_owner_ids; get get_path_owner_ids]
  The ObjectIDs of the Objects which manage the regions and links each point of the path goes through.

- path_rids: RID[] = [] [set set_path_rids; get get_path_rids]
  The RIDs of the regions and links that each point of the path goes through.

- path_types: PackedInt32Array = PackedInt32Array() [set set_path_types; get get_path_types]
  The type of navigation primitive (region or link) that each point of the path goes through.

## Constants

### Enum PathSegmentType

- PATH_SEGMENT_TYPE_REGION = 0
  This segment of the path goes through a region.

- PATH_SEGMENT_TYPE_LINK = 1
  This segment of the path goes through a link.
