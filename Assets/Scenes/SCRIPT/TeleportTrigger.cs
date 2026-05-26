using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalTeleport : MonoBehaviour
{
    [Header("Target Portal")]
    public Transform targetPortal;

    [Header("Teleport Settings")]
    public float teleportDelay = 0.5f;
    public float cooldown = 1.0f;
    public float exitDistance = 1.5f;

    [Header("Ground Check")]
    public float groundRayHeight = 2f;
    public float groundRayDistance = 5f;
    public LayerMask groundMask;

    private static Dictionary<GameObject, float> cooldownMap = new Dictionary<GameObject, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GameObject obj = other.gameObject;

        if (cooldownMap.TryGetValue(obj, out float t) && Time.time < t)
            return;

        StartCoroutine(TeleportRoutine(obj));
    }

    private IEnumerator TeleportRoutine(GameObject obj)
    {
        // 先锁CD（防止重复触发）
        cooldownMap[obj] = Time.time + cooldown;

        yield return new WaitForSeconds(teleportDelay);

        if (obj == null) yield break;

        CharacterController cc = obj.GetComponent<CharacterController>();

        // ===============================
        // 1. 计算出口方向（水平化）
        // ===============================
        Vector3 flatForward = Vector3.ProjectOnPlane(targetPortal.forward, Vector3.up).normalized;

        Vector3 basePos = targetPortal.position + flatForward * exitDistance;

        // ===============================
        // 2. 地面修正（防掉地图核心）
        // ===============================
        Vector3 rayOrigin = basePos + Vector3.up * groundRayHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask))
        {
            basePos = hit.point;
        }

        // 轻微抬高避免穿模
        basePos += Vector3.up * 0.1f;

        // ===============================
        // 3. 传送
        // ===============================
        if (cc != null)
        {
            cc.enabled = false;
            obj.transform.position = basePos;
            cc.enabled = true;
        }
        else
        {
            obj.transform.position = basePos;
        }
    }
}