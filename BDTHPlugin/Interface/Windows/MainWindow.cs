using System;
using System.Numerics;

using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;

using BDTHPlugin.Interface.Components;

namespace BDTHPlugin.Interface.Windows
{
  public class MainWindow : Window
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();

    private static readonly Vector4 RED_COLOR = new(1, 0, 0, 1);

    private readonly Gizmo Gizmo;
    private readonly ItemControls ItemControls = new();

    public bool Reset;

    public MainWindow(Gizmo gizmo) : base(
      "Burning Down the House##BDTH",
      ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize |
      ImGuiWindowFlags.AlwaysAutoResize
    )
    {
      Gizmo = gizmo;
    }

    public override void PreDraw()
    {
      if (Reset)
      {
        Reset = false;
        ImGui.SetNextWindowPos(new Vector2(69, 69), ImGuiCond.Always);
      }
    }

    public unsafe override void Draw()
    {
      ImGui.BeginGroup();

      var placeAnywhere = Configuration.PlaceAnywhere;
      if (ImGui.Checkbox("自由放置##Place Anywhere", ref placeAnywhere))
      {
        // Set the place anywhere based on the checkbox state.
        Memory.SetPlaceAnywhere(placeAnywhere);
        Configuration.PlaceAnywhere = placeAnywhere;
        Configuration.Save();
      }
      DrawTooltip("解除遊戲引擎的放置限制，允許自由擺放家具。");

      ImGui.SameLine();

      // Checkbox is clicked, set the configuration and save.
      var useGizmo = Configuration.UseGizmo;
      if (ImGui.Checkbox("操作軸##Gizmo", ref useGizmo))
      {
        Configuration.UseGizmo = useGizmo;
        Configuration.Save();
      }
      DrawTooltip("在所選家具上顯示操作軸，以沿各軸移動家具。");

      ImGui.SameLine();

      // Checkbox is clicked, set the configuration and save.
      var doSnap = Configuration.DoSnap;
      if (ImGui.Checkbox("吸附##Snap", ref doSnap))
      {
        Configuration.DoSnap = doSnap;
        Configuration.Save();
      }
      DrawTooltip("依下方設定的拖曳數值，讓操作軸移動套用固定間距。");

      ImGui.SameLine();
      if (ImGuiComponents.IconButton(1, Gizmo.Mode == ImGuizmoMode.Local ? Dalamud.Interface.FontAwesomeIcon.ArrowsAlt : Dalamud.Interface.FontAwesomeIcon.Globe))
        Gizmo.Mode = Gizmo.Mode == ImGuizmoMode.Local ? ImGuizmoMode.World : ImGuizmoMode.Local;

      DrawTooltip(
      [
        $"模式：{(Gizmo.Mode == ImGuizmoMode.Local ? "局部" : "世界")}",
        "切換操作軸的局部座標與世界座標模式。"
      ]);

      ImGui.Separator();

      if (Memory.HousingStructure->Mode == HousingLayoutMode.None)
        DrawError("請先進入房屋布置模式");
      else if (PluginMemory.GamepadMode)
        DrawError("不支援控制器模式");
      else if (Memory.HousingStructure->ActiveItem == null || Memory.HousingStructure->Mode != HousingLayoutMode.Rotate)
      {
        DrawError("請在旋轉模式中選擇家具");
        ImGuiComponents.HelpMarker("若操作方式正確仍出現此訊息，請使用 /bdth debug 指令，並至 Discord 回報問題。");
      }
      else
        ItemControls.Draw();

      ImGui.Separator();

      // Drag amount for the inputs.
      var drag = Configuration.Drag;
      if (ImGui.InputFloat("拖曳間距##drag", ref drag, 0.05f))
      {
        drag = Math.Min(Math.Max(0.001f, drag), 10f);
        Configuration.Drag = drag;
        Configuration.Save();
      }
      DrawTooltip("設定拖曳控制項時的變化量，也會影響操作軸的吸附間距。");

      var dummyHousingGoods = AtkManager.HousingGoods != null && AtkManager.HousingGoods.IsVisible;
      var dummyInventory = AtkManager.InventoryVisible;

      if (ImGui.Checkbox("顯示遊戲內家具清單##Display in-game list", ref dummyHousingGoods))
      {
        AtkManager.ShowFurnishingList(dummyHousingGoods);

        Configuration.DisplayFurnishingList = dummyHousingGoods;
        Configuration.Save();
      }
      ImGui.SameLine();

      if (ImGui.Checkbox("顯示物品欄##Display inventory", ref dummyInventory))
      {
        AtkManager.ShowInventory(dummyInventory);

        Configuration.DisplayInventory = dummyInventory;
        Configuration.Save();
      }

      if (ImGui.Button("開啟家具清單##Open Furnishing List"))
        Plugin.CommandManager.ProcessCommand("/bdth list");
      DrawTooltip(
      [
        "開啟家具清單，可依距離排序並點擊選擇家具。",
        "注意：目前無法在戶外使用。"
      ]);

      var autoVisible = Configuration.AutoVisible;
      if (ImGui.Checkbox("自動開啟##Auto Open", ref autoVisible))
      {
        Configuration.AutoVisible = autoVisible;
        Configuration.Save();
      }

      ImGui.EndGroup();
    }

    private static void DrawTooltip(string[] text)
    {
      if (ImGui.IsItemHovered())
      {
        ImGui.BeginTooltip();
        foreach (var t in text)
          ImGui.Text(t);
        ImGui.EndTooltip();
      }
    }

    private static void DrawTooltip(string text)
    {
      DrawTooltip([text]);
    }

    private static void DrawError(string text)
    {
      ImGui.PushStyleColor(ImGuiCol.Text, RED_COLOR);
      ImGui.Text(text);
      ImGui.PopStyleColor();
    }
  }
}
