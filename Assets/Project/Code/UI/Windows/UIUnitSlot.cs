using UnityEngine;
using UnityEngine.UI;
using System;

public class UIUnitSlot : MonoBehaviour
{
    public event EventHandler SlotIsSelected;

    [SerializeField]
    private Button _button;
    public Button Button
    {
        get { return _button; }
    }

    [SerializeField]
    private UnitPlace _place = new UnitPlace() { Range = EUnitRange.None, Position = EUnitPosition.None };
    public UnitPlace Place
    {
        get { return _place; }
    }

    BaseSoldierData _unitData = null;
    public BaseSoldierData UnitData
    {
        get
        {
            return _unitData;
        }
    }

    bool _isSelected = false;
    public bool IsSelected
    {
        get
        {
            return _isSelected;
        }
    }

    public void Awake()
    {
        _button.image.enabled = false;
        _button.onClick.AddListener(OnBtnClick);
    }

    public void SetUnitData(BaseSoldierData unitData)
    {
        if (_unitData != null)
            UIResourcesManager.Instance.FreeResource(GameConstants.Paths.GetUnitIconResourcePath(_unitData.IconName));             

        _unitData = unitData;
        if (_unitData != null)
        {
            _button.image.enabled = true;
            _button.image.sprite = UIResourcesManager.Instance.GetResource<Sprite>(GameConstants.Paths.GetUnitIconResourcePath(_unitData.IconName));

        }
        else
        {
            _button.image.enabled = false;
            _button.image.sprite = null;
            _isSelected = false;
        }
    }

    private void OnBtnClick()
    {
        if (_unitData == null)
        {
            ShowUnitSelect();       
        }
        else
        {
            SelectSlot(true);
        }
    }

    public void ShowUnitSelect()
    {
        UIWindowUnitSelect wus = UIWindowsManager.Instance.GetWindow(EUIWindowKey.UnitSelect) as UIWindowUnitSelect;
        wus.UnitIsSelected += new System.EventHandler(wus_UnitIsSelected);
        wus.UnitSelectIsHided += new System.EventHandler(wus_UnitSelectIsHided);
        wus.Show(_unitData); 
    }

    void wus_UnitIsSelected(object sender, System.EventArgs e)
    {
        SetUnitData((BaseSoldierData)sender);
        SelectSlot(true);
    }

    public void SelectSlot(bool isSelected)
    {
        if (isSelected)
        {
            if (SlotIsSelected != null)
                SlotIsSelected(_unitData, new EventArgs());         
        }
        _isSelected = isSelected;
    }

    void wus_UnitSelectIsHided(object sender, System.EventArgs e)
    {
        UIWindowUnitSelect wus = (UIWindowUnitSelect)sender;
        wus.UnitIsSelected -= wus_UnitIsSelected;
        wus.UnitSelectIsHided -= wus_UnitSelectIsHided;
    }
}
