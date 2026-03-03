using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class CharacterAnimation : MonoBehaviour
{
    private Animator anim;
    private IBaseEntity entityData;
    private ICharacterController actionsEvent;

    private void Awake()
    {
        anim = GetComponent<Animator>();

        //获取接口引用
        entityData = GetComponent<IBaseEntity>();
        actionsEvent = GetComponent<ICharacterController>();
    }

    private void OnEnable()
    {
        if (actionsEvent != null)
        {
            actionsEvent.OnJump += HandleJump;
            actionsEvent.OnLand += HandleLand;

        }
    }

    private void OnDisable()
    {
        if(actionsEvent != null)
        {
            actionsEvent.OnJump -= HandleJump;
            actionsEvent.OnLand -= HandleLand;
        }
    }

    private void HandleJump() => anim.SetTrigger("Jump");
    private void HandleLand() => anim.SetTrigger("Land");

    private void Update()
    {
        if (entityData == null)
        {
            Debug.LogError("没有获取到 IBaseEntity 接口！请检查脚本是否挂载在同一个物体上。");
            return;
        }

        //设置水平速度参数，动画控制器可以根据这个参数来切换不同的动画状态，例如从站立到跑步
        float hSpeed = entityData.HorizontalSpeed;
        Debug.Log($"逻辑层速度: {hSpeed}");
        anim.SetFloat("HorizontalSpeed", hSpeed > 0.01 ? hSpeed : 0f);

        //设置垂直速度参数，动画控制器可以根据这个参数来切换不同的动画状态，例如从跳跃到下落
        anim.SetFloat("VerticalSpeed", entityData.VerticalSpeed);

        //设置是否在地面上的参数，动画控制器可以根据这个参数来切换不同的动画状态，例如从跳跃到落地
        anim.SetBool("IsGrounded", entityData.IsGrounded);

        //设置是否死亡的参数，动画控制器可以根据这个参数来切换不同的动画状态，例如从任何状态到死亡
        anim.SetBool("IsDead", entityData.IsDead);

        FlipCharacter();
    }

    /// <summary>
    /// 改变人物朝向，基于IBaseEntity接口中的FacingDirection属性来判断人物应该面朝左还是面朝右
    /// </summary>
    private void FlipCharacter()
    {
        float direction = entityData.FacingDirection;

        if(direction > 0.1f)
        {
            transform.localScale = new Vector3(1, 1, 1); //面朝右
        }
        else if(direction < -0.1f)
        {
            transform.localScale = new Vector3(-1, 1, 1); //面朝左
        }
    }
}
