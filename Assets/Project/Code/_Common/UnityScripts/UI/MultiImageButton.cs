using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class MultiImageButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {
	[SerializeField]
	private Image[] _affectedImages;

	private Button _myButton;

	public void Awake() {
		_myButton = GetComponent<Button>();
	}

	public void OnPointerEnter(PointerEventData eventData) {
		if (!_myButton.interactable) {
			return;
		}

		for (int i = 0; i < _affectedImages.Length; i++) {
			_affectedImages[i].CrossFadeColor(_myButton.colors.highlightedColor, _myButton.colors.fadeDuration, true, true);
		}
	}

	public void OnPointerExit(PointerEventData eventData) {
		if (!_myButton.interactable) {
			return;
		}

		for (int i = 0; i < _affectedImages.Length; i++) {
			_affectedImages[i].CrossFadeColor(_myButton.colors.normalColor, _myButton.colors.fadeDuration, true, true);
		}
	}

	public void OnPointerDown(PointerEventData eventData) {
		if (!_myButton.interactable) {
			return;
		}

		for (int i = 0; i < _affectedImages.Length; i++) {
			_affectedImages[i].CrossFadeColor(_myButton.colors.pressedColor, _myButton.colors.fadeDuration, true, true);
		}
	}

	public void OnPointerUp(PointerEventData eventData) {
		if (!_myButton.interactable) {
			return;
		}

		for (int i = 0; i < _affectedImages.Length; i++) {
			_affectedImages[i].CrossFadeColor(_myButton.colors.highlightedColor, _myButton.colors.fadeDuration, true, true);
		}
	}

	public void OnPointerClick(PointerEventData eventData) {
		if (!_myButton.interactable) {
			return;
		}

		//unimplemented
	}
}
