# XRControllerTracker

## Meta

- Name: XRControllerTracker
- Source: XRControllerTracker.xml
- Inherits: XRPositionalTracker
- Inheritance Chain: XRControllerTracker -> XRPositionalTracker -> XRTracker -> RefCounted -> Object

## Brief Description

A tracked controller.

## Description

An instance of this object represents a controller that is tracked. As controllers are turned on and the XRInterface detects them, instances of this object are automatically added to this list of active tracking objects accessible through the XRServer. The XRController3D consumes objects of this type and should be used in your project.

## Quick Reference

```
[properties]
type: int (XRServer.TrackerType) = 2
```

## Tutorials

- [XR documentation index]($DOCS_URL/tutorials/xr/index.html)

## Properties

- type: int (XRServer.TrackerType) = 2 [set set_tracker_type; get get_tracker_type; override XRTracker]
