using UnityEngine;
using System.Collections.Generic;
using System;

public class HCCPathfinder {
	private class OpenSet<T> where T : class {
		private T[] _data;
		private int _length = 0;
		public int Length {
			get { return _length; }
		}

		public OpenSet(int capacity) {
			_data = new T[capacity];
		}

		public T this[int i] {
			get {
				if (i < 0 || i >= _length) {
					throw new ArgumentOutOfRangeException();
				}

				return _data[i];
			}
		}

		public void Add(T item) {
			if (_length >= _data.Length) {
				throw new OverflowException();
			}

			_data[_length] = item;
			_length++;
		}

		public void RemoveAt(int index) {
			if (index < 0 || index >= _length) {
				throw new ArgumentOutOfRangeException();
			}

			_data[index] = _data[_length - 1];
			_data[_length - 1] = null;
			_length--;
		}

		public int IndexOf(T item) {
			for (int i = 0; i < _length; i++) {
				if (_data[i].Equals(item)) {
					return i;
				}
			}
			return -1;
		}

		public void Clear() {
			for (int i = 0; i < _length; i++) {
				_data[i] = null;
			}
			_length = 0;
		}
	}

	private HCCGridData _gridData = null;

	private OpenSet<HCCCell> _openSet = null;
	private HCCCell[,] _closedSet = null;
	private HCCCell[,] _cameFrom = null;
	private float[,] _gScore = null;
	private float[,] _fScore = null;

	private Vector2 v1 = Vector2.zero;
	private Vector2 v2 = Vector2.zero;

	public HCCPathfinder(HCCGridData gridData) {
		_gridData = gridData;

		_openSet = new OpenSet<HCCCell>(_gridData.XSize * _gridData.ZSize);
		_closedSet = new HCCCell[_gridData.XSize, _gridData.ZSize];
		_cameFrom = new HCCCell[_gridData.XSize, _gridData.ZSize];
		_gScore = new float[_gridData.XSize, _gridData.ZSize];
		_fScore = new float[_gridData.XSize, _gridData.ZSize];
	}

	public List<HCCCell> FindPath(HCCGridObject self, HCCGridObject target) {
		HCCGridRect selfGridRect;

		HCCGridPoint start = HCCGridController.Instance.GridView.WorldToGridPos(self.transform.position);
		HCCGridPoint end = HCCGridController.Instance.GridView.WorldToGridPos(target.transform.position);

		_openSet.Add(_gridData.GetCell(start.X, start.Z));
		_gScore[start.X, start.Z] = 0f;
		_fScore[start.X, start.Z] = _gScore[start.X, start.Z] + HeuristicCost(start, end);

		HCCCell currentCell = null;
		while (_openSet.Length > 0) {
			int currentIndex = GetLowestFScoreIndex(_openSet);
			currentCell = _openSet[currentIndex];

			//if destination reached
			//if (currentCell.GridPosition.X == end.X && currentCell.GridPosition.Z == end.Z) {
			//	List<HCCCell> result = ReconstructPath(currentCell.GridPosition);
			//	Cleanup();
			//	return result;
			//}
			//project update 4: move only to rects interception - 1 step
			//if (target.GridRect.Interacts(self.GridRectAtPosition(HCCGridController.Instance.GridView.GridToWorldPos(currentCell.GridPosition)))) {
			if (target.GridRect.Interacts(self.GridRectAtPoint(currentCell.GridPosition))) {
				List<HCCCell> result = ReconstructPath(currentCell.GridPosition);
				result.RemoveAt(result.Count - 1);
				Cleanup();
				return result;
			}

			_openSet[currentIndex].inOpenSet = false;
			_openSet.RemoveAt(currentIndex);
			_closedSet[currentCell.GridPosition.X, currentCell.GridPosition.Z] = currentCell;

			for (int i = currentCell.GridPosition.X - 1; i <= currentCell.GridPosition.X + 1; i++) {
				for (int j = currentCell.GridPosition.Z - 1; j <= currentCell.GridPosition.Z + 1; j++) {
					if (i == currentCell.GridPosition.X && j == currentCell.GridPosition.Z) {
						continue;
					}

					HCCCell neighbourCell = _gridData.GetCell(i, j);
					if (neighbourCell == null || _closedSet[neighbourCell.GridPosition.X, neighbourCell.GridPosition.Z] != null) {
						continue;
					}

					//project update 1: pass static objects
					//if (neighbour.IsBusyByStaticObject) {
					//	_closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] = neighbour;
					//	continue;
					//}

					//project update 2: pass all objects except self, target and excluded
					bool blockNeighbour = false;
					//selfGridRect = self.GridRectAtPosition(HCCGridController.Instance.GridView.GridToWorldPos(neighbourCell.GridPosition));
					selfGridRect = self.GridRectAtPoint(neighbourCell.GridPosition);
					for (int q = selfGridRect.XMin; q <= selfGridRect.XMax; q++) {
						for (int k = selfGridRect.ZMin; k <= selfGridRect.ZMax; k++) {
							HCCCell cellData = _gridData.GetCell(q, k);

							if (cellData == null) {
								//our rect is out of grid bounds - need to block
								blockNeighbour = true;
								break;
							}

							if (!cellData.IsFree) {
								if (cellData.ObjectData.IndexOf(self) == -1 && cellData.ObjectData.IndexOf(target) == -1) {
									blockNeighbour = true;
									break;
								}
							}
						}

						if (blockNeighbour) { break; }
					}
					if (blockNeighbour) {
						_closedSet[neighbourCell.GridPosition.X, neighbourCell.GridPosition.Z] = neighbourCell;
						continue;
					}
					
					//project update 3: pass all paths except self and target
					//if (neighbour.ObjectPathReservation != null) {
					//	if (neighbour.ObjectPathReservation != self || neighbour.ObjectPathReservation != target) {
					//		_closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] = neighbour;
					//		continue;
					//	}
					//}

					float tentativeGScore = _gScore[currentCell.GridPosition.X, currentCell.GridPosition.Z] + HeuristicCost(currentCell.GridPosition, neighbourCell.GridPosition);
					if (!neighbourCell.inOpenSet || tentativeGScore < _gScore[neighbourCell.GridPosition.X, neighbourCell.GridPosition.Z]) {
						_cameFrom[neighbourCell.GridPosition.X, neighbourCell.GridPosition.Z] = currentCell;
						_gScore[neighbourCell.GridPosition.X, neighbourCell.GridPosition.Z] = tentativeGScore;
						_fScore[neighbourCell.GridPosition.X, neighbourCell.GridPosition.Z] = _gScore[neighbourCell.GridPosition.X, neighbourCell.GridPosition.Z] + HeuristicCost(neighbourCell.GridPosition, end);
						if (!neighbourCell.inOpenSet) {
							_openSet.Add(neighbourCell);
							neighbourCell.inOpenSet = true;
						}
					}
				}
			}
		}

		Cleanup();
		return new List<HCCCell>();
	}
	
