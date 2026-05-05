# Purpose

This directory contains converted Godot API documentation for fast local lookup.

# Structure

- `api/<domain>/ClassName.md`
  - Main documentation entry point.
  - Each file contains one merged class document.
  - Typical contents include:
    - metadata such as class name, source file, base class, and inheritance chain
    - a brief description
    - a longer description
    - a quick reference section with methods and properties
    - detailed sections for methods, properties, signals, constants, tutorials, operators, constructors, or other class-specific items when available

# Recommended Usage

- Search in `api/` first.
- Prefer targeted searches by class name, method name, property name, signal, or constant.
- Open only the files that match the current question.
- Do not scan or list the entire repository unless explicitly needed.

# Notes

- The converted Markdown is optimized for search and reading, not for mirroring the original website layout.
- File names may be shortened when the shorter class name is unique within a domain.
