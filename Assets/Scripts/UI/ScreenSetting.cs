using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreenSetting : MonoBehaviour
{
    [Header("组件的引用")]
    [Tooltip("分辨率下拉菜单的引用，用于显示和选择屏幕分辨率。")]
    public TMP_Dropdown resolutionDropdown;
    [Tooltip("窗口模式下拉菜单的引用，用于显示和选择窗口模式（全屏、窗口化等）。")]
    public TMP_Dropdown windowModeDropdown;

    private Resolution[] resolutions;

    // Start is called before the first frame update
    void Start()
    {
        InitResolution();
        InitWindowMode();
    }

    #region 屏幕分辨率设置
    private void InitResolution()
    {
        //获取所有可用的屏幕分辨率
        Resolution[] allResolutions = Screen.resolutions;

        //清除现有的选项
        resolutionDropdown.ClearOptions();

        //把所有分辨率转换为字符串格式
        List<string> options = new List<string>();

        //使用HashSet来跟踪已经添加的分辨率，避免重复项
        List<Resolution> uniqueResolutions = new List<Resolution>();
        HashSet<string> seenResolutions = new HashSet<string>();

        int currentResolutionIndex = 0;

        //遍历所有分辨率，构建选项列表，并找到当前屏幕分辨率的索引
        for (int i = 0; i < allResolutions.Length; i++)
        {
            //构建分辨率字符串，例如 "1920 x 1080"
            string resKey = allResolutions[i].width + " x " + allResolutions[i].height;

            //检查当前分辨率是否已经添加过，如果没有，则添加到唯一分辨率列表和选项列表中
            if (!seenResolutions.Contains(resKey))
            {
                seenResolutions.Add(resKey);
                uniqueResolutions.Add(allResolutions[i]);

                string option = allResolutions[i].width + " x " + allResolutions[i].height;
                options.Add(option);
            }

            if (allResolutions[i].width == Screen.currentResolution.width &&
                allResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = options.Count - 1;
            }
        }
        //遍历完成后，将唯一的分辨率列表转换为数组并存储在成员变量中，以便后续使用
        this.resolutions = uniqueResolutions.ToArray();

        //将选项列表添加到下拉菜单中，并设置当前分辨率为默认选项
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;

        //刷新下拉菜单显示当前选项
        resolutionDropdown.RefreshShownValue();
    }

    /// <summary>
    /// 设置屏幕分辨率的方法，根据用户在下拉菜单中选择的分辨率索引来调整游戏的显示设置。
    /// </summary>
    /// <param name="resolutionIndex">分辨率索引</param>
    public void SetResolution(int resolutionIndex)
    {
        Resolution res = resolutions[resolutionIndex];
        //设置屏幕分辨率，保持当前的全屏模式
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        Debug.Log("分辨率已设置为: " + res.width + " x " + res.height);
    }
    #endregion

    #region 窗口模式设置
    private void InitWindowMode()
    {
        //清除现有的选项
        windowModeDropdown.ClearOptions();
        //添加窗口模式选项到下拉菜单中
        windowModeDropdown.AddOptions(new List<string> { "全屏", "无边框","窗口化" });
        //根据当前的全屏模式设置下拉菜单的默认选项
        if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
        {
            windowModeDropdown.value = 0; //无边框
        }
        else if(Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
        {
            windowModeDropdown.value = 1; //无边框
        }
        else
        {
            windowModeDropdown.value = 2; //窗口化
        }
        //刷新下拉菜单显示当前选项
        windowModeDropdown.RefreshShownValue();
    }

    /// <summary>
    /// 设置窗口模式的方法，根据用户在下拉菜单中选择的窗口模式索引来调整游戏的显示设置。
    /// </summary>
    /// <param name="modeIndex">窗口模式索引</param>
    public void SetWindowMode(int modeIndex)
    {
        //根据用户选择的窗口模式索引设置全屏模式
        switch (modeIndex)
        {
            case 0: //全屏
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                Debug.Log("已设置为全屏模式");
                break;
            case 1: //无边框
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Debug.Log("已设置为无边框模式");
                break;
            case 2: //窗口化
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Debug.Log("已设置为窗口化模式");
                break;
        }
        Debug.Log("窗口模式已设置为: " + Screen.fullScreenMode);
    }
    #endregion
}