	//working, simple
	public List<HCCCell> FindPath(HCCGridPoint start, HCCGridPoint end, HCCGridObject[] excludedObjects) {
		_openSet.Add(_gridData.GetCell(start.X, start.Z));
		_gScore[start.X, start.Z] = 0f;
		_fScore[start.X, start.Z] = _gScore[start.X, start.Z] + HeuristicCost(start, end);

		HCCCell current;
		while (_openSet.Length > 0) {
			int currentIndex = GetLowestFScoreIndex(_openSet);
			current = _openSet[currentIndex];
			if (current.GridPosition.X == end.X && current.GridPosition.Z == end.Z) {
				List<HCCCell> result = ReconstructPath(current.GridPosition);
				Cleanup();
				return result;
			}

			_openSet[currentIndex].inOpenSet = false;
			_openSet.RemoveAt(currentIndex);
			_closedSet[current.GridPosition.X, current.GridPosition.Z] = current;

			for (int i = current.GridPosition.X - 1; i <= current.GridPosition.X + 1; i++) {
				for (int j = current.GridPosition.Z - 1; j <= current.GridPosition.Z + 1; j++) {
					if (i == current.GridPosition.X && j == current.GridPosition.Z) {
						continue;
					}

					HCCCell neighbour = _gridData.GetCell(i, j);
					if (neighbour == null || _closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] != null) {
						continue;
					}

					//project update 1: pass static objects
					//if (neighbour.IsBusyByStaticObject) {
					//	_closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] = neighbour;
					//	continue;
					//}

					//project update 2: pass all objects except excluded
					if (excludedObjects != null && !neighbour.IsFree) {
						bool objectIsExcluded = false;
						for (int p = 0; p < excludedObjects.Length; p++) {
							if (neighbour.ObjectData.IndexOf(excludedObjects[p]) != -1) {
								objectIsExcluded = true;
								break;
							}
						}
						if (!objectIsExcluded) {
							_closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] = neighbour;
							continue;
						}
					}

					float tentativeGScore = _gScore[current.GridPosition.X, current.GridPosition.Z] + HeuristicCost(current.GridPosition, neighbour.GridPosition);
					if (!neighbour.inOpenSet || tentativeGScore < _gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z]) {
						_cameFrom[neighbour.GridPosition.X, neighbour.GridPosition.Z] = current;
						_gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] = tentativeGScore;
						_fScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] = _gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] + HeuristicCost(neighbour.GridPosition, end);
						if (!neighbour.inOpenSet) {
							_openSet.Add(neighbour);
							neighbour.inOpenSet = true;
						}
					}
				}
			}
		}

		Cleanup();
		return new List<HCCCell>();
	}

	/*
	public List<HCCCell> FindPath(HCCGridPoint start, HCCGridPoint end, HCCGridRect startRect, params HCCGridObject[] excludedObjects) {
		int xSize = startRect.XSize;
		int zSize = startRect.ZSize;
		int xMinus = start.X - startRect.XMin;
		int zMinus = start.Z - startRect.ZMin;

		List<HCCCell> openSet = new List<HCCCell>() { _gridData.GetCell(start.X, start.Z) };
		_gScore[start.X, start.Z] = 0f;
		_fScore[start.X, start.Z] = _gScore[start.X, start.Z] + HeuristicCost(start, end);

		HCCCell current;
		while (openSet.Count > 0) {
			int currentIndex = GetLowestFScoreIndex(openSet);
			current = openSet[currentIndex];

			for (int q = current.GridPosition.X - xMinus; q < current.GridPosition.X - xMinus + xSize; q++) {
				for (int k = current.GridPosition.Z - zMinus; k < current.GridPosition.Z - zMinus + zSize; k++) {
					if (q == end.X && k == end.Z) {
						List<HCCCell> result = ReconstructPath(current.GridPosition);
						Cleanup();
						return result;
					}
				}
			}

			openSet.RemoveAt(currentIndex);
			_closedSet[current.GridPosition.X, current.GridPosition.Z] = current;

			for (int i = current.GridPosition.X - 1; i <= current.GridPosition.X + 1; i++) {
				for (int j = current.GridPosition.Z - 1; j <= current.GridPosition.Z + 1; j++) {
					if (i == current.GridPosition.X && j == current.GridPosition.Z) {
						continue;
					}

					HCCCell neighbour = _gridData.GetCell(i, j);
					//check null or in closed set already
					if (neighbour == null || _closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] != null) {
						continue;
					}
					//check at the end of the grid
					if (neighbour.GridPosition.X - xMinus >= _gridData.XSize - xSize || neighbour.GridPosition.Z - zMinus >= _gridData.ZSize - zSize) {
						_closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] = neighbour;
						continue;
					}

					//check blocked by something
					bool isBlockedBySomething = false;
					for (int q = neighbour.GridPosition.X - xMinus; q < neighbour.GridPosition.X - xMinus + xSize; q++) {
						for (int k = neighbour.GridPosition.Z - zMinus; k < neighbour.GridPosition.Z - zMinus + zSize; k++) {
							if (!_gridData.GetCell(q, k).IsFree) {
								bool objectIsExcluded = false;
								for (int p = 0; p < excludedObjects.Length; p++) {
									if (excludedObjects[p] == neighbour.ObjectData) {
										objectIsExcluded = true;
										break;
									}
								}
								if (!objectIsExcluded) {
									isBlockedBySomething = true;
									break;
								}
							}
						}
						if (isBlockedBySomething) {
							break;
						}
					}
					if (isBlockedBySomething) {
						_closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] = neighbour;
						continue;
					}

					float tentativeGScore = _gScore[current.GridPosition.X, current.GridPosition.Z] + HeuristicCost(current.GridPosition, neighbour.GridPosition);
					if (openSet.IndexOf(neighbour) == -1 || tentativeGScore < _gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z]) {
						_cameFrom[neighbour.GridPosition.X, neighbour.GridPosition.Z] = current;
						_gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] = tentativeGScore;
						_fScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] = _gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] + HeuristicCost(neighbour.GridPosition, end);
						if (openSet.IndexOf(neighbour) == -1) {
							openSet.Add(neighbour);
						}
					}
				}
			}
		}

		Cleanup();
		return new List<HCCCell>();
	}

	public List<HCCCell> FindPath(HCCGridRect start, HCCGridPoint end, params HCCGridObject[] excludedObjects) {
		int xSize = start.XSize;
		int zSize = start.ZSize;

		List<HCCCell> openSet = new List<HCCCell>() { _gridData.GetCell(start.XMin, start.ZMin) };
		_gScore[start.XMin, start.ZMin] = 0f;
		_fScore[start.XMin, start.ZMin] = _gScore[start.XMin, start.ZMin] + HeuristicCost(start.Min, end);

		HCCCell current;
		while (openSet.Count > 0) {
			int currentIndex = GetLowestFScoreIndex(openSet);
			current = openSet[currentIndex];

			for (int q = current.GridPosition.X; q < current.GridPosition.X + xSize; q++) {
				for (int k = current.GridPosition.Z; k < current.GridPosition.Z + zSize; k++) {
					if (q == end.X && k == end.Z) {
						List<HCCCell> result = ReconstructPath(current.GridPosition);
						Cleanup();
						return result;
					}
				}
			}

			openSet.RemoveAt(currentIndex);
			_closedSet[current.GridPosition.X, current.GridPosition.Z] = current;

			for (int i = current.GridPosition.X - 1; i <= current.GridPosition.X + 1; i++) {
				for (int j = current.GridPosition.Z - 1; j <= current.GridPosition.Z + 1; j++) {
					if (i == current.GridPosition.X && j == current.GridPosition.Z) {
						continue;
					}

					HCCCell neighbour = _gridData.GetCell(i, j);
					//check null or in closed set already
					if (neighbour == null || _closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] != null) {
						continue;
					}
					//check at the end of the grid
					if (neighbour.GridPosition.X >= _gridData.XSize - xSize || neighbour.GridPosition.Z >= _gridData.ZSize - zSize) {
						_closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] = neighbour;
						continue;
					}

					//check blocked by something
					bool isBlockedBySomething = false;
					for (int q = neighbour.GridPosition.X; q < neighbour.GridPosition.X + xSize; q++) {
						for (int k = neighbour.GridPosition.Z; k < neighbour.GridPosition.Z + zSize; k++) {
							if (!_gridData.GetCell(q, k).IsFree) {
								bool objectIsExcluded = false;
								for (int p = 0; p < excludedObjects.Length; p++) {
									if (excludedObjects[p] == neighbour.ObjectData) {
										objectIsExcluded = true;
										break;
									}
								}
								if (!objectIsExcluded) {
									isBlockedBySomething = true;
									break;
								}
							}
						}
						if (isBlockedBySomething) {
							break;
						}
					}
					if (isBlockedBySomething) {
						_closedSet[neighbour.GridPosition.X, neighbour.GridPosition.Z] = neighbour;
						continue;
					}

					float tentativeGScore = _gScore[current.GridPosition.X, current.GridPosition.Z] + HeuristicCost(current.GridPosition, neighbour.GridPosition);
					if (openSet.IndexOf(neighbour) == -1 || tentativeGScore < _gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z]) {
						_cameFrom[neighbour.GridPosition.X, neighbour.GridPosition.Z] = current;
						_gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] = tentativeGScore;
						_fScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] = _gScore[neighbour.GridPosition.X, neighbour.GridPosition.Z] + HeuristicCost(neighbour.GridPosition, end);
						if (openSet.IndexOf(neighbour) == -1) {
							openSet.Add(neighbour);
						}
					}
				}
			}
		}

		Cleanup();
		return new List<HCCCell>();
	}
	*/

	private float HeuristicCost(HCCGridPoint p1, HCCGridPoint p2) {
		return HeuristicCost(p1.X, p1.Z, p2.X, p2.Z);
	}

	private float HeuristicCost(int x1, int z1, int x2, int z2) {
		v1.x = x1;
		v1.y = z1;

		v2.x = x2;
		v2.y = z2;


		return Vector2.Distance(v1, v2);
	}

	private int GetLowestFScoreIndex(OpenSet<HCCCell> openSet) {
		HCCGridPoint gridPos;
		float fScoreMin;

		int index = 0;
		fScoreMin = _fScore[openSet[index].GridPosition.X, openSet[index].GridPosition.Z];
		for (int i = 1; i < openSet.Length; i++) {
			gridPos = openSet[i].GridPosition;
			if (_fScore[gridPos.X, gridPos.Z] < fScoreMin) {
				index = i;
				fScoreMin = _fScore[openSet[index].GridPosition.X, openSet[index].GridPosition.Z];
			}
		}
		return index;
	}

	private List<HCCCell> ReconstructPath(HCCGridPoint current) {
		List<HCCCell> result = new List<HCCCell>() { _gridData.GetCell(current.X, current.Z) };

		while(_cameFrom[current.X, current.Z] != null) {
			current = _cameFrom[current.X, current.Z].GridPosition;
			result.Add(_gridData.GetCell(current.X, current.Z));
		}
		result.Reverse();

		return result;
	}

	private void Cleanup() {
		for (int i = 0; i < _closedSet.GetLength(0); i++) {
			for (int j = 0; j < _closedSet.GetLength(1); j++) {
				_closedSet[i, j] = null;
				_cameFrom[i, j] = null;
				_gScore[i, j] = 0f;
				_fScore[i, j] = 0f;
			}
		}

		for (int i = 0; i < _openSet.Length; i++) {
			_openSet[i].inOpenSet = false;
		}
		_openSet.Clear();
	}
}
