using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UISoundType { Default, Important, Back, Tab}

[System.Serializable]
public class SoundMapping
{
    public UISoundType type;
    public AudioClip hoverclip;
    public AudioClip clickclip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance; //单例实例

    public AudioSource uiSource; //UI音效播放器

    [Tooltip("UI音效映射列表，包含不同类型的UI音效及其对应的音效片段。")]
    public List<SoundMapping> soundLibrary;

    private void Awake()
    {
        //确保只有一个实例存在
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); //在场景切换时不销毁
        }
        else
        {
            Destroy(gameObject); //销毁多余的实例
        }
    }

    /// <summary>
    /// 播放UI音效的方法，接受一个UISoundType类型的参数type，
    /// 表示要播放的UI音效类型，以及一个布尔值参数isClick，表示是否是点击事件。
    /// </summary>
    /// <param name="type">音效类型</param>
    /// <param name="isClick">是否点击</param>
    public void PlaySound(UISoundType type, bool isClick)
    {
        var mapping = soundLibrary.Find(s => s.type == type);
        if (mapping != null)
        {
            AudioClip audioClip = isClick ? mapping.clickclip : mapping.hoverclip;
            if (audioClip != null)
            {
                uiSource.PlayOneShot(audioClip);
            }
        }
    }
}
