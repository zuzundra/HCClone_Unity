using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UIWindowUnitConfirm : UIWindow
{
    public event EventHandler UnitIsConfirmed;
    public event EventHandler ConfirmIsHided;

    [SerializeField]
    private Button _btnCancel;
    [SerializeField]
    private Button _btnOK;
    [SerializeField]
    private Image _imgUnit;

    [SerializeField]
    private Text _lblLeadershipCost;
    public Text LblLeadershipCost
    {
        get { return _lblLeadershipCost; }
    }

    [SerializeField]
    private Text _lblLevel;
    public Text LblLevel
    {
        get { return _lblLevel; }
    }

    [SerializeField]
    private Text _lblDescription;
    public Text LblDescription
    {
        get { return _lblDescription; }
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
        AddDisplayAction(EUIWindowDisplayAction.PostHide, OnWindowHide);

        _btnCancel.onClick.AddListener(OnBtnCancelClick);
        _btnOK.onClick.AddListener(OnBtnOKClick);
    }

    public void Show(BaseSoldierData unitData)
    {
        SetUnit(unitData);
        Show();
    }

    void SetUnit(BaseSoldierData unitData)
    {
        _unitData = unitData;
        _lblLeadershipCost.text = _unitData.LeadershipCost.ToString(); 
        _lblLevel.text = Global.Instance.Player.City.GetSoldierUpgradesInfo(_unitData.Key).Level.ToString();
        _lblDescription.text = _unitData.PrefabName;

        Sprite iconResource = UIResourcesManager.Instance.GetResource<Sprite>(GameConstants.Paths.GetUnitIconResourcePath(_unitData.IconName));
        if (iconResource != null)
        {
            _imgUnit.enabled = true;
            _imgUnit.sprite = iconResource;
        }
    }

    void OnBtnOKClick()
    {
        if (UnitIsConfirmed != null)
            UnitIsConfirmed(this, new EventArgs());
        Hide();
    }

    void OnBtnCancelClick()
    {
        Hide();
    }

	private void OnWindowHide(UIWindow window)
    {
        if (_imgUnit.sprite != null)
        {
            _imgUnit.sprite = null;
            UIResourcesManager.Instance.FreeResource(GameConstants.Paths.GetUnitIconResourcePath(_unitData.IconName));
        }
        _unitData = null;
        if (ConfirmIsHided != null)
            ConfirmIsHided(this, new EventArgs());
    }
}