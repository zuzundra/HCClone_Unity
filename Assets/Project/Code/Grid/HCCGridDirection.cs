using UnityEngine;
using System.Collections.Generic;

public enum EHCCGridDirection {
	None = 0,
	Left,
	LeftTop,
	Top,
	TopRight,
	Right,
	RightBottom,
	Bottom,
	BottomLeft
}

public class HCCGridDirection {
	private static Dictionary<EHCCGridDirection, HCCGridPoint> _directionToVector = new Dictionary<EHCCGridDirection, HCCGridPoint>() {
		{ EHCCGridDirection.None, new HCCGridPoint(0, 0) },
		{ EHCCGridDirection.Left, new HCCGridPoint(-1, 0) },
		{ EHCCGridDirection.LeftTop, new HCCGridPoint(-1, 1) },
		{ EHCCGridDirection.Top, new HCCGridPoint(0, 1) },
		{ EHCCGridDirection.TopRight, new HCCGridPoint(1, 1) },
		{ EHCCGridDirection.Right, new HCCGridPoint(1, 0) },
		{ EHCCGridDirection.RightBottom, new HCCGridPoint(1, -1) },
		{ EHCCGridDirection.Bottom, new HCCGridPoint(0, -1) },
		{ EHCCGridDirection.BottomLeft, new HCCGridPoint(-1, -1) }
	};

	public static HCCGridPoint DirectionToVector(EHCCGridDirection direction) {
		return _directionToVector[direction];
	}

	public static EHCCGridDirection VectorToDirection(HCCGridPoint vector) {
		vector = NormalizeVector(vector);
		foreach (KeyValuePair<EHCCGridDirection, HCCGridPoint> kvp in _directionToVector) {
			if (kvp.Value.Equals(vector)) {
				return kvp.Key;
			}
		}
		return EHCCGridDirection.None;
	}

	public static HCCGridPoint NormalizeVector(HCCGridPoint vector) {
		return new HCCGridPoint(1 * (int)Mathf.Sign(vector.X), 1 * (int)Mathf.Sign(vector.Z));
	}

	public static List<EHCCGridDirection> GetFreeDirections(List<EHCCGridDirection> busyDirections, bool includeZeroDirection) {
		List<EHCCGridDirection> freeDirections = new List<EHCCGridDirection>();
		foreach (KeyValuePair<EHCCGridDirection, HCCGridPoint> kvp in _directionToVector) {
			if (kvp.Key == EHCCGridDirection.None && !includeZeroDirection) {
				continue;
			}
			if (busyDirections.IndexOf(kvp.Key) == -1) {
				freeDirections.Add(kvp.Key);
			}
		}
		return freeDirections;
	}
}
