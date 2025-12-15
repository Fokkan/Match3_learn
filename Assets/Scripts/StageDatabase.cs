using UnityEngine;

/// <summary>
/// 스테이지 데이터를 모아두는 ScriptableObject.
/// 인스펙터에서 StageData 배열을 채운 뒤, StageManager에 연결해서 사용.
/// </summary>
[CreateAssetMenu(fileName = "StageDatabase", menuName = "Match3/StageDatabase")]
public class StageDatabase : ScriptableObject
{
    public StageData[] stages;
}
