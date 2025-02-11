using HexaEngine.ImGuiNET;
using Prowl.Icons;
using Prowl.Runtime;
using Prowl.Runtime.Assets;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using static Assimp.Metadata;

namespace Prowl.Editor.EditorWindows;

/// <summary>
/// Project Assets Tree Window
/// Shows all Folder and Files in a Tree Format
/// </summary>
public class AssetsWindow : EditorWindow {

    public static EditorSettings Settings => Project.ProjectSettings.GetSetting<EditorSettings>();

    protected override ImGuiWindowFlags Flags => ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse;

    private string _searchText = "";
    private readonly List<FileInfo> _found = [];

    public AssetsWindow() : base()
    {
        Title = FontAwesome6.FolderTree + " Assets";
    }

    protected override void Draw()
    {
        if (Project.HasProject == false) return;

        float cPX = ImGui.GetCursorPosX();
        if (GUIHelper.Search("##searchBox", ref _searchText, ImGui.GetContentRegionAvail().X))
        {
            _found.Clear();
            if (!string.IsNullOrEmpty(_searchText))
            {
                _found.AddRange(AssetDatabase.GetRootfolders()[2].EnumerateFiles("*", SearchOption.AllDirectories)); // Assets
                _found.AddRange(AssetDatabase.GetRootfolders()[0].EnumerateFiles("*", SearchOption.AllDirectories)); // Defaults
                _found.AddRange(AssetDatabase.GetRootfolders()[1].EnumerateFiles("*", SearchOption.AllDirectories)); // Packages Folder
                // Remove Meta's & only keep the ones with SearchText inside them
                _found.RemoveAll(f => f.Extension.Equals(".meta", StringComparison.OrdinalIgnoreCase) || !f.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            }
        }

        ImGui.BeginChild("Tree");
        int count = 0;
        if (!string.IsNullOrEmpty(_searchText))
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            foreach (var file in _found)
            {
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.FramePadding;
                if (AssetsWindow.IsSelected(file)) flags |= ImGuiTreeNodeFlags.Selected;

                string ext = file.Extension.ToLower().Trim();

                var curPos = ImGui.GetCursorPos();
                bool opened = ImGui.TreeNodeEx($"      {Path.GetFileNameWithoutExtension(file.Name)}", flags);
                if (count++ % 2 == 0) drawList.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.1f)));
                if (ImGui.IsItemClicked()) Selection.Select(file, true);
                GUIHelper.Tooltip(file.Name);
                ImGui.PushStyleColor(ImGuiCol.Text, GetFileColor(ext));
                // Display icon behind text
                ImGui.SetCursorPos(new System.Numerics.Vector2(curPos.X + 26, curPos.Y));
                ImGui.TextUnformatted(GetIcon(ext));
                ImGui.PopStyleColor();
                if (opened) ImGui.TreePop();
            }
        }
        else
        {
            RenderRootFolter(ref count, true, AssetDatabase.GetRootfolders()[2]); // Assets Folder
            RenderRootFolter(ref count, false, AssetDatabase.GetRootfolders()[0]); // Defaults Folder
            RenderRootFolter(ref count, true, AssetDatabase.GetRootfolders()[1]); // Packages Folder
        }
        ImGui.EndChild();
    }

    private static void RenderRootFolter(ref int count, bool defaultOpen, DirectoryInfo root)
    {
        ImGuiTreeNodeFlags rootFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.FramePadding;
        if(defaultOpen) rootFlags |= ImGuiTreeNodeFlags.DefaultOpen;

        ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(250f / 255f, 210f / 255f, 100f / 255f, 1f));
        bool opened = ImGui.TreeNodeEx($"{FontAwesome6.FolderTree} {root.Name}", rootFlags);
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(new System.Numerics.Vector4(1f, 1f, 1f, 0.2f)));
        if (ImGui.IsItemClicked()) Selection.Select(root, true);
        FileRightClick(null);
        ImGui.PopStyleColor();

        if (opened)
        {
            DrawDirectory(root, ref count);
            ImGui.TreePop();
        }
    }

    private static void DrawDirectory(DirectoryInfo directory, ref int count)
    {
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        // Folders
        foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
        {
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.FramePadding;
            bool isLeaf = subDirectory.GetFiles().Length == 0 && subDirectory.GetDirectories().Length == 0;
            if (isLeaf) flags |= ImGuiTreeNodeFlags.Leaf;

            if (AssetsWindow.IsSelected(subDirectory)) flags |= ImGuiTreeNodeFlags.Selected;

            bool opened = ImGui.TreeNodeEx($"{(isLeaf ? FontAwesome6.FolderOpen : FontAwesome6.Folder)} {subDirectory.Name}", flags);
            if (ImGui.IsItemClicked()) Selection.Select(subDirectory, true);
            FileRightClick(subDirectory);
            GUIHelper.Tooltip(subDirectory.Name);

            if (count++ % 2 == 0) drawList.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.1f)));

            if (opened)
            {
                DrawDirectory(subDirectory, ref count);
                ImGui.TreePop();
            }
        }

        // Files
        foreach (FileInfo file in directory.EnumerateFiles())
        {
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.FramePadding;
            if (AssetsWindow.IsSelected(file)) flags |= ImGuiTreeNodeFlags.Selected;

            string ext = file.Extension.ToLower().Trim();
            if (ext.Equals(".meta", StringComparison.OrdinalIgnoreCase)) continue;

            var curPos = ImGui.GetCursorPos();
            var name = (Settings.m_HideExtensions ? Path.GetFileNameWithoutExtension(file.Name) : file.Name);
            bool opened = ImGui.TreeNodeEx($"      {name}", flags);
            if (count++ % 2 == 0) drawList.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.1f)));
            if (ImGui.IsItemClicked()) Selection.Select(file, true);
            FileRightClick(file);
            GUIHelper.Tooltip(file.Name);
            ImGui.PushStyleColor(ImGuiCol.Text, GetFileColor(ext));
            // Display icon behind text
            ImGui.SetCursorPos(new System.Numerics.Vector2(curPos.X + 26, curPos.Y));
            ImGui.TextUnformatted(GetIcon(ext));
            ImGui.PopStyleColor();
            if (opened) ImGui.TreePop();
        }
    }

    public static void FileRightClick(FileSystemInfo? fileInfo)
    {
        // Lets fallback to whatever is Selected if fileInfo is null
        if (fileInfo == null && Selection.Count > 0 && Selection.CurrentType.IsAssignableTo(typeof(FileSystemInfo)))
            fileInfo = Selection.Get(0) as FileSystemInfo;

        // If still null then show a simplified context menu
        if(fileInfo == null)
        {
            if (ImGui.BeginPopupContextItem())
            {
                MainMenuItems.Directory = null;
                MenuItem.DrawMenuRoot("Create");
                if (ImGui.MenuItem("Show In Explorer"))
                {
                    using Process fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = "\"" + Project.ProjectAssetDirectory + "\"";
                    fileopener.Start();
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Reimport All"))
                    AssetDatabase.ReimportAll();

                ImGui.EndPopup();
            }
            return;
        }
        else if (fileInfo is FileInfo file)
        {
            if (ImGui.BeginPopupContextItem())
            {
                var relativeAssetPath = AssetDatabase.FileToRelative(file);
                if (ImGui.MenuItem("Reimport"))
                    AssetDatabase.Reimport(relativeAssetPath);
                ImGui.Separator();
                MainMenuItems.Directory = file.Directory;
                MenuItem.DrawMenuRoot("Create");
                if (ImGui.MenuItem("Show In Explorer"))
                {
                    using Process fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = "\"" + file.Directory!.FullName + "\"";
                    fileopener.Start();
                }
                if (ImGui.MenuItem("Open"))
                    AssetDatabase.OpenAsset(relativeAssetPath);
                if (ImGui.MenuItem("Delete"))
                    file.Delete(); // Will trigger the AssetDatabase file watchers
                ImGui.Separator();
                if (ImGui.MenuItem("Reimport All"))
                    AssetDatabase.ReimportAll();

                ImGui.EndPopup();
            }
        }
        else if (fileInfo is DirectoryInfo directory)
        {
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.MenuItem("Reimport"))
                    AssetDatabase.ReimportFolder(directory);
                ImGui.Separator();
                MainMenuItems.Directory = directory;
                MenuItem.DrawMenuRoot("Create");
                if (ImGui.MenuItem("Show In Explorer"))
                {
                    using Process fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = "\"" + directory.Parent!.FullName + "\"";
                    fileopener.Start();
                }
                if (ImGui.MenuItem("Open"))
                {
                    using Process fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = "\"" + directory.FullName + "\"";
                    fileopener.Start();
                }
                if (ImGui.MenuItem("Delete"))
                    directory.Delete(); // Will trigger the AssetDatabase file watchers
                ImGui.Separator();
                if (ImGui.MenuItem("Reimport All"))
                    AssetDatabase.ReimportAll();

                ImGui.EndPopup();
            }
        }
    }

    public static uint GetFileColor(string ext)
    {
        switch (ext)
        {
            case ".png":
            case ".bmp":
            case ".jpg":
            case ".jpeg":
            case ".qoi":
            case ".psd":
            case ".tga":
            case ".dds":
            case ".hdr":
            case ".ktx":
            case ".pkm":
            case ".pvr":
                return ImGui.GetColorU32(new Color(31, 230, 71));
            case ".obj":
            case ".blend":
            case ".dae":
            case ".fbx":
            case ".gltf":
            case ".ply":
            case ".pmx":
            case ".stl":
                return ImGui.GetColorU32(new Color(243, 232, 47));
            case ".scriptobj":
                return ImGui.GetColorU32(new Color(245, 245, 1));
            case ".mat":
                return ImGui.GetColorU32(new Color(43, 211, 212));
            case ".shader":
                return ImGui.GetColorU32(new Color(239, 12, 106));
            case ".glsl":
                return ImGui.GetColorU32(new Color(254, 22, 2));
            case ".md":
            case ".txt":
                return ImGui.GetColorU32(new Color(228, 238, 5));
            case ".cs":
                return ImGui.GetColorU32(new Color(244, 101, 2));
            default:
                return ImGui.GetColorU32(new Color(1.0f, 1.0f, 1.0f, 0.6f));
        }
    }

    private static string GetIcon(string ext)
    {
        switch (ext)
        {
            case ".png":
            case ".bmp":
            case ".jpg":
            case ".jpeg":
            case ".qoi":
            case ".psd":
            case ".tga":
            case ".dds":
            case ".hdr":
            case ".ktx":
            case ".pkm":
            case ".pvr":
                return FontAwesome6.Image;
            case ".obj":
            case ".blend":
            case ".dae":
            case ".fbx":
            case ".gltf":
            case ".ply":
            case ".pmx":
            case ".stl":
                return FontAwesome6.Cube;
            case ".scriptobj":
                return FontAwesome6.Database;
            case ".mat":
                return FontAwesome6.Circle;
            case ".shader":
                return FontAwesome6.CameraRetro;
            case ".glsl":
                return FontAwesome6.DiagramNext;
            case ".md":
            case ".txt":
                return FontAwesome6.FileLines;
            case ".cs":
                return FontAwesome6.Code;
            default:
                return FontAwesome6.File;
        }
    }

    public static bool IsSelected(FileSystemInfo entry)
    {
        if (Selection.Count > 0 && Selection.CurrentType.IsAssignableTo(typeof(FileSystemInfo)))
        {
            int count = Selection.Count;
            for (int i = 0; i < count; i++)
            {
                var obj = Selection.Get(i);
                if (obj is FileSystemInfo file && file.FullName.Equals(entry.FullName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

}
