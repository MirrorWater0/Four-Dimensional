# RDAttachmentFormat

## Meta

- Name: RDAttachmentFormat
- Source: RDAttachmentFormat.xml
- Inherits: RefCounted
- Inheritance Chain: RDAttachmentFormat -> RefCounted -> Object

## Brief Description

Attachment format (used by RenderingDevice).

## Description

This object is used by RenderingDevice.

## Quick Reference

```
[properties]
format: int (RenderingDevice.DataFormat) = 36
samples: int (RenderingDevice.TextureSamples) = 0
usage_flags: int = 0
```

## Properties

- format: int (RenderingDevice.DataFormat) = 36 [set set_format; get get_format]
  The attachment's data format.

- samples: int (RenderingDevice.TextureSamples) = 0 [set set_samples; get get_samples]
  The number of samples used when sampling the attachment.

- usage_flags: int = 0 [set set_usage_flags; get get_usage_flags]
  The attachment's usage flags, which determine what can be done with it.
