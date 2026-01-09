using TMPro;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TMPArcText : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private TMP_Text target;

    [Header("Arc Settings")]
    [Tooltip("전체 아치 각도(도). 90~160 사이 권장")]
    [SerializeField, Range(0f, 180f)] private float arcAngle = 120f;

    [Tooltip("반지름(px). 클수록 완만, 작을수록 강한 아치")]
    [SerializeField, Range(50f, 2000f)] private float radius = 450f;

    [Tooltip("아치 전체를 위/아래로 이동(px)")]
    [SerializeField] private float yOffset = 0f;

    [Tooltip("방향 반전(아치가 뒤집힘)")]
    [SerializeField] private bool invert = false;

    [Header("Update")]
    [Tooltip("에디터/런타임에서 매 프레임 갱신(필요할 때만 켜기)")]
    [SerializeField] private bool updateEveryFrame = false;

    private bool _dirty = true;

    private void Reset()
    {
        target = GetComponent<TMP_Text>();
        _dirty = true;
    }

    private void OnEnable()
    {
        if (target == null) target = GetComponent<TMP_Text>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        _dirty = true;
        Warp();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void OnValidate()
    {
        _dirty = true;
        if (!updateEveryFrame) Warp();
    }

    private void LateUpdate()
    {
        if (updateEveryFrame || _dirty)
            Warp();
    }

    private void OnTextChanged(Object obj)
    {
        if (target == null) return;
        if (obj == target)
            _dirty = true;
    }

    public void Warp()
    {
        if (target == null) return;

        target.ForceMeshUpdate();
        TMP_TextInfo textInfo = target.textInfo;
        if (textInfo == null || textInfo.characterCount == 0) return;

        // 유효 글자들의 X 범위 구하기
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var ch = textInfo.characterInfo[i];
            if (!ch.isVisible) continue;

            int matIndex = ch.materialReferenceIndex;
            int vertIndex = ch.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            Vector3 bl = verts[vertIndex + 0];
            Vector3 tr = verts[vertIndex + 2];
            float midX = (bl.x + tr.x) * 0.5f;

            if (midX < minX) minX = midX;
            if (midX > maxX) maxX = midX;
        }

        float range = maxX - minX;
        if (range <= 0.0001f) return;

        float totalAngle = arcAngle * Mathf.Deg2Rad;
        float halfAngle = totalAngle * 0.5f;
        float dir = invert ? -1f : 1f;

        // 글자별 변형
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var ch = textInfo.characterInfo[i];
            if (!ch.isVisible) continue;

            int matIndex = ch.materialReferenceIndex;
            int vertIndex = ch.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            Vector3 bl = verts[vertIndex + 0];
            Vector3 tl = verts[vertIndex + 1];
            Vector3 tr = verts[vertIndex + 2];
            Vector3 br = verts[vertIndex + 3];

            Vector3 oldMid = (bl + tr) * 0.5f;

            // 0..1 -> -0.5..+0.5 로 정규화
            float u = (oldMid.x - minX) / range - 0.5f;

            // 좌우로 펼쳐진 각도
            float ang = u * totalAngle * dir;

            // 원호 위 목표 위치(중앙이 가장 높고, 양끝이 내려가는 형태)
            float x = Mathf.Sin(ang) * radius;
            float y = Mathf.Cos(ang) * radius - radius + yOffset;

            Vector3 newMid = new Vector3(x, y, 0f);

            // 글자 회전(원호 접선에 맞춤)
            float rotZ = -ang * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, rotZ);

            // 각 버텍스 이동/회전 적용
            verts[vertIndex + 0] = rot * (bl - oldMid) + newMid;
            verts[vertIndex + 1] = rot * (tl - oldMid) + newMid;
            verts[vertIndex + 2] = rot * (tr - oldMid) + newMid;
            verts[vertIndex + 3] = rot * (br - oldMid) + newMid;
        }

        // 메시 반영
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            var meshInfo = textInfo.meshInfo[m];
            meshInfo.mesh.vertices = meshInfo.vertices;
            target.UpdateGeometry(meshInfo.mesh, m);
        }

        _dirty = false;
    }
}
