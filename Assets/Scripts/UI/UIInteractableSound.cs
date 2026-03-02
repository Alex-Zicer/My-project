using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIInteractableSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public UISoundType soundType = UISoundType.Default; //默认音效类型

    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //如果没有Toggle组件，直接播放点击音效；如果有Toggle组件，只有在切换状态时才播放点击音效
        if (toggle == null)
        {
            AudioManager.Instance.PlaySound(soundType, true);
        }
        else
        {
            //如果是Toggle组件，只有在切换状态时才播放点击音效
            if (toggle.isOn)
            {
                AudioManager.Instance.PlaySound(soundType, true);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //无论是否有Toggle组件，悬停时都播放悬停音效
        AudioManager.Instance.PlaySound(soundType, false); 
    }
}
