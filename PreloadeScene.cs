using Godot;
using System;
using System.Collections.Generic;

public partial class PreloadeScene : Node2D
{
    // Store references to keep them in memory
    public static Dictionary<string, PackedScene> PreloadedScenes = new Dictionary<string, PackedScene>();

    public override void _Ready()
    {
        GD.Print("--- Start Preloading Scenes ---");
        ScanAndLoad("res://");
        GD.Print($"--- Finished Preloading. Loaded {PreloadedScenes.Count} scenes ---");
    }

    private void ScanAndLoad(string dirPath)
    {
        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            GD.PrintErr($"Could not open directory: {dirPath}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != "")
        {
            if (dir.CurrentIsDir())
            {
                // Skip hidden folders (like .godot) which start with .
                if (!fileName.StartsWith("."))
                {
                    ScanAndLoad(dirPath.PathJoin(fileName));
                }
            }
            else
            {
                if (fileName.EndsWith(".tscn") || fileName.EndsWith(".scn"))
                {
                    string fullPath = dirPath.PathJoin(fileName);
                    LoadResource(fullPath);
                }
            }
            fileName = dir.GetNext();
        }
    }

    private void LoadResource(string path)
    {
        try
        {
            // Skip loading the preload scene itself to avoid redundancy/issues
            if (path.Contains("PreloadeScene.tscn")) return;

            // Load and cache the scene
            var resource = GD.Load<PackedScene>(path);
            if (resource != null)
            {
                PreloadedScenes[path] = resource;
                GD.Print($"Preloaded: {path}");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load {path}: {e.Message}");
        }
    }
}
