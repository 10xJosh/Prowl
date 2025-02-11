﻿using HexaEngine.ImGuiNET;
using Prowl.Icons;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Prowl.Runtime
{
    public class GUIHelper
    {
        /// <summary>
        /// Creates a tooltip for the hovered item drawn before this is called.
        /// </summary>
        public static void Tooltip(string tooltip, string shortcut = "")
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(tooltip);
                ImGui.EndTooltip();
            }
        }

        public static void TextCenter(string text, float size = 1f)
        {
            float oldX = ImGui.GetCursorPosX();
            ImGui.SetWindowFontScale(size);
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(text).X) / 2f);
            ImGui.Text(text);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.SetCursorPosX(oldX);
        }

        public static bool MenuItemTooltip(string name, string tooltip, string shortcut = "")
        {
            bool item = ImGui.MenuItem(name, shortcut);

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(tooltip);
                if (!string.IsNullOrEmpty(shortcut))
                {
                    ImGui.Text($"Shortcut: {shortcut}");
                }
                ImGui.EndTooltip();
            }
            return item;
        }

        public static bool ComboScrollable<T>(string key, string text, ref T selectedItem, Action propertyChanged = null, ImGuiComboFlags flags = ImGuiComboFlags.None)
        {
            return ComboScrollable(key, text, ref selectedItem, Enum.GetValues(typeof(T)).Cast<T>(), propertyChanged, flags);
        }

        public static bool ComboScrollable<T>(string key, string text, ref T selectedItem, IEnumerable<T> items, Action propertyChanged = null, ImGuiComboFlags flags = ImGuiComboFlags.None)
        {
            if (ImGui.BeginCombo(key, text, flags)) //Check for combo box popup and add items
            {
                foreach (T item in items)
                {
                    bool isSelected = item.Equals(selectedItem);
                    if (ImGui.Selectable(item.ToString(), isSelected))
                    {
                        selectedItem = item;
                        propertyChanged?.Invoke();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();

                return true;
            }
            if (ImGui.IsItemHovered()) //Check for combo box hover
            {
                var delta = ImGui.GetIO().MouseWheel;
                if (delta < 0) //Check for mouse scroll change going up
                {
                    var list = items.ToList();
                    int index = list.IndexOf(selectedItem);
                    if (index < list.Count - 1)
                    { //Shift upwards if possible
                        selectedItem = list[index + 1];
                        propertyChanged?.Invoke();
                    }
                }
                if (delta > 0) //Check for mouse scroll change going down
                {
                    var list = items.ToList();
                    int index = list.IndexOf(selectedItem);
                    if (index > 0)
                    { //Shift downwards if possible
                        selectedItem = list[index - 1];
                        propertyChanged?.Invoke();

                        return true;
                    }
                }
            }
            return false;
        }

        public unsafe bool CustomTreeNode(string label)
        {
            var style = ImGui.GetStyle();
            var storage = ImGui.GetStateStorage();

            int id = ImGui.GetID(label);
            int opened = storage.GetInt(id, 0);
            float x = ImGui.GetCursorPosX();
            ImGui.BeginGroup();
            if (ImGui.InvisibleButton(label, new Vector2(-1, ImGui.GetFontSize() + style.FramePadding.Y * 2)))
            {
                opened = storage.GetInt(id, 0);
                //  opened = p_opened == p_opened;
            }
            bool hovered = ImGui.IsItemHovered();
            bool active = ImGui.IsItemActive();
            if (hovered || active)
            {
                var col = ImGui.GetStyle().Colors[(int)(active ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered)];
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.ColorConvertFloat4ToU32(col));
            }
            ImGui.SameLine();
            ImGui.ColorButton("color_btn", opened == 1 ? new Vector4(1, 1, 1, 1) : new Vector4(1, 0, 0, 1));
            ImGui.SameLine();
            ImGui.Text(label);
            ImGui.EndGroup();
            if (opened == 1)
                ImGui.TreePush(label);
            return opened != 0;
        }

        public static void ItemRectFilled(float r, float g, float b, float a, float expand = 0.0f, float roundness = 0.0f)
        {
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            if(expand > 0)
            {
                min.X -= expand;
                min.Y -= expand;
                max.X += expand;
                max.Y += expand;
            }
            ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(r, g, b, a)), roundness);
        }

        public static void ItemRect(float r, float g, float b, float a, float expand = 0.0f, float roundness = 0.0f, float thickness = 1.0f)
        {
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            if(expand > 0)
            {
                min.X -= expand;
                min.Y -= expand;
                max.X += expand;
                max.Y += expand;
            }
            ImGui.GetWindowDrawList().AddRect(min, max, ImGui.GetColorU32(new Vector4(r, g, b, a)), roundness, thickness);
        }

        public static bool Search(string v, ref string searchText, float x)
        {
            searchText ??= "";
            float cPX = ImGui.GetCursorPosX();
            ImGui.SetNextItemWidth(x);
            bool changed = ImGui.InputText(v, ref searchText, 0x100);
            bool isSearching = !string.IsNullOrEmpty(searchText);
            if (!isSearching)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(cPX + ImGui.GetFontSize() * 0.5f);
                ImGui.TextUnformatted(FontAwesome6.MagnifyingGlass + " Search...");
            }
            return changed;
        }

        public static bool DragDouble(string v1, ref double value, float v2)
        {
            unsafe
            {
                fixed (double* v = &value)
                    return ImGui.DragScalar(v1, ImGuiDataType.Double, v, v2, "%g");
            }
        }
    }
}
