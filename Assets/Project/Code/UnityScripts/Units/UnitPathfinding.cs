using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(HCCGridObject))]
public class UnitPathfinding : MonoBehaviour {
	[SerializeField]
	private HCCGridObject _gridObject;
	[SerializeField]
	private UnitModelView _model;

	private float _searchesPerSecond = 1f;
	private float _minDistanceToTargetUnit = 1f;
	private float _speed = 1f;
	private float _rotationSpeed = 5f;

	private EHCCGridDirection _freePointDirection = EHCCGridDirection.None;
	private HCCGridPoint _freePointDirectionVector = HCCGridPoint.Zero;

	private Vector3 _targetPosition = Vector3.zero;
	private Transform _nearestTarget = null;
	private BaseUnitBehaviour _nearestTargetBUB = null;

	private Transform _cachedTransform;
	private Transform _cachedModelTransform;
	private WaitForSeconds _cachedWaitForSeconds;

	private Dictionary<EUnitMovementState, Action> _movementStateActions = new Dictionary<EUnitMovementState, Action>();
	private EUnitMovementState _currentState = EUnitMovementState.None;
	public EUnitMovementState CurrentState {
		get { return _currentState; }
		private set {
			_currentState = value;
			Update();
		}
	}

	private ArrayRO<BaseUnitBehaviour> _possibleTargets = null;
	private Action<BaseUnitBehaviour> _onTargetReached;

	public int UnitNumber { get; set; }

	public void Awake() {
		if (_model == null) {
			_model = gameObject.GetComponentInChildren<UnitModelView>();
		}
		_model.MovementSpeed = _speed;

		_movementStateActions.Add(EUnitMovementState.MoveToFreePoint, MoveToFreePoint);
		_movementStateActions.Add(EUnitMovementState.MoveToPrepPoint, MoveToPreparationPoint);
		_movementStateActions.Add(EUnitMovementState.MoveToAttackPoint, MoveToAttackPoint);
		_movementStateActions.Add(EUnitMovementState.WatchEnemy, WatchEnemy);
		_movementStateActions.Add(EUnitMovementState.LookIntoSunset, LookForward);
		_movementStateActions.Add(EUnitMovementState.WaklIntoSunset, MoveForward);

		_cachedTransform = transform;
		_cachedModelTransform = _model.transform;
		_cachedWaitForSeconds = new WaitForSeconds(1f / _searchesPerSecond);
	}

	public void Update() {
		if (_currentState != EUnitMovementState.None && _currentState != EUnitMovementState.NoEnemy) {
			_movementStateActions[_currentState]();
		}
	}

	public void Reset(bool full) {
		_nearestTarget = null;
		_nearestTargetBUB = null;
		_onTargetReached = null;
		StopAllCoroutines();

		if (full) {
			CurrentState = EUnitMovementState.None;
		}
	}

	private IEnumerator FindPathTimer() {
		_gridObject.FindPath(_nearestTarget.gameObject.GetComponent<HCCGridObject>());

		yield return _cachedWaitForSeconds;
		StartCoroutine(FindPathTimer());
	}

	private void FindNearestTarget(ArrayRO<BaseUnitBehaviour> possibleTargets) {
		_possibleTargets = possibleTargets;

		if (possibleTargets == null) {
			_nearestTarget = null;
			_nearestTargetBUB = null;
			return;
		}

		BaseUnitBehaviour nearestTarget = null;

		float distance = Mathf.Infinity;
		Vector3 position = transform.position;
		for (int i = 0; i < possibleTargets.Length; i++) {
			if (possibleTargets[i] == null || possibleTargets[i].UnitData.IsDead) {
				continue;
			}

			Vector3 diff = possibleTargets[i].transform.position - position;
			float curDistance = diff.sqrMagnitude;
			if (curDistance < distance) {
				nearestTarget = possibleTargets[i];
				distance = curDistance;
			}
		}

		if (nearestTarget != null) {
			_nearestTarget = nearestTarget.transform;
			_nearestTargetBUB = nearestTarget;
			_targetPosition = _nearestTarget.transform.position;
		} else {
			Debug.LogWarning(string.Format("{0} \"{1}\" reporting: No target found!", gameObject.tag, gameObject.name));
		}
	}

	#region movement
	public void LookIntoSunset() {
		CurrentState = EUnitMovementState.LookIntoSunset;
	}

	public void WalkIntoSunset() {
		CurrentState = EUnitMovementState.WaklIntoSunset;
		_model.PlayRunAnimation();
	}

