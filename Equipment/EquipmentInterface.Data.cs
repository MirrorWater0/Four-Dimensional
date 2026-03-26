using System;
using System.Collections.Generic;
using Godot;

public partial class EquipmentInterface
{
    private bool TryGetSelectedPlayerInfo(out PlayerInfoStructure info)
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || (uint)_selectedCharacterIndex >= (uint)players.Length)
        {
            info = default;
            return false;
        }

        info = players[_selectedCharacterIndex];
        return true;
    }

    private bool TryGetSelectedPlayerWithEquipments(out PlayerInfoStructure info)
    {
        if (!TryGetSelectedPlayerInfo(out info))
            return false;

        EnsureEquipmentArray(ref info);
        SaveSelectedPlayerInfo(in info);
        return true;
    }

    private void SaveSelectedPlayerInfo(in PlayerInfoStructure info)
    {
        GameInfo.PlayerCharacters[_selectedCharacterIndex] = info;
    }

    private static void EnsureEquipmentArray(ref PlayerInfoStructure info)
    {
        var source = info.Equipments ?? Array.Empty<Equipment>();
        if (source.Length >= SlotCount)
        {
            for (int i = 0; i < SlotCount; i++)
                if (!HasValidEquipment(source[i]))
                    source[i] = null;
            return;
        }

        Equipment[] result = new Equipment[SlotCount];
        Array.Copy(source, result, Math.Min(source.Length, SlotCount));
        for (int i = 0; i < result.Length; i++)
            if (!HasValidEquipment(result[i]))
                result[i] = null;
        info.Equipments = result;
    }

    private static Equipment[] CloneEquipArray(Equipment[] source)
    {
        var result = new Equipment[SlotCount];
        if (source == null)
            return result;

        Array.Copy(source, result, Math.Min(source.Length, SlotCount));
        for (int i = 0; i < result.Length; i++)
            result[i] = Equipment.Clone(result[i]);
        return result;
    }

    private static int SumEquipment(Equipment[] equips, Func<Equipment, int> selector)
    {
        if (equips == null)
            return 0;

        int sum = 0;
        foreach (var equip in equips)
            if (HasValidEquipment(equip))
                sum += selector(equip);
        return sum;
    }

    private static int GetStatPreviewValue(
        int baseValue,
        Equipment[] equipments,
        Func<Equipment, int> selector
    )
    {
        return baseValue + SumEquipment(equipments, selector);
    }

    private static string BuildShortPassiveText(string passive)
    {
        if (string.IsNullOrWhiteSpace(passive))
            return "暂无被动描述。";

        // Accept escaped "\\n" as line breaks in data.
        return passive.Replace("\\n", "\n");
    }

    private static string BuildEquipmentBonusInline(Equipment equip)
    {
        if (equip == null)
            return "力量 +0  生存 +0  速度 +0  生命 +0";

        return $"力量 +{equip.Power}  生存 +{equip.Survivability}  速度 +{equip.Speed}  生命 +{equip.MaxLife}";
    }

    private static string FormatPreview(int current, int next)
    {
        return current == next ? current.ToString() : $"{current} → {next}";
    }

    private int FindCatalogIndexByName(Equipment.EquipmentName equipmentName)
    {
        for (int i = 0; i < _catalog.Length; i++)
        {
            if (_catalog[i].Name == equipmentName)
                return i;
        }
        return -1;
    }

    private int FindLastCatalogIndexByName(Equipment.EquipmentName equipmentName)
    {
        for (int i = _catalog.Length - 1; i >= 0; i--)
        {
            if (_catalog[i].Name == equipmentName)
                return i;
        }
        return -1;
    }

    private void RefreshAvailableCatalog()
    {
        _catalog = BuildAvailableCatalog();

        if (_catalog.Length == 0)
        {
            _selectedEquipment = null;
            _selectedInventoryCardIndex = -1;
            return;
        }

        if ((uint)_selectedInventoryCardIndex < (uint)_catalog.Length)
        {
            _selectedEquipment = Equipment.Clone(_catalog[_selectedInventoryCardIndex]);
            return;
        }

        if (_selectedInventoryCardIndex >= 0)
        {
            if (_selectedEquipment != null)
            {
                int recoveredIndex = FindCatalogIndexByName(_selectedEquipment.Name);
                if (recoveredIndex >= 0)
                {
                    _selectedInventoryCardIndex = recoveredIndex;
                    _selectedEquipment = Equipment.Clone(_catalog[recoveredIndex]);
                    return;
                }
            }

            _selectedInventoryCardIndex = -1;
        }

        if (_selectedEquipment == null)
            return;

        for (int i = 0; i < _catalog.Length; i++)
        {
            if (_catalog[i].Name != _selectedEquipment.Name)
                continue;

            _selectedEquipment = Equipment.Clone(_catalog[i]);
            return;
        }

        if (TryGetSelectedPlayerWithEquipments(out var info))
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var equipped = info.Equipments[i];
                if (!HasValidEquipment(equipped))
                    continue;
                if (equipped.Name != _selectedEquipment.Name)
                    continue;

                _selectedEquipment = Equipment.Clone(equipped);
                return;
            }
        }

        // Keep an empty selection when current selected card is removed from available list.
        _selectedEquipment = null;
    }

    private Equipment[] BuildAvailableCatalog()
    {
        EnsureOwnedEquipmentsInitialized();
        var owned = GameInfo.OwnedEquipments;
        var available = new List<Equipment>(owned.Count);
        foreach (var equip in owned)
        {
            if (!HasValidEquipment(equip))
                continue;

            available.Add(Equipment.Clone(equip));
        }
        return available.ToArray();
    }

    private static void EnsureOwnedEquipmentsInitialized()
    {
        if (GameInfo.OwnedEquipments == null)
            GameInfo.OwnedEquipments = new List<Equipment>();

        if (GameInfo.OwnedEquipments.Count > 0)
            return;

        // Only seed starter inventory for fresh/legacy data (no characters yet).
        if (GameInfo.PlayerCharacters != null && GameInfo.PlayerCharacters.Length > 0)
            return;

        // Keep current behavior for old saves that have no inventory yet.
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.RiftBlade));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.PhaseShoulder));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.EchoCore));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.LumenBadge));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.SilentPendant));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.FoldedBulwark));
        GameInfo.OwnedEquipments.RemoveAll(x => !HasValidEquipment(x));
    }

    private static void RemoveOwnedEquipmentByAvailableIndex(int availableIndex)
    {
        if (availableIndex < 0)
            return;

        EnsureOwnedEquipmentsInitialized();
        int visibleIndex = 0;
        for (int i = 0; i < GameInfo.OwnedEquipments.Count; i++)
        {
            if (!HasValidEquipment(GameInfo.OwnedEquipments[i]))
                continue;
            if (visibleIndex == availableIndex)
            {
                GameInfo.OwnedEquipments.RemoveAt(i);
                return;
            }
            visibleIndex++;
        }
    }

    private static bool HasValidEquipment(Equipment equip)
    {
        return equip != null && !string.IsNullOrWhiteSpace(equip.DisplayName);
    }

    private static void EnsureGameData()
    {
        EnsureOwnedEquipmentsInitialized();
        bool createdDefaults = false;

        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
        {
            GameInfo.PlayerCharacters =
            [
                new PlayerCharacterRegistry().Echo,
                new PlayerCharacterRegistry().Kasiya,
                new PlayerCharacterRegistry().Mariya,
                new PlayerCharacterRegistry().Nightingale,
            ];
            createdDefaults = true;
        }

        GameInfo.NormalizePlayerCharacters();
        if (createdDefaults)
            GameInfo.SeedTakenSkillsAsGained();

        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            EnsureEquipmentArray(ref info);
            GameInfo.PlayerCharacters[i] = info;
        }
    }
}
