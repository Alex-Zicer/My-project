using UnityEngine;

/// <summary>
/// 挂在主菜单场景内的对象上，供 Button.OnClick 调用。
/// 这样按钮引用的是场景内对象，不会因为常驻单例销毁场景副本而丢失绑定。
/// </summary>
public class MainMenuButtonActions : MonoBehaviour
{
    public void LoadGamePlay()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance 为空，无法加载 GamePlay。");
            return;
        }

        UIManager.Instance.LoadGamePlay();
    }
}