	public void MoveToTarget(BaseUnitBehaviour self, ArrayRO<BaseUnitBehaviour> possibleTargets, Action<BaseUnitBehaviour> onTargetFound, Action<BaseUnitBehaviour> onTargetReached) {
		Reset(false);

		_minDistanceToTargetUnit = self.UnitData.AttackRange;
		_onTargetReached = onTargetReached;

		FindNearestTarget(possibleTargets);
		if (_nearestTarget == null) {
			Reset(false);
			CurrentState = EUnitMovementState.NoEnemy;
			return;
		}
		onTargetFound(_nearestTarget.gameObject.GetComponent<BaseUnitBehaviour>());

		_model.PlayRunAnimation();

		//setup path to appear on screen
		if (_currentState == EUnitMovementState.None) {
			//if there's interaction with some object we should get rid of it first
			//List<HCCGridObject> objectsAtMyRect = HCCGridController.Instance.GetObjectsAtRect(_gridObject.GridRect);
			//if (objectsAtMyRect.Count > 1) {
			//	CalculateFreePointDirection(objectsAtMyRect);
			//	CurrentState = EUnitMovementState.MoveToFreePoint;
			//} else {
			//	OnFreePointReached();
			//}
			OnFreePointReached();
		} else {
			if (Vector3.Distance(_cachedTransform.position, _targetPosition) <= _minDistanceToTargetUnit) {
				OnAttackPointReached(_nearestTargetBUB);
			} else {
				OnPreparationPointReached();
			}
		}
	}

	private void CalculateFreePointDirection() {
		CalculateFreePointDirection(HCCGridController.Instance.GetObjectsAtRect(_gridObject.GridRect));
	}

	private void CalculateFreePointDirection(List<HCCGridObject> objectsAtMyRect) {
		if (objectsAtMyRect.Count > 1) {
			List<EHCCGridDirection> directions = new List<EHCCGridDirection>();
			for (int i = 0; i < objectsAtMyRect.Count; i++) {
				if (objectsAtMyRect[i] != _gridObject) {
					directions.Add(objectsAtMyRect[i].MoveDirection);
				}
			}
			directions = HCCGridDirection.GetFreeDirections(directions, false);

			if (directions.IndexOf(_freePointDirection) == -1) {
				_freePointDirection = directions[UnityEngine.Random.Range(0, directions.Count - 1)];
				_freePointDirectionVector = HCCGridDirection.DirectionToVector(_freePointDirection);
			}
		}
	}

	private void CheckFreePoint() {
		List<HCCGridObject> objectsAtMyRect = HCCGridController.Instance.GetObjectsAtRect(_gridObject.GridRect);
		if (objectsAtMyRect.Count > 1) {
			CalculateFreePointDirection(objectsAtMyRect);

			HCCGridPoint myPosition = _gridObject.GridPosition;
			_gridObject.Path.Add(HCCGridController.Instance.GridData.GetCell(myPosition.X + _freePointDirectionVector.X, myPosition.Z + _freePointDirectionVector.Z));
			_gridObject.Path.Add(HCCGridController.Instance.GridData.GetCell(myPosition.X + _freePointDirectionVector.X * 2, myPosition.Z + _freePointDirectionVector.Z * 2));
		} else {
			OnFreePointReached();
		}
	}

	private void MoveToFreePoint() {
		if (!PerformMovement()) {
			CheckFreePoint();
		}

		if (_gridObject.Path.Count > 0) {
			_cachedModelTransform.localRotation = Quaternion.Lerp(_cachedModelTransform.localRotation, Quaternion.LookRotation(HCCGridController.Instance.GridView.GridToWorldPos(_gridObject.Path[0].GridPosition) - _cachedTransform.position), _rotationSpeed * Time.deltaTime);
			//_cachedModelTransform.LookAt(HCCGridController.Instance.GridView.GridToWorldPos(_gridObject.Path[0].GridPosition));
		}
	}

	private void OnFreePointReached() {
		_freePointDirection = EHCCGridDirection.None;
		_freePointDirectionVector = HCCGridPoint.Zero;

		_targetPosition.z = transform.position.z;
		_targetPosition.x = gameObject.CompareTag(GameConstants.Tags.UNIT_ALLY) ? FightManager.SceneInstance.AllyStartLine.position.x : FightManager.SceneInstance.EnemyStartLine.position.x;

		_gridObject.FindPath(_targetPosition, _gridObject);

		CurrentState = EUnitMovementState.MoveToPrepPoint;
	}

