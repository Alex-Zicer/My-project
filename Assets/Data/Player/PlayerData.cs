using System.IO;
using UnityEngine;

/// <summary>
/// 玩家数值配置资产。
/// 保存移动、跳跃、冲刺、贴墙与攻击反馈等基础参数，供 PlayerController 和各状态读取。
/// </summary>
[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Data/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("玩家的基本属性")]
    public float maxHealth;//最大生命值
    public float moveSpeed;//移动速度
    [Tooltip("启动加速度，保证人物不会突然变向")]
    public float moveSpeedMultiplier;//移动加速度，确保人物不会突然转向
    [Tooltip("基础攻击力")]
    public float attack;//玩家攻击力
    [Tooltip("跳跃速度")]
    public float JumpForce;

    [Header("位移技能")]
    [Tooltip("冲刺速度")]
    public float dashSpeed = 14f;
    [Tooltip("冲刺持续时间")]
    public float dashDuration = 0.16f;
    [Tooltip("冲刺冷却时间")]
    public float dashCooldown = 0.5f;

    [Header("贴墙与墙跳")]
    [Tooltip("贴墙下滑时的最大下落速度")]
    public float wallSlideSpeed = 2f;
    [Tooltip("墙跳的水平速度")]
    public float wallJumpHorizontalSpeed = 8f;
    [Tooltip("墙跳的竖直速度")]
    public float wallJumpForce = 12f;

    [Header("动作状态")]
    [Tooltip("斩击动作的锁定时长")]
    public float slashDuration = 0.22f;

    [Header("受击反馈")]
    public float hitShakeIntensity = 0.3f;
    public float hitShakeFrequency = 1f;
    public float hitShakeDuration = 0.2f;

    [Header("攻击命中反馈")]
    public float attackHitStopDuration = 0.05f;
    public float attackShakeIntensity = 0.2f;
    public float attackShakeFrequency = 1f;
    public float attackShakeDuration = 0.1f;
    /// <summary>
    /// 从 StreamingAssets 中读取玩家数据 JSON，并覆盖当前 ScriptableObject。
    /// 该函数主要用于个人练习 Demo 中的快速调参与导入。
    /// </summary>
    [ContextMenu("Load From JSON")] // 直接在 Inspector 菜单里右键点击即可触发。
    public void LoadFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "PlayerData.json");

        if (File.Exists(path))
        {
            string jsonContent = File.ReadAllText(path);

            // 将 JSON 数据直接覆盖到当前 ScriptableObject 中。
            JsonUtility.FromJsonOverwrite(jsonContent, this);

            Debug.Log("json文件数据导入成功");
        }
        else
        {
            Debug.Log("找不到JSON文件，路径为:" + path);
        }
    }
}