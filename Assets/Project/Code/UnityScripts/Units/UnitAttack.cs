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

    static BaseUnitBehaviour _lastAttackUnit = null;
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

    public void FindTarget(BaseUnitBehaviour unit, List<BaseUnitBehaviour>[] rangeUnits,
        Action<BaseUnitBehaviour> onTargetFound, Action onTargetAttacked)
    {

        Reset(false);
        //UnitSet.Instance.SetUnitPositions();

        _onTargetFound = onTargetFound;
        _onTargetAttacked = onTargetAttacked;
        _target = GetTarget(unit, rangeUnits);
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

    BaseUnitBehaviour GetTarget(BaseUnitBehaviour unit, List<BaseUnitBehaviour>[] rangeUnits)
    {
        BaseUnitBehaviour target = GetTarget(unit.UnitData.Data.BasePriority == EUnitRange.Melee ? rangeUnits[UnitSet.ThirdZoneIndex]
            : (unit.UnitData.Data.BasePriority == EUnitRange.Ranged ? rangeUnits[UnitSet.SecondZoneIndex] : null));
        if (target == null)
        {
            target = GetTarget(unit.UnitData.Data.BasePriority == EUnitRange.Melee ? rangeUnits[UnitSet.SecondZoneIndex]
                : (unit.UnitData.Data.BasePriority == EUnitRange.Ranged ? rangeUnits[UnitSet.ThirdZoneIndex] : null));
            if (target == null)
            {
                target = GetTarget(rangeUnits[UnitSet.FirstZoneIndex]);
            }
        }
        return target; 
    }

    BaseUnitBehaviour GetTarget(List<BaseUnitBehaviour> targetUnits)
    {
        if (targetUnits == null)
            return null;
        return targetUnits.Find(x => x.UnitData != null && !x.UnitData.IsDead);
    }

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
        BaseUnitBehaviour attackUnit = UnitSet.Instance.GetAttackUnit(_lastAttackUnit);
        //Debug.Log("Attack " + attackUnit.name + ", last " + (_lastAttackUnit != null ? _lastAttackUnit.name : string.Empty));
        //Debug.Log("Attack " + unit.name + ", next " + attackUnit.name);

        if (attackUnit != null && attackUnit.Equals(unit) 
            && (_lastAttackUnit == null || _lastAttackUnit != null && _lastAttackUnit.UnitAttack.State != EUnitAttackState.AttackTarget))
        {
            State = EUnitAttackState.AttackTarget;
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
        //if (_lastAttackUnit != null)
        //    Debug.Log("Prev " + _lastAttackUnit.name);

        _lastAttackUnit = currentUnit;
        //if (_lastAttackUnit != null)
        //    Debug.Log("Current " + _lastAttackUnit.name);

        if (_lastAttackUnit != null)
        {
            BaseUnitBehaviour nextAttackUnit = UnitSet.Instance.GetAttackUnit(_lastAttackUnit);
            if (nextAttackUnit != null)
            {
                //Debug.Log("ToNext " + nextAttackUnit.name);
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