	private void MoveToPreparationPoint() {
		if (!PerformMovement()) {
			OnPreparationPointReached();
		}

		if(_gridObject.Path.Count > 0) {
			_cachedModelTransform.localRotation = Quaternion.Lerp(_cachedModelTransform.localRotation, Quaternion.LookRotation(HCCGridController.Instance.GridView.GridToWorldPos(_gridObject.Path[0].GridPosition) - _cachedTransform.position), _rotationSpeed * Time.deltaTime);
			//_cachedModelTransform.LookAt(HCCGridController.Instance.GridView.GridToWorldPos(_gridObject.Path[0].GridPosition));
		}
	}

	private void OnPreparationPointReached() {
		CurrentState = EUnitMovementState.MoveToAttackPoint;

		//start searching path to moving target unit
		StartCoroutine(PrestartFindPath());
	}

	private IEnumerator PrestartFindPath() {
		int delay = UnitNumber;
		for (; delay >= 0; delay--) {
			yield return null;
		}
		StartCoroutine(FindPathTimer());
	}

	private void MoveToAttackPoint() {
		_targetPosition = _nearestTarget.position;

		PerformMovement();
		if (_gridObject.Path.Count > 0) {
			_cachedModelTransform.localRotation = Quaternion.Lerp(_cachedModelTransform.localRotation, Quaternion.LookRotation(HCCGridController.Instance.GridView.GridToWorldPos(_gridObject.Path[0].GridPosition) - _cachedTransform.position), _rotationSpeed * Time.deltaTime);
			//_cachedModelTransform.LookAt(HCCGridController.Instance.GridView.GridToWorldPos(_gridObject.Path[0].GridPosition));
		}

		BaseUnitBehaviour nearestTargetInRange = GetNearestTargetInRange();
		if (nearestTargetInRange != null) {
			_nearestTarget = nearestTargetInRange.CachedTransform;
			_nearestTargetBUB = nearestTargetInRange;
			OnAttackPointReached(nearestTargetInRange);
		}
	}

	private void OnAttackPointReached(BaseUnitBehaviour nearesTarget) {
		_model.StopCurrentAnimation();

		Action<BaseUnitBehaviour> onTargetReached = _onTargetReached;
		_onTargetReached = null;
		StopAllCoroutines();

		CurrentState = EUnitMovementState.WatchEnemy;

		onTargetReached(nearesTarget);
	}

	private void WatchEnemy() {
		_cachedModelTransform.localRotation = Quaternion.Lerp(_cachedModelTransform.localRotation, Quaternion.LookRotation(_nearestTarget.transform.position - _cachedTransform.position), _rotationSpeed * Time.deltaTime);
		//_cachedModelTransform.LookAt(_nearestTarget.transform);
	}

	private bool PerformMovement(float minDistance = 0.6f) {
		bool result = false;

		for (int i = 0; i < _gridObject.Path.Count; i++) {
			Vector3 pos = HCCGridController.Instance.GridView.GridToWorldPos(_gridObject.Path[i].GridPosition);
			_cachedTransform.position = Vector3.MoveTowards(_cachedTransform.position, pos, Time.deltaTime * _speed);
			if (Vector3.Distance(_cachedTransform.position, pos) < minDistance) {
				_gridObject.PathPointReached();
				result = true;
			} else {
				break;
			}
		}

		return result;
	}

	private void LookForward() {
		_cachedModelTransform.localRotation = Quaternion.Lerp(_cachedModelTransform.localRotation, Quaternion.LookRotation(new Vector3(1f, 0f, 0f)), _rotationSpeed * Time.deltaTime);
	}

	private void MoveForward() {
		Vector3 destinationPoint = _cachedModelTransform.position + new Vector3(1f, 0f, 0f);
		_cachedModelTransform.localRotation = Quaternion.Lerp(_cachedModelTransform.localRotation, Quaternion.LookRotation(new Vector3(1f, 0f, 0f)), _rotationSpeed * Time.deltaTime);
		//_cachedModelTransform.LookAt(destinationPoint);
		_cachedTransform.position = Vector3.MoveTowards(_cachedTransform.position, destinationPoint, Time.deltaTime * _speed);
	}
	#endregion

	#region auxiliary
	private BaseUnitBehaviour GetNearestTargetInRange() {
		for (int i = 0; i < _possibleTargets.Length; i++) {
			if (_possibleTargets[i] != null && !_possibleTargets[i].UnitData.IsDead) {
				if (Vector3.Distance(_cachedTransform.position, _possibleTargets[i].CachedTransform.position) <= _minDistanceToTargetUnit) {
					return _possibleTargets[i];
				}
			}
		}

		return null;
	}
	#endregion
}
