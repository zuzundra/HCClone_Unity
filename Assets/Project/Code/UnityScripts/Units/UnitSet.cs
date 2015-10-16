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

    List<BaseUnitBehaviour>[] _rangeAllyUnits = null;
    public List<BaseUnitBehaviour>[] RangeAllyUnits
    {
        get
        {
            if (_rangeAllyUnits == null)
            {
                _rangeAllyUnits = GetRangeUnits(FightManager.SceneInstance.AllyUnits);
            }
            return _rangeAllyUnits;
        }
    }

    List<BaseUnitBehaviour>[] _rangeEnemyUnits = null;
    public List<BaseUnitBehaviour>[] RangeEnemyUnits
    {
        get
        {
            if (_rangeEnemyUnits == null)
            {
                _rangeEnemyUnits = GetRangeUnits(FightManager.SceneInstance.EnemyUnits);
            }
            return _rangeEnemyUnits;
        }
    }

    List<BaseUnitBehaviour>[] GetRangeUnits(ArrayRO<BaseUnitBehaviour> units)
    {
        List<BaseUnitBehaviour>[] rangeUnits = new List<BaseUnitBehaviour>[3] { new List<BaseUnitBehaviour>(),
            new List<BaseUnitBehaviour>(), new List<BaseUnitBehaviour>() };
        List<BaseUnitBehaviour> heroes = rangeUnits[FirstZoneIndex];
        List<BaseUnitBehaviour> remoteUnits = rangeUnits[SecondZoneIndex];
        List<BaseUnitBehaviour> nearUnits = rangeUnits[ThirdZoneIndex];
        for (int i = 0; i < units.Length; i++)
        {
            BaseUnitBehaviour unit = units[i];
            if (unit != null)
            {
                EUnitPosition position = unit.Place.Position;
                if (UnitsConfig.Instance.IsHero(unit.UnitData.Data.Key))
                {
                    if (heroes.Count < 3)
                        heroes.Add(unit);
                    else if (remoteUnits.Count < 3)
                    {
                        unit.Place = new UnitPlace() { Range = EUnitRange.Ranged, Position = position };
                        remoteUnits.Add(unit);
                    }
                    else if (nearUnits.Count < 3)
                    {
                        unit.Place = new UnitPlace() { Range = EUnitRange.Melee, Position = position };
                        nearUnits.Add(unit);
                    }
                }
                else
                {
                    if (unit.UnitData.Data.BaseRange == EUnitRange.Ranged)
                    {
                        if (remoteUnits.Count < 3)
                        {
                            unit.Place = new UnitPlace() { Range = EUnitRange.Ranged, Position = position };
                            remoteUnits.Add(unit);
                        }
                        else if (nearUnits.Count < 3)
                        {
                            unit.Place = new UnitPlace() { Range = EUnitRange.Melee, Position = position };
                            nearUnits.Add(unit);
                        }
                    }
                    else if (unit.UnitData.Data.BaseRange == EUnitRange.Melee)
                    {
                        if (nearUnits.Count < 3)
                        {
                            unit.Place = new UnitPlace() { Range = EUnitRange.Melee, Position = position };
                            nearUnits.Add(unit);
                        }
                        else if (remoteUnits.Count < 3)
                        {
                            unit.Place = new UnitPlace() { Range = EUnitRange.Ranged, Position = position };
                            remoteUnits.Add(unit);
                        }
                    }
                }
            }
        }
        return rangeUnits;
    }

    public void SetUnitPositions()
    {
        _rangeAllyUnits = null;
        SetUnitPositions(RangeAllyUnits, true);

        _rangeEnemyUnits = null;
        SetUnitPositions(RangeEnemyUnits, false);
    }

    void SetUnitPositions(List<BaseUnitBehaviour>[] rangeUnits, bool isAlly)
    {
        Canvas canvas = FightManager.SceneInstance.UI.CanvasBG;
        float width = GameConstants.DEFAULT_RESOLUTION_WIDTH * canvas.transform.localScale.x;
        float height = GameConstants.DEFAULT_RESOLUTION_HEIGHT * canvas.transform.localScale.y;
        float xMin = -width / 2;
        float xMax = width / 2;
        float yMin = -height / 2;
        float yMax = height / 2;
        float delta = (xMax - xMin) / 12;

        List<BaseUnitBehaviour> heroes = rangeUnits[FirstZoneIndex];
        SetZonePositions(heroes, isAlly ? xMin + delta : xMax - delta, yMin, yMax);

        List<BaseUnitBehaviour> remoteUnits = rangeUnits[SecondZoneIndex];
        SetZonePositions(remoteUnits, isAlly ? xMin + delta * 3 : xMax - delta * 3, yMin, yMax);

        List<BaseUnitBehaviour> nearUnits = rangeUnits[ThirdZoneIndex];
        SetZonePositions(nearUnits, isAlly ? xMin + delta * 5 : xMax - delta * 5, yMin, yMax);

        if (heroes.Count != 0)
        {
            List<BaseUnitBehaviour> units = remoteUnits.Count != 0 ? remoteUnits : nearUnits;
            if (units.Count != 0)
            {
                remoteUnits[remoteUnits.Count - 1].NextAttackUnit = heroes[0];
                heroes[0].PrevAttackUnit = remoteUnits[remoteUnits.Count - 1];
            }
        }
        if (nearUnits.Count != 0 && remoteUnits.Count != 0)
        {
            nearUnits[nearUnits.Count - 1].NextAttackUnit = remoteUnits[0];
            remoteUnits[0].PrevAttackUnit = nearUnits[nearUnits.Count - 1];
        }
    }

    void SetZonePositions(List<BaseUnitBehaviour> units, float x, float minZ, float maxZ)
    {
        if (units.Count == 0)
            return;
        units.Sort();
        for (int i = 0; i < units.Count; i++)
        {
            if (i == 0)
                units[i].PrevAttackUnit = null;
            if (i < units.Count - 1)
            {
                units[i].NextAttackUnit = units[i + 1];
                units[i + 1].PrevAttackUnit = units[i];
            }
            else
                units[i].NextAttackUnit = null;
        }
        if (units.Count > 3)
        {
            units = units.GetRange(0, 3);
        }
        BaseUnitBehaviour firstUnit = units[0];
        firstUnit.SetPosition(new Vector3(x, 0, (maxZ + minZ) / 2));
        firstUnit.Place = new UnitPlace() { Range = firstUnit.Place.Range, Position = EUnitPosition.Middle };
        if (units.Count > 1)
        {
            BaseUnitBehaviour secondUnit = units[1];
            secondUnit.SetPosition(new Vector3(x, 0, minZ + (maxZ - minZ) / 6));
            secondUnit.Place = new UnitPlace() { Range = secondUnit.Place.Range, Position = EUnitPosition.Bottom };
            if (units.Count > 2)
            {
                BaseUnitBehaviour thirdUnit = units[2];
                thirdUnit.SetPosition(new Vector3(x, 0, maxZ - (maxZ - minZ) / 6));
                thirdUnit.Place = new UnitPlace() { Range = thirdUnit.Place.Range, Position = EUnitPosition.Top };
            }
        }
    }    

    public BaseUnitBehaviour GetAttackUnit(bool isAlly, BaseUnitBehaviour lastAttackUnit)
    {
        BaseUnitBehaviour attackUnit = null;
        if (lastAttackUnit != null)
        {
            attackUnit = GetNextAttackUnit(lastAttackUnit);
            if (attackUnit == null)
            {
                attackUnit = GetFirstAttackUnit(isAlly);
            }
        }
        else
        {
            attackUnit = GetFirstAttackUnit(isAlly);
        }
        return attackUnit;
    }

    public BaseUnitBehaviour GetFirstAttackUnit(bool isAlly)
    {
        BaseUnitBehaviour first = null;
        ArrayRO<BaseUnitBehaviour> units = isAlly ? FightManager.SceneInstance.AllyUnits 
            : FightManager.SceneInstance.EnemyUnits;

        for (int i = 0; i < units.Length; i++ )
            if (units[i] != null && !units[i].UnitData.IsDead)
                first = units[i];
        if (first == null)
            return null;

        while (first.PrevAttackUnit != null && !first.PrevAttackUnit.UnitData.IsDead)
            first = first.PrevAttackUnit;
        return first;
    }

    BaseUnitBehaviour GetNextAttackUnit(BaseUnitBehaviour last)
    {
        BaseUnitBehaviour next = last.NextAttackUnit;
        while (next != null)
        {
            if (!next.UnitData.IsDead)
                return next;
            next = next.NextAttackUnit;
        }
        return next;
    }
}
