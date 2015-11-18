﻿using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    Vector3 _slotPosition = Vector3.zero;

    static UIUnitSlot _touchSlot = null;
    UIUnitSlot _targetSlot = null;

    public void Awake()
    {
        _button.image.enabled = false;
        _button.onClick.AddListener(OnBtnClick);
    }

    public void Update()
    {
        if (_unitData == null)
        {
            _slotPosition = transform.position;
            return;
        }
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector2 position = touch.position;
                if (_touchSlot == null)
                {
                    List<UIUnitSlot> slots = GetSlots(position);
                    if (slots.Contains(this))
                        _touchSlot = this;
                }
                if (_touchSlot == this)
                {
                    _targetSlot = GetTargetSlot(position);
                    Vector3 targetPosition = new Vector3(position.x, position.y, transform.position.z);
                    transform.position = targetPosition;
                }
            }
        }
        else
        {
            if (_targetSlot != null)
            {
                BaseSoldierData targetData = _targetSlot.UnitData;
                _targetSlot.SetUnitData(_unitData);
                _targetSlot.SelectSlot(true);

                SetUnitData(targetData);
                _targetSlot = null;
            }
            transform.position = _slotPosition;
            _touchSlot = null;
        }
    }

    UIUnitSlot GetTargetSlot(Vector2 position)
    {
        List<UIUnitSlot> slots = GetSlots(position);
        foreach (UIUnitSlot slot in slots)
        {
            if (slot != this)
                return slot;
        }
        return null;
    }

    List<UIUnitSlot> GetSlots(Vector2 position)
    {
        List<UIUnitSlot> slots = new List<UIUnitSlot>();
        Canvas canvas = Utils.UI.GetCanvas(RenderMode.ScreenSpaceOverlay);
        if (canvas != null)
        {
            GraphicRaycaster rayCaster = canvas.GetComponent<GraphicRaycaster>();
            List<RaycastResult> results = new List<RaycastResult>();
            PointerEventData eventData = new PointerEventData(null);
            eventData.position = position;
            rayCaster.Raycast(eventData, results);
            foreach (RaycastResult result in results)
            {
                UIUnitSlot slot = result.gameObject.GetComponent<UIUnitSlot>();
                if (slot != null)
                    slots.Add(slot);
            }
        }
        return slots;
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