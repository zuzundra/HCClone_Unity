using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UnitAttack : MonoBehaviour
{
    [SerializeField]
    private UnitModelView _model;
    Transform _modelTransform = null;
    Transform _transform = null;

    float _speed = 1f;
    float _rotationSpeed = 5f;

    static bool _isAllyAttack = true;
    static BaseUnitBehaviour _lastAllyAttackUnit = null;
    static BaseUnitBehaviour _lastEnemyAttackUnit = null;
    BaseUnitBehaviour _target = null;
    Transform _targetTransform = null;

    Vector3 _destinationPosition = Vector3.zero;

    Dictionary<EUnitAttackState, Action> _attackActions = new Dictionary<EUnitAttackState, Action>();

    Action<BaseUnitBehaviour> _onTargetFound = null;
    Action _onTargetAttacked = null;

    EUnitAttackState _state = EUnitAttackState.None;
    public EUnitAttackState State
    {
        get { return _state; }
        private set
        {
            if (value != _state)
            {
                BaseUnitBehaviour unit = gameObject.GetComponent<BaseUnitBehaviour>();
                //Debug.Log("State " + unit.name + " " + _state + "->" + value);
            }
            _state = value;
            Update();
        }
    }

    public void Awake()
    {
		if (_model == null) 
			_model = gameObject.GetComponentInChildren<UnitModelView>();
        _model.MovementSpeed = _speed;
        _modelTransform = _model.transform;
        _transform = transform;

        _attackActions.Add(EUnitAttackState.NoAttack, MoveToPosition); 
        _attackActions.Add(EUnitAttackState.WatchTarget, WatchTarget);
        _attackActions.Add(EUnitAttackState.AttackTarget, AttackTarget);

        _attackActions.Add(EUnitAttackState.LookIntoSunset, LookForward);
        _attackActions.Add(EUnitAttackState.WalkIntoSunset, MoveForward);
    }

    public void Update()
    {
        if (_state != EUnitAttackState.None && _state != EUnitAttackState.NoTarget)
        {
            if (_attackActions.ContainsKey(_state))
                _attackActions[_state]();
        }
    }

    #region Movement

    public void LookIntoSunset()
    {
        State = EUnitAttackState.LookIntoSunset;
    }

    public void WalkIntoSunset()
    {
        State = EUnitAttackState.WalkIntoSunset;
        _model.PlayRunAnimation();
    }

    public void FindTarget(BaseUnitBehaviour unit, BaseUnitBehaviour currentTarget, ArrayRO<BaseUnitBehaviour> possibleUnits,
        Action<BaseUnitBehaviour> onTargetFound, Action onTargetAttacked)
    {

        Reset(false);
        //UnitSet.Instance.SetUnitPositions();

        _onTargetFound = onTargetFound;
        _onTargetAttacked = onTargetAttacked;
        _target = GetTarget(unit, currentTarget, possibleUnits);
        if (_target != null)
        {
            _targetTransform = _target.transform;
        }
        if (_targetTransform == null)
        {
            Reset(false);
            State = EUnitAttackState.NoTarget;
            return;
        }
        onTargetFound(_targetTransform.gameObject.GetComponent<BaseUnitBehaviour>());
        if (State == EUnitAttackState.None || State == EUnitAttackState.NoAttack || State == EUnitAttackState.WatchTarget)
        {
            StartTargetAttack(_target);
        }
    }

    public BaseUnitBehaviour GetBaseUnitBehaviour(ArrayRO<BaseUnitBehaviour> possibleUnits, UnitPlace place)
    {
        for (int i = 0; i < possibleUnits.Length; i++)
        {
            BaseUnitBehaviour possibleUnit = possibleUnits[i];
            if (possibleUnit.Place.Range == place.Range && possibleUnit.Place.Position == place.Position)
                return possibleUnit;
        }
        return null;
    }

    BaseUnitBehaviour GetTarget(BaseUnitBehaviour unit, BaseUnitBehaviour currentTarget, ArrayRO<BaseUnitBehaviour> possibleUnits)
    {
        UnitPlace currentPlace = currentTarget != null ? currentTarget.Place
            : new UnitPlace() { Position = EUnitPosition.Middle };
        BaseUnitBehaviour nextUnit = GetNextBaseUnitBehaviour(unit, currentPlace, possibleUnits);
        Debug.Log(unit.name + " attack " + (nextUnit != null ? nextUnit.name : string.Empty));
        while (nextUnit != null && nextUnit.UnitData.Data.Key != unit.UnitData.Data.Key && nextUnit.UnitData.IsDead)
        {
            nextUnit = GetNextBaseUnitBehaviour(unit, nextUnit.Place, possibleUnits);
            Debug.Log(unit.name + " attack " + (nextUnit != null ? nextUnit.name : string.Empty));
        }
        return nextUnit;
    }

    BaseUnitBehaviour GetNextBaseUnitBehaviour(BaseUnitBehaviour attackUnit, UnitPlace currentPlace, ArrayRO<BaseUnitBehaviour> possibleUnits)
    {
        UnitPlace nextPlace = GetNextPlace(attackUnit.Place, currentPlace);
        if (nextPlace.Range != EUnitRange.None || nextPlace.Position != EUnitPosition.None)
        {
            return GetBaseUnitBehaviour(possibleUnits, nextPlace);
        }    
        return null;
    }

    UnitPlace GetNextPlace(UnitPlace attackPlace, UnitPlace currentPlace)
    {
        Debug.Log("Place " + attackPlace.Range + " - " + attackPlace.Position);
        switch (attackPlace.Position)
        {
            case EUnitPosition.Middle:
                if (attackPlace.Range == EUnitRange.Ranged || attackPlace.Range == EUnitRange.Melee)
                {
                    if (currentPlace.Range == EUnitRange.Melee)
                        return new UnitPlace() { Range = EUnitRange.Ranged, Position = currentPlace.Position };
                    else if (currentPlace.Range == EUnitRange.Ranged)
                    {
                        if (currentPlace.Position == EUnitPosition.Middle)
                            return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Top };
                        else if (currentPlace.Position == EUnitPosition.Top)
                            return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Bottom };
                        else if (currentPlace.Position == EUnitPosition.Bottom)
                            return new UnitPlace() { Position = EUnitPosition.Middle };
                    }
                    else
                        return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Middle };
                }
                else
                {
                    if (currentPlace.Position == EUnitPosition.Middle)
                    {
                        if (currentPlace.Range == EUnitRange.Ranged || currentPlace.Range == EUnitRange.Melee)
                            return new UnitPlace() { Range = currentPlace.Range, Position = EUnitPosition.Top };
                        else
                            return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Middle };
                    }
                    else if (currentPlace.Position == EUnitPosition.Top)
                        return new UnitPlace() { Range = currentPlace.Range, Position = EUnitPosition.Bottom };
                    else if (currentPlace.Position == EUnitPosition.Bottom)
                    {
                        if (currentPlace.Range == EUnitRange.Melee)
                            return new UnitPlace() { Range = EUnitRange.Ranged, Position = EUnitPosition.Middle };
                        else if (currentPlace.Range == EUnitRange.Ranged)
                            return new UnitPlace() { Position = EUnitPosition.Middle };
                    }
                }
                break;

            case EUnitPosition.Top:
                if (currentPlace.Range == EUnitRange.Melee)
                    return new UnitPlace() { Range = EUnitRange.Ranged, Position = currentPlace.Position };
                else if (currentPlace.Range == EUnitRange.Ranged)
                {
                    if (currentPlace.Position == EUnitPosition.Top)
                        return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Middle };
                    else if (currentPlace.Position == EUnitPosition.Middle)
                        return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Bottom };
                    else if (currentPlace.Position == EUnitPosition.Bottom)
                        return new UnitPlace() { Position = EUnitPosition.Middle };
                }
                else
                    return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Top };
                break;

            case EUnitPosition.Bottom:
                if (currentPlace.Range == EUnitRange.Melee)
                    return new UnitPlace() { Range = EUnitRange.Ranged, Position = currentPlace.Position };
                else if (currentPlace.Range == EUnitRange.Ranged)
                {
                    if (currentPlace.Position == EUnitPosition.Bottom)
                        return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Middle };
                    else if (currentPlace.Position == EUnitPosition.Middle)
                        return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Top };
                    else if (currentPlace.Position == EUnitPosition.Top)
                        return new UnitPlace() { Position = EUnitPosition.Middle };
                }
                else
                    return new UnitPlace() { Range = EUnitRange.Melee, Position = EUnitPosition.Bottom };
                break;
        }
        return new UnitPlace();
    }

    //BaseUnitBehaviour GetTarget(BaseUnitBehaviour unit, List<BaseUnitBehaviour>[] rangeUnits)
    //{
    //    BaseUnitBehaviour target = GetTarget(unit.UnitData.Data.BasePriority == EUnitRange.Melee ? rangeUnits[UnitSet.ThirdZoneIndex]
    //        : (unit.UnitData.Data.BasePriority == EUnitRange.Ranged ? rangeUnits[UnitSet.SecondZoneIndex] : null));
    //    if (target == null)
    //    {
    //        target = GetTarget(unit.UnitData.Data.BasePriority == EUnitRange.Melee ? rangeUnits[UnitSet.SecondZoneIndex]
    //            : (unit.UnitData.Data.BasePriority == EUnitRange.Ranged ? rangeUnits[UnitSet.ThirdZoneIndex] : null));
    //        if (target == null)
    //        {
    //            target = GetTarget(rangeUnits[UnitSet.FirstZoneIndex]);
    //        }
    //    }
    //    return target; 
    //}

    //BaseUnitBehaviour GetTarget(List<BaseUnitBehaviour> targetUnits)
    //{
    //    if (targetUnits == null)
    //        return null;
    //    return targetUnits.Find(x => x.UnitData != null && !x.UnitData.IsDead);
    //}

    void StartTargetAttack(BaseUnitBehaviour target)
    {
        _model.StopCurrentAnimation();
        Action<BaseUnitBehaviour> onTargetFound = _onTargetFound;
        _onTargetFound = null;
        StopAllCoroutines();

        State = EUnitAttackState.WatchTarget;
        onTargetFound(target);
    }

    private void WatchTarget()
    {
        _modelTransform.localRotation = Quaternion.Lerp(_modelTransform.localRotation, 
            Quaternion.LookRotation(_targetTransform.transform.position - _transform.position), _rotationSpeed * Time.deltaTime);

        BaseUnitBehaviour unit = gameObject.GetComponent<BaseUnitBehaviour>();
        BaseUnitBehaviour attackUnit = UnitSet.Instance.GetAttackUnit(_isAllyAttack,
            _isAllyAttack ? _lastAllyAttackUnit : _lastEnemyAttackUnit);

        if (attackUnit != null && attackUnit.Equals(unit))
        {
            BaseUnitBehaviour lastAttackUnit = _isAllyAttack ? _lastEnemyAttackUnit : _lastAllyAttackUnit;
            if (lastAttackUnit == null || lastAttackUnit != null && lastAttackUnit.UnitAttack.State != EUnitAttackState.AttackTarget)
            {
                State = EUnitAttackState.AttackTarget;
            }
        }
    }

    private void AttackTarget()
    {
        _modelTransform.localRotation = Quaternion.Lerp(_modelTransform.localRotation,
            Quaternion.LookRotation(_targetTransform.transform.position - _transform.position), _rotationSpeed * Time.deltaTime);

        if (_onTargetAttacked != null)
        {
            _onTargetAttacked();
        }
    }

    public void ToNextAttackUnit(BaseUnitBehaviour currentUnit)
    {
        if (currentUnit.IsAlly)
            _lastAllyAttackUnit = currentUnit;
        else
            _lastEnemyAttackUnit = currentUnit;
        if (currentUnit != null)
        {
            _isAllyAttack = !currentUnit.IsAlly;
            BaseUnitBehaviour nextAttackUnit = UnitSet.Instance.GetAttackUnit(_isAllyAttack,
                _isAllyAttack ? _lastAllyAttackUnit : _lastEnemyAttackUnit);
            if (nextAttackUnit != null)
            {
                nextAttackUnit.FindTarget();
            }
        }
    }

    private void LookForward()
    {
        _modelTransform.localRotation = Quaternion.Lerp(_modelTransform.localRotation, 
            Quaternion.LookRotation(new Vector3(1f, 0f, 0f)), _rotationSpeed * Time.deltaTime);
    }

    private void MoveForward()
    {
        Vector3 destination = _modelTransform.position + new Vector3(1f, 0f, 0f);
        _modelTransform.localRotation = Quaternion.Lerp(_modelTransform.localRotation,
            Quaternion.LookRotation(new Vector3(1f, 0f, 0f)), _rotationSpeed * Time.deltaTime);
        //_modelTransform.LookAt(destination);
        _transform.position = Vector3.MoveTowards(_transform.position, destination, Time.deltaTime * _speed);
    }

    public void SetPosition(BaseUnitBehaviour unit, Vector3 position)
    {
        if (_destinationPosition == Vector3.zero)
            _destinationPosition = unit.transform.position = position;
        else
        {
            _destinationPosition = position;
            if (_destinationPosition != _modelTransform.position)
            {
                if (State != EUnitAttackState.AttackTarget)
                    State = EUnitAttackState.NoAttack;
                //_model.PlayRunAnimation();
            }
        }
    }

    public void MoveToPosition()
    {
        float minDistance = 0.1f;
        _transform.position = Vector3.MoveTowards(_modelTransform.position, _destinationPosition, Time.deltaTime * _speed * 3);
        if (Vector3.Distance(_transform.position, _destinationPosition) < minDistance)
        {
            //_model.StopCurrentAnimation();
            _destinationPosition = _transform.position;
            State = EUnitAttackState.None;
        }
    }

    #endregion

    public void Reset(bool full)
    {
        _target = null;
        _targetTransform = null;

        _onTargetFound = null;
        _onTargetAttacked = null;
		StopAllCoroutines();
        if (full)
        {
            State = EUnitAttackState.None;
            //Debug.Log("None " + gameObject.GetComponent<BaseUnitBehaviour>().name);
        }
    }
}

