using UnityEngine;
using UnityEngine.UI;
using System;

public class UIUnitSlot : MonoBehaviour
{
    public event EventHandler UnitIsSelected;

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

    public void Awake()
    {
        _button.image.enabled = false;
        _button.onClick.AddListener(OnBtnClick);
    }

    public void SetUnitData(BaseSoldierData unitData)
    {
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
        }
    }

    private void OnBtnClick()
    {
        UIWindowUnitSelect wus = UIWindowsManager.Instance.GetWindow(EUIWindowKey.UnitSelect) as UIWindowUnitSelect;
        wus.UnitIsSelected += new System.EventHandler(wus_UnitIsSelected);
        wus.UnitSelectIsHided += new System.EventHandler(wus_UnitSelectIsHided);
        wus.Show(_unitData);
    }

    void wus_UnitIsSelected(object sender, System.EventArgs e)
    {
        SelectSlot((BaseSoldierData)sender);
    }

    public void SelectSlot(BaseSoldierData soldierData)
    {
        SetUnitData(soldierData);
        if (UnitIsSelected != null)
            UnitIsSelected(soldierData, new EventArgs()); 
    }

    void wus_UnitSelectIsHided(object sender, System.EventArgs e)
    {
        UIWindowUnitSelect wus = (UIWindowUnitSelect)sender;
        wus.UnitIsSelected -= wus_UnitIsSelected;
        wus.UnitSelectIsHided -= wus_UnitSelectIsHided;
    }
}
