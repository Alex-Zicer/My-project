using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Data/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("玩家的基本属性")]
    public float maxHealth;//最大生命值
    public float moveSpeed;//移动速度
    [Tooltip("启动加速度，保证人物不会突然变向")]
    public float moveSpeedMultiplier;//移动加速度，确保人物不会突然转向
    [Tooltip("防御力")]
    public float defence;//玩家防御力

    [Header("玩家的攻击属性")]
    public float atkPower;
    public float atkCoolDown;

    [ContextMenu("Load From JSON")] //直接在Inspector菜单里右键点击即可触发
    public void LoadFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "PlayerData.json");

        if (File.Exists(path))
        {
            string jsonContent = File.ReadAllText(path);

            //将json数据直接注入到这个脚本中
            JsonUtility.FromJsonOverwrite(jsonContent, this);

            Debug.Log("json文件数据导入成功");
        }
        else
        {
            Debug.Log("找不到JSON文件，路径为:" +  path);
        }
    }
}
