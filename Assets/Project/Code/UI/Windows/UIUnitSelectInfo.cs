using UnityEngine;
using UnityEngine.UI;
using System;

public class UIUnitSelectInfo : MonoBehaviour
{
    public event EventHandler UnitIsConfirmed;

    BaseSoldierData _unitData = null;
    public BaseSoldierData UnitData
    {
        get
        {
            return _unitData;
        }
    }

    Button _button = null; 
    public Button Button
    {
        get
        {
            if (_button == null)
                _button = gameObject.GetComponent<Button>();
            return _button;
        }
    }

    public void Awake()
    {
        Button.image.enabled = false;
        Button.onClick.AddListener(OnBtnClick);
    }

    public void LoadSoldierData(BaseSoldierData unitData, bool isSelected)
    {
        _unitData = unitData;
        if (_unitData == null)
            return;

        Button button = Button;
        GameObject cardResource = UIResourcesManager.Instance.GetResource<GameObject>(
            string.Format("{0}/{1}", GameConstants.Paths.UNIT_PREFAB_RESOURCES, _unitData.CardPrefab));
        GameObject cardUnitData = GameObject.Instantiate(cardResource) as GameObject;
        cardUnitData.transform.SetParent(button.transform, false);

        RectTransform rectCard = cardUnitData.GetComponent<RectTransform>();
        Rect rectImage = button.image.rectTransform.rect;
        rectCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectImage.width);
        rectCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectImage.height);
        rectCard.anchoredPosition = new Vector2(rectImage.width / 2, -rectImage.height / 2);

        (gameObject.GetComponent<MultiImageButton>()).AddChildImages(cardUnitData);
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
        Button button = Button;
        for (int i = button.transform.childCount; i > 0; i--)
            button.transform.GetChild(i - 1).SetParent(null);

        _unitData = null;
    }
}
