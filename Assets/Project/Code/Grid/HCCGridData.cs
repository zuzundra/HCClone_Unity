public class HCCGridData {
	private int _xSize = 0;
	public int XSize {
		get { return _xSize; }
	}

	private int _zSize = 0;
	public int ZSize {
		get { return _zSize; }
	}

	private HCCCell[,] _cells = null;

	public HCCGridData(int xSize, int zSize) {
		_xSize = xSize;
		_zSize = zSize;
		_cells = new HCCCell[xSize, zSize];

		for (int x = 0; x < xSize; x++) {
			for (int z = 0; z < zSize; z++) {
				_cells[x, z] = new HCCCell(x, z);
			}
		}
	}

	public HCCCell GetCell(int x, int z) {
		if (x < 0 || z < 0 || x >= XSize || z >= ZSize) {
			return null;
		}
		
		return _cells[x, z];
	}
}
