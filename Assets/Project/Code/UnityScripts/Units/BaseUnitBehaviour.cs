using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BaseUnitBehaviour : MonoBehaviour, IComparable {
	[SerializeField]
	private UnitPathfinding _unitPathfinder;
	public UnitPathfinding UnitPathfinder {
		get { return _unitPathfinder; }
	}

    [SerializeField]
    private UnitAttack _unitAttack;
    public UnitAttack UnitAttack
    {
        get { return _unitAttack; }
    }

	[SerializeField]
	private UnitModelView _model;
	public UnitModelView ModelView {
		get { return _model; }
	}
	
	[SerializeField]
	private Vector3 _healthBarPosition = new Vector3(0f, 1.2f, 0f);
	
	private UnitUI _ui;
	
	private BaseUnit _unitData = null;
	public BaseUnit UnitData {
		get { return _unitData; }
	}
	
	private BaseUnitBehaviour _targetUnit;
	public BaseUnitBehaviour TargetUnit {
		get { return _targetUnit; }
	}
	
	private bool _isAlly = false;
	public bool IsAlly {
		get { return _isAlly; }
	}

    public BaseUnitBehaviour NextAttackUnit { get; set; }
    public BaseUnitBehaviour PrevAttackUnit { get; set; }
	
	private float _attackTime = 0f;
	
	private WaitForSeconds _cachedWaitForSeconds;
	//private float _lastAttackTime = 0f;
    private static float _lastAllyAttackTime = 0f;
    private static float _lastEnemyAttackTime = 0f;

    private static BaseUnitBehaviour _lastAllyAttackUnit = null;
    private static BaseUnitBehaviour _lastEnemyAttackUnit = null;

	private Coroutine _corTargetAttack;
	
	private Transform _cachedTransform = null;
	public Transform CachedTransform {
		get { return _cachedTransform; }
	}

	public float DistanceToTarget {
		get { return _targetUnit != null ? Vector3.Distance(_cachedTransform.position, _targetUnit.CachedTransform.position) : 0f; }
	}
    //public bool TargetInRange {
    //    get { return _targetUnit != null && DistanceToTarget <= _unitData.AR; }
    //}
	
	private Dictionary<ESkillKey, BaseUnitSkill> _skills;
	public bool CastingSkill { get; set; }
	
	public void Awake() {
		_cachedTransform = transform;
		
		if (_model == null) {
			_model = gameObject.GetComponentInChildren<UnitModelView>();
		}
		
		EventsAggregator.Units.AddListener<BaseUnit, HitInfo>(EUnitEvent.HitReceived, OnHitReceived);
		EventsAggregator.Units.AddListener<BaseUnit>(EUnitEvent.DeathCame, OnUnitDeath);
		EventsAggregator.Fight.AddListener(EFightEvent.Pause, OnFightPause);
		EventsAggregator.Fight.AddListener(EFightEvent.Resume, OnFightResume);
		EventsAggregator.Fight.AddListener(EFightEvent.MapComplete, OnMapComplete);
		EventsAggregator.Fight.AddListener(EFightEvent.MapFail, OnMapFail);
	}
	
	private bool _isStarted = false;
	private Action _onStart = null;
	public IEnumerator Start() {
		_model.SimulateAttack();
		
		while (_model.Animator.GetCurrentAnimationClipState(0).Length == 0) {
			yield return null;
		}
		_model.SetupWeapon();
		
		_isStarted = true;
		if (_onStart != null) {
			_onStart();
			_onStart = null;
		}
		
		EventsAggregator.Units.Broadcast<BaseUnitBehaviour>(EUnitEvent.ReadyToFight, this);
	}
	
	public void OnDestroy() {
		EventsAggregator.Units.RemoveListener<BaseUnit, HitInfo>(EUnitEvent.HitReceived, OnHitReceived);
		EventsAggregator.Units.RemoveListener<BaseUnit>(EUnitEvent.DeathCame, OnUnitDeath);
		EventsAggregator.Fight.RemoveListener(EFightEvent.Pause, OnFightPause);
		EventsAggregator.Fight.RemoveListener(EFightEvent.Resume, OnFightResume);
		EventsAggregator.Fight.RemoveListener(EFightEvent.MapComplete, OnMapComplete);
		EventsAggregator.Fight.RemoveListener(EFightEvent.MapFail, OnMapFail);
		
		if (_isAlly && UnitsConfig.Instance != null && UnitsConfig.Instance.IsHero(_unitData.Data.Key)) {
			EventsAggregator.Units.RemoveListener<ESkillKey>(EUnitEvent.SkillUsage, UseSkill);
		}
		
		_unitData = null;
		_targetUnit = null;
        _unitAttack = null;
        //_unitPathfinder = null;
        _model = null;
		_ui = null;
	}
	
	public void Setup(BaseUnit unitData, Dictionary<ESkillKey, BaseUnitSkill> skills, string tag, GameObject uiResource, int unitNumber) {
		_unitData = unitData;
		gameObject.tag = tag;
		_isAlly = gameObject.CompareTag(GameConstants.Tags.UNIT_ALLY);

        _attackTime = FightManager.SceneInstance.AttackInterval;// 1f / unitData.AttackSpeed;
		_cachedWaitForSeconds = new WaitForSeconds(_attackTime - _model.ShootPositionTimeOffset);
		
		_skills = skills != null ? skills : new Dictionary<ESkillKey, BaseUnitSkill>();
		
		if (_isAlly && UnitsConfig.Instance.IsHero(_unitData.Data.Key)) {
			EventsAggregator.Units.AddListener<ESkillKey>(EUnitEvent.SkillUsage, UseSkill);
		}
		
		if (_ui == null) {
			_ui = (GameObject.Instantiate(uiResource) as GameObject).GetComponent<UnitUI>();
			_ui.transform.SetParent(transform, false);
			_ui.transform.localPosition = _healthBarPosition;
			_ui.transform.localRotation = Quaternion.Euler(GameConstants.CAMERA_ROTATION);
		} else {
			_ui.Reset();
		}
		
		if (unitData.DamageTaken > 0) {
			_ui.UpdateHealthBar(Mathf.Max(unitData.HealthPoints - unitData.DamageTaken, 0) / (unitData.HealthPoints * 1f));
		}
		
		if (_isStarted) {
			EventsAggregator.Units.Broadcast<BaseUnitBehaviour>(EUnitEvent.ReadyToFight, this);
		}		
		//_unitPathfinder.UnitNumber = unitNumber;
	}
	
	public void Stun(float duration) {
		_model.PlayStunAnimation();
		
		for (int i = 0; i < UnitData.ActiveSkills.ActiveSkills.Count; i++) {
			UnitData.ActiveSkills.ActiveSkills[i].OnCasterStunned();
		}
		
		StopTargetAttack(false);
		_targetUnit = null;
        _unitAttack.Reset(true);
		//_unitPathfinder.Reset(true);
		
		if (IsInvoking("Run")) {
			CancelInvoke("Run");
		}
		Invoke("Run", duration);
	}
	
	public void Run() {
		if (!_isStarted) {
			_onStart += Run;
			return;
		}
        _unitAttack.TargetAttack(this, _isAlly ? FightManager.SceneInstance.EnemyUnits : FightManager.SceneInstance.AllyUnits,
            OnTargetFound, OnTargetReached);
        //_unitPathfinder.MoveToTarget(this, _isAlly ? FightManager.SceneInstance.EnemyUnits : FightManager.SceneInstance.AllyUnits, 
        //    OnTargetFound, OnTargetReached);
	}

    public void SetPosition(Vector3 position)
    {
        UnitAttack.SetPosition(this, position);
    }

	public void GoToMapEnd() {
        _unitAttack.WalkIntoSunset();
		//_unitPathfinder.WalkIntoSunset();
	}
	
	public void StopAllActions() {
        _unitAttack.Reset(true);
		//_unitPathfinder.Reset(true);
		_model.PlayIdleAnimation();
	}
	
	public void UseSkill(ESkillKey skillKey) {
		if (FightManager.SceneInstance.Status == EFightStatus.InProgress && _skills.ContainsKey(skillKey) && _skills[skillKey] != null) {
			_skills[skillKey].Use(this);
		}
	}
	
	public void StartTargetAttack() {
		if (UnitsConfig.Instance.IsHero(UnitData.Data.Key) || CastingSkill) {
			return;
		}
        BaseUnitBehaviour nextAttackUnit = UnitSet.Instance.GetNextAttackUnit(this, IsAlly ? _lastAllyAttackUnit : _lastEnemyAttackUnit);
        float lastAttackTime = IsAlly ? _lastAllyAttackTime : _lastEnemyAttackTime;
        if (lastAttackTime != 0f && Time.time - lastAttackTime < _attackTime || nextAttackUnit != null && !nextAttackUnit.Equals(this))
        {
			_model.PlayAttackAnimation(0);
			_model.StopCurrentAnimation();
			Invoke("StartTargetAttack", _attackTime - (Time.time - lastAttackTime));
		} 
        else 
			_corTargetAttack = StartCoroutine(AttackTarget());
	}
	
	public void StopTargetAttack(bool resetAttackTimer) {
		if (resetAttackTimer) {
			//_lastAttackTime = 0f;
            if (IsAlly)
            {
                _lastAllyAttackUnit = null;
                _lastAllyAttackTime = 0f;
            }
            else
            {
                _lastEnemyAttackUnit = null;
                _lastEnemyAttackTime = 0f;
            }
		}
		if (IsInvoking("StartTargetAttack")) {
			CancelInvoke("StartTargetAttack");
		}
		if (_corTargetAttack != null) {
			StopCoroutine(_corTargetAttack);
			_corTargetAttack = null;
		}
		
		_model.StopAttackAnimation();
	}
	
	#region unit controller
	private void OnTargetFound(BaseUnitBehaviour target) {
		_targetUnit = target;
	}
	
	private void OnTargetReached(BaseUnitBehaviour nearesTarget) {
		_targetUnit = nearesTarget;
		StartTargetAttack();
	}
	
	private void OnTargetDeath() {
		for (int i = 0; i < UnitData.ActiveSkills.ActiveSkills.Count; i++) {
			UnitData.ActiveSkills.ActiveSkills[i].OnCasterTargetDeath();
		}		
		StopTargetAttack(false);
		_targetUnit = null;
        _unitAttack.TargetAttack(this, _isAlly ? FightManager.SceneInstance.EnemyUnits : FightManager.SceneInstance.AllyUnits,
            OnTargetFound, OnTargetReached);
        //_unitPathfinder.MoveToTarget(this, _isAlly ? FightManager.SceneInstance.EnemyUnits : FightManager.SceneInstance.AllyUnits, 
        //    OnTargetFound, OnTargetReached);
	}
	
	private void OnSelfDeath() {
		for (int i = 0; i < UnitData.ActiveSkills.ActiveSkills.Count; i++) {
			UnitData.ActiveSkills.ActiveSkills[i].OnCasterDeath();
		}
		
		if (IsInvoking("Run")) {
			CancelInvoke("Run");
		}
		
		StopTargetAttack(false);//(true);
		_targetUnit = null;
        _unitAttack.Reset(true);
		//_unitPathfinder.Reset(true);
		
		EventsAggregator.Fight.Broadcast<BaseUnit>(gameObject.tag == GameConstants.Tags.UNIT_ALLY ? EFightEvent.AllyDeath : EFightEvent.EnemyDeath, _unitData);
		
		_model.PlayDeathAnimation(OnDeathAnimationEnd);
	}
	
	private void OnMapEnd() {
        for (int i = 0; i < UnitData.ActiveSkills.ActiveSkills.Count; i++)
        {
			UnitData.ActiveSkills.ActiveSkills[i].Break();
		}
        if (IsInvoking("Run"))
        {
			CancelInvoke("Run");
		}		
		StopTargetAttack(true);
		_targetUnit = null;
        _unitAttack.Reset(true);
		//_unitPathfinder.Reset(true);
		
		if (_unitData != null && !_unitData.IsDead) {
			_model.PlayWinAnimation();
            _unitAttack.LookIntoSunset();
			//_unitPathfinder.LookIntoSunset();
		}
	}
	
	private IEnumerator AttackTarget() {
		//_lastAttackTime = Time.time;
        if (IsAlly)
        {
            _lastAllyAttackUnit = this;
            _lastAllyAttackTime = Time.time;
        }
        else
        {
            _lastEnemyAttackUnit = this;
            _lastEnemyAttackTime = Time.time;
        }		
		_model.PlayAttackAnimation(DistanceToTarget);
		
		if (_model.WFSAttackDelay != null) {
			yield return _model.WFSAttackDelay;
		}
		
		EventsAggregator.Fight.Broadcast<BaseUnitBehaviour, BaseUnitBehaviour>(EFightEvent.PerformAttack, this, _targetUnit);

        if (_unitAttack.State == EUnitAttackState.WatchTarget)
		//if (_unitPathfinder.CurrentState == EUnitMovementState.WatchEnemy) 
        {
			yield return _cachedWaitForSeconds;
			
			if (_targetUnit != null && !_targetUnit.UnitData.IsDead) {
				_corTargetAttack = StartCoroutine(AttackTarget());
			} else {
				_corTargetAttack = null;
			}
		}
	}
	
	private void OnHitReceived(BaseUnit unit, HitInfo hitInfo) {
		if (unit == _unitData) {
			_model.PlayHitAnimation(unit.HealthPoints, hitInfo);
			_ui.ApplyDamage(unit.HealthPoints, hitInfo);
		}
	}
	
	private IEnumerator Vanish() {
		yield return new WaitForSeconds(1f);
		
		GameObject.Destroy(gameObject);
	}
	#endregion
	
	#region listeners
	private void OnUnitDeath(BaseUnit unitData) {
		if (unitData == _unitData) {
			OnSelfDeath();
		} else if (_targetUnit != null && unitData == _targetUnit._unitData) {
			OnTargetDeath();
		}
	}
	
	private void OnFightPause() {
		
	}
	
	private void OnFightResume() {
		
	}
	
	private void OnMapComplete() {
		OnMapEnd();
	}
	
	private void OnMapFail() {
		OnMapEnd();
	}
	
	private void OnDeathAnimationEnd() {
		StartCoroutine(Vanish());
	}
	#endregion

    #region Члены IComparable

    public int CompareTo(object obj)
    {
        BaseUnitBehaviour unitBehaviour = obj as BaseUnitBehaviour;
        if (unitBehaviour != null)
        {
            if (IsAlly == unitBehaviour.IsAlly)
            {
                if (UnitsConfig.Instance.IsHero(UnitData.Data.Key))
                    return 1;
                else if (UnitsConfig.Instance.IsHero(unitBehaviour.UnitData.Data.Key))
                    return -1;
                else
                {
                    if (UnitData.Data.BaseRange == unitBehaviour.UnitData.Data.BaseRange)
                    {
                        return UnitData.HealthPoints.CompareTo(unitBehaviour.UnitData.HealthPoints);
                    }
                    else if (UnitData.Data.BaseRange == EUnitRange.Melee)
                        return 1;
                    else
                        return -1;
                }
            }
            else if (IsAlly)
                return 1;
            else
                return -1;
        }
        else
        {
            throw new ArgumentException("Cравниваемый объект не является BaseUnitBehaviour!");
        }
    }

    #endregion
}
