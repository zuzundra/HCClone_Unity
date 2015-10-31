using UnityEngine;
using UnityEngine.UI;
using System;

public class UIUnitSelectInfo : MonoBehaviour
{
    public event EventHandler UnitIsConfirmed;

    [SerializeField]
    private Button _button;
    public Button Button
    {
        get { return _button; }
    }

    public MultiImageButton MultiImageButton
    {
        get { return _button != null ? _button.GetComponent<MultiImageButton>() : null; }
    }

    [SerializeField]
    private Text _lblLeadershipCost;
    public Text LblLeadershipCost
    {
        get { return _lblLeadershipCost; }
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

    Color _normalColor = Color.clear;
    public void Awake()
    {
        _button.image.enabled = false;
        _button.onClick.AddListener(OnBtnClick);
        _normalColor = _button.colors.normalColor;
    }

    public void LoadSoldierData(BaseSoldierData unitData, bool isSelected)
    {
        MultiImageButton multiButton = _button.GetComponent<MultiImageButton>();
        _unitData = unitData;
        if (_unitData != null)
        {
            Sprite iconResource = UIResourcesManager.Instance.GetResource<Sprite>(GameConstants.Paths.GetUnitIconResourcePath(_unitData.IconName));
            if (iconResource != null)
            {
                multiButton.SetEnabled(true);
                _button.image.sprite = iconResource;
            }
            _lblLeadershipCost.text = _unitData.LeadershipCost.ToString();
            _lblDescription.text = _unitData.PrefabName;
        }
        else
        {
            multiButton.SetEnabled(false);
            _button.image.sprite = null;
            _lblLeadershipCost.text = _lblDescription.text = string.Empty;
        }
    }

    private void OnBtnClick()
    {
        if (_unitData != null)
        {
            UIWindowUnitConfirm wuc = UIWindowsManager.Instance.GetWindow(EUIWindowKey.UnitConfirm) as UIWindowUnitConfirm;
            wuc.UnitIsConfirmed += new System.EventHandler(wuc_UnitIsConfirmed);
            wuc.ConfirmIsHided += new System.EventHandler(wuc_ConfirmIsHided);
            wuc.Show(_unitData);
        }
    }

    void wuc_UnitIsConfirmed(object sender, System.EventArgs e)
    {
        if (UnitIsConfirmed != null)
            UnitIsConfirmed(this, e);
    }

    void wuc_ConfirmIsHided(object sender, System.EventArgs e)
    {
        UIWindowUnitConfirm wuc = (UIWindowUnitConfirm)sender;
        wuc.UnitIsConfirmed -= new System.EventHandler(wuc_UnitIsConfirmed);
        wuc.ConfirmIsHided -= new System.EventHandler(wuc_ConfirmIsHided);
    }

    public void ClearData()
    {
        if (_button.image.sprite != null)
        {
            _button.image.sprite = null;
            if (_unitData != null)
                UIResourcesManager.Instance.FreeResource(GameConstants.Paths.GetUnitIconResourcePath(_unitData.IconName));
        }
        _unitData = null;
    }
}
