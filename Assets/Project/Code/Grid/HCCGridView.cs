using UnityEngine;

public class HCCGridView : MonoBehaviourResourceSingleton<HCCGridView> {
#pragma warning disable 0414
	private static string _path = "Grid/GridView";
#pragma warning restore 0414

	[SerializeField]
	private bool _drawMapInEditor = true;

	[SerializeField]
	private float _tileSize = 1f;
	public float TileSize {
		get { return _tileSize; }
	}

	[SerializeField]
	private Vector3 _zeroPoint = Vector3.zero;
	public Vector3 ZeroPoint {
		get { return _zeroPoint; }
	}

	private int _xSize = 1;
	public int XSize {
		get { return _xSize; }
	}

	private int _zSize = 1;
	public int ZSize {
		get { return _zSize; }
	}

	//public void Start() {
	//	//WARNING! temp code for camera calibration
	//	GameObject p = GameObject.CreatePrimitive(PrimitiveType.Plane);
	//	p.transform.position = Vector3.zero;
	//	p.transform.localScale = new Vector3(_xSize * _tileSize / 10, 0.1f, _zSize * _tileSize / 10);
	//}

	public void LateUpdate() {
#if UNITY_EDITOR
		DrawMap();
#endif
	}

	public void CreateGrid(int xSize, int zSize) {
		_xSize = xSize;
		_zSize = zSize;
	}

	public HCCGridPoint WorldToGridPos(Vector3 position) {
		return WorldToGridPos(position.x, position.z);
	}

	public HCCGridPoint WorldToGridPos(float xPos, float zPos) {
		return new HCCGridPoint((int)((xPos - _zeroPoint.x) / _tileSize), (int)((zPos - _zeroPoint.z) / _tileSize));
	}

	public HCCGridRect WorldPosToGridRect(HCCGridObject obj) {
		HCCGridPoint minPoint = WorldToGridPos(obj.XMinWorldPos, obj.ZMinWorldPos);
		HCCGridPoint maxPoint = WorldToGridPos(obj.XMaxWorldPos, obj.ZMaxWorldPos);

		return new HCCGridRect(minPoint.X, maxPoint.X, minPoint.Z, maxPoint.Z);;
	}

	public HCCGridRect WorldPosToGridRect(HCCGridObject obj, Vector3 position) {
		Vector3 positionOffset = position - obj.transform.position;

		HCCGridPoint minPoint = WorldToGridPos(obj.XMinWorldPos + positionOffset.x, obj.ZMinWorldPos + positionOffset.z);
		HCCGridPoint maxPoint = WorldToGridPos(obj.XMaxWorldPos + positionOffset.x, obj.ZMaxWorldPos + positionOffset.z);

		return new HCCGridRect(minPoint.X, maxPoint.X, minPoint.Z, maxPoint.Z); ;
	}

	public Vector3 GridToWorldPos(HCCGridPoint gridPos) {
		return GridToWorldPos(gridPos.X, gridPos.Z);
	}

	public Vector3 GridToWorldPos(int xPos, int zPos) {
		return new Vector3(_zeroPoint.x + xPos * _tileSize, _zeroPoint.y, _zeroPoint.z + zPos * _tileSize);
	}

	public HCCGridPoint GetGridSize(HCCGridObject obj) {
		return new HCCGridPoint(Mathf.CeilToInt((obj.XWorldSize / _tileSize) * 0.5f), Mathf.CeilToInt((obj.ZWorldSize / _tileSize) * 0.5f));
	}

	#region drawing
#if UNITY_EDITOR
	private void DrawMap() {
		if (_drawMapInEditor) {
			Vector3 start = new Vector3(0f, _zeroPoint.y + 0.1f, 0f);
			Vector3 end = new Vector3(0f, _zeroPoint.y + 0.1f, 0f);
			HCCCell cellData = null;
			Color cellStateColor = Color.green;
			for (int i = 0; i < _xSize; i++) {
				for (int j = 0; j < _zSize; j++) {
					start.x = _zeroPoint.x + i * _tileSize;
					start.z = _zeroPoint.z + j * _tileSize;

					//draw horizontal bottom line
					end.x = start.x + _tileSize;
					end.z = start.z;
					Debug.DrawLine(start, end, Color.green);

					//draw vertical left line
					end.x = start.x;
					end.z = start.z + _tileSize;
					Debug.DrawLine(start, end, Color.green);

					//draw vertical right line for last cell
					if (i == _xSize - 1) {
						start.x = _zeroPoint.x + i * _tileSize + _tileSize;
						start.z = _zeroPoint.z + j * _tileSize;

						//draw horizontal bottom line
						end.x = start.x;
						end.z = start.z + _tileSize;
						Debug.DrawLine(start, end, Color.green);
					}

					//draw horizontal top line for last cell
					if (j == _zSize - 1) {
						start.x = _zeroPoint.x + i * _tileSize;
						start.z = _zeroPoint.z + j * _tileSize + _tileSize;

						//draw horizontal bottom line
						end.x = start.x + _tileSize;
						end.z = start.z;
						Debug.DrawLine(start, end, Color.green);
					}

					//draw cell state
					if (HCCGridController.Instance.GridData != null) {
						cellData = HCCGridController.Instance.GridData.GetCell(i, j);
						/*if (!cellData.IsFree) {
							if (cellData.IsBusyByStaticObject) {
								cellStateColor = Color.red;
							} else {
								cellStateColor = Color.yellow;
							}
						} else {
							if (cellData.ObjectPathReservation != null) {
								cellStateColor = Color.blue;
							} else {
								cellStateColor = Color.green;
							}
						}*/

						if (cellData.ObjectPathReservation != null) {
							cellStateColor = Color.blue;
						} else if (!cellData.IsFree) {
							if (cellData.IsBusyByStaticObject) {
								cellStateColor = Color.red;
							} else {
								cellStateColor = Color.yellow;
							}
						} else {
							cellStateColor = Color.green;
						}

						start.x = _zeroPoint.x + i * _tileSize;
						start.z = _zeroPoint.z + j * _tileSize;
						end.x = start.x + _tileSize;
						end.z = start.z + _tileSize;
						Debug.DrawLine(start, end, cellStateColor);
						start.x = _zeroPoint.x + i * _tileSize + _tileSize;
						start.z = _zeroPoint.z + j * _tileSize;
						end.x = start.x - _tileSize;
						end.z = start.z + _tileSize;
						Debug.DrawLine(start, end, cellStateColor);
					}
				}
			}
		}
	}
#endif
	#endregion
}
