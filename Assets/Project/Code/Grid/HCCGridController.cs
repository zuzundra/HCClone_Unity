using UnityEngine;
using System;
using System.Collections.Generic;

public class HCCGridController {
	#region instance
	private static HCCGridController _instance = null;
	public static HCCGridController Instance {
		get {
			if (_instance == null) {
				_instance = new HCCGridController();
			}
			return _instance;
		}
	}
	#endregion

	public HCCGridData GridData { get; private set; }
	public HCCGridView GridView { get; private set; }
	public HCCPathfinder Pathfinder { get; private set; }

	private bool _isInitialized = false;
	private Action _initActions = null;

	public HCCGridController() {
		GridView = HCCGridView.Instance;
	}

	public void Initialize(int xSize, int zSize) {
		GridData = new HCCGridData(xSize, zSize);
		GridView.CreateGrid(xSize, zSize);
		Pathfinder = new HCCPathfinder(GridData);

		_isInitialized = true;

		if (_initActions != null) {
			_initActions();
		}
	}

	public void RegisterObject(HCCGridObject obj) {
		//check initialization
		if (!_isInitialized) {
			_initActions += delegate() { RegisterObject(obj); };
			return;
		}

		List<HCCCell> targetCells = GetGridObjectCellsList(obj);
		if (targetCells != null) {
			for (int i = 0; i < targetCells.Count; i++) {
				targetCells[i].ObjectData.Add(obj);
			}
		}
	}

	public void UnregisterObject(HCCGridObject obj) {
		//TODO
	}

	public List<HCCCell> GetGridObjectCellsList(HCCGridObject obj) {
		//check if object is on grid bounds
		HCCGridRect objGridRect = HCCGridController.Instance.GridView.WorldPosToGridRect(obj);
		if (objGridRect.XMin < 0 || objGridRect.ZMin < 0 ||
			objGridRect.XMax >= GridView.XSize || objGridRect.ZMax >= GridView.ZSize) {
			Debug.LogError(string.Format("Object \"{0}\" is out of grid bounds", obj.gameObject.name));
			return null;
		}

		//check if object does not interact with static objects
		List<HCCCell> result = new List<HCCCell>();
		HCCCell cell = null;
		for (int i = objGridRect.XMin; i <= objGridRect.XMax; i++) {
			for (int j = objGridRect.ZMin; j <= objGridRect.ZMax; j++) {
				cell = GridData.GetCell(i, j);
				if (cell == null) {
					Debug.LogError(string.Format("Can't get cell [{0}, {1}] for object \"{2}\"", i, j, obj.gameObject.name));
					return null;
				}
				if (cell.IsBusyByStaticObject) {
					Debug.LogError(string.Format("Attempt to register \"{0}\" object failed: desired cell [{1}, {2}] is busy", obj.gameObject.name, i, j));
					return null;
				}

				result.Add(cell);
			}
		}

		return result;
	}

	public List<HCCCell> GetGridRectCellsList(HCCGridRect gridRect) {
		//check if object is on grid bounds
		if (gridRect.XMin < 0 || gridRect.ZMin < 0 ||
			gridRect.XMax >= GridView.XSize || gridRect.ZMax >= GridView.ZSize) {
			return null;
		}

		//check if object does not interact with others
		List<HCCCell> result = new List<HCCCell>();
		HCCCell cell = null;
		for (int i = gridRect.XMin; i <= gridRect.XMax; i++) {
			for (int j = gridRect.ZMin; j <= gridRect.ZMax; j++) {
				cell = GridData.GetCell(i, j);
				if (cell == null) {
					return null;
				}

				result.Add(cell);
			}
		}

		return result;
	}

	public bool IsGridSpaceAvailable(HCCGridRect gridRect) {
		if (gridRect.XMin < 0 || gridRect.ZMin < 0 ||
			gridRect.XMax >= GridView.XSize || gridRect.ZMax >= GridView.ZSize) {
				return false;
		}

		HCCCell cell = null;
		for (int i = gridRect.XMin; i <= gridRect.XMax; i++) {
			for (int j = gridRect.ZMin; j <= gridRect.ZMax; j++) {
				cell = GridData.GetCell(i, j);
				if (cell == null) {
					return false;
				}
				if (!cell.IsFree) {
					return false;
				}
			}
		}

		return true;
	}

	public List<HCCGridObject> GetObjectsAtRect(HCCGridRect gridRect) {
		if (gridRect.XMin < 0 || gridRect.ZMin < 0 ||
			gridRect.XMax >= GridView.XSize || gridRect.ZMax >= GridView.ZSize) {
			return null;
		}

		List<HCCGridObject> result = new List<HCCGridObject>();

		HCCCell cell = null;
		for (int i = gridRect.XMin; i <= gridRect.XMax; i++) {
			for (int j = gridRect.ZMin; j <= gridRect.ZMax; j++) {
				cell = GridData.GetCell(i, j);
				if (cell != null && !cell.IsFree) {
					for (int k = 0; k < cell.ObjectData.Count; k++) {
						if(result.IndexOf(cell.ObjectData[k]) == -1) {
							result.Add(cell.ObjectData[k]);
						}
					}
				}
			}
		}

		return result;
	}

	public bool IsGridSpaceAvailable(HCCGridRect gridRect, HCCGridObject excludedObject) {
		if (gridRect.XMin < 0 || gridRect.ZMin < 0 ||
			gridRect.XMax >= GridView.XSize || gridRect.ZMax >= GridView.ZSize) {
			return false;
		}

		HCCCell cell = null;
		for (int i = gridRect.XMin; i <= gridRect.XMax; i++) {
			for (int j = gridRect.ZMin; j <= gridRect.ZMax; j++) {
				cell = GridData.GetCell(i, j);
				if (cell == null) {
					return false;
				}
				if (!cell.IsFree && cell.ObjectData.IndexOf(excludedObject) == -1) {
					return false;
				}
			}
		}

		return true;
	}
}
