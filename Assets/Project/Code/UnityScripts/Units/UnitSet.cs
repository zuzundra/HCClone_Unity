using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSet
{
    #region instance

    private static UnitSet _instance = null;
    public static UnitSet Instance
    {
        get
        {
            if (_instance == null)
                _instance = new UnitSet();
            return _instance;
        }
    }

    #endregion

    public const int FirstZoneIndex = 0;
    public const int SecondZoneIndex = 1;
    public const int ThirdZoneIndex = 2;

    public void SetUnitPositions()
    {
        UnitSet.Instance.SetUnitPositions(FightManager.SceneInstance.AllyUnits, true);
        UnitSet.Instance.SetUnitPositions(FightManager.SceneInstance.EnemyUnits, false);
    }

    void SetUnitPositions(ArrayRO<BaseUnitBehaviour> units, bool isAlly)
    {
        List<BaseUnitBehaviour>[] rangeUnits = GetRangeUnits(units);
        Canvas canvas = FightManager.SceneInstance.UI.CanvasBG;
        float width = GameConstants.DEFAULT_RESOLUTION_WIDTH * canvas.transform.localScale.x;
        float height = GameConstants.DEFAULT_RESOLUTION_HEIGHT * canvas.transform.localScale.y;
        float xMin = -width / 2;
        float xMax = width / 2;
        float yMin = -height / 2;
        float yMax = height / 2;
        float delta = (xMax - xMin) / 12;

        List<BaseUnitBehaviour> thirdUnits = rangeUnits[SecondZoneIndex];
        List<BaseUnitBehaviour> secondUnits = rangeUnits[ThirdZoneIndex];
        SetZonePositions(rangeUnits[FirstZoneIndex], isAlly ? xMin + delta : xMax - delta, yMin, yMax);
        SetZonePositions(secondUnits, isAlly ? xMin + delta * 3 : xMax - delta * 3, yMin, yMax);
        SetZonePositions(thirdUnits, isAlly ? xMin + delta * 5 : xMax - delta * 5, yMin, yMax);
        if (thirdUnits.Count != 0 && secondUnits.Count != 0)
        {
            thirdUnits[thirdUnits.Count - 1].NextAttackUnit = secondUnits[0];
            secondUnits[0].PrevAttackUnit = thirdUnits[thirdUnits.Count - 1];
        }
    }

    public List<BaseUnitBehaviour>[] GetRangeUnits(ArrayRO<BaseUnitBehaviour> units)
    {
        List<BaseUnitBehaviour>[] rangeUnits = new List<BaseUnitBehaviour>[3] { new List<BaseUnitBehaviour>(),
            new List<BaseUnitBehaviour>(), new List<BaseUnitBehaviour>() };
        List<BaseUnitBehaviour> heroes = rangeUnits[FirstZoneIndex];
        List<BaseUnitBehaviour> remoteUnits = rangeUnits[SecondZoneIndex];
        List<BaseUnitBehaviour> nearUnits = rangeUnits[ThirdZoneIndex];
        for (int i = 0; i < units.Length; i++)
        {
            BaseUnitBehaviour unit = units[i];
            if (unit != null && !unit.UnitData.IsDead)
            {
                if (UnitsConfig.Instance.IsHero(unit.UnitData.Data.Key))
                {
                    if (heroes.Count < 3)
                        heroes.Add(unit);
                    else if (remoteUnits.Count < 3)
                        remoteUnits.Add(unit);
                    else if (nearUnits.Count < 3)
                        nearUnits.Add(unit);
                }
                else
                {
                    if (unit.UnitData.Data.BaseRange == EUnitRange.Ranged)
                    {
                        if (remoteUnits.Count < 3)
                            remoteUnits.Add(unit);
                        else if (nearUnits.Count < 3)
                            nearUnits.Add(unit);
                    }
                    else if (unit.UnitData.Data.BaseRange == EUnitRange.Melee)
                    {
                        if (nearUnits.Count < 3)
                            nearUnits.Add(unit);
                        else if (remoteUnits.Count < 3)
                            remoteUnits.Add(unit);
                    }
                } 
            }
        }
        return rangeUnits;
    }

    void SetZonePositions(List<BaseUnitBehaviour> units, float x, float minZ, float maxZ)
    {
        if (units.Count == 0)
            return;
        units.Sort();
        for (int i = 0; i < units.Count; i++)
        {
            if (i < units.Count - 1)
            {
                units[i].NextAttackUnit = units[i + 1];
                units[i + 1].PrevAttackUnit = units[i];
            }
        }
        if (units.Count > 3)
        {
            units = units.GetRange(0, 3);
        }
        BaseUnitBehaviour firstUnit = units[0];
        if (units.Count == 1)
        {
            firstUnit.SetPosition(new Vector3(x, 0, (maxZ + minZ) / 2));
        }
        else
        {
            BaseUnitBehaviour secondUnit = units[1];
            if (units.Count == 2)
            {
                firstUnit.SetPosition(new Vector3(x, 0, minZ + (maxZ - minZ) / 3));
                secondUnit.SetPosition(new Vector3(x, 0, maxZ - (maxZ - minZ) / 3));
            }
            else
            {
                firstUnit.SetPosition(new Vector3(x, 0, (maxZ + minZ) / 2));
                secondUnit.SetPosition(new Vector3(x, 0, minZ + (maxZ - minZ) / 6));
                BaseUnitBehaviour thirdUnit = units[2];
                thirdUnit.SetPosition(new Vector3(x, 0, maxZ - (maxZ - minZ) / 6));
            } 
        }
    }

    public BaseUnitBehaviour GetNextAttackUnit(BaseUnitBehaviour currentUnit, BaseUnitBehaviour lastAttackUnit)
    {
        BaseUnitBehaviour nextAttackUnit = null;
        if (lastAttackUnit != null)
        {
            nextAttackUnit = lastAttackUnit.NextAttackUnit;
            while (nextAttackUnit != null)
            {
                if (!nextAttackUnit.UnitData.IsDead)
                    return nextAttackUnit;
                nextAttackUnit = nextAttackUnit.NextAttackUnit;
            }
        }
        if (nextAttackUnit == null)
        {
            nextAttackUnit = currentUnit;
            while (nextAttackUnit.PrevAttackUnit != null && !nextAttackUnit.PrevAttackUnit.UnitData.IsDead)
                nextAttackUnit = nextAttackUnit.PrevAttackUnit;
        }
        return nextAttackUnit;
    }
}
